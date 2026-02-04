using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.UI;

namespace UnityEngine.InputSystem
{
    [UnityEditor.CustomEditor(typeof(VirtualMouseInput))]
    public class VirtualMouseInputEditor : UnityEditor.Editor
    {
        public void OnDisable()
        {
            new InputComponentEditorAnalytic(InputSystemComponent.VirtualMouseInput).Send();
            new VirtualMouseInputEditorAnalytic(this).Send();
        }
    }
}
