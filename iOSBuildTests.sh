#!/bin/bash

curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr --output utr
chmod +x utr
EDITOR_LOCATION=
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

./utr --suite=playmode --platform=iOS --editor-location="$EDITOR_LOCATION" --testproject=. --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=il2cpp --build-only

read -p 'Press Any Key to Exit'