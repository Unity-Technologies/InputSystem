#if UNITY_EDITOR
#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
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
        //InitializeOnLoad on classes (see GXDKSupport) and their static constructors are compiled before methods with InitializeOnLoadMethod attribute (like this one)
        //Therefore the order of registering platforms from plugins is guaranteed
        [InitializeOnLoadMethod]
        private static void CheckForExtension()
        {
            ThrowWarningOnMissingPlugin();
        }

        //This static HashSet will be reset OnDomainReload and so it will be emptied and refilled every [InitializeOnLoad]]
        private static HashSet<BuildTarget> s_targetNoPluginNeeded = new HashSet<BuildTarget>()
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

        static bool BuildTargetNeedsPlugin()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            foreach (var platform in s_targetNoPluginNeeded)
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
        public static void RegisterPlatform(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                s_targetNoPluginNeeded.Add(target);
        }

        private static bool IsPluginInstalled()
        {
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
