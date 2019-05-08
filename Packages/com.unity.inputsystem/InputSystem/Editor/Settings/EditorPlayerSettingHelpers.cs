#if UNITY_EDITOR
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
                var property = GetPropertyOrNull(kEnableNewSystemProperty);
                if (property != null)
                    return property.boolValue;
                return true;
            }
            set
            {
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
                var property = GetPropertyOrNull(kDisableOldSystemProperty);
                if (property != null)
                    return !property.boolValue;
                return true;
            }
            set
            {
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
            }
        }

        private const string kEnableNewSystemProperty = "enableNativePlatformBackendsForNewInputSystem";
        private const string kDisableOldSystemProperty = "disableOldInputManagerSupport";

        private static SerializedProperty GetPropertyOrNull(string name)
        {
            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (playerSettings != null)
            {
                var playerSettingsObject = new SerializedObject(playerSettings);
                return playerSettingsObject.FindProperty(name);
            }
            return null;
        }
    }
}
#endif // UNITY_EDITOR
