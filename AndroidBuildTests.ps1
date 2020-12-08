$Env:EDITOR_LOCATION = ""

If (!$Env:EDITOR_LOCATION)
{
	Write-Output "Please specify Editor path, for ex., C:/Program Files/Unity/Hub/Editor/2019.3.13f1/Editor or modify this bat file"
	$Env:EDITOR_LOCATION = Read-Host -Prompt 'Editor Location'
}

If (!$Env:EDITOR_LOCATION)
{
	Write-Output "Invalid editor location, exiting"
	exit
}

Write-Output "Editor location is $Env:EDITOR_LOCATION"

Remove-Item 'build\players' -Recurse
Invoke-WebRequest -Uri "https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat" -OutFile "utr.bat"
./utr.bat --suite=playmode --platform=Android --editor-location="$Env:EDITOR_LOCATION" --testproject=. --player-save-path=build/players --artifacts_path=build/logs --scripting-backend=mono --build-only
