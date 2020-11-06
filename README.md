# Kermalis's VG Music Studio

[![Join on Discord](https://discordapp.com/api/guilds/571068449103675394/widget.png?style=shield)][Discord]
[![LatestVer](https://img.shields.io/github/v/release/Kermalis/VGMusicStudio.svg?include_prereleases)](https://github.com/Kermalis/VGMusicStudio/releases/latest)
[![Releases](https://img.shields.io/github/downloads/Kermalis/VGMusicStudio/total.svg)](https://github.com/Kermalis/VGMusicStudio/releases/latest)
[![License](https://img.shields.io/badge/License-LGPLv3-blue.svg)](LICENSE.md)

VG Music Studio is a music player and visualizer for the most common GBA music format (MP2K), AlphaDream's GBA music format, the most common NDS music format (SDAT), and a more rare NDS/WII music format (DSE) [found in PMD2 among others].

[![VG Music Studio Preview](https://i.imgur.com/hWJGG83.png)](https://www.youtube.com/watch?v=s1BZ7cRbtBU "VG Music Studio Preview")

If you want to talk or would like a game added to our configs, join our [Discord server][Discord]

----
## To Do:
### General
* MIDI saving - Preview the MIDI with the Sequencer class
* MIDI saving - UI with saving options, such as remapping
* MIDI saving - Make errors more clear
* Voice table viewer - Tooltips which provide a huge chunk of information
* Detachable piano
* Tempo numerical (it fits)
* Help dialog that explains the commands and config for each engine

### AlphaDream Engine
* ADSR
* Voice table - Find out the last 4 bytes in voice entry struct (probably ADSR)
* PSG channels 3 and 4
* Some more unknown commands
* Tempo per track

### DSE Engine
* ADSR
* Pitch bend
* LFO
* Ability to load SMDB and SWDB (Big Endian as opposed to SMDL and SWDL for Little Endian)
* Some more unknown commands

### MP2K Engine
* Add Golden Sun 2 reverb effect
* Add reverse playback
* Add SquareWave sweeping
* XCMD command
* REPT command
* Support pret dissassembly projects
* Running status in song disassembler
* Add "Metroid Fusion" & "Metroid: Zero Mission" engine information
* Mario Power Tennis compressed samples

### SDAT Engine
* Find proper formulas for LFO

----
## Special Thanks To:
### General
* Stich991 - Italian translation
* tuku473 - Design suggestions, colors, Spanish translation

### AlphaDream Engine
* irdkwia - Finding games that used the engine
* Jesse (jelle) - Engine research
* Platinum Lucario - Engine research

### DSE Engine
* PsyCommando - Extensive research [(and his DSE music tools)](https://github.com/PsyCommando/ppmdu)

### MP2K Engine
* Bregalad - Extensive documentation
* Ipatix - Engine research, help, [(and his MP2K music player)](https://github.com/ipatix/agbplay) from which some of my code is based on
* mimi - Told me about a hidden feature of the engine
* SomeShrug - Engine research and helped me understand more about the engine parameters

### SDAT Engine
* kiwi.ds SDAT Specification - Extensive documentation

----
## VG Music Studio Uses:
* [DLS2](https://github.com/Kermalis/DLS2)
* [EndianBinaryIO](https://github.com/Kermalis/EndianBinaryIO)
* [NAudio](https://github.com/naudio/NAudio)
* [ObjectListView](http://objectlistview.sourceforge.net)
* [My fork of Sanford.Multimedia.Midi](https://github.com/Kermalis/Sanford.Multimedia.Midi)
* [SoundFont2](https://github.com/Kermalis/SoundFont2)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet/wiki)

[Discord]: https://discord.gg/mBQXCTs