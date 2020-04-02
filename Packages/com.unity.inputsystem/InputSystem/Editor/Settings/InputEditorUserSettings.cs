#if UNITY_EDITOR
using System;
using System.IO;

////TODO: event diagnostics should have a bool here

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Settings that are local to the current user and specific to the input system's use in the editor.
    /// </summary>
    /// <remarks>
    /// These settings are not stored in assets in the project along with the other input settings.
    /// Instead, they are serialized to JSON and stored in the project's Library/ folder.
    /// </remarks>
    internal static class InputEditorUserSettings
    {
        /// <summary>
        /// Where the settings are stored in the user's project.
        /// </summary>
        private const string kSavePath = "Library/InputUserSettings.json";

        public static bool simulateTouch
        {
            get => s_Settings.simulateTouch;
            set
            {
                if (s_Settings.simulateTouch == value)
                    return;
                s_Settings.simulateTouch = value;
                OnChange();
            }
        }

        public static bool lockInputToGameView
        {
            get => s_Settings.lockInputToGameView;
            set
            {
                if (s_Settings.lockInputToGameView == value)
                    return;
                s_Settings.lockInputToGameView = value;
                OnChange();
            }
        }

        /// <summary>
        /// If this is true, then if <see cref="InputSettings.supportedDevices"/> is not empty, do
        /// not use it to prevent native devices
        /// </summary>
        /// <remarks>
        /// This switch is useful to preserve use of devices in the editor regardless of whether they the
        /// kind of devices used by the game at runtime. For example, a game may support only gamepads but
        /// in the editor, the keyboard, mouse, and tablet should still be usable.
        ///
        /// This switch is enabled by default.
        /// </remarks>
        public static bool addDevicesNotSupportedByProject
        {
            get => s_Settings.addDevicesNotSupportedByProject;
            set
            {
                if (s_Settings.addDevicesNotSupportedByProject == value)
                    return;
                s_Settings.addDevicesNotSupportedByProject = value;
                OnChange();
            }
        }

        /// <summary>
        /// If this is true, then instead of having an explicit save button in the .inputactions asset editor,
        /// any changes will be saved automatically and immediately.
        /// </summary>
        /// <remarks>
        /// Disabled by default.
        /// </remarks>
        public static bool autoSaveInputActionAssets
        {
            get => s_Settings.autoSaveInputActionAssets;
            set
            {
                if (s_Settings.autoSaveInputActionAssets == value)
                    return;
                s_Settings.autoSaveInputActionAssets = value;
                OnChange();
            }
        }

        [Serializable]
        internal struct SerializedState
        {
            public bool lockInputToGameView;
            public bool addDevicesNotSupportedByProject;
            public bool autoSaveInputActionAssets;
            public bool simulateTouch;
        }

        internal static SerializedState s_Settings;

        private static void OnChange()
        {
            Save();
            InputSystem.s_Manager.ApplySettings();
        }

        internal static void Load()
        {
            try
            {
                var json = File.ReadAllText(kSavePath);
                s_Settings = JsonUtility.FromJson<SerializedState>(json);
            }
            catch (Exception)
            {
                s_Settings = new SerializedState();
            }
        }

        internal static void Save()
        {
            var json = JsonUtility.ToJson(s_Settings, prettyPrint: true);
            File.WriteAllText(kSavePath, json);
        }
    }
}
#endif
