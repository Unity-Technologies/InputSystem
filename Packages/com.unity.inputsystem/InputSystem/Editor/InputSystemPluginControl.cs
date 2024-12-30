using System;
using System.Linq;
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
            BuildTarget.QNX,
            BuildTarget.VisionOS,
            BuildTarget.ReservedCFE,
            BuildTarget.NoTarget
        };

        static bool BuildTargetNeedsPlugin()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            return !TargetNoPluginNeeded.Contains(target);
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
