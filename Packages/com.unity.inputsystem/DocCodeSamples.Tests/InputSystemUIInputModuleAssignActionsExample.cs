#if UNITY_INPUT_SYSTEM_ENABLE_UI

using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace DocCodeSamples.Tests
{
    internal class InputSystemUIInputModuleAssignActionsExample : MonoBehaviour
    {
        // Reference to the InputSystemUIInputModule component, needs to be provided in the Inspector
        public InputSystemUIInputModule uiModule;

        void Start()
        {
            // Assign default actions
            AssignActions();
        }

        void AssignActions()
        {
            if (uiModule != null)
                uiModule.AssignDefaultActions();
            else
                Debug.LogError("InputSystemUIInputModule not found.");
        }

        void UnassignActions()
        {
            if (uiModule != null)
                uiModule.UnassignActions();
            else
                Debug.LogError("InputSystemUIInputModule not found.");
        }

        void OnDestroy()
        {
            // Unassign actions when the object is destroyed
            UnassignActions();
        }
    }
}
#endif
