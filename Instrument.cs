using System;
using System.Linq;
using System.Threading.Tasks;
using AudioUnit;
using CoreFoundation;
using Foundation;
using System.Collections.Generic;
using System.Threading;

namespace Lullabies
{
	public enum PlayChordStyle
	{
		SingleNote,
		Simultaneous,
		Strum = 75,
		FastStrum = 30,
		SlowStrum = 125,
	}

	public class InstrumentInfo
	{
		public int Preset; 
		public int MinNote; 
		public int MaxNote; 
		public PlayChordStyle ChordStyle;
		public int VelocityChange;
		public byte BankMSB;

		public InstrumentInfo (int preset, int minNote = 36, int maxNote = 96, int velocityChange = 0, PlayChordStyle chordStyle = PlayChordStyle.Simultaneous)
		{
			Preset = preset;
			MinNote = minNote;
			MaxNote = maxNote;
			ChordStyle = chordStyle;
			VelocityChange = velocityChange;
			BankMSB = SamplerInstrumentData.DefaultMelodicBankMSB;
		}
	}

	public static class ArrangementPresets
	{
		public static InstrumentInfo[] Melody = new [] {
			new InstrumentInfo (0), // Piano 1
			new InstrumentInfo (24, velocityChange: 30), // Nylon Guitar
			new InstrumentInfo (69, minNote: 40, maxNote: 60, velocityChange: 10), // English Horn
//			new InstrumentInfo (52, minNote: 50, maxNote: 90), // Choral aaahhhs
			new InstrumentInfo (26, velocityChange: 10), // Jazz Guitar
			new InstrumentInfo (46, velocityChange: 20), // Harp
		};

		public static InstrumentInfo[] Changes = new [] {
			new InstrumentInfo (0, velocityChange: -20), // Piano 1
			new InstrumentInfo (75, minNote: 40, maxNote: 80, chordStyle: PlayChordStyle.SingleNote), // Pan FLute
			new InstrumentInfo (11, chordStyle: PlayChordStyle.SlowStrum, velocityChange: -20), // Vibraphone
			new InstrumentInfo (42, minNote: 40, maxNote: 70, chordStyle: PlayChordStyle.SingleNote, velocityChange: -30), // Cello
//			new InstrumentInfo (52, minNote: 50, maxNote: 90, chordStyle: PlayChordStyle.SingleNote, velocityChange: -30), // Choral aaahhhs
			new InstrumentInfo (24, minNote: 40, maxNote: 80, chordStyle: PlayChordStyle.Strum, velocityChange: -10), // Nylon Guitar
			new InstrumentInfo (25, minNote: 40, maxNote: 70, chordStyle: PlayChordStyle.FastStrum, velocityChange: -10), // Nylon Guitar

//			new InstrumentInfo (33),
		};

		public static InstrumentInfo[] Basses = new [] {
			new InstrumentInfo (32, minNote: 36, maxNote: 60, chordStyle: PlayChordStyle.SingleNote), // Acouustic Bass
		};

		public static InstrumentInfo Percussion = 
			new InstrumentInfo (0, minNote: 0, maxNote: 100, chordStyle: PlayChordStyle.SingleNote) {
				BankMSB = SamplerInstrumentData.DefaultPercussionBankMSB,
			};
	}

	public class ArrangementInstruments
	{
		public Instrument Melody;
		public Instrument Melody2;
		public Instrument Changes;
		public Instrument Bass;
		public Instrument Percussion;

		public ArrangementInstruments (Random r, AUGraph graph, int ioNode)
		{
			var mp = ArrangementPresets.Melody [r.Next (ArrangementPresets.Melody.Length)];
			Melody = new Instrument (mp, 0, graph, ioNode);

			var mp2 = mp;
			while (mp2.Preset == mp.Preset)
				mp2 = ArrangementPresets.Changes [r.Next (ArrangementPresets.Changes.Length)];
			Melody2 = new Instrument (mp2, 1, graph, ioNode);

			var cp = mp;
			while (cp.Preset == mp.Preset || cp.Preset == mp2.Preset)
				cp = ArrangementPresets.Changes [r.Next (ArrangementPresets.Changes.Length)];
			Changes = new Instrument (cp, 2, graph, ioNode);

			var bp = ArrangementPresets.Basses [r.Next (ArrangementPresets.Basses.Length)];
			Bass = new Instrument (bp, 3, graph, ioNode);

			Percussion = new Instrument (ArrangementPresets.Percussion, 4, graph, ioNode);
		}
	}

	public class Instrument
	{
		readonly AudioUnit.AudioUnit samplerUnit;
		int channel;

		public readonly InstrumentInfo Info;

		public Instrument (InstrumentInfo info, int channel, AUGraph graph, int ioNode)
		{
			Info = info;

			var samplerNode = graph.AddNode (AudioComponentDescription.CreateMusicDevice (AudioTypeMusicDevice.Sampler));

			graph.ConnnectNodeInput (samplerNode, 0, ioNode, (uint)channel);

			samplerUnit = graph.GetNodeInfo (samplerNode);
			samplerUnit.SetMaximumFramesPerSlice (4096, AudioUnitScopeType.Global, 0);

			this.channel = channel;

			LoadInstrument (info);
		}

		public async Task PlayChordAsync (int tonic, Chord chord, int duration, double velocity, CancellationToken token)
		{
			switch (Info.ChordStyle) {
			case PlayChordStyle.Simultaneous:
				await PlayNotesAsync (chord.GetNotes (tonic), duration, velocity, token);
				return;
			case PlayChordStyle.SingleNote:
				await PlayNoteAsync (tonic, duration, velocity, token);
				return;
			case PlayChordStyle.Strum:
			case PlayChordStyle.FastStrum:
				{
					var interval = (int)Info.ChordStyle;
					var ts = new List<Task> ();
					foreach (var n in chord.GetNotes (tonic)) {
						ts.Add (PlayNoteAsync (n, duration, velocity, token));
						await Task.Delay (interval);
					}
					await ts [0];
				}
				return;
			}
		}

		public Task PlayNotesAsync (IEnumerable<int> notes, int duration, double velocity, CancellationToken token)
		{
			return Task.WhenAll (notes.Select (x => PlayNoteAsync (x, duration, velocity, token)).ToArray ());
		}

		public async Task PlayAsync (Playable p, CancellationToken token)
		{
			var c = p as PlayableChord;
			if (c != null) {
				await PlayChordAsync (c.Tonic, c.Chord, c.Duration, c.Velocity, token);
				return;
			}

			var n = p as PlayableNote;
			if (n != null) {
				await PlayNoteAsync (n.Note, n.Duration, n.Velocity, token);
				return;
			}

			var r = p as PlayableRest;
			if (r != null) {
				await Task.Delay (r.Duration);
				return;
			}
		}

		public async Task PlayNoteAsync (int note, int duration, double velocity, CancellationToken token)
		{
			token.ThrowIfCancellationRequested ();
			var v = (byte)(velocity + Info.VelocityChange);
			var status = (9 << 4) | channel;
			samplerUnit.MusicDeviceMIDIEvent ((byte)status, (byte)note, v);
			await Task.Delay (duration);
			status = (8 << 4) | channel;
			token.ThrowIfCancellationRequested ();
			samplerUnit.MusicDeviceMIDIEvent ((byte)status, (byte)note, v);
		}

		void LoadInstrument (InstrumentInfo info)
		{
			var preset = info.Preset;

			var soundFontPath = NSBundle.MainBundle.PathForResource ("ChoriumRevA", "sf2");
			var soundFontUrl = CFUrl.FromFile (soundFontPath);

			samplerUnit.LoadInstrument (new SamplerInstrumentData (soundFontUrl, InstrumentType.SF2Preset) {
				BankLSB = SamplerInstrumentData.DefaultBankLSB,
				BankMSB = info.BankMSB,
				PresetID = (byte)preset,
			});
		}
	}
}

