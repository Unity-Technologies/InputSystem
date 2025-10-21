#!/bin/sh
find Assets -name "*.preformat*" -exec rm {} \;
find Packages -name "*.preformat*" -exec rm {} \;
find Assets -name "InitTestScene*" -exec rm {} \;
