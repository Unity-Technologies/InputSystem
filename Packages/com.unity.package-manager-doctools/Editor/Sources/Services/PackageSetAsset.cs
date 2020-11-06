using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PackageDocGenSet", menuName = "Doc Tools/Create Package Set Asset", order = 1)]
public class PackageSetAsset : ScriptableObject
{
    [TextArea(15,20)]
    public string SpaceDelimitedPackageNames;

    public string DestinationPath;
}
