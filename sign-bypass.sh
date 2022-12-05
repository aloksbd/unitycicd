#! /bin/bash

pwd=$(dirname -- $(readlink -fn -- "$0"))

chmod +x "$pwd/mac/Earth9-Creator/Earth9-Creator.app/Contents/MacOS/Earth9"
xattr -cr "$pwd/mac/Earth9-Creator/Earth9-Creator.app"
