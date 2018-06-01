# Kermalis's GBA Music Studio

A tool that is designed to be an updated Sappy.

![Preview](https://i.imgur.com/fsfxud4.gif)

## Some Advantages Over Sappy
* Pause button
* You can play the in-game instruments with a MIDI-in keyboard
* You can save the voice tables to a SF2 soundfont file
* The UI scales to the desired window size
* It is not intimidating to use

## To Do (No particular order)

* Add reverb DSP effect
* Add reverse playback
* Add SquareWave sweeping
* Add "note off with noise" for SquareWaves
* Fix ADSR on PSG instruments
* Find out why some instruments sound strange \[example: Mario Kart Snow Land drum\]
* Make the program run at 60 updates like the GBA, which might decrease CPU usage and will correct ADSR
* Add playing playlist from Games.yaml and fading out after a configurable amount of loops
* Maybe a nice waveform
* Scale the top panel of the UI
* Finish integrating SF2 writing
* Songtable finder
* Give instruments a defined color and have the bars show those colors, along with the keys on the piano
* Exception handling for invalid config
* Mute checkboxes next to tracks, along with another that indicates the piano will reflect their notes
* Add shortcuts for the UI
* Let on-screen piano play notes or interact with MIDI keyboard
* Fix application hanging when you close it
* A song class that contains the commands in order, that can be read from ROM/MIDI/SSEQ and then played/written
* Convert Config to a yaml deserialize call, if possible

## Special Thanks To:
* Ipatix