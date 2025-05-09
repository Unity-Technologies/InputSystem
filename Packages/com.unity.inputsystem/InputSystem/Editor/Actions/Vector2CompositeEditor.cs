#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;
using UnityEngine.InputSystem.Composites;
#endif

namespace UnityEditor.InputSystem.Composites {
    #if UNITY_EDITOR
    internal class Vector2CompositeEditor : InputParameterEditor<Vector2Composite>
    {
        private GUIContent m_ModeLabel = new GUIContent("Mode",
            "How to synthesize a Vector2 from the inputs. Digital "
            + "treats part bindings as buttons (on/off) whereas Analog preserves "
            + "floating-point magnitudes as read from controls.");

        public override void OnGUI()
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (!UnityEngine.InputSystem.InputSystem.settings.useIMGUIEditorForAssets) return;
#endif
            target.mode = (Vector2Composite.Mode)EditorGUILayout.EnumPopup(m_ModeLabel, target.mode);
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
                target.mode = (Vector2Composite.Mode)evt.newValue;
                onChangedCallback();
            });

            root.Add(modeField);
        }

#endif
    }
    #endif
}