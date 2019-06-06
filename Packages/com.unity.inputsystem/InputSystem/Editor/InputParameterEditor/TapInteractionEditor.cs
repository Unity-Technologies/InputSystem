using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Interactions
{
    internal class TapInteractionEditor : InputParameterEditor<TapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Max Tap Duration",
                "Time (in seconds) within with a control has to be released again for it to register as a tap. If the control is held "
                + "for longer than this time, the tap is canceled.",
                "Default Tap Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultTapTime);
            m_PressPointSetting.Initialize("Press Point",
                "The amount of actuation a control requires before being considered pressed. If not set, default to "
                + "'Default Button Press Point' in the global input settings.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v,
                () => InputSystem.settings.defaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
}
