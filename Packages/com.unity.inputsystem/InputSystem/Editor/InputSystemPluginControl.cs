#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// This class controls all required plugins and extension packages are installed for the InputSystem.
    /// </summary>
    /// <remarks>
    /// For some platforms, the InputSystem requires additional plugins to be installed. This class checks if the required plugins are installed and throws a warning if they are not.
    /// </remarks>
    public class InputSystemPluginControl
    {
        //At the time of InitializeOnLoad the packages are compiled and registered
        [InitializeOnLoadMethod]
        private static void CheckForExtension()
        {
            ThrowWarningOnMissingPlugin();
            m_pluginPackageRegistered = false;
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

        private static bool m_pluginPackageRegistered = false;

        static bool BuildTargetNeedsPlugin()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            foreach (var platform in TargetNoPluginNeeded)
            {
                if (platform == target) return false;
            }
            return true;
        }

        private const string PlugInName = "com.unity.inputsystem.";

        /// <summary>
        /// Used to register extensions externally to the InputSystem, this is needed for all Platforms that require a plugin to be installed.
        /// </summary>
        /// <remarks>
        /// This method is internally called by the InputSystem package extensions to register the PlugIn. This can be called for custom extensions on custom platforms.
        /// </remarks>
        public static void RegisterPluginPackage()
        {
            m_pluginPackageRegistered = true;
        }

        private static bool IsPluginInstalled()
        {
            if (m_pluginPackageRegistered)
                return true;
            var registeredPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            var plugInName = PlugInName + EditorUserBuildSettings.activeBuildTarget.ToString().ToLower();
            foreach (var package in registeredPackages)
            {
                if (package.name.Equals(plugInName))
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
#endif
