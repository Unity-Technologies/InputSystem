#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    public class ActionInspectorWindow : EditorWindow
    {
        static class Styles
        {
            public static GUIStyle darkGreyBackgroundWithBorder = new GUIStyle("Label");
            public static GUIStyle whiteBackgroundWithBorder = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);

            static Styles()
            {
                Initialize();
                EditorApplication.playModeStateChanged += s =>
                {
                    if (s == PlayModeStateChange.ExitingPlayMode)
                        Initialize();
                };
            }

            static void Initialize()
            {
                var darkGreyBackgroundWithBorderTexture = StyleHelpers.CreateTextureWithBorder(new Color32(181, 181, 181, 255), Color.grey);
                darkGreyBackgroundWithBorder.normal.background = darkGreyBackgroundWithBorderTexture;
                darkGreyBackgroundWithBorder.border = new RectOffset(3, 3, 3, 3);

                var whiteBackgroundWithBorderTexture = StyleHelpers.CreateTextureWithBorder(Color.white, Color.grey);
                whiteBackgroundWithBorder.normal.background = whiteBackgroundWithBorderTexture;
                whiteBackgroundWithBorder.border = new RectOffset(3, 3, 3, 3);

                columnHeaderLabel.alignment = TextAnchor.MiddleLeft;
                columnHeaderLabel.fontStyle = FontStyle.Bold;
                columnHeaderLabel.padding.left = 10;
            }
        }

        [MenuItem("Input System/Show Input Manager")]
        public static void ShowActionInspectorWindow()
        {
            var w = GetWindow<ActionInspectorWindow>("Input Manager");
            w.Show();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (path.EndsWith(".inputactions"))
            {
                var obj = EditorUtility.InstanceIDToObject(instanceID);
                var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
                var window = inputManagers.FirstOrDefault(w => w.m_ReferencedObject.Equals(obj));
                if (window != null)
                {
                    window.Show();
                    window.Focus();
                    return true;
                }
                window = CreateInstance<ActionInspectorWindow>();
                window.title = "Input Manager - " + obj.name;
                window.m_ReferencedObject = obj;
                window.Show();
                return true;
            }
            return false;
        }

        [SerializeField]
        Object m_ReferencedObject;
        [SerializeField]
        TreeViewState m_TreeViewState;
        [SerializeField]
        TreeViewState m_PickerTreeViewState;
        internal InputActionListTreeView m_TreeView;
        internal SerializedObject m_SerializedObject;
        PropertiesView m_PropertyView;
        List<string> m_GroupPopupList;
        CopyPasteUtility m_CopyPasteUtility;
        SearchField m_SearchField;
        string m_SearchText;
        int m_GroupIndex;
        [SerializeField]
        string m_AssetPath;

        public void OnEnable()
        {
            InitiateTrees();
            Undo.undoRedoPerformed += OnUndoCallback;
        }

        void OnUndoCallback()
        {
            if (m_TreeView == null)
                return;
            m_TreeView.Reload();
            m_TreeView.Repaint();
            OnSelectionChanged();
        }

        void OnSelectionChanged()
        {
            LoadPropertiesForSelection();
        }

        void LoadPropertiesForSelection()
        {
            m_PropertyView = null;
            if (m_TreeView.GetSelectedProperty() == null)
            {
                return;
            }
            var p = m_TreeView.GetSelectedRow();
            if (p.hasProperties)
            {
                m_PropertyView = new PropertiesView(p.elementProperty, Apply, ref m_PickerTreeViewState);
            }
        }

        void InitiateTrees()
        {
            if (m_SerializedObject != null)
            {
                ParseGroups(m_ReferencedObject as InputActionAsset);
                m_TreeView = InputActionListTreeView.Create(Apply, m_ReferencedObject as InputActionAsset, m_SerializedObject, ref m_TreeViewState);
                m_TreeView.OnSelectionChanged = OnSelectionChanged;
                m_TreeView.OnContextClick = OnContextClick;
                m_CopyPasteUtility = new CopyPasteUtility(this);
                m_SearchField = new SearchField();
                LoadPropertiesForSelection();
            }
        }

        void ParseGroups(InputActionAsset actionMapAsset)
        {
            HashSet<string> allGroups = new HashSet<string>();
            allGroups.Clear();
            m_GroupPopupList = new List<string>() { "<no group>" };
            foreach (var actionMap in actionMapAsset.actionMaps)
            {
                foreach (var binding in actionMap.bindings)
                {
                    if (binding.groups == null)
                        continue;

                    foreach (var group in binding.groups.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (!string.IsNullOrEmpty(@group))
                            allGroups.Add(@group);
                    }
                }
            }
            m_GroupPopupList.AddRange(allGroups);
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
            SaveChangesToAsset();
            m_SerializedObject.Update();
            m_TreeView.Reload();
            Repaint();
        }

        void SaveChangesToAsset()
        {
            var asset = (InputActionAsset)m_ReferencedObject;
            var path = AssetDatabase.GetAssetPath(asset);
            File.WriteAllText(path, asset.ToJson());
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            if (m_ReferencedObject == null && !string.IsNullOrEmpty(m_AssetPath))
            {
                m_ReferencedObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_AssetPath);
                m_SerializedObject = null;
                m_TreeView = null;
                return;
            }

            if (m_SerializedObject == null && m_ReferencedObject != null)
            {
                m_AssetPath = AssetDatabase.GetAssetPath(m_ReferencedObject);
                m_SerializedObject = new SerializedObject(m_ReferencedObject);
                var pr = m_SerializedObject.FindProperty("m_ActionMaps");
                if (pr == null)
                {
                    m_ReferencedObject = null;
                    m_SerializedObject = null;
                    return;
                }
                if (m_TreeView == null)
                {
                    InitiateTrees();
                }
            }

            if (m_ReferencedObject == null)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Group filter", GUILayout.MaxWidth(70));
            m_GroupIndex = EditorGUILayout.Popup(m_GroupIndex, m_GroupPopupList.ToArray(), GUILayout.MaxWidth(200));
            if (EditorGUI.EndChangeCheck())
            {
                var filter = m_GroupIndex > 0 ? m_GroupPopupList[m_GroupIndex] : null;
                m_TreeView.SetGroupFilter(filter);
            }

            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.SetNameFilter(m_SearchText);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            DrawMainTree();
            DrawProperties();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.ValidateCommand)
            {
                if (Event.current.commandName == "Copy"
                    || Event.current.commandName == "Paste"
                    || Event.current.commandName == "Cut"
                    || Event.current.commandName == "Duplicate"
                    || Event.current.commandName == "Delete")
                {
                    Event.current.Use();
                }
            }
            if (Event.current.type == EventType.ExecuteCommand)
            {
                switch (Event.current.commandName)
                {
                    case "Copy":
                        m_CopyPasteUtility.HandleCopyEvent();
                        Event.current.Use();
                        break;
                    case "Paste":
                        m_CopyPasteUtility.HandlePasteEvent();
                        Event.current.Use();
                        break;
                    case "Cut":
                        m_CopyPasteUtility.HandleCopyEvent();
                        DeleteSelectedRows();
                        Event.current.Use();
                        break;
                    case "Duplicate":
                        m_CopyPasteUtility.HandleCopyEvent();
                        m_CopyPasteUtility.HandlePasteEvent();
                        Event.current.Use();
                        break;
                    case "Delete":
                        DeleteSelectedRows();
                        Event.current.Use();
                        break;
                }
            }
        }

        void DeleteSelectedRows()
        {
            var rows = m_TreeView.GetSelectedRows().ToArray();
            foreach (var compositeGroup in rows.Where(r => r.GetType() == typeof(CompositeGroupTreeItem)).OrderByDescending(r => r.index).Cast<CompositeGroupTreeItem>())
            {
                var actionMapProperty = (compositeGroup.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (compositeGroup.parent as ActionTreeItem).elementProperty;
                for (var i = compositeGroup.children.Count - 1; i >= 0; i--)
                {
                    var composite = (CompositeTreeItem)compositeGroup.children[i];
                    InputActionSerializationHelpers.RemoveBinding(actionProperty, composite.index, actionMapProperty);
                }
                InputActionSerializationHelpers.RemoveBinding(actionProperty, compositeGroup.index, actionMapProperty);
            }
            foreach (var bindingRow in rows.Where(r => r.GetType() == typeof(BindingTreeItem)).OrderByDescending(r => r.index).Cast<BindingTreeItem>())
            {
                var actionMapProperty = (bindingRow.parent.parent as InputTreeViewLine).elementProperty;
                var actionProperty = (bindingRow.parent as InputTreeViewLine).elementProperty;
                InputActionSerializationHelpers.RemoveBinding(actionProperty, bindingRow.index, actionMapProperty);
            }
            foreach (var actionRow in rows.Where(r => r.GetType() == typeof(ActionTreeItem)).OrderByDescending(r => r.index).Cast<ActionTreeItem>())
            {
                var actionProperty = (actionRow).elementProperty;
                var actionMapProperty = (actionRow.parent as InputTreeViewLine).elementProperty;

                for (var i = actionRow.bindingsCount - 1; i >= 0; i--)
                    InputActionSerializationHelpers.RemoveBinding(actionProperty, i, actionMapProperty);

                InputActionSerializationHelpers.DeleteAction(actionMapProperty, actionRow.index);
            }
            foreach (var mapRow in rows.Where(r => r.GetType() == typeof(ActionMapTreeItem)).OrderByDescending(r => r.index).Cast<ActionMapTreeItem>())
            {
                InputActionSerializationHelpers.DeleteActionMap(m_SerializedObject, mapRow.index);
            }
            Apply();
            OnSelectionChanged();
        }

        void DrawMainTree()
        {
            EditorGUILayout.BeginVertical(Styles.darkGreyBackgroundWithBorder);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            var treeViewRect = GUILayoutUtility.GetLastRect();
            var labelRect = new Rect(treeViewRect);
            labelRect.height = 20;
            treeViewRect.y += 20;
            treeViewRect.height -= 20;
            treeViewRect.x += 1;
            treeViewRect.width -= 2;

            var header = "Action maps";
            if (!string.IsNullOrEmpty(m_SearchText))
                header += " (Searching)";

            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.darkGreyBackgroundWithBorder);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, header, Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - 18;
            labelRect.width = 18;
            var plusIconContext = EditorGUIUtility.IconContent("Toolbar Plus");
            if (GUI.Button(labelRect, plusIconContext, GUIStyle.none))
            {
                ShowAddMenu();
            }

            m_TreeView.OnGUI(treeViewRect);
        }

        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, false);
            menu.ShowAsContext();
        }

        void AddAddOptionsToMenu(GenericMenu menu, bool includeAddPrefix)
        {
            var hasSelection = m_TreeView.HasSelection();
            var canAddBinding = false;
            var row = m_TreeView.GetSelectedAction();
            if (row != null && hasSelection)
            {
                canAddBinding = true;
            }

            var canAddAction = false;
            var action = m_TreeView.GetSelectedActionMap();
            if (action != null && hasSelection)
            {
                canAddAction = true;
            }

            var actionString = includeAddPrefix ? "Add action" : "Action";
            if (canAddAction)
            {
                menu.AddItem(new GUIContent(actionString), false, OnAddAction);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(actionString), false);
            }

            var actionMapString = includeAddPrefix ? "Add action map" : "Action map";
            menu.AddItem(new GUIContent(actionMapString), false, OnAddActionMap);
            menu.AddSeparator("");
            var bindingString = includeAddPrefix ? "Add binding" : "Binding";
            var compositeString = includeAddPrefix ? "Add composite binding" : "Composite binding";
            if (canAddBinding)
            {
                menu.AddItem(new GUIContent(bindingString), false, OnAddBinding);
                menu.AddItem(new GUIContent(compositeString + "/2 dimensions"), false, OnAddCompositeBinding, 2);
                menu.AddItem(new GUIContent(compositeString + "/4 dimensions"), false, OnAddCompositeBinding, 4);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(bindingString), false);
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        void OnContextClick()
        {
            var canCopySelection = m_CopyPasteUtility.CanCopySelection();
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, true);
            menu.AddSeparator("");
            if (canCopySelection)
            {
                menu.AddItem(new GUIContent("Cut"), false, () => EditorApplication.ExecuteMenuItem("Edit/Cut"));
                menu.AddItem(new GUIContent("Copy"), false, () => EditorApplication.ExecuteMenuItem("Edit/Copy"));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Cut"), false);
                menu.AddDisabledItem(new GUIContent("Copy"), false);
            }
            menu.AddItem(new GUIContent("Paste"), false, () => EditorApplication.ExecuteMenuItem("Edit/Paste"));
            menu.AddItem(new GUIContent("Delete"), false, () => EditorApplication.ExecuteMenuItem("Edit/Delete"));
            if (canCopySelection)
            {
                menu.AddItem(new GUIContent("Duplicate"), false, () => EditorApplication.ExecuteMenuItem("Edit/Duplicate"));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Duplicate"), false);
            }
            menu.ShowAsContext();
        }

        void OnAddCompositeBinding(object dimensionNumber)
        {
            var actionMapLine = GetSelectedActionMapLine();
            var actionLine = GetSelectedActionLine();
            InputActionSerializationHelpers.AppendCompositeBinding(actionLine.elementProperty, actionMapLine.elementProperty, (int)dimensionNumber);
            Apply();
        }

        void OnAddBinding()
        {
            var actionMapLine = GetSelectedActionMapLine();
            var actionLine = GetSelectedActionLine();
            InputActionSerializationHelpers.AppendBinding(actionLine.elementProperty, actionMapLine.elementProperty);
            Apply();
        }

        void OnAddAction()
        {
            var actionLine = GetSelectedActionMapLine();
            InputActionSerializationHelpers.AddAction(actionLine.elementProperty);
            Apply();
        }

        void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_SerializedObject);
            Apply();
        }

        ActionTreeItem GetSelectedActionLine()
        {
            TreeViewItem selectedRow = m_TreeView.GetSelectedRow();
            do
            {
                if (selectedRow is ActionTreeItem)
                    return (ActionTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }

        ActionMapTreeItem GetSelectedActionMapLine()
        {
            TreeViewItem selectedRow = m_TreeView.GetSelectedRow();
            do
            {
                if (selectedRow is ActionMapTreeItem)
                    return (ActionMapTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }

        void DrawProperties()
        {
            EditorGUILayout.BeginVertical(Styles.darkGreyBackgroundWithBorder, GUILayout.Width(position.width / 2));

            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            rect.x -= 2;
            rect.y -= 1;
            rect.width += 4;

            EditorGUI.LabelField(rect, GUIContent.none, Styles.darkGreyBackgroundWithBorder);
            var headerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            EditorGUI.LabelField(headerRect, "Properties", Styles.columnHeaderLabel);

            if (m_PropertyView != null)
                m_PropertyView.OnGUI();

            EditorGUILayout.EndVertical();
        }
    }
}
#endif // UNITY_EDITOR
