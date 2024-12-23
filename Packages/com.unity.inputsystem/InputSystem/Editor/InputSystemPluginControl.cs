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

        private static readonly BuildTarget[] targetNoPluginNeeded = new BuildTarget[]
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
            return !targetNoPluginNeeded.Contains(target);
        }

        private static string plugInName = "com.unity.inputsystem.";
        private static bool IsPluginInstalled()
        {
            var registeredPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in registeredPackages)
            {
                if (package.name.StartsWith(plugInName)) //TODO better ??
                    return true;
            }
            return false;
        }

        private static void ThrowWarningOnMissingPlugin()
        {
            if (BuildTargetNeedsPlugin() && !IsPluginInstalled())
                Debug.Log("No plugin installed!");
            // TODO Assert.
        }
    }
}
