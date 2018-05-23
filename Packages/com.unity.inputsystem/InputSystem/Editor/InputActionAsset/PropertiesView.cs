using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class PropertiesView
    {
        private SerializedProperty m_BindingProperty;
        private SerializedProperty m_SetProperty;

        private bool m_GeneralFoldout = true;
        private bool m_ProcessorsFoldout = true;
        private bool m_ModifierSupport;
        private ReorderableList m_InteractionsListView;
        private ReorderableList m_ProcessorsList;
        private SerializedProperty m_Bindings;
        private GUIContent[] m_InteractionChoices;

        private SerializedProperty m_ModifiersProperty;
        private Action m_ReloadTree;

        public PropertiesView(SerializedProperty bindingProperty, Action reloadTree)
        {
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            
            m_InteractionsListView = new ReorderableList(new List<string>(), typeof(string));

            m_ModifiersProperty = bindingProperty.FindPropertyRelative("modifiers");
            foreach (var s in m_ModifiersProperty.stringValue.Split(','))
            {
                if(string.IsNullOrEmpty(s))
                    continue;
                m_InteractionsListView.list.Add(s);
            }
            
            m_InteractionsListView.drawHeaderCallback =
                (rect) => EditorGUI.LabelField(rect, "Interactions");
            
            var interactionOptions = InputSystem.ListBindingModifiers().ToList();
            interactionOptions.Sort();
            m_InteractionChoices = interactionOptions.Select(x => new GUIContent(x)).ToArray();
            m_InteractionsListView.onAddDropdownCallback =
                (rect, list) =>
                {
                    var menu = new GenericMenu();
                    for (var i = 0; i < m_InteractionChoices.Length; ++i)
                        menu.AddItem(m_InteractionChoices[i], false, AddModifier, m_InteractionChoices[i].text);
                    menu.ShowAsContext();
                };

            m_InteractionsListView.onRemoveCallback =
                (list) =>
                {
                    list.list.RemoveAt(list.index);
                    ApplyModifiers();
                };
            m_InteractionsListView.onReorderCallback = list => { ApplyModifiers(); };
            m_ProcessorsList = new ReorderableList(new List<string>{}, typeof(string));
        }
        
        private void AddModifier(object modifierNameString)
        {
            if (m_InteractionsListView.list.Count == 1 && m_InteractionsListView.list[0] == "")
            {
                m_InteractionsListView.list.Clear();
            }
                
            m_InteractionsListView.list.Add((string)modifierNameString);
            ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            var modifiers = string.Join(",", m_InteractionsListView.list.Cast<string>().Where(s=>!string.IsNullOrEmpty(s)).Select(x => x).ToArray());
            m_ModifiersProperty.stringValue = modifiers;
            m_ModifiersProperty.serializedObject.ApplyModifiedProperties();
            m_ReloadTree();
        }

        public void OnGUI()
        {
            if (m_BindingProperty == null)
                return;

            EditorGUILayout.BeginVertical();
            
            m_GeneralFoldout = EditorGUILayout.Foldout(m_GeneralFoldout, "General");

            if (m_GeneralFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                m_ModifierSupport = EditorGUILayout.Toggle("Modifier support", m_ModifierSupport, EditorStyles.toggle);
                EditorGUI.EndDisabledGroup();
                
                var pathProperty = m_BindingProperty.FindPropertyRelative("path");
                var path = InputActionListTreeView.BindingItem.ParseName(pathProperty.stringValue);
                
                var btnRect = GUILayoutUtility.GetRect(0, EditorStyles.miniButton.lineHeight);
                btnRect = EditorGUI.IndentedRect(btnRect);
                if (EditorGUI.DropdownButton(btnRect, new GUIContent(path), FocusType.Keyboard))
                {
                    PopupWindow.Show(btnRect,
                        new InputControlPicker(pathProperty) {onPickCallback = OnBindingModified});
                }
                
                EditorGUILayout.Space();
                
                var listRect = GUILayoutUtility.GetRect(200, m_InteractionsListView.GetHeight());
                listRect = EditorGUI.IndentedRect(listRect);
                m_InteractionsListView.DoList(listRect);
                
                EditorGUI.indentLevel--;
            }
          
            EditorGUI.BeginDisabledGroup(true);
            m_ProcessorsFoldout = EditorGUILayout.Foldout(m_ProcessorsFoldout, "Processors");

            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                var listRect = GUILayoutUtility.GetRect(200, m_ProcessorsList.GetHeight());
                listRect = EditorGUI.IndentedRect(listRect);
                m_ProcessorsList.DoList(listRect);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.LabelField("Axis Response Curve", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Response Curve");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void OnBindingModified(SerializedProperty obj)
        {
            var importerEditor = InputActionImporterEditor.FindFor(m_BindingProperty.serializedObject);
            if (importerEditor != null)
                importerEditor.OnAssetModified();
            m_ReloadTree();
        }
    }
}
