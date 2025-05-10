#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

using UnityEngine.InputSystem.Editor;

namespace UnityEditor.InputSystem.UI {
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
#endif // PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI