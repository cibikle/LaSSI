#!/bin/bash

target="$1"
echo "$target"
chmod -R +x "$target"
#ls -al "$target/Contents/MacOS/LaSSI"
xattr -c "$target"
