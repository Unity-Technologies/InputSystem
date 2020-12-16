#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

public class iOSPostProcessBuild
{
    [PostProcessBuild]
    public static void UpdateInfoPList(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS)
            return;
        string plistPath = pathToBuiltProject + "/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        // Get root
        PlistElementDict rootDict = plist.root;

        // Change value of CFBundleVersion in Xcode plist
        var buildKey = "CFBundleVersion";
        rootDict.SetString(buildKey, "2.3.4");

        // Write to file
        File.WriteAllText(plistPath, plist.WriteToString());
    }
}

#endif
