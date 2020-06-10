REM echo off
set ANDROID_SDK_ROOT=Empty
if "%ANDROID_SDK_ROOT%" == "Empty" (
 echo 'Please specify Android SDK Root path, for ex., C:/Program Files/Unity/Hub/Editor/2019.3.13f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK or modify this bat file'
 
 set /p ANDROID_SDK_ROOT=ANDROID_SDK_ROOT:
)

if "%ANDROID_SDK_ROOT%" == Empty (
 echo Invalid ANDROID_SDK_ROOT location, exiting
 pause
 GOTO :eof
)

echo ANDROID_SDK_ROOT is %ANDROID_SDK_ROOT%

curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output utr.bat
call ./utr --suite=playmode --platform=android --player-load-path=build/players --artifacts_path=build/test-results
pause