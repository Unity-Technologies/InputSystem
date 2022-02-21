#if UNITY_EDITOR
using UnityEditor;

// NOTE: This goes to the actual serialized data in PlayerSettings. As such, it circumvents (as it should) the trickery
//       that we perform in the editor where we pretend both the old and the new backends are always enabled (so as to
//       be able to switch between them without restarting).

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorPlayerSettingHelpers
    {
        public static InputHandler activeInputHandler
        {
            get
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                if (property == null)
                    return default;
                return (InputHandler)property.intValue;
            }
            set
            {
                var property = GetPropertyOrNull(kActiveInputHandler);
                if (property != null)
                {
                    property.intValue = (int)value;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogError($"Cannot find '{kActiveInputHandler}' in player settings");
                }
            }
        }

        private const string kActiveInputHandler = "activeInputHandler";

        public enum InputHandler
        {
            OldInputManager = 0,
            NewInputSystem = 1,
            InputBoth = 2
        };

        private static SerializedProperty GetPropertyOrNull(string name)
        {
            var playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>("ProjectSettings/ProjectSettings.asset");
            if (playerSettings == null)
                return null;
            var playerSettingsObject = new SerializedObject(playerSettings);
            return playerSettingsObject.FindProperty(name);
        }
    }
}
#endif // UNITY_EDITOR
