# Lullabies

**Lullabies** is a music generator for iOS using async methods and iOS midi synthesis.

The key components are:

* **Key.cs** defines musical `Keys`, `Chords`, and `Progressions`. It also contains a database of common western-music progressions.

* **Song.cs** defines the root `Song` class that is made up `Sections` (like verse, chorus, bridge). Each `Section` contains a `ChordProgression` and a tempo (BPM). `Melodies`, `Baselines`, etc. are generated for each section.

* **Instrument.cs** contains the functional interface for controlling (playing notes on) a MIDI instrument. You can create these and control them yourself, or create a `Song` that will control its own instruments.


