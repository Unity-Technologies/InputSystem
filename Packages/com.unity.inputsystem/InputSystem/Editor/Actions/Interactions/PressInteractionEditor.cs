#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// UI that is displayed when editing <see cref="PressInteraction"/> in the editor.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal class PressInteractionEditor : InputParameterEditor<PressInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            if (!InputSystem.settings.useIMGUIEditorForAssets)
                return;

            EditorGUILayout.HelpBox(s_HelpBoxText);
            target.behavior = (PressBehavior)EditorGUILayout.EnumPopup(s_PressBehaviorLabel, target.behavior);
            m_PressPointSetting.OnGUI();
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            root.Add(new HelpBox(s_HelpBoxText.text, HelpBoxMessageType.None));

            var behaviourDropdown = new EnumField(s_PressBehaviorLabel.text, target.behavior)
            {
                tooltip = s_PressBehaviorLabel.tooltip
            };
            behaviourDropdown.RegisterValueChangedCallback(evt =>
            {
                target.behavior = (PressBehavior)evt.newValue;
                onChangedCallback?.Invoke();
            });
            root.Add(behaviourDropdown);

            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
            CustomOrDefaultSetting.AddSharedDefaultSettingsFooter(root,
                new[] { m_PressPointSetting });
        }

        private CustomOrDefaultSetting m_PressPointSetting;

        private static readonly GUIContent s_HelpBoxText = EditorGUIUtility.TrTextContent("Note that the 'Press' interaction is only "
            + "necessary when wanting to customize button press behavior. For default press behavior, simply set the action type to 'Button' "
            + "and use the action without interactions added to it.");

        private static readonly GUIContent s_PressBehaviorLabel = EditorGUIUtility.TrTextContent("Trigger Behavior",
            "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.");
    }
}
#endif
