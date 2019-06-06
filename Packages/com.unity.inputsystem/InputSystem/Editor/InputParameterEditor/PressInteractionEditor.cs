using UnityEditor;
using UnityEngine.InputSystem.Editor;

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
            target.behavior = (PressBehavior)EditorGUILayout.EnumPopup(s_PressBehaviorLabel, target.behavior);
            m_PressPointSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_PressPointSetting;

        private static readonly GUIContent s_PressBehaviorLabel = EditorGUIUtility.TrTextContent("Trigger Behavior",
            "Determines how button presses trigger the action. By default (PressOnly), the action is performed on press. "
            + "With ReleaseOnly, the action is performed on release. With PressAndRelease, the action is performed on press and "
            + "canceled on release.");
    }
}
