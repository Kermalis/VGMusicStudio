# Kermalis's VG Music Studio

VG Music Studio is a music player and visualizer for the most common GBA music format (M4A/MP2K), Mario & Luigi: Superstar Saga, the most common NDS music format (SDAT), and a more rare NDS/WII music format (DSE) [found in PMD2 among others].

![Preview](https://i.imgur.com/BJI8GU3.gif)

Discord Server: https://discord.gg/q2pAw3x

----
# Some Advantages Over Sappy:
* Pause button & song position changing
* You can play the in-game instruments with a MIDI-in keyboard
* You can view and edit track events
* You can import MIDI files without having to convert them yourself
* You can save the voice tables to SF2 SoundFont files
* You can save songs to ASM S files
* You can play songs in a playlist like other music players
* The UI scales to the desired window size
* You can see representation of notes being played
* It is not intimidating to use
* Support for multiple song tables
* Support for "Mario & Luigi: Superstar Saga"
* Support for "Golden Sun" synth instruments

----
# To Do:
## General
* MIDI saving - Preview the MIDI with the Sequencer class
* MIDI saving - UI with saving options, such as remapping
* MIDI saving - Make errors more clear
* Playlist mode - A way to exit playlist mode
* Config - Exception handling
* Config - Saving
* Voice table editor - Tooltips which provide a huge chunk of information
* Track editor - Offset for the event you're editing
* Track editor - Put the event parameter as text in the parameter name label, so there is reference to the original value (or in MODT/Note's cases, text representation)
* Maybe a nice waveform
* Let the on-screen piano play notes or interact with a MIDI keyboard
* Detachable piano
* Tempo numerical (it fits)
* Help dialog that explains the commands for each engine and config options
* If I go insane I'll support the MOD music format

## M4A / MP2K Engine
* Add Golden Sun 2 reverb effect
* Add reverse playback
* Add SquareWave sweeping
* XCMD command
* Repeat command
* Nested PATT (3 is the maximum)
* Support pret dissassembly projects
* Running status in song disassembler
* Add "Metroid Fusion" & "Metroid: Zero Mission" engine information

## Mario & Luigi: Superstar Saga Engine
* Voice table - Find out the last 4 bytes in voice entry struct (probably ADSR)
* Find channel and track limits (most tracks is 9 in credits)

----
# Special Thanks To:
# General
* tuku473
* Stich991

## M4A/MP2K Engine
* Ipatix - Engine research, help, [(and his M4A/MP2K music player)](https://github.com/ipatix/agbplay) from which some of my code is based on
* Bregalad - Extensive documentation
* mimi - Told me about a hidden feature of the engine
* SomeShrug - Engine research and helped me understand more about the engine parameters

# Mario & Luigi: Superstar Saga Engine
* Jesse (jelle) - Engine research
* Platinum Lucario - Engine research

# SDAT Engine
* kiwi.ds SDAT Specification - Extensive documentation

# DSE Engine
* PsyCommando - Extensive research [(and his DSE music tools)](https://github.com/PsyCommando/ppmdu)

----
# VG Music Studio Uses:
* [NAudio](https://github.com/naudio/NAudio)
* [ObjectListView](http://objectlistview.sourceforge.net)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet/wiki)
* [My EndianBinaryIO library](https://github.com/Kermalis/EndianBinaryIO)
* [My SoundFont2 library](https://github.com/Kermalis/SoundFont2)
* [My fork of Sanford.Multimedia.Midi](https://github.com/Kermalis/Sanford.Multimedia.Midi)