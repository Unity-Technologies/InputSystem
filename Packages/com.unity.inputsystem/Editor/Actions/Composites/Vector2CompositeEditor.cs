using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Composites
{
    #if UNITY_EDITOR
    internal class Vector2CompositeEditor : InputParameterEditor<Vector2Composite>
    {
        private const string label = "Mode";
        private const string tooltipText = "How to synthesize a Vector2 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.";

        public override void OnGUI()
        {
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(label, target.mode)
            {
                tooltip = tooltipText
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.mode = (Vector2Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }
    }
    #endif
}
