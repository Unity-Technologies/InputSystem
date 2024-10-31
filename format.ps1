if ( Test-Path ${Home}/unity-meta )
{
    git -C ${Home}/unity-meta fetch
    git -C ${Home}/unity-meta pull
}
else
{
    git clone git@github.cds.internal.unity3d.com:unity/unity-meta.git ${Home}/unity-meta
}
perl "${Home}/unity-meta/Tools/Format/format.pl" Assets Packages

