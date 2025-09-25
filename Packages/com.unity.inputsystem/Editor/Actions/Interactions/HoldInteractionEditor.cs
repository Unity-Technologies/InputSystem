
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Interactions;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

namespace UnityEditor.InputSystem.Interactions
{
    #if UNITY_EDITOR
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
                () => target.pressPoint, v => target.pressPoint = v, () => ButtonControl.s_GlobalDefaultButtonPressPoint);
            m_DurationSetting.Initialize("Hold Time",
                "Time (in seconds) that a control has to be held in order for it to register as a hold.",
                "Default Hold Time",
                () => target.duration, x => target.duration = x, () => UnityEngine.InputSystem.InputSystem.settings.defaultHoldTime);
        }

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            m_PressPointSetting.OnGUI();
            m_DurationSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
            m_DurationSetting.OnDrawVisualElements(root, onChangedCallback);
        }

#endif

        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_DurationSetting;
    }
    #endif
}