#if UNITY_2021_1_OR_NEWER
using System;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    public class InputSystemPluginControl
    {
        [InitializeOnLoadMethod]
        private static void CheckForExtension()
        {
            ThrowWarningOnMissingPlugin();
        }

        private static readonly BuildTarget[] TargetNoPluginNeeded =
        {
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneWindows,
            BuildTarget.iOS,
            BuildTarget.Android,
            BuildTarget.StandaloneWindows64,
            BuildTarget.WebGL,
            BuildTarget.WSAPlayer,
            BuildTarget.StandaloneLinux64,
            BuildTarget.tvOS,
            BuildTarget.LinuxHeadlessSimulation,
            BuildTarget.PS5,
            BuildTarget.EmbeddedLinux,
            #if UNITY_2022_1_OR_NEWER
            BuildTarget.QNX,
            #endif
            #if UNITY_2023_3_OR_NEWER
            BuildTarget.VisionOS,
            #endif
            #if UNITY_6000_0_OR_NEWER
            BuildTarget.ReservedCFE,
            #endif
            #if UNITY_6000_0_7_OR_NEWER
            BuildTarget.Kepler
            #endif
            BuildTarget.NoTarget
        };

        static bool BuildTargetNeedsPlugin()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            foreach (var platform in TargetNoPluginNeeded)
            {
                if (platform == target) return true;
            }
            return false;
        }

        private const string PlugInName = "com.unity.inputsystem.";

        private static bool IsPluginInstalled()
        {
            var registeredPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in registeredPackages)
            {
                if (package.name.StartsWith(PlugInName))
                    return true;
            }
            return false;
        }

        private static void ThrowWarningOnMissingPlugin()
        {
            if (!BuildTargetNeedsPlugin())
                return;
            Debug.Assert(IsPluginInstalled(), "Active Input Handling is set to InputSystem, but no Plugin for " + EditorUserBuildSettings.activeBuildTarget + " was found. Please install the missing InputSystem package extensions.");
        }
    }
}
#endif
