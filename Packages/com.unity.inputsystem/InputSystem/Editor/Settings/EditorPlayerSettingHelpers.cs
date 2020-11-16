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
            return (InputHandler) value switch
            {
                InputHandler.OldInputManager => (false, true),
                InputHandler.NewInputSystem => (true, false),
                InputHandler.InputBoth => (true, true),
                _ => throw new ArgumentException($"Invalid value of 'activeInputHandler' setting: {value}")
            };
        }

        private static int TupleToActiveInputHandler((bool newSystemEnabled, bool oldSystemEnabled) tuple)
        {
            return tuple switch
            {
                (false, true) => (int)InputHandler.OldInputManager,
                (true, false) => (int)InputHandler.NewInputSystem,
                (true, true) => (int)InputHandler.InputBoth,
                _ => throw new ArgumentException($"Invalid value of player settings: {tuple}")
            };
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