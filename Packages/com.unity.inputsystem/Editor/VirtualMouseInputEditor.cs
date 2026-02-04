using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace UnityEngine.InputSystem.Editor
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
