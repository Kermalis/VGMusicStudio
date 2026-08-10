# Kermalis's VG Music Studio

[![Join on Discord](https://discordapp.com/api/guilds/571068449103675394/widget.png?style=shield)][Discord]
[![LatestVer](https://img.shields.io/github/v/release/Kermalis/VGMusicStudio.svg?include_prereleases)](https://github.com/Kermalis/VGMusicStudio/releases/latest)
[![Releases](https://img.shields.io/github/downloads/Kermalis/VGMusicStudio/total.svg)](https://github.com/Kermalis/VGMusicStudio/releases/latest)
[![License](https://img.shields.io/badge/License-LGPLv3-blue.svg)](LICENSE.md)

VG Music Studio is a cross-platform music player and visualizer for various video game sound formats, including:
* Music Player 2000 (MP2K), the most common GBA music format used by the official MKS4AGB toolkit.
* AlphaDream's GBA music format.
* Nitro SoundPlayer Sound Data (SDAT), the most common music format for Nintendo DS.
* Digital Sound Elements (DSE), developed by Hidenori Suzuki, a less common PS1/PS2/NDS/Wii music format [found in PMD2 among others], originally used internally at Square as 'Square Digital Sound Elements' and then licensed out as 'Procyon Studio Digital Sound Elements' after moving to Procyon Studio. Currently, only the DS and Wii versions are supported.

![VG Music Studio](.github/data/vgms-gui.png)

VG Music Studio can now automatically detect MP2K sound tables and player data without the need for configs. Config files can still be used to manually override any detected entries.

If you want to talk or would like a game added to our configs, join our [Discord server][Discord]

----
## To Do:
### General
* MIDI saving - Preview the MIDI with the Sequencer class
* MIDI saving - UI with saving options, such as remapping
* MIDI saving - Make errors more clear
* Voice table viewer - Tooltips which provide a huge chunk of information
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
* Support for SMDS and WDS (for the earlier PlayStation version, used in Xenogears). The chunks in this version have no labels in them.
* Support for SMDM and SWDM (for the earlier PlayStation 2 version, used in Xenosaga Episode I). The chunks in this version have no labels in them.

### MP2K Engine
* Add Golden Sun 2 reverb effect
* Add reverse playback
* Fix Square1 PSG sweeping issues
* Support pret dissassembly projects
* Running status in song disassembler
* Add "Metroid Fusion" & "Metroid: Zero Mission" engine information
* Mario Power Tennis compressed samples
* MP2K.yaml editor
* Automatically create MP2K YAML entries for games without a MP2K entry

### SDAT Engine
* Find proper formulas for LFO

----
## Building
### Windows
VG Music Studio already includes the native library Nuget package for libadwaita. So that it can build and run without any issues and without needing to install MSYS2.

But if for any reason you need to update the libadwaita Nuget, follow the steps below:
1. Download and install MSYS2 from [the official website](https://www.msys2.org/), and ensure it is installed in the default directory (``C:\``). If you install it in a different location, it will need to be set via the ``Path`` environment variable.
2. After installation, run the following commands in the MSYS2 terminal: ``pacman -Syuu`` to reload the database and update all the packages.
** If any errors occur saying that the signatures are of `unknown trust` during the update process and can't update, run ``pacman-key --refresh-keys`` to refresh the keys

If libawaita hasn't been installed in MSYS2:
2.1 (Optional) Run each of the following commands to install the required packages:
``pacman -S mingw-w64-x86_64-gtk4``
``pacman -S mingw-w64-x86_64-libadwaita``
``pacman -S mingw-w64-x86_64-gtksourceview5``

3. Now run ``Build.bat`` inside the CreateNugets folder, it should update the .csproj for the Nuget and build the Nuget.

### macOS
#### Intel (x86-64)
Even though it will build without any issues, since VG Music Studio runs on GTK4 bindings via Gir.Core, it requires some C libraries to be installed or placed within the same directory as the macOS executable.

Otherwise it will complain upon launch with the following System.TypeInitializationException error:
``DllNotFoundException: Unable to load DLL 'libgtk-4-1.dylib' or one of its dependencies: The specified module could not be found. (0x8007007E)``

To avoid this error while debugging VG Music Studio, you will need to do the following:
1. Download and install [Homebrew](https://brew.sh/) with the following macOS terminal command:
``/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"``
This will ensure Homebrew is installed in the default directory, which is ``/usr/local``.
2. After installation, run the following command from the macOS terminal to update all packages: ``brew update``
3. Run each of the following commands to install the required packages:
``brew install gtk4``
``brew install libadwaita``
``brew install gtksourceview5``

#### Apple Silicon (AArch64)
Currently unknown if this will work on Apple Silicon, since it's a completely different CPU architecture, it may need some ARM-specific APIs to build or function correctly.

If you have figured out a way to get it to run under Apple Silicon, please let us know!

### Linux
Most Linux distributions should be able to build this without anything extra to download and install.

However, if you get the following System.TypeInitializationException error upon launching VG Music Studio during debugging:
``DllNotFoundException: Unable to load DLL 'libgtk-4-1.so.0' or one of its dependencies: The specified module could not be found. (0x8007007E)``
Then it means that either ``gtk4``, ``libadwaita`` or ``gtksourceview5`` is missing from your current installation of your Linux distribution. Often occurs if a non-GTK based desktop environment is installed by default, or the Linux distribution has been installed without a GUI.

To install them, run the following commands:
#### Debian (or Debian based distributions, such as Ubuntu, elementary OS, Pop!_OS, Zorin OS, Kali Linux etc.)
First, update the current packages with ``sudo apt update && sudo apt upgrade`` and install any updates, then run:
``sudo apt install libgtk-4-1``
``sudo apt install libadwaita-1``
``sudo apt install libgtksourceview-5``

##### Vanilla OS (Debian based distribution)
Debian based distribution, Vanilla OS, uses the Distrobox based package management system called 'apx' instead of apt (apx as in 'apex', not to be confused with Microsoft Windows's UWP appx packages).
But it is still a Debian based distribution, nonetheless. And fortunately, it comes pre-installed with GNOME, which means you don't need to install any libraries!

You will, however, still need to install the .NET SDK and .NET Runtime using apx, and cannot be used with 'sudo'.

Instead, run any commands to install packages like this:
``apx install [package-name]``

#### Arch Linux (or Arch Linux based distributions, such as Manjaro, Garuda Linux, EndeavourOS, SteamOS etc.)
First, update the current packages with ``sudo pacman -Syy && sudo pacman -Syuu`` and install any updates, then run:
``sudo pacman -S gtk4``
``sudo pacman -S libadwaita``
``sudo pacman -S gtksourceview5``

#### Fedora (or other Red Hat based distributions, such as Red Hat Enterprise Linux, AlmaLinux, Rocky Linux etc.)
First, update the current packages with ``sudo dnf check-update && sudo dnf update`` and install any updates, then run:
``sudo dnf install gtk4``
``sudo dnf install libadwaita``
``sudo dnf install gtksourceview5``

#### openSUSE (or other SUSE Linux based distributions, such as SUSE Linux Enterprise, GeckoLinux etc.)
First, update the current packages with  ``sudo zypper up`` and install any updates, then run:
``sudo zypper in libgtk-4-1``
``sudo zypper in libadwaita-1-0``
``sudo zypper in libgtksourceview-5-0``

#### Alpine Linux (or Alpine Linux based distributions, such as postmarketOS etc.)
First, update the current packages with ``apk -U upgrade`` to their latest versions, then run:
``apk add gtk4.0``
``apk add libadwaita``
``apk add gtksourceview5``

Please note that VG Music Studio may not be able to build on other CPU architectures (such as AArch64, ppc64le, s390x etc.), since it hasn't been developed to support those architectures yet. Same thing applies for postmarketOS.

#### Void Linux
First, update the current packages with ``sudo xbps-install -Su`` to their latest versions, then run:
``sudo xbps-install gtk4``
``sudo xbps-install libadwaita``
``sudo xbps-install gtksourceview5``

### FreeBSD
Currently, .NET has not been ported to FreeBSD or similar operating systems. As a result, it cannot run natively under FreeBSD and may require a Linux to FreeBSD compatibility layer to run it.

----
## Special Thanks To:
### General
* Stich991 - Italian translation
* tuku473 - Design suggestions, colors, Spanish translation
* J. Ritchie Carroll (from Grid Protection Alliance) - Int24 and UInt24 classes and functions
* Benjamin Summerton (define-private-public) - PortAudio bindings for C#
* LSXPrime - Developing [the SoundFlow library](https://github.com/LSXPrime/SoundFlow) and helping out with using QueueDataProvider correctly

### AlphaDream Engine
* irdkwia - Finding games that used the engine
* Jesse (jelle) - Engine research
* Platinum Lucario - Engine research

### DSE Engine
* PsyCommando - Extensive research [(and his DSE music tools)](https://github.com/PsyCommando/ppmdu)

### MP2K Engine
* Bregalad - Extensive documentation
* Ipatix - Engine research, help, [(his MP2K music player)](https://github.com/ipatix/agbplay) from which some of my code is based on, and the [LowLatencyRingbuffer](https://github.com/ipatix/agbplay/blob/agbplay_v2/src/agbplay/LowLatencyRingbuffer.cpp) which helps PortAudio to process audio with low latency
* mimi - Told me about a hidden feature of the engine
* SomeShrug - Engine research and helped me understand more about the engine parameters

### SDAT Engine
* kiwi.ds SDAT Specification - Extensive documentation

----
## VG Music Studio Uses:
### Core
* [EndianBinaryIO](https://github.com/Kermalis/EndianBinaryIO)
* [KMIDI](https://github.com/Kermalis/KMIDI)
* [DLS2](https://github.com/Kermalis/DLS2)
* [SoundFont2](https://github.com/Kermalis/SoundFont2)
* [PortAudio bindings](https://github.com/PlatinumLucario/VGMusicStudio/tree/new-gui-experimental/VG%20Music%20Studio%20-%20Core/PortAudio) [from Benjamin Summerton's Bassoon Project](https://gitlab.com/define-private-public/Bassoon)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet/wiki)
### New GUI (Cross platform):
* [GTK4](https://gtk.org)
* [Adwaita](https://gitlab.gnome.org/GNOME/libadwaita)
* [Gir.Core](https://github.com/gircore/gir.core)

### Old Legacy GUI (Windows only)
* [NAudio](https://github.com/naudio/NAudio)
* [ObjectListView](http://objectlistview.sourceforge.net)

[Discord]: https://discord.gg/mBQXCTs