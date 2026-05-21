#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class HoldInteractionEditor : InputParameterEditor<HoldInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Hold Time",
                "Time (in seconds) that a control has to be held in order for it to register as a hold.",
                "Default Hold Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultHoldTime);
            m_PressPointSetting.Initialize("Press Point",
                "Float value that an axis control has to cross for it to be considered pressed.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v, () => ButtonControl.s_GlobalDefaultButtonPressPoint);
        }

        public override void OnGUI()
        {
            if (!InputSystem.settings.useIMGUIEditorForAssets)
                return;

            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_DurationSetting.OnDrawVisualElements(root, onChangedCallback);
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
            CustomOrDefaultSetting.AddSharedDefaultSettingsFooter(root,
                new[] { m_PressPointSetting, m_DurationSetting });
        }

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
}
#endif
