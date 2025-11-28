using System;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Interactions
{
    #if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class MultiTapInteractionEditor : InputParameterEditor<MultiTapInteraction>
    {
        protected override void OnEnable()
        {
            m_TapTimeSetting.Initialize("Max Tap Duration",
                "Time (in seconds) within with a control has to be released again for it to register as a tap. If the control is held "
                + "for longer than this time, the tap is canceled.",
                "Default Tap Time",
                () => target.tapTime, x => target.tapTime = x, () => InputSystem.settings.defaultTapTime);
            m_TapDelaySetting.Initialize("Max Tap Spacing",
                "The maximum delay (in seconds) allowed between each tap. If this time is exceeded, the multi-tap is canceled.",
                "Default Tap Spacing",
                () => target.tapDelay, x => target.tapDelay = x, () => InputSystem.settings.multiTapDelayTime);
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
            var tapCountField = new IntegerField(tapLabel)
            {
                value = target.tapCount,
                tooltip = tapTooltip
            };
            tapCountField.RegisterValueChangedCallback(evt =>
            {
                target.tapCount = evt.newValue;
                onChangedCallback?.Invoke();
            });
            root.Add(tapCountField);

            m_TapDelaySetting.OnDrawVisualElements(root, onChangedCallback);
            m_TapTimeSetting.OnDrawVisualElements(root, onChangedCallback);
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
        }

        private const string tapLabel = "Tap Count";
        private const string tapTooltip = "How many taps need to be performed in succession. Two means double-tap, three means triple-tap, and so on.";

        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_TapTimeSetting;
        private CustomOrDefaultSetting m_TapDelaySetting;
    }
    #endif
}
