#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

#if UNITY_6000_0_OR_NEWER
using System.Reflection;
using UnityEditor.Build.Profile;
#endif

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorPlayerSettingHelpers
    {
        /// <summary>
        /// Whether the backends for the new input system are enabled in the
        /// player settings for the Unity runtime.
        /// </summary>
        public static bool newSystemBackendsEnabled
        {
            get
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                return property == null || ActiveInputHandlerToTuple(property.intValue).newSystemEnabled;
            }
            set
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                if (property != null)
                {
                    var tuple = ActiveInputHandlerToTuple(property.intValue);
                    tuple.newSystemEnabled = value;
                    property.intValue = TupleToActiveInputHandler(tuple);
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError($"Cannot find '{kActiveInputHandler}' in player settings");
                }
            }
        }

        /// <summary>
        /// Whether the backends for the old input system are enabled in the
        /// player settings for the Unity runtime.
        /// </summary>
        public static bool oldSystemBackendsEnabled
        {
            get
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                return property == null || ActiveInputHandlerToTuple(property.intValue).oldSystemEnabled;
            }
            set
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                if (property != null)
                {
                    var tuple = ActiveInputHandlerToTuple(property.intValue);
                    tuple.oldSystemEnabled = value;
                    property.intValue = TupleToActiveInputHandler(tuple);
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError($"Cannot find '{kActiveInputHandler}' in player settings");
                }
            }
        }

        private const string kActiveInputHandler = "activeInputHandler";

        private enum InputHandler
        {
            OldInputManager = 0,
            NewInputSystem = 1,
            InputBoth = 2
        };

        private static (bool newSystemEnabled, bool oldSystemEnabled) ActiveInputHandlerToTuple(int value)
        {
            switch ((InputHandler)value)
            {
                case InputHandler.OldInputManager:
                    return (false, true);
                case InputHandler.NewInputSystem:
                    return (true, false);
                case InputHandler.InputBoth:
                    return (true, true);
                default:
                    throw new ArgumentException($"Invalid value of 'activeInputHandler' setting: {value}");
            }
        }

        private static int TupleToActiveInputHandler((bool newSystemEnabled, bool oldSystemEnabled) tuple)
        {
            switch (tuple)
            {
                case (false, true):
                    return (int)InputHandler.OldInputManager;
                case (true, false):
                    return (int)InputHandler.NewInputSystem;
                case (true, true):
                    return (int)InputHandler.InputBoth;
                // Special case, when using two separate bool's of the public API here,
                // it's possible to end up with both settings in false, for example:
                // - EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
                // - EditorPlayerSettingHelpers.oldSystemBackendsEnabled = false;
                // - EditorPlayerSettingHelpers.newSystemBackendsEnabled = false;
                // - EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
                // On line 3 both settings will be false, even if we set old system to true on line 4.
                case (false, false):
                    return (int)InputHandler.OldInputManager;
            }
        }

        private static SerializedProperty GetPropertyOrNull(string name)
        {
#if UNITY_6000_0_OR_NEWER
            // HOTFIX: the code below works around an issue causing an infinite reimport loop
            // this will be replaced by a call to an API in the editor instead of using reflection once it is available
            var buildProfileType = typeof(BuildProfile);
            var globalPlayerSettingsField = buildProfileType.GetField("s_GlobalPlayerSettings", BindingFlags.Static | BindingFlags.NonPublic);
            if (globalPlayerSettingsField == null)
            {
                Debug.LogError($"Could not find global player settings field in build profile when trying to get property {name}. Please try to update the Input System package.");
                return null;
            }
            var playerSettings = (PlayerSettings)globalPlayerSettingsField.GetValue(null);
            var activeBuildProfile = BuildProfile.GetActiveBuildProfile();
            if (activeBuildProfile != null)
            {
                var playerSettingsOverrideField = buildProfileType.GetField("m_PlayerSettings", BindingFlags.Instance | BindingFlags.NonPublic);
                if (playerSettingsOverrideField == null)
                {
                    Debug.LogError($"Could not find player settings override field in build profile when trying to get property {name}. Please try to update the Input System package.");
                    return null;
                }
                var playerSettingsOverride = (PlayerSettings)playerSettingsOverrideField.GetValue(activeBuildProfile);
                if (playerSettingsOverride != null)
                    playerSettings = playerSettingsOverride;
            }
#else
            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
#endif
            if (playerSettings == null)
                return null;
            var playerSettingsObject = new SerializedObject(playerSettings);
            return playerSettingsObject.FindProperty(name);
        }
    }
}
#endif // UNITY_EDITOR
