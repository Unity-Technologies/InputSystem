$Env:ANDROID_SDK_ROOT = ""

If (!$Env:ANDROID_SDK_ROOT)
{
	Write-Output "Please specify Android SDK Root path, for ex., C:/Program Files/Unity/Hub/Editor/2019.3.13f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK or modify this bat file"
	$Env:ANDROID_SDK_ROOT = Read-Host -Prompt 'Android SDK ROOT'
}

If (!$Env:ANDROID_SDK_ROOT)
{
	Write-Output "Invalid ANDROID_SDK_ROOT location, exiting"
	exit
}

Write-Output "ANDROID_SDK_ROOT is $Env:ANDROID_SDK_ROOT"

Invoke-WebRequest -Uri "https://artifactory.internal.unity3d.com/core-automation/tools/utr-standalone/utr.bat" -OutFile "utr.bat"
./utr.bat --suite=playmode --platform=android --player-load-path=build/players --artifacts_path=build/test-results
