using UnityEditor;
using System.IO;

namespace UnityEngine.InputSystem
{
    [InitializeOnLoad]
    public class FeatureFlags
    {
#if UNITY_EDITOR_WIN
        private static bool ChangePlayerSetting(WindowsGamepadBackendHint hint)
        {
            if (hint != PlayerSettings.windowsGamepadBackendHint)
            {
                if (EditorUtility.DisplayDialog(
                    "Change Windows Gamepad Backend Hint Setting in PlayerSettings?",
                    $"Are you sure you want to change PlayerSettings.windowsGamepadBackendHint to {hint}? This requires Unity to be restarted to have effect.",
                    "Yes (Restart Now)",
                    "No"))
                {
                    PlayerSettings.windowsGamepadBackendHint = hint;
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                }
            }

            return true;
        }

        [MenuItem("QA Tools/Input Features/Windows Gamepad Backend Hint/Set to \"Default\"", false, 0)]
        static bool UseDefault()
        {
            return ChangePlayerSetting(WindowsGamepadBackendHint.kWindowsGamepadBackendHintDefault);
        }

        [MenuItem("QA Tools/Input Features/Windows Gamepad Backend Hint/Set to \"XInput\"", false, 0)]
        static bool UseXInput()
        {
            return ChangePlayerSetting(WindowsGamepadBackendHint.kWindowsGamepadBackendHintXInput);
        }

        [MenuItem("QA Tools/Input Features/Windows Gamepad Backend Hint/Set to \"Windows.Gaming.Input\"", false, 0)]
        static bool UseWgi()
        {
            return ChangePlayerSetting(WindowsGamepadBackendHint.kWindowsGamepadBackendHintWindowsGamingInput);
        }

#endif // UNITY_EDITOR_WIN
    }
}
