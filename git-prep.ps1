# Preps a working copy.

# Put doctools package in place.
if ( Test-Path Packages/com.unity.package-manager-doctools )
{
    rm -Recurse -Force Packages/com.unity.package-manager-doctools
}
git clone git@github.cds.internal.unity3d.com:unity/com.unity.package-manager-doctools.git Packages/com.unity.package-manager-doctools
git apply --directory=Packages/com.unity.package-manager-doctools Tools/doctools.patch
