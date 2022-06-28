using UnityEditor;

namespace UnityEngine.InputSystem
{
    [InitializeOnLoad]
    public class FeatureFlags
    {
        private static readonly string useWindowsGamingInputBackendName = "USE_WINDOWS_GAMING_INPUT_BACKEND";
        private static bool useWindowsGamingInputBackend = false;

        private static void SetInternalFeatureFlag(string setting, bool enabled)
        {
            Debug.Log($"Feature flag \"{setting}\" set to: {(enabled ? "On" : "Off")}");
            InputSystem.settings.SetInternalFeatureFlag(setting, enabled);
        }

        [MenuItem("QA Tools/Input Features/Toggle USE_WINDOWS_GAMING_INPUT_BACKEND", false, 0)]
        static bool SwitchGamepadBackend()
        {
            useWindowsGamingInputBackend = !useWindowsGamingInputBackend;
            SetInternalFeatureFlag(useWindowsGamingInputBackendName, useWindowsGamingInputBackend);
            return true;
        }
    }
}
