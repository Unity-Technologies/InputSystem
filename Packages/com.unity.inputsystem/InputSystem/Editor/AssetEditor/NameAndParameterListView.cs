#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor.Lists
{
    /// <summary>
    /// A <see cref="ReorderableList"/> to manage a set of name-and-parameter pairs and a <see cref="ParameterListView"/>
    /// to edit the parameters of the currently selected pair.
    /// </summary>
    /// <remarks>
    /// Produces output that can be consumed by <see cref="NameAndParameters.ParseMultiple"/>.
    /// </remarks>
    internal abstract class NameAndParameterListView
    {
        protected NameAndParameterListView(SerializedProperty property, Action applyAction, string expectedControlLayout)
        {
            m_DeleteButton = EditorGUIUtility.TrIconContent("Toolbar Minus", $"Delete {itemName}");
            m_UpButton = EditorGUIUtility.TrIconContent(GUIHelpers.LoadIcon("ChevronUp"), $"Move {itemName} up");
            m_DownButton = EditorGUIUtility.TrIconContent(GUIHelpers.LoadIcon("ChevronDown"), $"Move {itemName} down");

            m_Property = property;
            m_Apply = applyAction;
            m_ListOptions = GetOptions();

            m_ExpectedControlLayout = expectedControlLayout;
            if (!string.IsNullOrEmpty(m_ExpectedControlLayout))
                m_ExpectedValueType = EditorInputControlLayoutCache.GetValueType(m_ExpectedControlLayout);

            m_ParametersForEachListItem = NameAndParameters.ParseMultiple(m_Property.stringValue).ToArray();
            m_EditableParametersForEachListItem = new ParameterListView[m_ParametersForEachListItem.Length];

            for (var i = 0; i < m_ParametersForEachListItem.Length; i++)
            {
                m_EditableParametersForEachListItem[i] = new ParameterListView { onChange = OnParametersChanged };
                var typeName = m_ParametersForEachListItem[i].name;
                var rowType = m_ListOptions.LookupTypeRegistration(typeName);
                m_EditableParametersForEachListItem[i].Initialize(rowType, m_ParametersForEachListItem[i].parameters);

                var name = ObjectNames.NicifyVariableName(typeName);

                ////REVIEW: finding this kind of stuff should probably have better support globally on the asset; e.g. some
                ////        notification that pops up and allows fixing all occurrences in one click
                // Find out if we still support this option and indicate it in the list, if we don't.
                if (rowType == null)
                    name += " (Obsolete)";
                else if (m_ExpectedValueType != null)
                {
                    var valueType = GetValueType(rowType);
                    if (!m_ExpectedValueType.IsAssignableFrom(valueType))
                        name += " (Ignored)";
                }
                m_EditableParametersForEachListItem[i].name = name;
            }
        }

        protected abstract TypeTable GetOptions();
        protected abstract Type GetValueType(Type type);

        public void OnAddDropdown(Rect r)
        {
            // Add only original names to the menu and not aliases.
            var menu = new GenericMenu();
            foreach (var name in m_ListOptions.internedNames.Where(x => !m_ListOptions.ShouldHideInUI(x)).OrderBy(x => x.ToString()))
            {
                // Skip if not compatible with value type.
                if (m_ExpectedValueType != null)
                {
                    var type = m_ListOptions.LookupTypeRegistration(name);
                    var valueType = GetValueType(type);
                    if (valueType != null && !m_ExpectedValueType.IsAssignableFrom(valueType))
                        continue;
                }

                var niceName = ObjectNames.NicifyVariableName(name);
                menu.AddItem(new GUIContent(niceName), false, OnAddElement, name.ToString());
            }
            menu.ShowAsContext();
        }

        private void OnAddElement(object data)
        {
            var name = (string)data;

            ArrayHelpers.Append(ref m_ParametersForEachListItem,
                new NameAndParameters {name = name});
            ArrayHelpers.Append(ref m_EditableParametersForEachListItem,
                new ParameterListView { onChange = OnParametersChanged });

            var index = m_EditableParametersForEachListItem.Length - 1;
            var typeName = m_ParametersForEachListItem[index].name;
            var rowType = m_ListOptions.LookupTypeRegistration(typeName);
            m_EditableParametersForEachListItem[index].Initialize(rowType, m_ParametersForEachListItem[index].parameters);
            m_EditableParametersForEachListItem[index].name = ObjectNames.NicifyVariableName(name);

            m_Apply();
        }

        private void OnParametersChanged()
        {
            for (var i = 0; i < m_ParametersForEachListItem.Length; i++)
            {
                m_ParametersForEachListItem[i] = new NameAndParameters
                {
                    name = m_ParametersForEachListItem[i].name,
                    parameters = m_EditableParametersForEachListItem[i].GetParameters(),
                };
            }

            m_Apply();
        }

        private static class Styles
        {
            public static readonly GUIStyle s_FoldoutStyle = new GUIStyle("foldout");
            public static readonly GUIStyle s_UpDownButtonStyle = new GUIStyle("label");

            static Styles()
            {
                s_FoldoutStyle.fontStyle = FontStyle.Bold;
                s_UpDownButtonStyle.fixedWidth = 12;
                s_UpDownButtonStyle.fixedHeight = 12;
                s_UpDownButtonStyle.padding = new RectOffset();
            }
        }

        private void SwapEntry(int oldIndex, int newIndex)
        {
            MemoryHelpers.Swap(ref m_ParametersForEachListItem[oldIndex], ref m_ParametersForEachListItem[newIndex]);
            MemoryHelpers.Swap(ref m_EditableParametersForEachListItem[oldIndex], ref m_EditableParametersForEachListItem[newIndex]);
            m_Apply();
        }

        public void OnGUI()
        {
            if (m_EditableParametersForEachListItem == null || m_EditableParametersForEachListItem.Length == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"No {itemName}s have been added.");
                    EditorGUI.indentLevel--;
                }
            }
            else
                for (var i = 0; i < m_EditableParametersForEachListItem.Length; i++)
                {
                    var editableParams = m_EditableParametersForEachListItem[i];
                    EditorGUILayout.BeginHorizontal();
                    if (editableParams.hasUIToShow)
                        editableParams.visible = EditorGUILayout.Foldout(editableParams.visible, editableParams.name, Styles.s_FoldoutStyle);
                    else
                    {
                        GUILayout.Space(16);
                        EditorGUILayout.LabelField(editableParams.name, EditorStyles.boldLabel);
                    }
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button(m_UpButton, Styles.s_UpDownButtonStyle))
                            SwapEntry(i, i - 1);
                    }
                    using (new EditorGUI.DisabledScope(i == m_EditableParametersForEachListItem.Length - 1))
                    {
                        if (GUILayout.Button(m_DownButton, Styles.s_UpDownButtonStyle))
                            SwapEntry(i, i + 1);
                    }
                    if (GUILayout.Button(m_DeleteButton, EditorStyles.label))
                    {
                        ArrayHelpers.EraseAt(ref m_ParametersForEachListItem, i);
                        ArrayHelpers.EraseAt(ref m_EditableParametersForEachListItem, i);
                        m_Apply();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (editableParams.visible)
                    {
                        EditorGUI.indentLevel++;
                        editableParams.OnGUI();
                        EditorGUI.indentLevel--;
                    }
                    GUIHelpers.DrawLineSeparator();
                }
        }

        public string ToSerializableString()
        {
            if (m_ParametersForEachListItem == null)
                return string.Empty;

            return string.Join(NamedValue.Separator,
                m_ParametersForEachListItem.Select(x => x.ToString()).ToArray());
        }

        protected abstract string itemName { get; }

        private SerializedProperty m_Property;
        private readonly TypeTable m_ListOptions;
        private readonly string m_ExpectedControlLayout;
        private readonly Type m_ExpectedValueType;
        private readonly GUIContent m_DeleteButton;
        private readonly GUIContent m_UpButton;
        private readonly GUIContent m_DownButton;
        private NameAndParameters[] m_ParametersForEachListItem;
        private ParameterListView[] m_EditableParametersForEachListItem;
        private readonly Action m_Apply;
    }

    /// <summary>
    /// A list of processors and their parameters.
    /// </summary>
    internal class ProcessorsListView : NameAndParameterListView
    {
        public ProcessorsListView(SerializedProperty property, Action applyAction, string expectedControlLayout)
            : base(property, applyAction, expectedControlLayout)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputProcessor.s_Processors;
        }

        protected override Type GetValueType(Type type)
        {
            return InputProcessor.GetValueTypeFromType(type);
        }

        protected override string itemName => "Processor";
    }

    /// <summary>
    /// A list view of interactions and their parameters.
    /// </summary>
    internal class InteractionsListView : NameAndParameterListView
    {
        public InteractionsListView(SerializedProperty property, Action applyAction, string expectedControlLayout)
            : base(property, applyAction, expectedControlLayout)
        {
        }

        protected override TypeTable GetOptions()
        {
            return InputInteraction.s_Interactions;
        }

        protected override Type GetValueType(Type type)
        {
            return InputInteraction.GetValueType(type);
        }

        protected override string itemName => "Interaction";
    }
}
#endif // UNITY_EDITOR
