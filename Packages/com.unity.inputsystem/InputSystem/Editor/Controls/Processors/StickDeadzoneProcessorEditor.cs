#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Processors;
#endif

namespace UnityEditor.InputSystem.Processors {

    #if UNITY_EDITOR
    internal class StickDeadzoneProcessorEditor : InputParameterEditor<StickDeadzoneProcessor>
    {
        protected override void OnEnable()
        {
            m_MinSetting.Initialize("Min",
                "Vector length  below which input values will be clamped. After clamping, vector lengths will be renormalized to [0..1] between min and max.",
                "Default Deadzone Min",
                () => target.min, v => target.min = v,
                () => UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMin);
            m_MaxSetting.Initialize("Max",
                "Vector length above which input values will be clamped. After clamping, vector lengths will be renormalized to [0..1] between min and max.",
                "Default Deadzone Max",
                () => target.max, v => target.max = v,
                () => UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMax);
        }

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!UnityEngine.InputSystem.InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            m_MinSetting.OnGUI();
            m_MaxSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_MinSetting.OnDrawVisualElements(root, onChangedCallback);
            m_MaxSetting.OnDrawVisualElements(root, onChangedCallback);
        }

#endif

        private CustomOrDefaultSetting m_MinSetting;
        private CustomOrDefaultSetting m_MaxSetting;
    }
    #endif
}