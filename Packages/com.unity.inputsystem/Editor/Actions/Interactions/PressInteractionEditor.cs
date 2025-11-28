using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Interactions
{
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
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            root.Add(new HelpBox(helpLabel, HelpBoxMessageType.None));

            var behaviourDropdown = new EnumField(triggerLabel, target.behavior)
            {
                tooltip = triggerTooltip
            };
            behaviourDropdown.RegisterValueChangedCallback(evt =>
            {
                target.behavior = (PressBehavior)evt.newValue;
                onChangedCallback?.Invoke();
            });
            root.Add(behaviourDropdown);

            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
        }

        private CustomOrDefaultSetting m_PressPointSetting;

        private const string helpLabel = "Note that the 'Press' interaction is only "
            + "necessary when wanting to customize button press behavior. For default press behavior, simply set the action type to 'Button' "
            + "and use the action without interactions added to it.";
        private const string triggerLabel = "Trigger Behavior";
        private const string triggerTooltip = "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.";
    }
    #endif
}
