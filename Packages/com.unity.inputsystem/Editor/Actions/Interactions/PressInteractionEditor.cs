
using System;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace UnityEditor.InputSystem.Interactions {

    #if UNITY_EDITOR
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
                () => UnityEngine.InputSystem.InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!UnityEngine.InputSystem.InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            EditorGUILayout.HelpBox(s_HelpBoxText);
            target.behavior = (PressBehavior)EditorGUILayout.EnumPopup(s_PressBehaviorLabel, target.behavior);
            m_PressPointSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
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
        }

#endif

        private CustomOrDefaultSetting m_PressPointSetting;

        private static readonly GUIContent s_HelpBoxText = EditorGUIUtility.TrTextContent("Note that the 'Press' interaction is only "
            + "necessary when wanting to customize button press behavior. For default press behavior, simply set the action type to 'Button' "
            + "and use the action without interactions added to it.");

        private static readonly GUIContent s_PressBehaviorLabel = EditorGUIUtility.TrTextContent("Trigger Behavior",
            "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.");
    }
    #endif
}