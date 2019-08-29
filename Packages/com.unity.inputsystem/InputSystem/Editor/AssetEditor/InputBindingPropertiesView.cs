#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Editor.Lists;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////REVIEW: when we start with a blank tree view state, we should initialize the control picker to select the control currently
////        selected by the path property

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// UI for editing properties of an <see cref="InputBinding"/>. Right-most pane in action editor when
    /// binding is selected in middle pane.
    /// </summary>
    internal class InputBindingPropertiesView : PropertiesViewBase, IDisposable
    {
        public static FourCC k_GroupsChanged => new FourCC("GRPS");
        public static FourCC k_PathChanged => new FourCC("PATH");
        public static FourCC k_CompositeTypeChanged => new FourCC("COMP");
        public static FourCC k_CompositePartAssignmentChanged => new FourCC("PART");

        public InputBindingPropertiesView(
            SerializedProperty bindingProperty,
            Action<FourCC> onChange = null,
            InputControlPickerState controlPickerState = null,
            string expectedControlLayout = null,
            ReadOnlyArray<InputControlScheme> controlSchemes = new ReadOnlyArray<InputControlScheme>(),
            IEnumerable<string> controlPathsToMatch = null)
            : base(InputActionSerializationHelpers.IsCompositeBinding(bindingProperty) ? "Composite" : "Binding",
                   bindingProperty, onChange, expectedControlLayout)
        {
            m_BindingProperty = bindingProperty;
            m_GroupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
            m_PathProperty = bindingProperty.FindPropertyRelative("m_Path");
            m_BindingGroups = m_GroupsProperty.stringValue
                .Split(new[] {InputBinding.Separator}, StringSplitOptions.RemoveEmptyEntries).ToList();
            m_ExpectedControlLayout = expectedControlLayout;
            m_ControlSchemes = controlSchemes;

            var flags = (InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue;
            m_IsPartOfComposite = (flags & InputBinding.Flags.PartOfComposite) != 0;
            m_IsComposite = (flags & InputBinding.Flags.Composite) != 0;

            // Set up control picker for m_Path. Not needed if the binding is a composite.
            if (!m_IsComposite)
            {
                m_ControlPickerState = controlPickerState ?? new InputControlPickerState();
                m_ControlPathEditor = new InputControlPathEditor(m_PathProperty, m_ControlPickerState, OnPathChanged);
                m_ControlPathEditor.SetExpectedControlLayout(m_ExpectedControlLayout);
                if (controlPathsToMatch != null)
                    m_ControlPathEditor.SetControlPathsToMatch(controlPathsToMatch);
            }
        }

        public void Dispose()
        {
            m_ControlPathEditor?.Dispose();
        }

        protected override void DrawGeneralProperties()
        {
            if (m_IsComposite)
            {
                if (m_CompositeParameters == null)
                    InitializeCompositeProperties();

                // Composite type dropdown.
                var selectedCompositeType = EditorGUILayout.Popup(s_CompositeTypeLabel, m_SelectedCompositeType, m_CompositeTypeOptions);
                if (selectedCompositeType != m_SelectedCompositeType)
                {
                    m_SelectedCompositeType = selectedCompositeType;
                    OnCompositeTypeChanged();
                }

                // Composite parameters.
                m_CompositeParameters.OnGUI();
            }
            else
            {
                // Path.
                m_ControlPathEditor.OnGUI();

                // Composite part.
                if (m_IsPartOfComposite)
                {
                    if (m_CompositeParts == null)
                        InitializeCompositePartProperties();

                    var selectedPart = EditorGUILayout.Popup(s_CompositePartAssignmentLabel, m_SelectedCompositePart,
                        m_CompositePartOptions);
                    if (selectedPart != m_SelectedCompositePart)
                    {
                        m_SelectedCompositePart = selectedPart;
                        OnCompositePartAssignmentChanged();
                    }
                }

                // Control scheme matrix.
                DrawUseInControlSchemes();
            }
        }

        /// <summary>
        /// Draw control scheme matrix that allows selecting which control schemes a particular
        /// binding appears in.
        /// </summary>
        private void DrawUseInControlSchemes()
        {
            if (m_ControlSchemes.Count <= 0)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_UseInControlSchemesLAbel, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical();

            foreach (var scheme in m_ControlSchemes)
            {
                EditorGUI.BeginChangeCheck();
                var result = EditorGUILayout.Toggle(scheme.name, m_BindingGroups.Contains(scheme.bindingGroup));
                if (EditorGUI.EndChangeCheck())
                {
                    if (result)
                    {
                        m_BindingGroups.Add(scheme.bindingGroup);
                    }
                    else
                    {
                        m_BindingGroups.Remove(scheme.bindingGroup);
                    }
                    OnBindingGroupsChanged();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void InitializeCompositeProperties()
        {
            // Find name of current composite.
            var path = m_PathProperty.stringValue;
            var compositeNameAndParameters = NameAndParameters.Parse(path);
            var compositeName = compositeNameAndParameters.name;
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

            // Collect all possible composite types.
            var selectedCompositeIndex = -1;
            var compositeTypeOptionsList = new List<GUIContent>();
            var compositeTypeList = new List<string>();
            var currentIndex = 0;
            foreach (var composite in InputBindingComposite.s_Composites.internedNames.Where(x =>
                !InputBindingComposite.s_Composites.aliases.Contains(x)).OrderBy(x => x))
            {
                if (!string.IsNullOrEmpty(m_ExpectedControlLayout))
                {
                    var valueType = InputBindingComposite.GetValueType(composite);
                    if (valueType != null &&
                        !InputControlLayout.s_Layouts.ValueTypeIsAssignableFrom(
                            new InternedString(m_ExpectedControlLayout), valueType))
                        continue;
                }

                if (InputBindingComposite.s_Composites.LookupTypeRegistration(composite) == compositeType)
                    selectedCompositeIndex = currentIndex;
                var name = ObjectNames.NicifyVariableName(composite);
                compositeTypeOptionsList.Add(new GUIContent(name));
                compositeTypeList.Add(composite);
                ++currentIndex;
            }

            // If the current composite type isn't a registered type, add it to the list as
            // an extra option.
            if (selectedCompositeIndex == -1)
            {
                selectedCompositeIndex = compositeTypeList.Count;
                compositeTypeOptionsList.Add(new GUIContent(ObjectNames.NicifyVariableName(compositeName)));
                compositeTypeList.Add(compositeName);
            }

            m_CompositeTypes = compositeTypeList.ToArray();
            m_CompositeTypeOptions = compositeTypeOptionsList.ToArray();
            m_SelectedCompositeType = selectedCompositeIndex;

            // Initialize parameters.
            m_CompositeParameters = new ParameterListView
            {
                onChange = OnCompositeParametersModified
            };
            if (compositeType != null)
                m_CompositeParameters.Initialize(compositeType, compositeNameAndParameters.parameters);
        }

        private void InitializeCompositePartProperties()
        {
            var currentCompositePart = m_BindingProperty.FindPropertyRelative("m_Name").stringValue;

            ////REVIEW: this makes a lot of assumptions about the serialized data based on the one property we've been given in the ctor
            // Determine the name of the current composite type that the part belongs to.
            var bindingArrayProperty = m_BindingProperty.GetArrayPropertyFromElement();
            var partBindingIndex = InputActionSerializationHelpers.GetIndex(bindingArrayProperty, m_BindingProperty);
            var compositeBindingIndex =
                InputActionSerializationHelpers.GetCompositeStartIndex(bindingArrayProperty, partBindingIndex);
            if (compositeBindingIndex == -1)
                return;
            var compositeBindingProperty = bindingArrayProperty.GetArrayElementAtIndex(compositeBindingIndex);
            var compositePath = compositeBindingProperty.FindPropertyRelative("m_Path").stringValue;
            var compositeNameAndParameters = NameAndParameters.Parse(compositePath);

            // Initialize option list from all parts available for the composite.
            var optionList = new List<GUIContent>();
            var nameList = new List<string>();
            var currentIndex = 0;
            var selectedPartNameIndex = -1;
            foreach (var partName in InputBindingComposite.GetPartNames(compositeNameAndParameters.name))
            {
                if (partName.Equals(currentCompositePart, StringComparison.InvariantCultureIgnoreCase))
                    selectedPartNameIndex = currentIndex;
                var niceName = ObjectNames.NicifyVariableName(partName);
                optionList.Add(new GUIContent(niceName));
                nameList.Add(partName);
                ++currentIndex;
            }

            // If currently selected part is not in list, add it as an option.
            if (selectedPartNameIndex == -1)
            {
                selectedPartNameIndex = nameList.Count;
                optionList.Add(new GUIContent(ObjectNames.NicifyVariableName(currentCompositePart)));
                nameList.Add(currentCompositePart);
            }

            m_CompositeParts = nameList.ToArray();
            m_CompositePartOptions = optionList.ToArray();
            m_SelectedCompositePart = selectedPartNameIndex;
        }

        private void OnCompositeParametersModified()
        {
            Debug.Assert(m_CompositeParameters != null);

            var path = m_PathProperty.stringValue;
            var nameAndParameters = NameAndParameters.Parse(path);
            nameAndParameters.parameters = m_CompositeParameters.GetParameters();

            m_PathProperty.stringValue = nameAndParameters.ToString();
            m_PathProperty.serializedObject.ApplyModifiedProperties();

            OnPathChanged();
        }

        private void OnBindingGroupsChanged()
        {
            m_GroupsProperty.stringValue = string.Join(InputBinding.kSeparatorString, m_BindingGroups.ToArray());
            m_GroupsProperty.serializedObject.ApplyModifiedProperties();

            onChange?.Invoke(k_GroupsChanged);
        }

        private void OnPathChanged()
        {
            m_BindingProperty.serializedObject.ApplyModifiedProperties();
            onChange?.Invoke(k_PathChanged);
        }

        private void OnCompositeTypeChanged()
        {
            var nameAndParameters = new NameAndParameters
            {
                name = m_CompositeTypes[m_SelectedCompositeType],
                parameters = m_CompositeParameters.GetParameters()
            };

            InputActionSerializationHelpers.ChangeCompositeBindingType(m_BindingProperty, nameAndParameters);
            m_PathProperty.serializedObject.ApplyModifiedProperties();

            onChange?.Invoke(k_CompositeTypeChanged);
        }

        private void OnCompositePartAssignmentChanged()
        {
            m_BindingProperty.FindPropertyRelative("m_Name").stringValue = m_CompositeParts[m_SelectedCompositePart];
            m_BindingProperty.serializedObject.ApplyModifiedProperties();

            onChange?.Invoke(k_CompositePartAssignmentChanged);
        }

        private readonly bool m_IsComposite;
        private ParameterListView m_CompositeParameters;
        private int m_SelectedCompositeType;
        private GUIContent[] m_CompositeTypeOptions;
        private string[] m_CompositeTypes;

        private int m_SelectedCompositePart;
        private GUIContent[] m_CompositePartOptions;
        private string[] m_CompositeParts;

        private readonly SerializedProperty m_GroupsProperty;
        private readonly SerializedProperty m_BindingProperty;
        private readonly SerializedProperty m_PathProperty;

        private readonly InputControlPickerState m_ControlPickerState;
        private readonly InputControlPathEditor m_ControlPathEditor;

        private static readonly GUIContent s_CompositeTypeLabel = EditorGUIUtility.TrTextContent("Composite Type",
            "Type of composite. Allows changing the composite type retroactively. Doing so will modify the bindings that are part of the composite.");
        private static readonly GUIContent s_UseInControlSchemesLAbel = EditorGUIUtility.TrTextContent("Use in control scheme",
            "In which control schemes the binding is active. A binding can be used by arbitrary many control schemes. If a binding is not "
            + "assigned to a specific control schemes, it is active in all of them.");
        private static readonly GUIContent s_CompositePartAssignmentLabel = EditorGUIUtility.TrTextContent(
            "Composite Part",
            "The named part of the composite that the binding is assigned to. Multiple bindings may be assigned the same part. All controls from "
            + "all bindings that are assigned the same part will collectively feed values into that part of the composite.");

        private ReadOnlyArray<InputControlScheme> m_ControlSchemes;
        private readonly List<string> m_BindingGroups;
        private readonly string m_ExpectedControlLayout;
    }
}
#endif // UNITY_EDITOR
