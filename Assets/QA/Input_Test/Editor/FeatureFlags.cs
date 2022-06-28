using UnityEditor;

namespace UnityEngine.InputSystem
{
    [InitializeOnLoad]
    public class FeatureFlags
    {
        private static readonly string useWindowsGamingInputBackendName = "USE_WINDOWS_GAMING_INPUT_BACKEND";
        private static bool useWindowsGamingInputBackend = false;

        private static void LogFeatureRequest(string setting, bool enabled)
        {
            Debug.Log($"Feature flag \"{setting}\" set to: {(enabled ? "On" : "Off")}");
        }

        [MenuItem("QA Tools/Input Features/Toggle USE_WINDOWS_GAMING_INPUT_BACKEND", false, 0)]
        static bool SwitchGamepadBackend()
        {
            useWindowsGamingInputBackend = !useWindowsGamingInputBackend;
            InputSystem.settings.SetInternalFeatureFlag(useWindowsGamingInputBackendName, useWindowsGamingInputBackend);
            return true;
        }
    }
}
