#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor.Lists
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
            m_Property = property;
            m_Apply = applyAction;
            m_ListItems = new List<string>();
            m_ListOptions = GetOptions();
            m_ParametersForEachListItem = NameAndParameters.ParseMultiple(m_Property.stringValue).ToArray();
            m_EditableParametersForEachListItem = new ParameterListView[m_ParametersForEachListItem.Length];
            for (int i = 0; i < m_ParametersForEachListItem.Length; i++)
            {
                m_EditableParametersForEachListItem[i] = new ParameterListView{ onChange = OnParametersChanged };
                var typeName = m_ParametersForEachListItem[i].name;
                var rowType = m_ListOptions.LookupTypeRegistration(typeName);
                m_EditableParametersForEachListItem[i].Initialize(rowType, m_ParametersForEachListItem[i].parameters);

            }

            m_ExpectedControlLayout = expectedControlLayout;
            if (!string.IsNullOrEmpty(m_ExpectedControlLayout))
                m_ExpectedValueType = EditorInputControlLayoutCache.GetValueType(m_ExpectedControlLayout);

            foreach (var nameAndParams in m_ParametersForEachListItem)
            {
                var name = ObjectNames.NicifyVariableName(nameAndParams.name);

                ////REVIEW: finding this kind of stuff should probably have better support globally on the asset; e.g. some
                ////        notification that pops up and allows fixing all occurrences in one click
                // Find out if we still support this option and indicate it in the list, if we don't.
                var type = m_ListOptions.LookupTypeRegistration(new InternedString(nameAndParams.name));
                if (type == null)
                    name += " (Obsolete)";
                else if (m_ExpectedValueType != null)
                {
                    var valueType = GetValueType(type);
                    if (!m_ExpectedValueType.IsAssignableFrom(valueType))
                        name += " (Ignored)";
                }

                m_ListItems.Add(name);
            }
        }

        protected abstract TypeTable GetOptions();
        protected abstract Type GetValueType(Type type);

        public void OnAddDropdown(Rect r)
        {
            // Add only original names to the menu and not aliases.
            var menu = new GenericMenu();
            foreach (var name in m_ListOptions.internedNames.Where(x => !m_ListOptions.aliases.Contains(x)).OrderBy(x => x.ToString()))
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

            m_ListItems.Add(ObjectNames.NicifyVariableName(name));
            ArrayHelpers.Append(ref m_ParametersForEachListItem,
                new NameAndParameters {name = name});
            ArrayHelpers.Append(ref m_EditableParametersForEachListItem,
                new ParameterListView { onChange = OnParametersChanged });

            var index = m_EditableParametersForEachListItem.Length - 1;
            var typeName = m_ParametersForEachListItem[index].name;
            var rowType = m_ListOptions.LookupTypeRegistration(typeName);
            m_EditableParametersForEachListItem[index].Initialize(rowType, m_ParametersForEachListItem[index].parameters);

            m_Apply();
        }

        private void OnParametersChanged()
        {
            for (int i = 0; i < m_ParametersForEachListItem.Length; i++)
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

            static Styles()
            {
                s_FoldoutStyle.fontStyle = FontStyle.Bold;
            }
        }

        public void OnGUI()
        {
            if (m_EditableParametersForEachListItem == null || m_EditableParametersForEachListItem.Length == 0)
            {
                using (var scope = new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"No {itemName}s have been added.");
                    EditorGUI.indentLevel--;
                }
            }
            else for (var i = 0; i < m_EditableParametersForEachListItem.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                m_EditableParametersForEachListItem[i].visible = EditorGUILayout.Foldout(m_EditableParametersForEachListItem[i].visible, ObjectNames.NicifyVariableName(m_ParametersForEachListItem[i].name), Styles.s_FoldoutStyle);//, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                using (var scope = new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("NodeChevronUp"), EditorStyles.label))
                    {
                        MemoryHelpers.Swap(ref m_ParametersForEachListItem[i], ref m_ParametersForEachListItem[i - 1]);
                        MemoryHelpers.Swap(ref m_EditableParametersForEachListItem[i], ref m_EditableParametersForEachListItem[i - 1]);
                        m_Apply();
                    }
                }
                using (var scope = new EditorGUI.DisabledScope(i == m_EditableParametersForEachListItem.Length - 1))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("NodeChevronDown"), EditorStyles.label))
                    {
                        MemoryHelpers.Swap(ref m_ParametersForEachListItem[i], ref m_ParametersForEachListItem[i + 1]);
                        MemoryHelpers.Swap(ref m_EditableParametersForEachListItem[i], ref m_EditableParametersForEachListItem[i + 1]);
                        m_Apply();
                    }
                }
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("Toolbar Minus", $"Delete {itemName}"), EditorStyles.label))
                {
                    ArrayHelpers.EraseAt(ref m_ParametersForEachListItem, i);
                    ArrayHelpers.EraseAt(ref m_EditableParametersForEachListItem, i);
                    m_Apply();
                }
                EditorGUILayout.EndHorizontal();
                if (m_EditableParametersForEachListItem[i].visible)
                {
                    EditorGUI.indentLevel++;
                    m_EditableParametersForEachListItem[i].OnGUI();
                    EditorGUI.indentLevel--;
                }
                AdvancedDropdownGUI.DrawLineSeparator(null);
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
        private readonly List<string> m_ListItems;
        private SerializedProperty m_Property;
        private TypeTable m_ListOptions;
        private string m_ExpectedControlLayout;
        private Type m_ExpectedValueType;

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
