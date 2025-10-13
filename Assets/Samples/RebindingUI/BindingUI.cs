#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Common binding UI helper to allow editor composition.
    /// </summary>
    internal class BindingUI
    {
        private readonly SerializedProperty m_ActionProperty;
        private readonly SerializedProperty m_BindingIdProperty;
        private readonly SerializedProperty m_DisplayStringOptionsProperty;

        public BindingUI(SerializedObject serializedObject)
            : this(serializedObject.FindProperty("m_Action"), serializedObject.FindProperty("m_BindingId"),
            serializedObject.FindProperty("m_DisplayStringOptions"))
        {}

        public BindingUI(SerializedProperty actionProperty, SerializedProperty bindingIdProperty,
                         SerializedProperty displayStringOptionsProperty = null)
        {
            m_ActionProperty = actionProperty;
            m_BindingIdProperty = bindingIdProperty;
            m_DisplayStringOptionsProperty = displayStringOptionsProperty;

            Reset();
            Refresh();
        }

        private void Reset()
        {
            bindingOptions = Array.Empty<GUIContent>();
            bindingOptionValues = Array.Empty<string>();
            selectedBindingIndex = -1;
        }

        public void Draw()
        {
            // Binding section.
            EditorGUILayout.LabelField(m_BindingLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_ActionProperty);

                var newSelectedBinding = EditorGUILayout.Popup(m_BindingLabel, selectedBindingIndex, bindingOptions);
                if (newSelectedBinding != selectedBindingIndex)
                {
                    var id = bindingOptionValues[newSelectedBinding];
                    m_BindingIdProperty.stringValue = id;
                    selectedBindingIndex = newSelectedBinding;
                }

                if (m_DisplayStringOptionsProperty != null)
                {
                    var optionsOld = (InputBinding.DisplayStringOptions)m_DisplayStringOptionsProperty.intValue;
                    var optionsNew = (InputBinding.DisplayStringOptions)EditorGUILayout.EnumFlagsField(m_DisplayOptionsLabel, optionsOld);
                    if (optionsOld != optionsNew)
                        m_DisplayStringOptionsProperty.intValue = (int)optionsNew;
                }
            }
        }

        public bool Refresh()
        {
            if (action == null)
            {
                Reset();
                return false;
            }

            var bindings = action.bindings;
            var bindingCount = bindings.Count;

            bindingOptions = new GUIContent[bindingCount];
            bindingOptionValues = new string[bindingCount];
            selectedBindingIndex = -1;

            var currentBindingId = m_BindingIdProperty.stringValue;
            for (var i = 0; i < bindingCount; ++i)
            {
                var binding = bindings[i];
                var id = binding.id.ToString();
                var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

                // If we don't have a binding groups (control schemes), show the device that if there are, for example,
                // there are two bindings with the display string "A", the user can see that one is for the keyboard
                // and the other for the gamepad.
                var displayOptions =
                    InputBinding.DisplayStringOptions.DontUseShortDisplayNames | InputBinding.DisplayStringOptions.IgnoreBindingOverrides;
                if (!haveBindingGroups)
                    displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

                // Create display string.
                var displayString = action.GetBindingDisplayString(i, displayOptions);

                // If binding is part of a composite, include the part name.
                if (binding.isPartOfComposite)
                    displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

                // Some composites use '/' as a separator. When used in popup, this will lead to to submenus. Prevent
                // by instead using a backlash.
                displayString = displayString.Replace('/', '\\');

                // If the binding is part of control schemes, mention them.
                if (haveBindingGroups)
                {
                    var asset = action.actionMap?.asset;
                    if (asset != null)
                    {
                        var controlSchemes = string.Join(", ",
                            binding.groups.Split(InputBinding.Separator)
                                .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                        displayString = $"{displayString} ({controlSchemes})";
                    }
                }

                bindingOptions[i] = new GUIContent(displayString);
                bindingOptionValues[i] = id;

                if (currentBindingId == id)
                    selectedBindingIndex = i;
            }

            return true;
        }

        public string bindingId => m_BindingIdProperty.stringValue;
        public int bindingIndex => action.FindBindingById(m_BindingIdProperty.stringValue);

        public InputAction action => ((InputActionReference)m_ActionProperty.objectReferenceValue)?.action;

        private GUIContent[] bindingOptions { get; set; }
        private string[] bindingOptionValues { get; set; }
        private int selectedBindingIndex { get; set; }

        private readonly GUIContent m_BindingLabel = new GUIContent("Binding");
        private readonly GUIContent m_DisplayOptionsLabel = new GUIContent("Display Options");
    }
}

#endif // UNITY_EDITOR
