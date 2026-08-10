#!/usr/bin/env bash
#
# Fetches the paths to the libraries and generates the csproj
#
source ./setup-vcpkg.sh
if [[ -z "./Adwaita/org.adwaita.native.linux-x64.csproj" ]]; then
    rm ./Adwaita/org.adwaita.native.linux-x64.csproj
fi
python3 update-libs.py
python3 generate-csproj.py
pushd PortAudio
dotnet pack -c Release
#rm org.portaudio.runtime.linux-x64.csproj
popd
