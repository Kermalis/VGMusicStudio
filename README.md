# Kermalis's GBA Music Studio

A tool that is designed to be a Sappy replacement and also support different game engines.

![Preview](https://i.imgur.com/MOBHq03.gif)

----
# Some Advantages Over Sappy:
* Pause button & song position changing
* You can play the in-game instruments with a MIDI-in keyboard
* You can view and edit track events
* You can import MIDI files without having to convert them yourself
* You can save the voice tables to SF2 soundfont files
* You can save songs to ASM S files
* The UI scales to the desired window size
* You can see representation of notes being played
* It is not intimidating to use
* Support for multiple song tables
* Support for "Mario & Luigi: Superstar Saga"

----
# To Do:
## M4A / MP2K Engine
* Add reverb DSP effect
* Add reverse playback
* Add SquareWave sweeping
* Add "note off with noise" for SquareWaves
* Repeat command
* Nested PATT (3 is the maximum)
* Confirm that priority kills the next lowest (6 kills 5 even if 4,5,7 are playing) in-game then implement it
* Fix ADSR on PSG instruments
* Find out why some instruments sound strange \[Example: Mario Kart Snow Land drum\]
* Support pret dissassembly projects
* Running status in song disassembler
* MIDI saving - preview the MIDI with the Sequencer class
* MIDI saving - UI with saving options, such as remapping

## Mario & Luigi: Superstar Saga Engine
* Properly implement "free notes"
* Voice tables

## General
* Add playing playlist from Games.yaml and fading out after a configurable amount of loops
* Maybe a nice waveform
* Exception handling for invalid config
* Add keyboard shortcuts to the UI
* Let on-screen piano play notes or interact with MIDI keyboard
* Remove "private set" in config and add saving of config
* Default remap voice
* Offset for event you're editing
* Put the event parameter as text in the parameter name label, so there is reference to the original value or in MODT/Note's cases, text representation
* Buttons in the taskbar like with most music players
* Edit "Mario & Luigi: Superstar Saga" playlist
* Minish cap remap
* Tempo numerical (it fits)
* Detachable piano
* Help dialog that explains the commands for each format
* Think about if keeping all songs in memory (and their voice tables in them) was *actually* a good idea
* If I go insane I'll support the MOD music format

----
# Special Thanks To:
* Ipatix
* tuku473
* Bregalad
* mimi
* Jesse (jelle)
* SomeShrug