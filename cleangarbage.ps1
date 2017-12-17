Get-ChildItem Assets -Recurse -Name "*.preformat*" | foreach { rm Assets/$_ }
Get-ChildItem Assets -Recurse -Name "InitTestScene*" | foreach { rm Assets/$_ }
