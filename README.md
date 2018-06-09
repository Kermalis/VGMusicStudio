# Kermalis's GBA Music Studio

A tool that is designed to be an updated Sappy.

![Preview](https://i.imgur.com/joFBQMv.gif)

## Some Advantages Over Sappy
* Pause button
* You can play the in-game instruments with a MIDI-in keyboard
* You can save the voice tables to a SF2 soundfont file
* The UI scales to the desired window size
* It is not intimidating to use
* You can see representation of notes being played

## To Do (No particular order)

* Add reverb DSP effect
* Add reverse playback
* Add SquareWave sweeping
* Add "note off with noise" for SquareWaves
* Fix ADSR on PSG instruments
* Find out why some instruments sound strange \[Example: Mario Kart Snow Land drum\]
* Make the program run at 60 updates like the GBA, which might decrease CPU usage and will correct ADSR
* Add playing playlist from Games.yaml and fading out after a configurable amount of loops
* Maybe a nice waveform
* Scale the top panel of the UI
* Finish integrating SF2 writing
* Songtable finder
* Exception handling for invalid config
* Add shortcuts for the UI
* Let on-screen piano play notes or interact with MIDI keyboard
* Fix application hanging when you close it
* A song class that contains the commands in order, that can be read from ROM/MIDI/SSEQ and then played/written
* Convert Config to a yaml deserialize call, if possible
* Remove "private set" in config and add saving of config
* Add remapping of instruments in their game config \[Example: instrument 95 in a game maps to instrument 48 in MIDI, so have colors represent that and have MIDI rips use 48\]
* Fix UI crashing when some invalid songs are switched to
* Support pret dissassembly projects if they need that

## Special Thanks To:
* Ipatix
* Tukku473