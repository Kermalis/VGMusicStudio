#!/usr/bin/python3

import os, sys, platform
import pathlib
import subprocess
import shutil

from pathlib import Path

# Some OS flags
is_windows = (sys.platform == 'win32') or (sys.platform == 'msys')
is_linux = sys.platform == 'linux'
is_macos = sys.platform == 'darwin'

deps = [
    'portaudio',
]
vcpkg_exe = ''
libs_portaudio = []

# To assign the correct platform moniker on the platform it's built on
def get_platform_and_architecture():
    os_platform = ""
    architecture = ""

    # Operating System
    if sys.platform == "win32":
        os_platform = "win"
    elif sys.platform == "darwin":
        os_platform = "osx"
    elif sys.platform == "linux":
        os_platform = sys.platform
    else:
        print("The OS type could not be determined.")
        exit()

    # CPU Architecture
    if (platform.machine() == 'x86_64') or (platform.machine() == 'amd64') or (platform.machine() == 'AMD64'):
        architecture = "x64"
    elif (platform.machine() == 'arm64') or (platform.machine() == 'ARM64') or (platform.machine() == 'aarch64') or (platform.machine() == 'Aarch64') or (platform.machine() == 'AARCH64'):
        architecture = "arm64"
    else:
        print("The CPU architecture type could not be determined.")
        exit()

    return os_platform + "-" + architecture

# Get platform and architecture variable, and path to copy to
dotnet_rid = get_platform_and_architecture()
lib_path = './PortAudio/lib/' + dotnet_rid

# Ensure we have access to the VCPKG executable, should be first argument
vcpkg_dir = os.path.abspath(os.environ.get('VCPKG_DIR'))
if not vcpkg_dir:
    print('Error, need environment variable VCPKG_DIR to point to directory where `vcpkg` executable is')
    sys.exit(1)
elif not os.path.exists(vcpkg_dir):
    print('Error, not able to find %s' % vcpkg_dir)
    sys.exit(1)

# Make sure the executable is correctly defined, based on the OS
if is_windows:
    vcpkg_exe = os.path.join(vcpkg_dir, 'vcpkg.exe')
else:
    vcpkg_exe = os.path.join(vcpkg_dir, 'vcpkg')

# Pull the latest commits from VCPKG repo, then upgrade all libraries
proc = subprocess.Popen(['git', 'pull'], cwd=vcpkg_dir)
proc.wait()
proc = subprocess.Popen([vcpkg_exe, 'upgrade', '--no-dry-run'])
proc.wait()

def BuildWindowsDeps(deps):
    # Build the x86-64 version of the Windows dependencies
    deps = ['%s:x64-windows' % x for x in deps]
    lib_src_dir = 'installed/x64-windows/bin/'
    libs_portaudio = [
        'portaudio.dll',
    ]

    # Upgrade MinGW deps
    subprocess.run("C:/msys64/usr/bin/bash.exe -lc '"'pacman-key --refresh-keys'"'", stdout=subprocess.PIPE, text=True)
    subprocess.run("C:/msys64/usr/bin/bash.exe -lc '"'pacman -Syuu --noconfirm'"'", stdout=subprocess.PIPE, text=True)
    subprocess.run("C:/msys64/usr/bin/bash.exe -lc '"'pacman -Syuu --noconfirm mingw-w64-x86_64-libadwaita'"'", stdout=subprocess.PIPE, text=True)

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    proc = subprocess.Popen([vcpkg_exe, 'install', *deps, '--overlay-triplets=dynamic-triplets'])
    proc.wait()
    print("All x86-64 Windows libraries built successfully!")

    # Now get the dlls that we really want
    for lib in libs_portaudio:
        src = os.path.join(vcpkg_dir, lib_src_dir, lib)
        shutil.copy(src, lib_path)
    print("All x86-64 Windows libraries copied successfully!")
    
    # Clear deps list and re-add needed deps
    # Required to prevent the following error:
    # <unknown>:1:23: error: expected eof
    #  on expression: portaudio:x64-windows:arm64-windows
    #                                      ^
    deps.clear()
    deps = [
        'portaudio',
    ]

    # Build the ARM64 version of the Windows dependencies
    deps = ['%s:arm64-windows' % x for x in deps]
    lib_src_dir = 'installed/arm64-windows/bin/'
    libs_portaudio = [
        'portaudio.dll',
    ]

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    proc = subprocess.Popen([vcpkg_exe, 'install', *deps, '--overlay-triplets=dynamic-triplets'])
    proc.wait()
    print("All ARM64 Windows libraries built successfully!")

    # Now get the dlls that we really want
    for lib in libs_portaudio:
        src = os.path.join(vcpkg_dir, lib_src_dir, lib)
        shutil.copy(src, lib_path)
    print("All ARM64 Windows libraries copied successfully!")
    
    # Clear deps list and re-add needed deps
    # Required to prevent the following error:
    # <unknown>:1:23: error: expected eof
    #  on expression: portaudio:arm64-windows:x64-linux
    #                                        ^
    deps.clear()
    deps = [
        'portaudio',
    ]

def BuildLinuxDeps(deps):
    # Build the x86-64 version of the Linux dependencies
    lib_src_dir_static = 'installed/x64-linux/lib/'
    lib_src_dir_dynamic = 'installed/x64-linux-dynamic/lib/'
    libs_portaudio = [
        'libportaudio.a',
        'libportaudio.so'
    ]
    libs_jack = [
        'libjack.a',
        'libjack.so'
    ]

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    for dep in deps:
        proc = subprocess.Popen([vcpkg_exe, 'install', dep, '--overlay-triplets=dynamic-triplets'])
        proc.wait()
        proc = subprocess.Popen([vcpkg_exe, 'install', dep + ":x64-linux-dynamic", '--overlay-triplets=dynamic-triplets'])
        proc.wait()
    print("All x86-64 Linux libraries built successfully!")

    # Now get the dlls that we really want
    for lib in deps:
        src_portaudio_static = os.path.join(vcpkg_dir, lib_src_dir_static, libs_portaudio[0])
        src_portaudio_dynamic = os.path.join(vcpkg_dir, lib_src_dir_dynamic, libs_portaudio[1])
        src_jack_static = os.path.join(vcpkg_dir, lib_src_dir_static, libs_jack[0])
        src_jack_dynamic = os.path.join(vcpkg_dir, lib_src_dir_dynamic, libs_jack[1])
        try:
            shutil.copy(src_portaudio_static, lib_path)
            shutil.copy(src_portaudio_dynamic, lib_path)
            shutil.copy(src_jack_static, lib_path)
            shutil.copy(src_jack_dynamic, lib_path)
        except:
            print("Unable to copy library: File not found.")
    print("All x86-64 Linux libraries copied successfully!")

    # Clear deps list and re-add needed deps
    # Required to prevent the following error:
    # <unknown>:1:23: error: expected eof
    #  on expression: portaudio:x64-linux:arm64-linux
    #                                    ^
    deps.clear()
    deps = [
        'portaudio:arm64-linux',
    ]

    # Build the ARM64 version of the Linux dependencies
    # Note: VCPKG doesn't support building dynamic libs yet,
    #       so we have to use the static ones instead.
    lib_src_dir_static = 'installed/arm64-linux/lib/'
    libs_portaudio = [
        'libportaudio.a',
    ]

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    proc = subprocess.Popen([vcpkg_exe, 'install', *deps, '--overlay-triplets=dynamic-triplets'])
    proc.wait()
    print("All ARM64 Linux libraries built successfully!")

    # Now get the dlls that we really want
    for lib in libs_portaudio:
        src_portaudio_static = os.path.join(vcpkg_dir, lib_src_dir_static, lib)
        try:
            shutil.copy(src_portaudio_static, lib_path)
        except:
            print("Unable to copy library: File not found.")
    print("All ARM64 Linux libraries copied successfully!")
    
    # Clear deps list and re-add needed deps
    # Required to prevent the following error:
    # <unknown>:1:23: error: expected eof
    #  on expression: portaudio:arm64-linux:x64-osx-dynamic
    #                                      ^
    deps.clear()
    deps = [
        'portaudio',
    ]

def BuildMacOSDeps(deps):
    # Build the x86-64 version of the macOS dependencies
    deps[0] = "portaudio:x64-osx-dynamic"
    
    lib_src_dir = 'installed/x64-osx-dynamic/lib/'
    libs_portaudio = [
        'libportaudio.dylib',
    ]

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    proc = subprocess.Popen([vcpkg_exe, 'install', *deps, '--overlay-triplets=dynamic-triplets'])
    proc.wait()
    print("All x86-64 macOS libraries built successfully!")

    # Now get the dlls that we really want
    for lib in libs_portaudio:
        src = os.path.join(vcpkg_dir, lib_src_dir, lib)
        shutil.copy(src, lib_path)
    print("All x86-64 macOS libraries copied successfully!")
    
    # Clear deps list and re-add needed deps
    # Required to prevent the following error:
    # <unknown>:1:23: error: expected eof
    #  on expression: portaudio:x64-os:arm64-osx-dynamic
    #                                 ^
    deps.clear()
    deps = [
        'portaudio:arm64-osx-dynamic',
    ]

    # Build the ARM64 version of the macOS dependencies
    lib_src_dir = 'installed/arm64-osx-dynamic/lib/'
    libs_portaudio = [
        'libportaudio.dylib',
    ]

    # First make sure the lib directory is there
    os.makedirs(pathlib.Path(lib_path), exist_ok=True)

    # Install the deps
    proc = subprocess.Popen([vcpkg_exe, 'install', *deps, '--overlay-triplets=dynamic-triplets'])
    proc.wait()
    print("All ARM64 macOS libraries built successfully!")

    # Now get the dlls that we really want
    for lib in libs_portaudio:
        src = os.path.join(vcpkg_dir, lib_src_dir, lib)
        shutil.copy(src, lib_path)
    print("All ARM64 macOS libraries copied successfully!")
        
def Main():
    # Currently, I don't know how to compile libraries of every OS within one OS
    if is_windows:
        BuildWindowsDeps(deps)
    elif is_linux:
        BuildLinuxDeps(deps)
    elif is_macos:
        BuildMacOSDeps(deps)
    print("All neccessary tasks completed!")

# Runs the main function
Main()
