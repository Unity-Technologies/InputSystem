#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

// NOTE: This goes to the actual serialized data in PlayerSettings. As such, it circumvents (as it should) the trickery
//       that we perform in the editor where we pretend both the old and the new backends are always enabled (so as to
//       be able to switch between them without restarting).

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
                    if (tuple.newSystemEnabled == value)
                        return;
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
                    if (tuple.oldSystemEnabled == value)
                        return;
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
            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (playerSettings == null)
                return null;
            var playerSettingsObject = new SerializedObject(playerSettings);
            return playerSettingsObject.FindProperty(name);
        }
    }
}
#endif // UNITY_EDITOR
