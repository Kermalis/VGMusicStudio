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
## Building
### Windows
Even though it will build without any issues, since VG Music Studio runs on GTK4 bindings via Gir.Core, it requires some C libraries to be installed or placed within the same directory as the Windows executable (.exe).

Otherwise it will complain upon launch with the following System.TypeInitializationException error:
``DllNotFoundException: Unable to load DLL 'libgtk-4-1.dll' or one of its dependencies: The specified module could not be found. (0x8007007E)``

To avoid this error while debugging VG Music Studio, you will need to do the following:
1. Download and install MSYS2 from [the official website](https://www.msys2.org/), and ensure it is installed in the default directory: ``C:\``.
2. After installation, run the following commands in the MSYS2 terminal: ``pacman -Syy`` to reload the package database, then ``pacman -Syuu`` to update all the packages.
3. Run each of the following commands to install the required packages:
``pacman -S mingw-w64-x86_64-gtk4``
``pacman -S mingw-w64-x86_64-libadwaita``
``pacman -S mingw-w64-x86_64-gtksourceview5``

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

##### ChimeraOS (Arch based distribution)
Note: Not to be confused with Chimera Linux, the Linux distribution made from scratch with a custom Linux kernel. This one is an Arch Linux based distribution.

Arch Linux based distribution, ChimeraOS, comes pre-installed with the GNOME desktop environment. To access it, open the terminal and type ``chimera-session desktop``.

But because it is missing the .NET SDK and .NET Runtime, and the root directory is read-only, you will need to run the following command: ``sudo frzr-unlock``

Then install any required packages like this example: ``sudo pacman -S [package-name]``

Note: Any installed packages installed in the root directory with the pacman utility will be undone when ChimeraOS is updated, due to the way [frzr](https://github.com/ChimeraOS/frzr) functions. Also, frzr may be what inspired Vanilla OS's [ABRoot](https://github.com/Vanilla-OS/ABRoot) utility.

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

#### Puppy Linux
Puppy Linux is an independent distribution that has many variants, each with packages from other Linux distributions.

It's not possible to find the gtk4, libadwaita and gtksourceview5 libraries or their dependencies in the GUI package management tool, Puppy Package Manager. Because Puppy Linux is built to be a portable and lightweight distribution and to be compatible with older hardware. And because of this, it is only possible to find gtk+2 libraries and other legacy dependencies that it relies on.

So therefore, VG Music Studio isn't supported on Puppy Linux.

#### Chimera Linux
Note: Not to be confused with the Arch Linux based distribution named ChimeraOS. This one is completely different and written from scratch, and uses a modified Linux kernel.

Chimera Linux already comes pre-installed with the GNOME desktop environment and uses the Alpine Package Kit. If you need to install any necessary packages, run the following command example:
``apk add [package-name]``

#### Void Linux
First, update the current packages with ``sudo xbps-install -Su`` to their latest versions, then run:
``sudo xbps-install gtk4``
``sudo xbps-install libadwaita``
``sudo xbps-install gtksourceview5``

### FreeBSD
It may be possible to build VG Music Studio on FreeBSD (and FreeBSD based operating systems), however this section will need to be updated with better accuracy on how to build on this platform.

If your operating system is FreeBSD, or is based on FreeBSD, the [portmaster](https://cgit.freebsd.org/ports/tree/ports-mgmt/portmaster/) utility will need to be installed before installing ``gtk40``, ``libadwaita`` and ``gtksourceview5``. A guide on how to do so can be found [here](https://docs.freebsd.org/en/books/handbook/ports/).

Once installed and configured, run the following commands to install these ports:
``portmaster -PP gtk40``
``portmaster -PP libadwaita``
``portmaster -PP gtksourceview5``

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