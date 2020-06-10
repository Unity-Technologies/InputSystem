echo off
set EDITOR_LOCATION=Empty
if "%EDITOR_LOCATION%" == "Empty" (
 echo Please specify Editor path, for ex., C:/Program Files/Unity/Hub/Editor/2019.3.13f1/Editor or modify this bat file
 set /p EDITOR_LOCATION=Editor Location:
)

if "%EDITOR_LOCATION%" == "Empty" (
 echo Invalid editor location, exiting
 pause
 GOTO :eof
)

echo Editor location is %EDITOR_LOCATION%
curl -s https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat --output utr.bat
call ./utr.bat --suite=playmode --platform=Android --editor-location="%EDITOR_LOCATION%" --testproject=F:/Projects/InputSystem --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=il2cpp --build-only
pause