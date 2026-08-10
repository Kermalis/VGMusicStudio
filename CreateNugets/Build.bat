::
:: Fetches the paths to the libraries and generates the csproj
::
:: C:/msys64/usr/bin/bash.exe -lc "ldd /mingw64/bin/libadwaita-1-0.dll | grep '\/mingw.*\.dll' -o" > output.txt
::
call setup-vcpkg.bat
if exist ./Adwaita/org.adwaita.native.win-x64.csproj (
    del ./Adwaita/org.adwaita.native.win-x64.csproj
)
if exist ./PortAudio/org.portaudio.runtime.win-x64.csproj (
    del ./PortAudio/org.portaudio.runtime.win-x64.csproj
)
python update-libs.py
python generate-csproj.py
pushd Adwaita
dotnet pack -c Release
::del org.adwaita.native.win-x64.csproj
popd
pushd PortAudio
dotnet pack -c Release
::del org.portaudio.runtime.win-x64.csproj
popd
