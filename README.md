# Kermalis's VG Music Studio

VG Music Studio is a music player and visualizer for the most common GBA music format (M4A/MP2K), Mario & Luigi: Superstar Saga, the most common NDS music format (SDAT), and a more rare NDS/WII music format (DSE) [found in PMD2 among others].

![Preview](https://i.imgur.com/BJI8GU3.gif)

Discord Server: https://discord.gg/q2pAw3x

----
# To Do:
## General
* MIDI saving - Preview the MIDI with the Sequencer class
* MIDI saving - UI with saving options, such as remapping
* MIDI saving - Make errors more clear
* Playlist mode - A way to exit playlist mode
* Voice table editor - Tooltips which provide a huge chunk of information
* Track editor - Offset for the event you're editing
* Track editor - Put the event parameter as text in the parameter name label, so there is reference to the original value (or in LFOType/Note's cases, text representation)
* Maybe a nice waveform
* Let the on-screen piano play notes or interact with a MIDI keyboard
* Detachable piano
* Tempo numerical (it fits)
* Help dialog that explains the commands for each engine and config options

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
* ADSR
* Voice table - Find out the last 4 bytes in voice entry struct (probably ADSR)

## DSE Engine
* ADSR
* Ability to load SMDB and SWDB (Big Endian as opposed to SMDL and SWDL for Little Endian)

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