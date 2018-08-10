#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class ActionInspectorWindow : EditorWindow
    {
        static class Styles
        {
            public static GUIStyle actionTreeBackground = new GUIStyle("Label");
            public static GUIStyle propertiesBackground = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);

            static string ResourcesPath
            {
                get
                {
                    var path = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
                    if (EditorGUIUtility.isProSkin)
                        return path + "pro/";
                    return path + "personal/";
                }
            }

            static Styles()
            {
                actionTreeBackground.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "actionTreeBackground.png");
                actionTreeBackground.border = new RectOffset(3, 3, 3, 3);

                propertiesBackground.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "propertiesBackground.png");
                propertiesBackground.border = new RectOffset(3, 3, 3, 3);

                columnHeaderLabel.alignment = TextAnchor.MiddleLeft;
                columnHeaderLabel.fontStyle = FontStyle.Bold;
                columnHeaderLabel.padding.left = 10;
            }
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            if (path.EndsWith(k_FileExtension))
            {
                var obj = EditorUtility.InstanceIDToObject(instanceId);
                var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
                var window = inputManagers.FirstOrDefault(w => w.m_ReferencedObject.Equals(obj));
                if (window != null)
                {
                    window.Show();
                    window.Focus();
                    return true;
                }
                window = CreateInstance<ActionInspectorWindow>();
                window.titleContent = new GUIContent(obj.name + " (Input Manager)");
                window.SetReferencedObject(obj);
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

        InputActionListTreeView m_TreeView;
        SerializedObject m_SerializedObject;
        InputBindingPropertiesView m_PropertyView;
        CopyPasteUtility m_CopyPasteUtility;
        SearchField m_SearchField;
        string m_SearchText;
        const string k_FileExtension = ".inputactions";

        GUIContent m_AddBindingGUI = EditorGUIUtility.TrTextContent("Binding");
        GUIContent m_AddBindingContextGUI = EditorGUIUtility.TrTextContent("Add binding");
        GUIContent m_AddActionGUI = EditorGUIUtility.TrTextContent("Action");
        GUIContent m_AddActionContextGUI = EditorGUIUtility.TrTextContent("Add action");
        GUIContent m_AddActionMapGUI = EditorGUIUtility.TrTextContent("Action map");
        GUIContent m_AddActionMapContextGUI = EditorGUIUtility.TrTextContent("Add action map");


        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoCallback;
            if (m_ReferencedObject == null)
                return;
            m_SerializedObject = new SerializedObject(m_ReferencedObject);
            InitializeTrees();
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoCallback;
        }

        void SetReferencedObject(Object referencedObject)
        {
            m_ReferencedObject = referencedObject;
            m_SerializedObject = new SerializedObject(referencedObject);
            InitializeTrees();
        }

        void OnUndoRedoCallback()
        {
            if (m_TreeView == null)
                return;
            m_TreeView.Reload();
            OnSelectionChanged();
            SaveChangesToAsset();
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
                m_PropertyView = p.GetPropertiesView(Apply, m_PickerTreeViewState);
            }
        }

        void InitializeTrees()
        {
            if (m_SerializedObject != null)
            {
                m_SearchField = new SearchField();
                m_TreeView = InputActionListTreeView.CreateFromSerializedObject(Apply, m_SerializedObject, ref m_TreeViewState);
                m_TreeView.OnSelectionChanged = OnSelectionChanged;
                m_TreeView.OnContextClick = OnContextClick;
                m_CopyPasteUtility = new CopyPasteUtility(Apply, m_TreeView, m_SerializedObject);
                if (m_PickerTreeViewState == null)
                    m_PickerTreeViewState = new TreeViewState();
                LoadPropertiesForSelection();
            }
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
            SaveChangesToAsset();
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
                if (!importedAssets.Any(s => s.EndsWith(k_FileExtension)))
                    return;
                var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
                foreach (var inputWindow in inputManagers)
                {
                    inputWindow.Repaint();
                }
            }
        }

        void OnGUI()
        {
            if (m_SerializedObject == null)
                return;

            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

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
            EditorGUILayout.BeginVertical(Styles.actionTreeBackground);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            var treeViewRect = GUILayoutUtility.GetLastRect();
            var labelRect = new Rect(treeViewRect);
            labelRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            treeViewRect.y += labelRect.height;
            treeViewRect.height -= labelRect.height;
            treeViewRect.x += 1;
            treeViewRect.width -= 2;

            GUIContent header;
            if (string.IsNullOrEmpty(m_SearchText))
                header = EditorGUIUtility.TrTextContent("Action maps");
            else
                header = EditorGUIUtility.TrTextContent("Action maps (Searching)");

            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, header, Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
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
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(m_AddBindingGUI);
            }
            if (canAddAction)
            {
                menu.AddItem(isContextMenu ? m_AddActionContextGUI : m_AddActionGUI, false, OnAddAction);
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(m_AddActionGUI, false);
            }
            menu.AddItem(isContextMenu ?  m_AddActionMapContextGUI : m_AddActionMapGUI, false, OnAddActionMap);

            var compositeString = isContextMenu ? EditorGUIUtility.TrTextContent("Add composite") : EditorGUIUtility.TrTextContent("Composite");
            if (canAddBinding)
            {
                foreach (var composite in InputBindingComposite.s_Composites.names)
                {
                    menu.AddItem(new GUIContent(compositeString.text + "/" + composite), false, OnAddCompositeBinding, composite);
                }
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        void OnContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, true);
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        void OnAddCompositeBinding(object compositeName)
        {
            var actionLine = GetSelectedActionLine();
            actionLine.AppendCompositeBinding((string)compositeName);
            Apply();
        }

        void OnAddBinding()
        {
            var actionLine = GetSelectedActionLine();
            actionLine.AppendBinding();
            Apply();
        }

        void OnAddAction()
        {
            var actionMapLine = GetSelectedActionMapLine();
            actionMapLine.AddAction();
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
            EditorGUILayout.BeginVertical(Styles.propertiesBackground, GUILayout.Width(position.width / 2));

            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2, GUILayout.ExpandWidth(true));
            rect.x -= 2;
            rect.y -= 1;
            rect.width += 4;

            EditorGUI.LabelField(rect, GUIContent.none, Styles.propertiesBackground);
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
