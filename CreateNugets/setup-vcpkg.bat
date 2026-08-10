:: This will set up the VCPKG environment into
:: C:\src\vcpkg on Windows
::
:: It's very useful if you want to save time setting up the dependencies
if not exist %VCPKG% (
	if not exist C:\src\ (
		mkdir C:\src
		pushd C:\src
		if not exist .\vcpkg\ (
			git clone https://github.com/microsoft/vcpkg.git
			call .\vcpkg\bootstrap-vcpkg.bat
		)
		popd
	)
	set VCPKG_DIR=C:\src\vcpkg
)
