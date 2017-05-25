using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Lullabies
{
	public class Playable
	{
	}

	public class PlayableChord : Playable
	{
		public int Tonic;
		public Chord Chord;
		public int Duration;
		public double Velocity;

		public PlayableChord (int tonic, Chord chord, int duration, double velocity = 127)
		{
			Tonic = tonic;
			Chord = chord;
			Duration = duration;
			Velocity = velocity;
		}
	}

	public class PlayableNote : Playable
	{
		public readonly int Note;
		public readonly int Duration;
		public readonly double Velocity;

		public PlayableNote (int note, int duration, double velocity = 127)
		{
			Note = note;
			Duration = duration;
			Velocity = velocity;
		}
	}

	public class PlayableRest : Playable
	{
		public readonly int Duration;

		public PlayableRest (int duration)
		{
			Duration = duration;
		}
	}

	public class Melody
	{
		public List<PlayableNote> chords = new List<PlayableNote> ();

		public readonly int OctaveShift;

		public static int PickOctave (Random r)
		{
			return r.Next (3) - 2;
		}

		public Melody (ChordProgression p, Random r, int octave)
		{
			int prevNote = -1;

			OctaveShift = octave;

			foreach (var c in p.chords) {

				var numBeats = c.Duration / p.BeatDuration;
				var maxEigths = numBeats * 2;

				var numEigthsComposed = 0;

				while (numEigthsComposed < maxEigths) {
					var e = 1 + r.Next ((3*maxEigths) / 4);
					if (numEigthsComposed + e > maxEigths) {
						e = maxEigths - numEigthsComposed;
					}
					numEigthsComposed += e;

					var n = PickNote (c.Chord.GetNotes (c.Tonic), r, prevNote)
						+ octave * 12;

					chords.Add (new PlayableNote (
						n,
						e * p.BeatDuration / 2,
						c.Velocity));
				}

//				Console.WriteLine (numBeats);
			}
		}

		static int PickNote (IEnumerable<int> notes, Random r, int prevNote)
		{
			var list = notes.ToList ();
			var n = list [r.Next (list.Count)];
			while (n == prevNote) {
				n = list [r.Next (list.Count)];
			}
			return n;
		}

		public async Task PlayAsync (Instrument ins, int noteChange, CancellationToken token)
		{
			var o = noteChange;
			var minNote = chords.Min (x => x.Note + o);
			while (minNote < ins.Info.MinNote) {
				o += 12;
				minNote = chords.Min (x => x.Note + o);
			}
			var maxNote = chords.Max (x => x.Note + o);
			while (maxNote > ins.Info.MaxNote) {
				o -= 12;
				maxNote = chords.Max (x => x.Note + o);
			}
			foreach (var c in chords) {
				await ins.PlayNoteAsync (c.Note + o, c.Duration, c.Velocity, token);
			}
		}
	}

	public class PercussionLine
	{
		public List<Playable> hiHat = new List<Playable> ();
		public List<Playable> bass = new List<Playable> ();
		public List<Playable> snare = new List<Playable> ();

		public PercussionLine (ChordProgression p, Random r)
		{
			Rock (p, r);
		}

		void Rock (ChordProgression p, Random r)
		{
			//
			// Hi Hat
			//
			foreach (var c in p.chords) {

				var numBeats = c.Duration / p.BeatDuration;
				var maxEigths = numBeats * 2;
				var numEigthsComposed = 0;

				var loud = true;

				while (numEigthsComposed < maxEigths) {
					var e = 1;
					if (numEigthsComposed + e > maxEigths) {
						e = maxEigths - numEigthsComposed;
					}
					numEigthsComposed += e;

					hiHat.Add (new PlayableNote (
						42,
						e * p.BeatDuration / 2,
						loud ? c.Velocity : c.Velocity/2));
					loud = !loud;
				}

				//				Console.WriteLine (numBeats);
			}

			//
			// Bass
			//
			var beatNumber = 0;
			foreach (var c in p.chords) {

				var chordBeats = c.Duration / p.BeatDuration;

				while (chordBeats > 0) {
					switch (beatNumber) {
					case 0:
					case 2:
						bass.Add (new PlayableNote (
							36,
							p.BeatDuration,
							c.Velocity + 20));
						snare.Add (new PlayableRest (p.BeatDuration));
						break;
					case 1:
					case 3:
						bass.Add (new PlayableRest (p.BeatDuration));
						snare.Add (new PlayableNote (
							37,
							p.BeatDuration,
							c.Velocity));
						break;
					}
					chordBeats--;
					beatNumber = (beatNumber + 1) % 4;
				}

				//				Console.WriteLine (numBeats);
			}
		}

		public async Task PlayAsync (Instrument ins, CancellationToken token)
		{
			var t = PlayAllAsync (bass, ins, token);
			PlayAllAsync (snare, ins, token);
			PlayAllAsync (hiHat, ins, token);
			await t;
		}

		static async Task PlayAllAsync (IEnumerable<Playable> hiHat, Instrument ins, CancellationToken token)
		{
			foreach (var c in hiHat) {
				await ins.PlayAsync (c, token);
			}
		}
	}

	public class Bassline
	{
		public List<Playable> chords = new List<Playable> ();

		public Bassline (ChordProgression p, Random r)
		{
//			SonClave (p, r);
			EigthNotes (p, r);
		}

		void SonClave (ChordProgression p, Random r)
		{
			var beatNumber = 0;

			foreach (var c in p.chords) {
				var chordBeats = c.Duration / p.BeatDuration;

				var dur = 4*p.BeatDuration;
				Console.WriteLine ("Bass dur = {0}", dur);

				var vel = c.Velocity + 10;

				var n = c.Tonic;
				while (n > 50) {
					n -= 12;
				}

				while (chordBeats > 0) {
					switch (beatNumber) {
					case 0:
						chords.Add (new PlayableNote (n, (dur * 3) / 16, vel));
						chords.Add (new PlayableNote (n, (dur * 1) / 16, vel));
						break;
					case 1:
					case 2:
						chords.Add (new PlayableRest (dur / 8));
						chords.Add (new PlayableNote (n, dur / 8, vel));
						break;
					case 3:
						chords.Add (new PlayableNote (n, dur / 4, vel));
						break;
					}
					chordBeats--;
					beatNumber = (beatNumber + 1) % 4;
				}
			}
		}

		void EigthNotes (ChordProgression p, Random r)
		{
			foreach (var c in p.chords) {

				var numBeats = c.Duration / p.BeatDuration;

				var maxEigths = numBeats * 2;

				var numEigthsComposed = 0;

				while (numEigthsComposed < maxEigths) {
					var e = 1;
					if (numEigthsComposed + e > maxEigths) {
						e = maxEigths - numEigthsComposed;
					}
					numEigthsComposed += e;

					var n = c.Tonic;
					while (n > 50) {
						n -= 12;
					}

					chords.Add (new PlayableNote (
						n,
						e * p.BeatDuration / 2,
						c.Velocity));
				}

				//				Console.WriteLine (numBeats);
			}
		}

		public async Task PlayAsync (Instrument ins, CancellationToken token)
		{
			foreach (var c in chords) {
				await ins.PlayAsync (c, token);
			}
		}
	}

	public class ChordProgression
	{
		public readonly int KeyTonic;
		public readonly int EndVel;

		public readonly int BeatDuration;

		public List<PlayableChord> chords = new List<PlayableChord> ();

		public ChordProgression (Random r)
			: this (r, PickBpm (r), PickKey (r), PickVel (r), PickVel (r))
		{
		}

		static int PickKey (Random r)
		{
			return 55 + r.Next (30) - 15;
			//return 60 + r.Next (24) - 12;
		}

		static int PickBpm (Random r)
		{
			return 80 + (int)((r.NextDouble ()-0.5)*60);
		}

		static int PickVel (Random r)
		{
			return 70 + (int)((r.NextDouble ()-0.5)*60);
		}

		const int MinDur = 15000;

		public readonly int Bpm;

		public ChordProgression (Random r, int bpm, int key, int vel, int nvel)
		{
//			bpm = 30;
			Bpm = bpm;

			KeyTonic = key;

			var b = (1000 * 60) / bpm;
			var ps = Progression.All.ToArray ();
			var p = ps [r.Next (ps.Length)];

			BeatDuration = b;

			if (vel < 0)
				vel = PickVel (r);
			if (nvel < 0)
				nvel = PickVel (r);
			EndVel = nvel;

			var totalDur = 0;

			while (totalDur <= MinDur) {
				foreach (var pp in p) {
					var tonic = Key.GetNote (key, Scale.Major, pp.Degree);
					var d = pp.Beats * b;
					totalDur += d;
					chords.Add (new PlayableChord (tonic, pp.Chord, d, vel));
				}
			}

			var dv = ((double)(nvel - vel)) / (chords.Count - 1);
			for (var i = 0; i < chords.Count; i++) {
				chords [i].Velocity = vel + i * dv;
			}
		}

		public async Task PlayAsync (Instrument ins, CancellationToken token)
		{
			foreach (var c in chords) {
				Console.WriteLine ("  {0}", c.Chord.Name);
				await ins.PlayChordAsync (c.Tonic, c.Chord, c.Duration, c.Velocity, token);
			}
		}
	}

	public class Section
	{
		public ChordProgression Progression;
		public Melody Melody;
		public Bassline Bassline;
		public PercussionLine Percussion;


		ArrangementInstruments ins;
		public Section (Random r, ArrangementInstruments ins)
			: this (r, ins, new ChordProgression (r), Melody.PickOctave (r))
		{
		}

		public Section (Random r, ArrangementInstruments ins, ChordProgression prog, int melodyOctave)
		{
			this.ins = ins;
			Progression = prog;
			Melody = new Melody (Progression, r, melodyOctave);
			Bassline = new Bassline (Progression, r);
			Percussion = new PercussionLine (Progression, r);
		}

		public async Task PlayAsync (bool melody1, bool melody2, Random r, CancellationToken token)
		{
			var t = Progression.PlayAsync (ins.Changes, token);
			Bassline.PlayAsync (ins.Bass, token);
			//Percussion.PlayAsync (ins.Percussion, token);
			if (melody1)
				Melody.PlayAsync (ins.Melody, 0, token);
			if (melody2) {
				var noteChange = 0;
				if (melody1) {
					noteChange = 12 * (r.Next (3) - 1);
					if (noteChange == 0)
						noteChange = 12;
				}
				Melody.PlayAsync (ins.Melody2, noteChange, token);
			}
			await t;
		}
	}

	public class Song
	{
		ArrangementInstruments ins;

		static Random r = new Random ();

		public Song (AudioUnit.AUGraph graph, int ioNode)
		{
			this.ins = new ArrangementInstruments (r, graph, ioNode);



			verse = new Section (r, ins);

			var verseNotes = Chord.Diminished7.GetNotes (verse.Progression.KeyTonic).ToList ();
			var verseKey = verse.Progression.KeyTonic;
			var chorusKey = PickRandom (verseNotes);
			while (chorusKey == verseKey)
				chorusKey = PickRandom (verseNotes);
			chorusKey += 12 * (r.Next (2) - 1); // Compensate for always going up on the scale
			var bridgeKey = PickRandom (verseNotes);
			while (bridgeKey == verseKey || bridgeKey == chorusKey)
				bridgeKey = PickRandom (verseNotes);
			bridgeKey += 12 * (r.Next (2) - 1); // Compensate for always going up on the scale

			chorus = new Section (r, ins, new ChordProgression (r, verse.Progression.Bpm, chorusKey, verse.Progression.EndVel, -1), verse.Melody.OctaveShift);
			bridge = new Section (r, ins, new ChordProgression (r, verse.Progression.Bpm, bridgeKey, chorus.Progression.EndVel, verse.Progression.EndVel), verse.Melody.OctaveShift);
		}

		Section verse, chorus, bridge;

		public async Task PlayAsync (CancellationToken token)
		{
			Console.WriteLine ("-----");
			Console.WriteLine ("Playing Verse");
			await verse.PlayAsync (true, false, r, token);
			Console.WriteLine ("Playing Chorus");
			await chorus.PlayAsync (true, false, r, token);
			Console.WriteLine ("Playing Verse");
			await verse.PlayAsync (true, false, r, token);
			Console.WriteLine ("Playing Chorus");
			await chorus.PlayAsync (true, false, r, token);
			Console.WriteLine ("Playing Bridge");
			await bridge.PlayAsync (false, true, r, token);
			Console.WriteLine ("Playing Chorus");
			await chorus.PlayAsync (true, true, r, token);
			Console.WriteLine ("Playing Chorus");
			await chorus.PlayAsync (true, true, r, token);

			await Task.Delay (2000);

//			await ins.PlayChord (tonic, pp.Chord, pp.Beats * b, vel);
		}

		static T PickRandom<T> (List<T> list)
		{
			var i = r.Next (list.Count);
			return list [i];
		}
	}
}

