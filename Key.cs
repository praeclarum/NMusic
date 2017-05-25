using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayMidi
{
	public class Key
	{
		public static int GetNote (int tonic, Scale scale, int degree)
		{
			var n = tonic;
			var b = (int)scale;
			var mask = 0x40; // 0b1000000
			while (--degree > 0) {
				var two = (b & mask) != 0;
				n += two ? 2 : 1;
				mask >>= 1;
			}
			return n;
		}
	}

	public enum Scale
	{
		Major = Ionian,
		Minor = Aeolian,
		Ionian    = 0x6E, // 0b1101110
		Dorian    = 0x5D, // 0b1011101
		Phrygian  = 0x3B, // 0b0111011
		Lydian    = 0x76, // 0b1110110
		Mixoldian = 0x6D, // 0b1101101
		Aeolian   = 0x5B, // 0b1011011
		Locrian   = 0x37, // 0b0110111
	}

	public class Chord
	{
		public readonly string Name;

		readonly int[] subintervals;

		public Chord (string name, params int[] subintervals)
		{
			this.Name = name;
			this.subintervals = subintervals;
		}

		public static readonly Chord Major = new Chord ("maj", 0, 4, 7);
		public static readonly Chord Minor = new Chord ("min", 0, 3, 7);
		public static readonly Chord Augmented = new Chord ("aug", 0, 4, 8);
		public static readonly Chord Diminished = new Chord ("dim", 0, 3, 6);

		public static readonly Chord Major7 = new Chord ("maj7", 0, 4, 7, 11);
		public static readonly Chord Minor7 = new Chord ("min7", 0, 3, 7, 10);
		public static readonly Chord Augmented7 = new Chord ("aug7", 0, 4, 8, 10);
		public static readonly Chord Diminished7 = new Chord ("dim7", 0, 3, 6, 9);
		public static readonly Chord Dominant7 = new Chord ("dom7", 0, 4, 7, 10);

		public IEnumerable<int> GetNotes (int tonic)
		{
			return subintervals.Select (x => x + tonic);
		}
	}

	public class ProgressionNotes
	{
		public int Degree;
		public Chord Chord;
		public int Beats;
		public ProgressionAction Action;
	}

	[Flags]
	public enum ProgressionAction
	{
		None = 0,
		Repeat = 1,
//		Bridge = 2,
	}

	public class Progression : IEnumerable<ProgressionNotes>
	{
		readonly List<ProgressionNotes> pps = new List<ProgressionNotes> ();
		ProgressionAction Action;
		public Progression (ProgressionAction action  =ProgressionAction.None)
		{
			Action = action;
		}

		public void Add (int degree, Chord chord, int beats = 2, ProgressionAction action = ProgressionAction.None)
		{
			pps.Add (new ProgressionNotes {
				Degree = degree, 
				Chord = chord, 
				Beats = beats, 
				Action = action,
			});
		}

		public static Progression Common1 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
		};

		public static Progression Common2 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
			{ 6, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },
		};

		public static Progression Common3 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
		};

		public static Progression Common4 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 2, Chord.Minor, 2 },
			{ 4, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
		};

		public static Progression Common5 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 2, Chord.Minor, 2 },
			{ 4, Chord.Major, 2 },
		};

		public static Progression Common6 = new Progression {
			{ 4, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
		};

		public static Progression Common7 = new Progression {
			{ 2, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
		};

		public static Progression Common8 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 4, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
		};

		public static Progression Major1 = new Progression {
			{ 1, Chord.Major, 2 },
			{ 3, Chord.Minor, 2 },
			{ 6, Chord.Minor, 2 },
			{ 2, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },
		};

		public static Progression PopPunk = new Progression {
			{ 1, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
			{ 6, Chord.Minor, 2 },
			{ 4, Chord.Major, 2 },
		};
		public static Progression SensitiveFemale = new Progression {
			{ 6, Chord.Minor, 2 },
			{ 4, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
		};
		public static Progression Turnaround = new Progression {
			{ 1, Chord.Major, 4 },
			{ 2, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
		};
		public static Progression Sears = new Progression {
			{ 1, Chord.Major, 2 },
			{ 6, Chord.Minor, 2 },
			{ 2, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },
			{ 1, Chord.Major, 2 },
			{ 6, Chord.Minor, 2 },
			{ 2, Chord.Minor, 2 },
			{ 5, Chord.Major, 2 },

			{ 1, Chord.Major, 2 },
			{ 1, Chord.Major7, 2 },
			{ 4, Chord.Major, 2 },
			{ 4, Chord.Minor7, 2 },
			{ 1, Chord.Major, 2 },
			{ 5, Chord.Major, 2 },
		};
		public static Progression Fifties = new Progression {
			{ 1, Chord.Major },
			{ 6, Chord.Minor },
			{ 4, Chord.Major },
			{ 5, Chord.Major },
		};
		public static Progression FiftiesMinor = new Progression {
			{ 1, Chord.Major },
			{ 6, Chord.Minor },
			{ 2, Chord.Minor },
			{ 5, Chord.Major },
		};
		public static Progression FiftiesLong = new Progression {
			{ 1, Chord.Major },
			{ 6, Chord.Minor },
			{ 1, Chord.Major },
			{ 6, Chord.Minor },
			{ 4, Chord.Major },
			{ 5, Chord.Major },
			{ 5, Chord.Major },
		};



		public static IEnumerable<Progression> All {
			get {
				return new [] {
					Common1,
					Common2,
					Common3,
					Common4,
					Common5,
					Common6,
					Common7,
					Common8,
					Major1,
					PopPunk,
					SensitiveFemale,
					Sears,
					Turnaround,
					Fifties,
					FiftiesLong,
				};
			}
		}

		IEnumerator<ProgressionNotes> IEnumerable<ProgressionNotes>.GetEnumerator ()
		{
			return pps.GetEnumerator ();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return pps.GetEnumerator ();
		}
	}
}

