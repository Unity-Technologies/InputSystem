#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.EnhancedTouch;

namespace UnityEngine.InputSystem.Editor
{
    [InitializeOnLoad]
    internal static class TouchSimulationEditorInitializer
    {
        static TouchSimulationEditorInitializer()
        {
            // We're initializing as part of [InitializeOnLoad]. We don't want to trigger InputSystem
            // initialization from the static cctor so delay-execute the registration.
            EditorApplication.delayCall += () =>
            {
                InputSystem.onSettingsChange += OnSettingsChanged;
                InputSystem.onBeforeUpdate += ReEnableAfterDomainReload;
            };
        }

        private static void ReEnableAfterDomainReload()
        {
            OnSettingsChanged();
            InputSystem.onBeforeUpdate -= ReEnableAfterDomainReload;
        }

        private static void OnSettingsChanged()
        {
            if (InputEditorUserSettings.simulateTouch)
                TouchSimulation.Enable();
            else
                TouchSimulation.Disable();
        }
    }

    [CustomEditor(typeof(TouchSimulation))]
    internal class TouchSimulationEditor : UnityEditor.Editor
    {
        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.TouchSimulation).Send();
        }
    }
}
#endif // UNITY_EDITOR
