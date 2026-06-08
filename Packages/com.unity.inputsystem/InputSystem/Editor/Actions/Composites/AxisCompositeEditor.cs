#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Composites
{
    internal class AxisCompositeEditor : InputParameterEditor<AxisComposite>
    {
        private GUIContent m_WhichAxisWinsLabel = new GUIContent("Which Side Wins",
            "Determine which axis 'wins' if both are actuated at the same time. "
            + "If 'Neither' is selected, the result is 0 (or, more precisely, "
            + "the midpoint between minValue and maxValue).");

        public override void OnGUI()
        {
            if (!InputSystem.settings.useIMGUIEditorForAssets)
                return;

            target.whichSideWins = (AxisComposite.WhichSideWins)EditorGUILayout.EnumPopup(m_WhichAxisWinsLabel, target.whichSideWins);
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(m_WhichAxisWinsLabel.text, target.whichSideWins)
            {
                tooltip = m_WhichAxisWinsLabel.tooltip
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.whichSideWins = (AxisComposite.WhichSideWins)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }
    }
}
#endif
