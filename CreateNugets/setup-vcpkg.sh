#!/usr/bin/env bash
#
# This will set up the VCPKG environment into
# '/home/$USER/source/vcpkg' on Linux
#
# It's very useful if you want to save time setting up the dependencies
if [[ -z "$VCPKG_DIR" ]]; then
    echo "VCPKG directory not found. Creating folder 'source'..."
	mkdir ~/source
    echo "Done."
	pushd ~/source
	if [[ ! -d "./vcpkg" ]]; then
        echo "Attempting to clone VCPKG repository..."
		git clone https://github.com/microsoft/vcpkg.git
        pushd ./vcpkg
		source ./bootstrap-vcpkg.sh
        popd
        echo "Done."
	fi
	popd
	export VCPKG_DIR=~/source/vcpkg
    echo "Set VCPKG_DIR variable to $VCPKG_DIR"
fi
