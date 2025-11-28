using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Composites
{
    #if UNITY_EDITOR
    internal class AxisCompositeEditor : InputParameterEditor<AxisComposite>
    {
        private const string label = "Which Side Wins";
        private const string tooltipText = "Determine which axis 'wins' if both are actuated at the same time. "
            + "If 'Neither' is selected, the result is 0 (or, more precisely, "
            + "the midpoint between minValue and maxValue).";

        public override void OnGUI()
        {
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(label, target.whichSideWins)
            {
                tooltip = tooltipText
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.whichSideWins = (AxisComposite.WhichSideWins)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }
    }
    #endif
}
