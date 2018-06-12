using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEngine.Experimental.Input.Editor
{
    class PropertiesView
    {
        SerializedProperty m_BindingProperty;
        SerializedProperty m_SetProperty;

        bool m_GeneralFoldout = true;
        bool m_ProcessorsFoldout = true;
        bool m_ModifierSupport;
        ReorderableList m_InteractionsListView;
        ReorderableList m_ProcessorsListView;
        SerializedProperty m_Bindings;
        GUIContent[] m_InteractionChoices;
        GUIContent[] m_ProcessorsChoices;


        SerializedProperty m_ModifiersProperty;
        SerializedProperty m_ProcessorsProperty;
        Action m_ReloadTree;

        public PropertiesView(SerializedProperty bindingProperty, Action reloadTree)
        {
            m_InteractionChoices = InputSystem.ListInteractions().OrderBy(a=>a).Select(x => new GUIContent(x)).ToArray();
            m_ProcessorsChoices = InputSystem.ListProcessors().OrderBy(a=>a).Select(x => new GUIContent(x)).ToArray();
            
            m_BindingProperty = bindingProperty;
            m_ReloadTree = reloadTree;
            
            m_InteractionsListView = new ReorderableList(new List<string>(), typeof(string));

            m_ModifiersProperty = bindingProperty.FindPropertyRelative("interactions");
            foreach (var s in m_ModifiersProperty.stringValue.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries))
            {
                if(string.IsNullOrEmpty(s))
                    continue;
                m_InteractionsListView.list.Add(s);
            }
            
            m_InteractionsListView.drawHeaderCallback =
                (rect) => EditorGUI.LabelField(rect, "Interactions");
            
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
            
            m_ProcessorsListView = new ReorderableList(new List<string>{}, typeof(string));
            m_ProcessorsListView.drawHeaderCallback =
                (rect) => EditorGUI.LabelField(rect, "Processors");
            m_ProcessorsProperty = bindingProperty.FindPropertyRelative("processors");
            foreach (var s in m_ProcessorsProperty.stringValue.Split(new []{InputBinding.kSeparatorString}, StringSplitOptions.RemoveEmptyEntries))
            {
                m_ProcessorsListView.list.Add(s);
            }
            m_ProcessorsListView.onAddDropdownCallback =
                (rect, list) =>
                {
                    var menu = new GenericMenu();
                    for (var i = 0; i < m_ProcessorsChoices.Length; ++i)
                        menu.AddItem(m_ProcessorsChoices[i], false, AddProcessor, m_ProcessorsChoices[i].text);
                    menu.ShowAsContext();
                };
            
            m_ProcessorsListView.onRemoveCallback =
                (list) =>
                {
                    list.list.RemoveAt(list.index);
                    ApplyModifiers();
                };
            m_ProcessorsListView.onReorderCallback = list => { ApplyModifiers(); };
        }

        void AddModifier(object modifierNameString)
        {
            if (m_InteractionsListView.list.Count == 1 && m_InteractionsListView.list[0] == "")
            {
                m_InteractionsListView.list.Clear();
            }
                
            m_InteractionsListView.list.Add((string)modifierNameString);
            ApplyModifiers();
        }

        void AddProcessor(object processorNameString)
        {
            if (m_ProcessorsListView.list.Count == 1 && m_ProcessorsListView.list[0] == "")
            {
                m_ProcessorsListView.list.Clear();
            }
                
            m_ProcessorsListView.list.Add((string)processorNameString);
            ApplyModifiers();
        }

        void ApplyModifiers()
        {
            var modifiers = string.Join(",", m_InteractionsListView.list.Cast<string>().Where(s=>!string.IsNullOrEmpty(s)).Select(x => x).ToArray());
            m_ModifiersProperty.stringValue = modifiers;
            m_ModifiersProperty.serializedObject.ApplyModifiedProperties();
            var processors = string.Join(InputBinding.kSeparatorString, m_ProcessorsListView.list.Cast<string>().Where(s=>!string.IsNullOrEmpty(s)).Select(x => x).ToArray());
            m_ProcessorsProperty.stringValue = processors;
            m_ProcessorsProperty.serializedObject.ApplyModifiedProperties();
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
                m_ModifierSupport = EditorGUILayout.Toggle("Interactions support", m_ModifierSupport, EditorStyles.toggle);
                EditorGUI.EndDisabledGroup();
                
                var pathProperty = m_BindingProperty.FindPropertyRelative("path");
                var path = BindingTreeItem.ParseName(pathProperty.stringValue);
                
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
          
            m_ProcessorsFoldout = EditorGUILayout.Foldout(m_ProcessorsFoldout, "Processors");

            if (m_ProcessorsFoldout)
            {
                EditorGUI.indentLevel++;
                var listRect = GUILayoutUtility.GetRect(200, m_ProcessorsListView.GetHeight());
                listRect = EditorGUI.IndentedRect(listRect);
                m_ProcessorsListView.DoList(listRect);
                EditorGUI.indentLevel--;
            }
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.LabelField("Axis Response Curve", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Response Curve");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        void OnBindingModified(SerializedProperty obj)
        {
            var importerEditor = InputActionImporterEditor.FindFor(m_BindingProperty.serializedObject);
            if (importerEditor != null)
                importerEditor.OnAssetModified();
            m_ReloadTree();
        }
    }
}
