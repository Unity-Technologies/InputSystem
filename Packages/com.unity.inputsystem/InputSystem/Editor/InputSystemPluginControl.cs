#if ((UNITY_EDITOR && UNITY_2021_1_OR_NEWER) || PACKAGE_DOCS_GENERATION)
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
        // Input system platform specific classes register with the input system via a class using InitializeOnLoad on static constructors.
        // Static constructors in classes that are tagged with the InitializeOnLoad attribute are called before methods using the InitializeOnLoadMethod attribute.
        // So the extra input system packages will be registered before this check which is done in InitializeOnLoadMethod.
        [InitializeOnLoadMethod]
        private static void CheckForExtension()
        {
            ThrowWarningOnMissingPlugin();
        }

        // This static HashSet will be reset OnDomainReload and so it will be reset every Domain Reload (at the time of InitializeOnLoad).
        // This is pre-populated with the list of platforms that don't need a extra platform specific input system package to add platform specific functionality.
        private static HashSet<BuildTarget> s_supportedBuildTargets = new HashSet<BuildTarget>()
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
            #if UNITY_2022_3_OR_NEWER
            BuildTarget.VisionOS,
            #endif
            (BuildTarget)49,
            BuildTarget.NoTarget
        };

        static bool BuildTargetNeedsPlugin()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            foreach (var platform in s_supportedBuildTargets)
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
            s_supportedBuildTargets.Add(target);
        }

        private static bool IsPluginInstalled()
        {
            var registeredPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            var plugInName = PlugInName + EditorUserBuildSettings.activeBuildTarget.ToString();
            foreach (var package in registeredPackages)
            {
                if (package.name.Equals(plugInName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void ThrowWarningOnMissingPlugin()
        {
            if (!BuildTargetNeedsPlugin())
                return;
            if (!IsPluginInstalled())
                Debug.LogError("Active Input Handling is set to InputSystem, but no Plugin for " + EditorUserBuildSettings.activeBuildTarget + " was found. Please install the missing InputSystem package extensions.");
        }
    }
}
#endif
