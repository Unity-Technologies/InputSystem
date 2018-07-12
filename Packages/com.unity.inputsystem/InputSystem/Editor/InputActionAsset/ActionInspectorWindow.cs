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
                window.title = obj.name + " (Input Manager)";
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
        [SerializeField]
        string m_AssetPath;
        
        InputActionListTreeView m_TreeView;
        SerializedObject m_SerializedObject;
        PropertiesView m_PropertyView;
        CopyPasteUtility m_CopyPasteUtility;
        SearchField m_SearchField;
        string m_SearchText;
        int m_IsAssetDirtyCounter;
        
        GUIContent m_AddBindingGUI = new GUIContent("Binding");
        GUIContent m_AddBindingContextGUI = new GUIContent("Add binding");
        GUIContent m_AddActionGUI = new GUIContent("Action");
        GUIContent m_AddActionContextGUI = new GUIContent("Add action");
        GUIContent m_AddActionMapGUI = new GUIContent("Action map");
        GUIContent m_AddActionMapContextGUI = new GUIContent("Add action map");

        public void OnEnable()
        {
            InitiateTrees();
            Undo.undoRedoPerformed += OnUndoCallback;
        }

        void OnUndoCallback()
        {
            if (m_TreeView == null)
                return;
            m_IsAssetDirtyCounter--;
            m_TreeView.Reload();
            OnSelectionChanged();
        }

        internal void OnSelectionChanged()
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
                m_CopyPasteUtility = new CopyPasteUtility(this, m_TreeView, m_SerializedObject);
                m_SearchField = new SearchField();
                m_TreeView = InputActionListTreeView.Create(Apply, m_ReferencedObject as InputActionAsset, m_SerializedObject, ref m_TreeViewState);
                m_TreeView.OnSelectionChanged = OnSelectionChanged;
                m_TreeView.OnContextClick = OnContextClick;
                LoadPropertiesForSelection();
            }
        }

        internal void Apply()
        {
            if (m_IsAssetDirtyCounter < 0)
            {
                m_IsAssetDirtyCounter = 0;
            }
            m_IsAssetDirtyCounter++;
            m_SerializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
        }

        void SaveChangesToAsset()
        {
            var asset = (InputActionAsset)m_ReferencedObject;
            var path = AssetDatabase.GetAssetPath(asset);
            File.WriteAllText(path, asset.ToJson());
        }
        
        class AssetChangeWatch : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
                foreach (var inputWindow in inputManagers)
                {
                    inputWindow.Repaint();
                }
            }
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
                return;
            }

            if (m_ReferencedObject == null)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(m_IsAssetDirtyCounter == 0);
            if (GUILayout.Button("Save"))
            {
                m_IsAssetDirtyCounter = 0;
                SaveChangesToAsset();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();
            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.SetNameFilter(m_SearchText);
            }
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            DrawMainTree();
            DrawProperties();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.ValidateCommand)
            {
                if (m_CopyPasteUtility.IsValidCommand(Event.current.commandName))
                {
                    Event.current.Use();
                }
            }
            if (Event.current.type == EventType.ExecuteCommand)
            {
                m_CopyPasteUtility.HandleCommandEvent(Event.current.commandName);
            }
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

        void AddAddOptionsToMenu(GenericMenu menu, bool isContextMenu)
        {
            var hasSelection = m_TreeView.HasSelection();
            var canAddBinding = false;
            var action = m_TreeView.GetSelectedAction();
            if (action != null && hasSelection)
            {
                canAddBinding = true;
            }
            var canAddAction = false;
            var actionMap = m_TreeView.GetSelectedActionMap();
            if (actionMap != null && hasSelection)
            {
                canAddAction = true;
            }
            if (canAddBinding)
            {
                menu.AddItem(isContextMenu ? m_AddBindingContextGUI : m_AddBindingGUI, false, OnAddBinding);
            }
            else if(!isContextMenu)
            {
                menu.AddDisabledItem(m_AddBindingGUI);
            }
            if (canAddAction)
            {
                menu.AddItem(isContextMenu ? m_AddActionContextGUI : m_AddActionGUI, false, OnAddAction);
            }
            else if(!isContextMenu)
            {
                menu.AddDisabledItem(m_AddActionGUI, false);
            }
            menu.AddItem(isContextMenu ?  m_AddActionMapContextGUI : m_AddActionMapGUI, false, OnAddActionMap);
            
            var compositeString = isContextMenu ? "Add composite" : "Composite";
            if (canAddBinding)
            {
                foreach (var composite in InputBindingComposite.s_Composites.names)
                {
                    menu.AddItem(new GUIContent(compositeString + "/" + composite + " composite"), false, OnAddCompositeBinding, composite);
                }
            }
            else if(!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        void OnContextClick()
        {
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, true);
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        void OnAddCompositeBinding(object compositeName)
        {
            var actionMapLine = GetSelectedActionMapLine();
            var actionLine = GetSelectedActionLine();
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration((string) compositeName);
            InputActionSerializationHelpers.AppendCompositeBinding(actionLine.elementProperty, actionMapLine.elementProperty, compositeType);
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
            {
                m_PropertyView.OnGUI();
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif // UNITY_EDITOR
