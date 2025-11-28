using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Composites
{
    #if UNITY_EDITOR
    internal class Vector3CompositeEditor : InputParameterEditor<Vector3Composite>
    {
        private const string label = "Mode";
        private const string tooltip = "How to synthesize a Vector3 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.";

        public override void OnGUI()
        {
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(label, target.mode)
            {
                tooltip = tooltip
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.mode = (Vector3Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }
    }
    #endif
}
