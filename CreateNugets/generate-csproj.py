#!/usr/bin/env python3

import os
import sys
import platform
import argparse
import re
import shutil
import subprocess
from pathlib import Path


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


def create_adwaita_csproj(mingw_folder, dotnet_rid, lib_paths, version):
    with open("./Adwaita/org.adwaita.native." + dotnet_rid + ".csproj", "w") as f:
        csproj_strings = [
            "<Project Sdk='"'Microsoft.NET.Sdk'"'>", "\n",
            "  <PropertyGroup>", "\n",
            "    <PackageLicenseExpression>LGPL-2.1-or-later</PackageLicenseExpression>", "\n",
            "    <PackageReadmeFile>README.md</PackageReadmeFile>", "\n",
            "    <OutputType>Library</OutputType>", "\n",
            "    <TargetFrameworks>netstandard2.0;netcoreapp3.1;net6.0;net8.0</TargetFrameworks>", "\n",
            "    <NoWarn>NU5128</NoWarn>", "\n",
            "    <RuntimeIdentifier>" + dotnet_rid + "</RuntimeIdentifier>", "\n",
            "    <AssemblyName>Adwaita.Native</AssemblyName>", "\n",
            "    <Version>" + version + "</Version>", "\n",
            "\n",
            "    <PackageProjectUrl>https://github.com/PlatinumLucario/VGMusicStudio/tree/new-gui-experimental/CreateNugets</PackageProjectUrl>", "\n",
            "    <RepositoryUrl>https://gitlab.gnome.org/GNOME/libadwaita/</RepositoryUrl>", "\n",
            "    <PackageTags>adwaita libadwaita gtk glib gio native runtime</PackageTags>", "\n",
            "\n",
            "    <!-- Nuget Properties -->", "\n",
            "    <Description>", "\n",
            "    Building blocks for modern GNOME applications.", "\n",
            "    Source code repository: https://gitlab.gnome.org/GNOME/libadwaita/", "\n",
            "    </Description>", "\n",
            "    <IncludeBuildOutput>false</IncludeBuildOutput>", "\n",
            "\n",
            "    <!-- Pack Option -->", "\n",
            "    <Title>Adwaita " + dotnet_rid + " v" + version + "</Title>", "\n",
            "    <PackageId>org.adwaita.native." + dotnet_rid + "</PackageId>", "\n",
            "\n",
            "    <!-- Signing -->", "\n",
            "    <SignAssembly>false</SignAssembly>", "\n",
            "    <PublicSign>false</PublicSign>", "\n",
            "    <DelaySign>false</DelaySign>", "\n",
            "  </PropertyGroup>", "\n",
            "\n",
            "  <ItemGroup>", "\n",
            "    <None Include='"'./README.md'"' Pack='"'true'"' PackagePath='"'/'"'/>", "\n",
            "  </ItemGroup>", "\n",
            "\n",
            "  <ItemGroup>", "\n",
            "    <!-- Native libraries can only be in native directories -->", "\n"
            ]
        prev_path = "" # To prevent any duplicate paths, this string variable is made
        for path in lib_paths:
            if path == prev_path:
                continue # That way, if the path is identical to the previous, it'll continue to the next one
            # Append the path to the csproj strings
            csproj_strings.append(
                "    <NativeLibrary Include=" + '"' + "C:/msys64" + path + '"' + " " + "/>" + "\n"
            )
            prev_path = path
        csproj_strings.append(
            "    \n" +
            "     <!-- SVG pixbuf loader -->" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/libcharset-1.dll" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/librsvg-2-2.dll" + '"' + " " + "/>" + "\n"
            "     \n" +
            "     <!-- Executable binaries -->" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gdbus.exe" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gdk-pixbuf-query-loaders.exe" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gspawn-win64-helper.exe" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gspawn-win64-helper-console.exe" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gtk4-query-settings.exe" + '"' + " " + "/>" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/bin/gtk4-update-icon-cache.exe" + '"' + " " + "/>" + "\n"
            "     \n" +
            "     <!-- Shared asset folders -->" + "\n"
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/lib/gdk-pixbuff-2.0/**" + '"' + " " + "LinkBase=" + '"' + "../lib/gdk-pixbuff-2.0" + '"' + " " + "/>" + "\n" +
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/share/glib-2.0/schemas/**" + '"' + " " + "LinkBase=" + '"' + "../share/glib-2.0/schemas" + '"' + " " + "/>" + "\n" +
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/share/locale/**" + '"' + " " + "LinkBase=" + '"' + "../share/locale" + '"' + " " + "/>" + "\n" +
            "     <NativeLibrary Include=" + '"' + mingw_folder + "/share/icons/Adwaita/**" + '"' + " " + "LinkBase=" + '"' + "../share/icons/Adwaita" + '"' + " " + "/>" + "\n"
        )
        csproj_strings.append(
            " </ItemGroup>" + "\n" +
            "  \n" +
            "  <ItemGroup>" + "\n" +
            "    <Content Include='"'@(NativeLibrary)'"'>" + "\n" +
            "      <PackagePath>runtimes/" + dotnet_rid + "/native/%(Filename)%(Extension)</PackagePath>" + "\n" +
            "      <Pack>true</Pack>" + "\n" +
            "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>" + "\n" +
            "    </Content>" + "\n"
            "  </ItemGroup>" + "\n" +
            "\n" +
            "</Project>" + "\n"
        )
        writable_strings = ' '.join(csproj_strings)
        f.write(writable_strings)

def create_portaudio_csproj(dotnet_rid, pa_libs, version):
    if dotnet_rid.startswith('win'):
        os_platform = "Windows"
    elif dotnet_rid.startswith('osx'):
        os_platform = "macOS"
    elif dotnet_rid.startswith('linux'):
        os_platform = "Linux"
    if dotnet_rid.endswith('x64'):
        cpu = "x64"
    elif dotnet_rid.endswith('arm64'):
        cpu = "ARM64"
    with open("./PortAudio/org.portaudio.runtime." + dotnet_rid + ".csproj", "w") as f:
        csproj_strings = [
            "<Project Sdk='"'Microsoft.NET.Sdk'"'>", "\n",
            "  <PropertyGroup>", "\n",
            "    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>", "\n",
            "    <PackageReadmeFile>README.md</PackageReadmeFile>", "\n",
            "    <OutputType>Library</OutputType>", "\n",
            "    <TargetFrameworks>netstandard2.0;netcoreapp3.1;net6.0;net8.0</TargetFrameworks>", "\n",
            "    <NoWarn>NU5128</NoWarn>", "\n",
            "    <RuntimeIdentifier>" + dotnet_rid + "</RuntimeIdentifier>", "\n",
            "    <AssemblyName>PortAudio.Native</AssemblyName>", "\n",
            "    <Version>" + version + "</Version>", "\n",
            "\n",
            "    <PackageProjectUrl>https://gitlab.com/PlatinumLucario/Bassoon</PackageProjectUrl>", "\n",
            "    <RepositoryUrl>https://github.com/PortAudio/portaudio</RepositoryUrl>", "\n",
            "    <PackageTags>native library audio music playback cross platform PortAudio port audio</PackageTags>", "\n",
            "\n",
            "    <!-- Nuget Properties -->", "\n",
            "    <Description>.NET native " + os_platform + " " + cpu + " wrapper for PortAudio.", "\n",
            "\n",
            "    This Nuget is useful for projects that rely on the native PortAudio library.", "\n",
            "\n",
            "    Also note that https://www.nuget.org/packages/PortAudioSharp uses this Nuget, and is helpful if you need to use PortAudio C# bindings.", "\n",
            "    </Description>", "\n",
            "    <IncludeBuildOutput>false</IncludeBuildOutput>", "\n",
            "\n",
            "    <!-- Pack Option -->", "\n",
            "    <Title>PortAudio " + dotnet_rid + " v" + version + "</Title>", "\n",
            "    <PackageId>org.portaudio.runtime." + dotnet_rid + "</PackageId>", "\n",
            "\n",
            "    <!-- Signing -->", "\n",
            "    <SignAssembly>false</SignAssembly>", "\n",
            "    <PublicSign>false</PublicSign>", "\n",
            "    <DelaySign>false</DelaySign>", "\n",
            "  </PropertyGroup>", "\n",
            "\n",
            "  <ItemGroup>", "\n",
            "    <None Include='"'./README.md'"' Pack='"'true'"' PackagePath='"'/'"'/>", "\n",
            "  </ItemGroup>", "\n",
            "\n",
            "  <ItemGroup>", "\n",
        ]
        for lib in pa_libs:
            csproj_strings.append(
                "    <NativeLibrary Include=" + '"' + 'lib/' + dotnet_rid + "/" + lib + '"/>' + "\n"
            )
        csproj_strings.append(
            "  </ItemGroup>" + "\n" +
            "  <ItemGroup>" + "\n" +
            "    <!-- Native library must be in native directory... -->" + "\n" +
            "    <!-- If project is built as a STATIC_LIBRARY (e.g. Windows) then we don't have to include it -->" + "\n" +
            "    <Content Include='"'@(NativeLibrary)'"'>" + "\n" + 
            "      <PackagePath>runtimes/" + dotnet_rid + "/native/%(Filename)%(Extension)</PackagePath>" + "\n" +
            "      <Pack>true</Pack>" + "\n" +
            "      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>" + "\n" +
            "    </Content>" + "\n" +
            "  </ItemGroup>" + "\n" +
            "</Project>" + "\n"
        )
        writable_strings = ' '.join(csproj_strings)
        f.write(writable_strings)


# Windows-specific functions
def windows(dotnet_rid):
    lib_paths = []
    mingw_folder = ""
    pa_version = ""
    adw_version = ""

    # Checks the Environment Variable
    if os.getenv("MinGWFolder") == None:
        mingw_folder = "C:/msys64/mingw64"
    else:
        mingw_folder = os.getenv("MinGWFolder")

    # Gets the output from the MSYS2 bash terminal
    bash_output_libadw = subprocess.run("C:/msys64/usr/bin/bash.exe -lc '"'ldd /mingw64/bin/libadwaita-1-0.dll | grep ''\\/mingw.*\\.dll'' -o'"'", stdout=subprocess.PIPE, text=True)
    bash_output_libadw_ver = subprocess.run("C:/msys64/usr/bin/bash.exe -lc '"'pacman -Qi mingw-w64-x86_64-libadwaita | grep ''\\1.*\\'' -o'"'", stdout=subprocess.PIPE, text=True)

    # Splits the lines, appends them to a new list, then adds the main library to the end of the list
    if bash_output_libadw.returncode == 0:
        lib_paths = bash_output_libadw.stdout.splitlines()
        lib_paths.append("/mingw64/bin/libadwaita-1-0.dll")
        adw_version = bash_output_libadw_ver.stdout
    else:
        print("Error: MSYS2 or libadwaita cannot be found.\nPlease install MSYS2, then run 'pacman -S libadwaita' in MSYS2 to install libadwaita.\n")
        exit()

    # Ensure we have access to the VCPKG executable, should be first argument
    vcpkg_dir = os.path.abspath(os.environ.get('VCPKG_DIR'))
    if not vcpkg_dir:
        print('Error, need environment variable VCPKG_DIR to point to directory where `vcpkg` executable is')
        sys.exit(1)
    elif not os.path.exists(vcpkg_dir):
        print('Error, not able to find %s' % vcpkg_dir)
        sys.exit(1)

    # Make sure the executable is correctly defined, based on the OS
    vcpkg_exe = os.path.join(vcpkg_dir, 'vcpkg.exe')
    cmd_output = subprocess.run(vcpkg_exe + " " + "list").stdout.splitlines()
    for line in cmd_output:
        if line is line.startswith("portaudio"):
            split_line = line.split()
            for word in split_line:
                if word is word.startswith(19):
                    pa_version = word
    
    pa_libs = ["portaudio.dll"]

    # Creates the csprojs with the params
    create_adwaita_csproj(mingw_folder, dotnet_rid, lib_paths, adw_version)
    create_portaudio_csproj(dotnet_rid, pa_libs, pa_version)

# Linux-specific functions
def linux(dotnet_rid):
    pa_version = ""

    # Ensure we have access to the VCPKG executable, should be first argument
    vcpkg_dir = os.path.abspath(os.environ.get('VCPKG_DIR'))
    if not vcpkg_dir:
        print('Error, need environment variable VCPKG_DIR to point to directory where `vcpkg` executable is')
        sys.exit(1)
    elif not os.path.exists(vcpkg_dir):
        print('Error, not able to find %s' % vcpkg_dir)
        sys.exit(1)

    # Make sure the executable is correctly defined, based on the OS
    vcpkg_exe = os.path.join(vcpkg_dir, 'vcpkg')
    cmd_output = subprocess.run([vcpkg_exe, 'list'], stdout=subprocess.PIPE, text=True).stdout.splitlines()
    print(cmd_output)
    for line in cmd_output:
        print(line)
        if line.startswith("portaudio"):
            print("THIS IS THE LINE WE NEED: " + line)
            split_line = line.split()
            for word in split_line:
                if word.startswith("19"):
                    pa_version = word.replace("#", ".") + ".1"
                    print("PortAudio Version: " + pa_version)
    
    pa_libs = [
        "libportaudio.a",
        "libportaudio.so",
        "libjack.a",
        "libjack.so"
    ]
    
    # Creates the csprojs with the params
    create_portaudio_csproj(dotnet_rid, pa_libs, pa_version)


# The main function
def main():

    # Appends platform moniker to variable
    dotnet_rid = get_platform_and_architecture()

    if dotnet_rid.startswith("win"):
        windows(dotnet_rid)
    elif dotnet_rid.startswith("linux"):
        linux(dotnet_rid)


if __name__ == "__main__":
    main()
