#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class TwoColumnAssetInspectorWindow : EditorWindow
    {
        public static class Styles
        {
            public static GUIStyle actionTreeBackground = new GUIStyle("Label");
            public static GUIStyle propertiesBackground = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);

            ////TODO: move to a better place
            public static string SharedResourcesPath = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
            public static string ResourcesPath
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                        return SharedResourcesPath + "pro/";
                    return SharedResourcesPath + "personal/";
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
            if (!path.EndsWith(k_FileExtension))
                return false;

            var obj = EditorUtility.InstanceIDToObject(instanceId) as InputActionAsset;
            if (obj == null)
                return false;

            // See if we have an existing editor window that has the asset open.
            var inputManagers = Resources.FindObjectsOfTypeAll<TwoColumnAssetInspectorWindow>();
            var window = inputManagers.FirstOrDefault(w => w.m_ActionAssetManager.ImportedAssetObjectEquals(obj));
            if (window != null)
            {
                window.Show();
                window.Focus();
                return true;
            }

            // No, so create a new window.
            window = CreateInstance<TwoColumnAssetInspectorWindow>();
            window.titleContent = new GUIContent(obj.name + " (Input Manager)");
            window.SetAsset(obj);
            window.Show();

            return true;
        }

        public static void RefreshAll()
        {
            if (s_RefreshPending)
                return;

            // We don't want to refresh right away but rather wait for the next editor update
            // to then do one pass of refreshing action editor windows.
            EditorApplication.delayCall += RefreshAllInternal;
            s_RefreshPending = true;
        }

        private static void RefreshAllInternal()
        {
            var windows = Resources.FindObjectsOfTypeAll<TwoColumnAssetInspectorWindow>();
            foreach (var window in windows)
                window.Refresh();

            // When the asset is modified outside of the editor
            // and the importer settings are visible in the inspector
            // the asset references in the importer inspector need to be force rebuild
            // (otherwise we gets lots of exceptions)
            ActiveEditorTracker.sharedTracker.ForceRebuild();

            s_RefreshPending = false;
        }

        private static bool s_RefreshPending;

        [SerializeField] private TreeViewState m_ActionMapsTreeState;
        [SerializeField] private TreeViewState m_ActionsTreeState;
        [SerializeField] private TreeViewState m_PickerTreeViewState;

        private ActionMapsTree m_ActionMapsTree;
        private ActionsTree m_ActionsTree;

        private InputBindingPropertiesView m_PropertyView;
        private CopyPasteUtility m_CopyPasteUtility;
        private SearchField m_SearchField;
        private string m_SearchText;

        private const string k_FileExtension = ".inputactions";

        private readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save");
        private readonly GUIContent m_AddBindingGUI = EditorGUIUtility.TrTextContent("Binding");
        private readonly GUIContent m_AddBindingContextGUI = EditorGUIUtility.TrTextContent("Add/Binding");
        private readonly GUIContent m_AddActionGUI = EditorGUIUtility.TrTextContent("Action");
        private readonly GUIContent m_AddActionContextGUI = EditorGUIUtility.TrTextContent("Add/Action");
        private readonly GUIContent m_AddActionMapGUI = EditorGUIUtility.TrTextContent("Action map");
        private readonly GUIContent m_AddActionMapContextGUI = EditorGUIUtility.TrTextContent("Add Action map");

        [SerializeField]
        InputActionAssetManager m_ActionAssetManager;
        [SerializeField]
        ControlSchemesToolbar m_ControlSchemesToolbar;

        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoCallback;
            if (m_ActionAssetManager == null)
            {
                return;
            }
            // Initialize after assembly reload
            m_ActionAssetManager.InitializeObjectReferences();
            InitializeTrees();
            OnActionMapSelection();
            LoadPropertiesForSelection(false);
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoCallback;
        }

        void OnDestroy()
        {
            if (m_ActionAssetManager.dirty)
            {
                var result = EditorUtility.DisplayDialogComplex("Unsaved changes", "Do you want to save the changes you made before quitting?", "Save", "Cancel", "Don't Save");
                switch (result)
                {
                    case 0:
                        // Save
                        m_ActionAssetManager.SaveChangesToAsset();
                        break;
                    case 1:
                        // Cancel
                        Instantiate(this).Show();
                        break;
                    case 2:
                        // Don't save
                        break;
                }
            }
        }

        // Set asset would usually only be called when the window is open
        private void SetAsset(InputActionAsset referencedObject)
        {
            m_ActionAssetManager = new InputActionAssetManager(referencedObject);
            m_ActionAssetManager.InitializeObjectReferences();
            m_ControlSchemesToolbar = new ControlSchemesToolbar(m_ActionAssetManager);
            InitializeTrees();

            // Make sure first actions map selected and actions tree expanded
            m_ActionMapsTree.SelectFirstRow();
            OnActionMapSelection();
            m_ActionsTree.ExpandAll();
            LoadPropertiesForSelection(true);
        }

        private void InitializeTrees()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();
            m_ActionMapsTree = ActionMapsTree.CreateFromSerializedObject(Apply, m_ActionAssetManager.serializedObject, ref m_ActionMapsTreeState);
            m_ActionMapsTree.OnSelectionChanged = OnActionMapSelection;
            m_ActionMapsTree.OnContextClick = OnActionMapContextClick;

            m_ActionsTree = ActionsTree.CreateFromSerializedObject(Apply, ref m_ActionsTreeState);
            m_ActionsTree.OnSelectionChanged = OnActionSelection;
            m_ActionsTree.OnContextClick = OnActionsContextClick;

            m_CopyPasteUtility = new CopyPasteUtility(Apply, m_ActionMapsTree, m_ActionsTree, m_ActionAssetManager.serializedObject);
            if (m_PickerTreeViewState == null)
                m_PickerTreeViewState = new TreeViewState();
        }

        private void OnUndoRedoCallback()
        {
            if (m_ActionMapsTree == null)
                return;

            m_ActionAssetManager.LoadImportedObjectFromGuid();

            // Since the Undo.undoRedoPerformed callback is global, the callback will be called for any undo/redo action
            // We need to make sure we dirty the state only in case of changes to the asset.
            if (m_ActionAssetManager.IsEditingAssetDifferent())
                m_ActionAssetManager.SetAssetDirty();

            m_ActionMapsTree.Reload();
            OnActionMapSelection();
        }

        private void OnActionMapSelection()
        {
            m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedRow().elementProperty;
            m_ActionsTree.Reload();
        }

        private void OnActionSelection()
        {
            LoadPropertiesForSelection(true);
        }

        private void LoadPropertiesForSelection(bool checkFocus)
        {
            m_PropertyView = null;

            if ((!checkFocus || m_ActionMapsTree.HasFocus()) && m_ActionMapsTree.GetSelectedRow() != null)
            {
                var row = m_ActionMapsTree.GetSelectedRow();
                if (row != null)
                {
                    m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedRow().elementProperty;
                    m_ActionsTree.Reload();
                }
            }
            if ((!checkFocus || m_ActionsTree.HasFocus()) && m_ActionsTree.HasSelection() && m_ActionsTree.GetSelection().Count == 1)
            {
                var p = m_ActionsTree.GetSelectedRow();
                if (p.hasProperties)
                {
                    m_PropertyView = p.GetPropertiesView(Apply, m_PickerTreeViewState);
                }
            }
        }

        private void Apply()
        {
            m_ActionAssetManager.SetAssetDirty();
            m_ActionAssetManager.ApplyChanges();
            m_ActionMapsTree.Reload();
            var selectedActionMap = m_ActionMapsTree.GetSelectedActionMap();
            if (selectedActionMap != null)
            {
                m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedActionMap().elementProperty;
            }
            m_ActionsTree.Reload();
            OnActionSelection();
        }

        private void Refresh()
        {
            // See if the data has actually changed.
            if (m_ActionAssetManager.IsEditedAssetDifferent())
            {
                // Still need to refresh reference to imported object in case we had a re-import.
                if (m_ActionAssetManager.IsAssetReferenceValid())
                    m_ActionAssetManager.LoadImportedObjectFromGuid();

                return;
            }

            // Perform a full refresh.
            m_ActionAssetManager.InitializeObjectReferences();
            InitializeTrees();
            LoadPropertiesForSelection(true);
            Repaint();
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (m_ActionMapsTree.HasFocus() && Event.current.keyCode == KeyCode.RightArrow)
                {
                    if (!m_ActionsTree.HasSelection())
                        m_ActionsTree.SelectFirstRow();
                    m_ActionsTree.SetFocus();
                }
            }

            EditorGUILayout.BeginVertical();
            // Toolbar.
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawToolbar();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            //Draw columns
            EditorGUILayout.BeginHorizontal();
            var columnOneRect = GUILayoutUtility.GetRect(0, 0, 0, 0, Styles.actionTreeBackground, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            var columnTwoRect = GUILayoutUtility.GetRect(0, 0, 0, 0, Styles.actionTreeBackground, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            DrawActionMapsColumn(columnOneRect);
            DrawActionsColumn(columnTwoRect);
            DrawPropertiesColumn();
            EditorGUILayout.EndHorizontal();

            // Bottom margin
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.ValidateCommand)
            {
                if (CopyPasteUtility.IsValidCommand(Event.current.commandName))
                {
                    Event.current.Use();
                }
            }
            if (Event.current.type == EventType.ExecuteCommand)
            {
                m_CopyPasteUtility.HandleCommandEvent(Event.current.commandName);
            }
        }

        void DrawToolbar()
        {
            m_ControlSchemesToolbar.OnGUI();
            EditorGUI.BeginDisabledGroup(!m_ActionAssetManager.dirty);
            if (GUILayout.Button(m_SaveAssetGUI, EditorStyles.toolbarButton))
                m_ActionAssetManager.SaveChangesToAsset();
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginChangeCheck();

            m_SearchText = m_SearchField.OnToolbarGUI(m_SearchText, GUILayout.MaxWidth(250));
            if (EditorGUI.EndChangeCheck())
            {
//                m_TreeView.SetNameFilter(m_SearchText);
            }
        }

        void DrawActionMapsColumn(Rect columnRect)
        {
            var labelRect = new Rect(columnRect);
            labelRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            columnRect.y += labelRect.height;
            columnRect.height -= labelRect.height;

            // Draw header
            var header = EditorGUIUtility.TrTextContent("Action maps");
            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, header, Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var plusIconContext = EditorGUIUtility.IconContent("Toolbar Plus");
            if (GUI.Button(labelRect, plusIconContext, GUIStyle.none))
            {
                ShowAddActionMapMenu();
            }

            // Draw border rect
            EditorGUI.LabelField(columnRect, GUIContent.none, Styles.propertiesBackground);
            // Compensate for the border rect
            columnRect.x += 1;
            columnRect.height -= 1;
            columnRect.width -= 2;
            m_ActionMapsTree.OnGUI(columnRect);
        }

        void DrawActionsColumn(Rect columnRect)
        {
            var labelRect = new Rect(columnRect);
            labelRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            columnRect.y += labelRect.height;
            columnRect.height -= labelRect.height;

            GUIContent header;
            if (string.IsNullOrEmpty(m_SearchText))
                header = EditorGUIUtility.TrTextContent("Actions");
            else
                header = EditorGUIUtility.TrTextContent("Actions (Searching)");

            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, header, Styles.columnHeaderLabel);

            labelRect.x = labelRect.x + labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (GUI.Button(labelRect, EditorGUIUtility.IconContent("Toolbar Plus"), GUIStyle.none))
            {
                ShowAddActionsMenu();
            }

            // Draw border rect
            EditorGUI.LabelField(columnRect, GUIContent.none, Styles.propertiesBackground);
            // Compensate for the border rect
            columnRect.x += 1;
            columnRect.height -= 1;
            columnRect.width -= 2;
            m_ActionsTree.OnGUI(columnRect);
        }

        private void DrawPropertiesColumn()
        {
            EditorGUILayout.BeginVertical(Styles.propertiesBackground, GUILayout.Width(position.width * (1 / 3f)));

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

        private void ShowAddActionMapMenu()
        {
            var menu = new GenericMenu();
            AddActionMapOptionsToMenu(menu, false);
            menu.ShowAsContext();
        }

        private void ShowAddActionsMenu()
        {
            var menu = new GenericMenu();
            AddActionsOptionsToMenu(menu, false);
            menu.ShowAsContext();
        }

        private void AddActionMapOptionsToMenu(GenericMenu menu, bool isContextMenu)
        {
            menu.AddItem(isContextMenu ?  m_AddActionMapContextGUI : m_AddActionMapGUI, false, OnAddActionMap);
        }

        private void AddActionsOptionsToMenu(GenericMenu menu, bool isContextMenu)
        {
            var hasSelection = m_ActionMapsTree.HasSelection();
            var canAddBinding = false;
            var action = m_ActionsTree.GetSelectedAction();
            if (action != null && hasSelection)
            {
                canAddBinding = true;
            }
            var canAddAction = false;
            var actionMap = m_ActionMapsTree.GetSelectedActionMap();
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

            var compositeString = isContextMenu ? EditorGUIUtility.TrTextContent("Add/Composite") : EditorGUIUtility.TrTextContent("Composite");
            if (canAddBinding)
            {
                foreach (var composite in InputBindingComposite.s_Composites.names)
                {
                    menu.AddItem(new GUIContent(compositeString.text + " " + composite), false, OnAddCompositeBinding, composite);
                }
            }
            else if (!isContextMenu)
            {
                menu.AddDisabledItem(new GUIContent(compositeString), false);
            }
        }

        private void OnActionMapContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            AddActionMapOptionsToMenu(menu, true);
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        private void OnActionsContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            AddActionsOptionsToMenu(menu, true);
            m_CopyPasteUtility.AddOptionsToMenu(menu);
            menu.ShowAsContext();
        }

        private void OnAddCompositeBinding(object compositeName)
        {
            var actionLine = GetSelectedActionLine();
            actionLine.AppendCompositeBinding((string)compositeName);
            Apply();
        }

        private void OnAddBinding()
        {
            var actionLine = GetSelectedActionLine();
            actionLine.AppendBinding();
            Apply();
        }

        private void OnAddAction()
        {
            var actionMapLine = GetSelectedActionMapLine();
            actionMapLine.AddAction();
            Apply();
        }

        private void OnAddActionMap()
        {
            InputActionSerializationHelpers.AddActionMap(m_ActionAssetManager.serializedObject);
            Apply();
        }

        private ActionTreeItem GetSelectedActionLine()
        {
            TreeViewItem selectedRow = m_ActionsTree.GetSelectedRow();
            do
            {
                if (selectedRow is ActionTreeItem)
                    return (ActionTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }

        private ActionMapTreeItem GetSelectedActionMapLine()
        {
            TreeViewItem selectedRow = m_ActionMapsTree.GetSelectedRow();
            do
            {
                if (selectedRow is ActionMapTreeItem)
                    return (ActionMapTreeItem)selectedRow;
                selectedRow = selectedRow.parent;
            }
            while (selectedRow.parent != null);

            return null;
        }
    }
}
#endif // UNITY_EDITOR
