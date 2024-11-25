#if UNITY_EDITOR
using System;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEditor.PackageManager;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Forces the Editor to restart if the InputSystem package is removed to activate and initialize it on the managed side.
    /// Automatically sets "Project Settings > Player > Active Input Handling" to "Input Manager" once the package is removed.
    /// </summary>
    internal class InputSystemPackageControl
    {
        const string packageName = "com.unity.inputsystem";

        [InitializeOnLoadMethod]
        static void  SubscribePackageManagerEvent()
        {
            //There's a number of cases where it might not be called, for instance if the user changed the project manifest and deleted the Library folder before opening the project
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
            //Set input handling to InputManager
            EditorPlayerSettingHelpers.newSystemBackendsEnabled = false;
            if (EditorUtility.DisplayDialog("The Unity Editor needs to be restarted", "You've removed the Input System package. This requires a restart of the Unity Editor.", "Restart the Editor", "Ignore (Not recommended)"))
                EditorApplication.OpenProject(Environment.CurrentDirectory);
        }
    }
}
#endif
