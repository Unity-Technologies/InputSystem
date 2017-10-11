#if UNITY_EDITOR
using UnityEngine;

namespace ISX
{
    // A hidden object we put in the editor to bundle input system state
    // and help us survive domain relods.
    internal class InputSystemObject : ScriptableObject
    {
        public InputManager manager;

        public void Awake()
        {
            hideFlags = HideFlags.HideAndDontSave;
            manager = new InputManager();
            manager.Initialize();
        }

        public void OnDestroy()
        {
            InputActionSet.ResetGlobals();
            manager.Destroy();
        }
    }
}
#endif
