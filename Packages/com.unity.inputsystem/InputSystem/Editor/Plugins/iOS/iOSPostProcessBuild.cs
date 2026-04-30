#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem
{
    internal class iOSPostProcessBuild
    {
        [PostProcessBuild]
        public static void UpdateInfoPList(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            var settings = InputSystem.settings.iOS;
            if (!settings.motionUsage.enabled)
                return;
            var plistPath = pathToBuiltProject + "/Info.plist";
            var contents = File.ReadAllText(plistPath);
            var description = InputSystem.settings.iOS.motionUsage.usageDescription;
#if UNITY_IOS || UNITY_TVOS
            var plist = new UnityEditor.iOS.Xcode.PlistDocument();
            plist.ReadFromString(contents);
            var root = plist.root;
            var buildKey = "NSMotionUsageDescription";
            if (root[buildKey] != null)
                Debug.LogWarning($"{buildKey} is already present in Info.plist, the value will be overwritten.");

            root.SetString(buildKey, description);
            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }
    }
}

#endif
