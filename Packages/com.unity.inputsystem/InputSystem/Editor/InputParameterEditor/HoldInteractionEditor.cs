using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class HoldInteractionEditor : InputParameterEditor<HoldInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "Float value that an axis control has to cross for it to be considered pressed.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v, () => InputSystem.settings.defaultButtonPressPoint);
            m_DurationSetting.Initialize("Hold Time",
                "Time (in seconds) that a control has to be held in order for it to register as a hold.",
                "Default Hold Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultHoldTime);
        }

        public override void OnGUI()
        {
            //target.startContinuously = GUILayout.Toggle(target.startContinuously, m_ContinuousStartsLabel);

            m_PressPointSetting.OnGUI();
            m_DurationSetting.OnGUI();
        }

        private GUIContent m_ContinuousStartsLabel = new GUIContent("Start Continuously",
            "If enabled, the Hold will triggered 'started' repeatedly for as long as ");
        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_DurationSetting;
    }
}
