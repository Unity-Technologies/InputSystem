#if UNITY_EDITOR
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Interactions;
#endif

namespace UnityEditor.InputSystem.Interactions {
    #if UNITY_EDITOR
    internal class SlowTapInteractionEditor : InputParameterEditor<SlowTapInteraction>
    {
        protected override void OnEnable()
        {
            m_DurationSetting.Initialize("Min Tap Duration",
                "Minimum time (in seconds) that a control has to be held for it to register as a slow tap. If the control is released "
                + "before this time, the slow tap is canceled.",
                "Default Slow Tap Time",
                () => target.duration, x => target.duration = x, () => UnityEngine.InputSystem.InputSystem.settings.defaultSlowTapTime);
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
            m_DurationSetting.OnGUI();
            m_PressPointSetting.OnGUI();
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            m_DurationSetting.OnDrawVisualElements(root, onChangedCallback);
            m_PressPointSetting.OnDrawVisualElements(root, onChangedCallback);
        }

#endif

        private CustomOrDefaultSetting m_DurationSetting;
        private CustomOrDefaultSetting m_PressPointSetting;
    }
    #endif
}