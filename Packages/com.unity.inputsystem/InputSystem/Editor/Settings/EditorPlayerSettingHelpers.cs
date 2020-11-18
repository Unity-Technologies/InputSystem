#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;

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
#if UNITY_2020_2_OR_NEWER
                var property = GetPropertyOrNull(kActiveInputHandler);
                return property == null || ActiveInputHandlerToTuple(property.intValue).newSystemEnabled;
#else
                var property = GetPropertyOrNull(kEnableNewSystemProperty);
                return property == null || property.boolValue;
#endif
            }
            set
            {
#if UNITY_2020_2_OR_NEWER
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
#else
                var property = GetPropertyOrNull(kEnableNewSystemProperty);
                if (property != null)
                {
                    property.boolValue = value;
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError($"Cannot find '{kEnableNewSystemProperty}' in player settings");
                }
#endif
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
#if UNITY_2020_2_OR_NEWER
                var property = GetPropertyOrNull(kActiveInputHandler);
                return property == null || ActiveInputHandlerToTuple(property.intValue).oldSystemEnabled;
#else
                var property = GetPropertyOrNull(kDisableOldSystemProperty);
                return property == null || !property.boolValue;
#endif
            }
            set
            {
#if UNITY_2020_2_OR_NEWER
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
#else
                var property = GetPropertyOrNull(kDisableOldSystemProperty);
                if (property != null)
                {
                    property.boolValue = !value;
                    property.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogError($"Cannot find '{kDisableOldSystemProperty}' in player settings");
                }
#endif
            }
        }


#if UNITY_2020_2_OR_NEWER
        private const string kActiveInputHandler = "activeInputHandler";

        private enum InputHandler
        {
            OldInputManager = 0,
            NewInputSystem = 1,
            InputBoth = 2
        };

        private static (bool newSystemEnabled, bool oldSystemEnabled) ActiveInputHandlerToTuple(int value)
        {
            switch ((InputHandler) value)
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
                    return (int) InputHandler.OldInputManager;
                case (true, false):
                    return (int) InputHandler.NewInputSystem;
                case (true, true):
                    return (int) InputHandler.InputBoth;
                // Special case, when using two separate bool's of the public API here,
                // it's possible to end up with both settings in false, for example:
                // - EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
                // - EditorPlayerSettingHelpers.oldSystemBackendsEnabled = false;
                // - EditorPlayerSettingHelpers.newSystemBackendsEnabled = false;
                // - EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
                // On line 3 both settings will be false, even if we set old system to true on line 4.
                case (false, false):
                    return (int) InputHandler.OldInputManager;
            }
        }
#else
        private const string kEnableNewSystemProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const string kDisableOldSystemProperty = "disableOldInputManagerSupport";
#endif

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