#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var currentPath = m_PathProperty.stringValue;
            InputSystem.OnDrawCustomWarningForBindingPath(currentPath);

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

                // Show the specific controls which match the current path
                DrawMatchingControlPaths();

                // Control scheme matrix.
                DrawUseInControlSchemes();
            }
        }

        /// <summary>
        /// Used to keep track of which foldouts are expanded.
        /// </summary>
        private static bool showMatchingLayouts = false;
        private static Dictionary<string, bool> showMatchingChildLayouts = new Dictionary<string, bool>();

        /// <summary>
        /// Finds all registered control paths implemented by concrete classes which match the current binding path and renders it.
        /// </summary>
        private void DrawMatchingControlPaths()
        {
            var path = m_ControlPathEditor.pathProperty.stringValue;
            if (path == string.Empty)
                return;

            var deviceLayoutPath = InputControlPath.TryGetDeviceLayout(path);
            var parsedPath = InputControlPath.Parse(path).ToArray();

            // If the provided path is parseable into device and control components, draw UI which shows control layouts that match the path.
            if (parsedPath.Length >= 2 && !string.IsNullOrEmpty(deviceLayoutPath))
            {
                bool matchExists = false;

                var rootDeviceLayout = EditorInputControlLayoutCache.TryGetLayout(deviceLayoutPath);
                bool isValidDeviceLayout = deviceLayoutPath == InputControlPath.Wildcard || (rootDeviceLayout != null && !rootDeviceLayout.isOverride && !rootDeviceLayout.hideInUI);
                // Exit early if a malformed device layout was provided,
                if (!isValidDeviceLayout)
                    return;

                bool controlPathUsagePresent = parsedPath[1].usages.Count() > 0;
                bool hasChildDeviceLayouts = deviceLayoutPath == InputControlPath.Wildcard || EditorInputControlLayoutCache.HasChildLayouts(rootDeviceLayout.name);

                // If the path provided matches exactly one control path (i.e. has no ui-facing child device layouts or uses control usages), then exit early
                if (!controlPathUsagePresent && !hasChildDeviceLayouts)
                    return;

                // Otherwise, we will show either all controls that match the current binding (if control usages are used)
                // or all controls in derived device layouts (if a no control usages are used).
                EditorGUILayout.BeginVertical();
                showMatchingLayouts = EditorGUILayout.Foldout(showMatchingLayouts, "Derived Bindings");

                if (showMatchingLayouts)
                {
                    // If our control path contains a usage, make sure we render the binding that belongs to the root device layout first
                    if (deviceLayoutPath != InputControlPath.Wildcard && controlPathUsagePresent)
                    {
                        matchExists |= DrawMatchingControlPathsForLayout(rootDeviceLayout, in parsedPath, true);
                    }
                    // Otherwise, just render the bindings that belong to child device layouts. The binding that matches the root layout is
                    // already represented by the user generated control path itself.
                    else
                    {
                        IEnumerable<InputControlLayout> matchedChildLayouts = Enumerable.Empty<InputControlLayout>();
                        if (deviceLayoutPath == InputControlPath.Wildcard)
                        {
                            matchedChildLayouts = EditorInputControlLayoutCache.allLayouts
                                .Where(x => x.isDeviceLayout && !x.hideInUI && !x.isOverride && x.isGenericTypeOfDevice && x.baseLayouts.Count() == 0).OrderBy(x => x.displayName);
                        }
                        else
                        {
                            matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(rootDeviceLayout.name);
                        }

                        foreach (var childLayout in matchedChildLayouts)
                        {
                            matchExists |= DrawMatchingControlPathsForLayout(childLayout, in parsedPath);
                        }
                    }

                    // Otherwise, indicate that no layouts match the current path.
                    if (!matchExists)
                    {
                        if (controlPathUsagePresent)
                            EditorGUILayout.HelpBox("No registered controls match this current binding. Some controls are only registered at runtime.", MessageType.Warning);
                        else
                            EditorGUILayout.HelpBox("No other registered controls match this current binding. Some controls are only registered at runtime.", MessageType.Warning);
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Returns true if the deviceLayout or any of its children has controls which match the provided parsed path. exist matching registered control paths.
        /// </summary>
        /// <param name="deviceLayout">The device layout to draw control paths for</param>
        /// <param name="parsedPath">The parsed path containing details of the Input Controls that can be matched</param>
        private bool DrawMatchingControlPathsForLayout(InputControlLayout deviceLayout, in InputControlPath.ParsedPathComponent[] parsedPath, bool isRoot = false)
        {
            string deviceName = deviceLayout.displayName;
            string controlName = string.Empty;
            bool matchExists = false;

            for (int i = 0; i < deviceLayout.m_Controls.Length; i++)
            {
                ref InputControlLayout.ControlItem controlItem = ref deviceLayout.m_Controls[i];
                if (InputControlPath.MatchControlComponent(ref parsedPath[1], ref controlItem, true))
                {
                    // If we've already located a match, append a ", " to the control name
                    // This is to accomodate cases where multiple control items match the same path within a single device layout
                    // Note, some controlItems have names but invalid displayNames (i.e. the Dualsense HID > leftTriggerButton)
                    // There are instance where there are 2 control items with the same name inside a layout definition, however they are not
                    // labeled significantly differently.
                    // The notable example is that the Android Xbox and Android Dualshock layouts have 2 d-pad definitions, one is a "button"
                    // while the other is an axis.
                    controlName += matchExists ? $", {controlItem.name}" : controlItem.name;

                    // if the parsePath has a 3rd component, try to match it with items in the controlItem's layout definition.
                    if (parsedPath.Length == 3)
                    {
                        var controlLayout = EditorInputControlLayoutCache.TryGetLayout(controlItem.layout);
                        if (controlLayout.isControlLayout && !controlLayout.hideInUI)
                        {
                            for (int j = 0; j < controlLayout.m_Controls.Count(); j++)
                            {
                                ref InputControlLayout.ControlItem controlLayoutItem = ref controlLayout.m_Controls[j];
                                if (InputControlPath.MatchControlComponent(ref parsedPath[2], ref controlLayoutItem))
                                {
                                    controlName += $"/{controlLayoutItem.name}";
                                    matchExists = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        matchExists = true;
                    }
                }
            }

            IEnumerable<InputControlLayout> matchedChildLayouts = EditorInputControlLayoutCache.TryGetChildLayouts(deviceLayout.name);

            // If this layout does not have a match, or is the top level root layout,
            // skip over trying to draw any items for it, and immediately try processing the child layouts
            if (!matchExists)
            {
                foreach (var childLayout in matchedChildLayouts)
                {
                    matchExists |= DrawMatchingControlPathsForLayout(childLayout, in parsedPath);
                }
            }
            // Otherwise, draw the items for it, and then only process the child layouts if the foldout is expanded.
            else
            {
                bool showLayout = false;
                EditorGUI.indentLevel++;
                if (matchedChildLayouts.Count() > 0 && !isRoot)
                {
                    showMatchingChildLayouts.TryGetValue(deviceName, out showLayout);
                    showMatchingChildLayouts[deviceName] = EditorGUILayout.Foldout(showLayout, $"{deviceName} > {controlName}");
                }
                else
                {
                    EditorGUILayout.LabelField($"{deviceName} > {controlName}");
                }

                showLayout |= isRoot;

                if (showLayout)
                {
                    foreach (var childLayout in matchedChildLayouts)
                    {
                        DrawMatchingControlPathsForLayout(childLayout, in parsedPath);
                    }
                }
                EditorGUI.indentLevel--;
            }

            return matchExists;
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
