
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
#endif

namespace UnityEditor.InputSystem.Composites {
    #if UNITY_EDITOR
    internal class Vector3CompositeEditor : InputParameterEditor<Vector3Composite>
    {
        private GUIContent m_ModeLabel = new GUIContent("Mode",
            "How to synthesize a Vector3 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.");

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            target.mode = (Vector3Composite.Mode)EditorGUILayout.EnumPopup(m_ModeLabel, target.mode);
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        public override void OnDrawVisualElements(VisualElement root, Action onChangedCallback)
        {
            var modeField = new EnumField(m_ModeLabel.text, target.mode)
            {
                tooltip = m_ModeLabel.tooltip
            };

            modeField.RegisterValueChangedCallback(evt =>
            {
                target.mode = (Vector3Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }

#endif
    }
    #endif
}