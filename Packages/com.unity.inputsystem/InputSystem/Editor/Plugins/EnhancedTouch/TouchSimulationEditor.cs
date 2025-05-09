#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;
#endif

namespace UnityEditor.InputSystem.EnhancedTouch {

    [InitializeOnLoad]
    class TouchSimulationEdtiorInitialization 
    {
        static TouchSimulationEdtiorInitialization()
        {
            // We're a MonoBehaviour so our cctor may get called as part of the MonoBehaviour being
            // created. We don't want to trigger InputSystem initialization from there so delay-execute
            // the code here.
            EditorApplication.delayCall +=
                () =>
            {
                UnityEngine.InputSystem.InputSystem.onSettingsChange += OnSettingsChanged;
                UnityEngine.InputSystem.InputSystem.onBeforeUpdate += ReEnableAfterDomainReload;
            };
        }

        private static void ReEnableAfterDomainReload()
        {
            OnSettingsChanged();
            UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= ReEnableAfterDomainReload;
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
    class TouchSimulationEditor : UnityEditor.Editor
    {
        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.TouchSimulation).Send();
        }
    }
}