# Copy .api files from Packages/com.unity.inputsystem to Tools/API/{CurrentVersion}.

# Read current version from package.json.
$CurrentVersion = (Get-Content Packages/com.unity.inputsystem/package.json | ConvertFrom-Json).version

# Create Tools/API/{CurrentVersion} directory.
if ( -Not ( Test-Path Tools/API/$CurrentVersion ) )
{
    mkdir Tools/API/$CurrentVersion
}

# Grap all .api files from Packages/com.unity.inputsystem and copy them to Tools/API/{CurrentVersion}.
Get-ChildItem Packages/com.unity.inputsystem -Recurse -Filter *.api | foreach { cp $_.FullName Tools/API/$CurrentVersion }

# Add them to git.
git add Tools/API/$CurrentVersion/*.api
