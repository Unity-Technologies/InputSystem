#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.UI;

namespace UnityEngine.InputSystem
{
    [UnityEditor.CustomEditor(typeof(VirtualMouseInput))]
    class VirtualMouseInputEditor : UnityEditor.Editor
    {
        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.VirtualMouseInput).Send();
            new VirtualMouseInputEditorAnalytic(this).Send();
        }
    }
}
#endif
