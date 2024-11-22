#if UNITY_EDITOR
using System;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEditor.PackageManager;


namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Force restart if InputSystem package is removed to activate and initialize it on managed side.
    /// Set Project Settings input handling to InputManager once the package is removed.
    /// </summary>
    internal class InputSystemPackageControl
    {
        const string packageName = "com.unity.inputsystem";

        [InitializeOnLoadMethod]
        static void  SubscribePackageManagerEvent()
        {
            //there's a number of cases where it might not be called, for instance if the user changed the project manifest and deleted the Library folder before opening the project
            UnityEditor.PackageManager.Events.registeringPackages += CheckForInputSystemPackageRemoved;
        }

        private static void CheckForInputSystemPackageRemoved(PackageRegistrationEventArgs packageArgs)
        {
            if (IsInputSystemRemoved(packageArgs.removed))
                HandleInputSystemRemoved();
        }

        private static bool IsInputSystemRemoved(ReadOnlyCollection<UnityEditor.PackageManager.PackageInfo> packages)
        {
            foreach (var package in packages)
            {
                if (package.name == packageName)
                    return true;
            }
            return false;
        }

        private static void HandleInputSystemRemoved()
        {
            //set input handling to InputManager
            EditorPlayerSettingHelpers.newSystemBackendsEnabled = false;
            if (EditorUtility.DisplayDialog("Unity editor restart required", "You've removed the input system package. This requires a restart of the Editor.", "Restart Editor", "Ignore (Not recommended)"))
                EditorApplication.OpenProject(Environment.CurrentDirectory);
        }
    }
}
#endif
