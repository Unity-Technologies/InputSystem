#!/bin/bash

curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr --output utr
chmod +x utr
# Keep this for informational purposes
#EDITOR_LOCATION=/Applications/Unity/Hub/Editor/2020.1.2f1

if [ ! -d "$EDITOR_LOCATION" ]; then
    echo Specify Editor location, for ex., /Applications/Unity/Hub/Editor/2020.1.2f1
    read -p 'Editor Location : ' EDITOR_LOCATION
fi

if [ ! -d "$EDITOR_LOCATION" ]; then
  echo "Editor location '$EDITOR_LOCATION' doesnt exit"
  exit -1
fi

echo Editor location is $EDITOR_LOCATION
# After Unity creates xCode project
# UTR compiles generated xCode project to produce iOS binary and deletes xCode project afterwards
# For debugging purposes we want xCode project to stay, but there's no option which would allow this
# Instead specify UNITY_XCODEFORIOSTESTS, this will tell UTR to try to use xCode from this folder to compile the project
# Since xCode doesn't exist in the folder, this process will fail, but at the same time, the generated xCode project will remain
export UNITY_XCODEFORIOSTESTS=.
./utr --suite=playmode --platform=iOS --editor-location="$EDITOR_LOCATION" --testproject=. --testfilter=ScreenKeyboardTests --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=il2cpp --build-only

read -p 'Press Any Key to Exit'