#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class ActionInspectorWindow : EditorWindow
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
            var inputManagers = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
            var window = inputManagers.FirstOrDefault(w => w.m_ImportedAssetObject.Equals(obj));
            if (window != null)
            {
                window.Show();
                window.Focus();
                return true;
            }

            // No, so create a new window.
            window = CreateInstance<ActionInspectorWindow>();
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
            var windows = Resources.FindObjectsOfTypeAll<ActionInspectorWindow>();
            foreach (var window in windows)
                window.Refresh();

            ////REVIEW: why do we need to do this? comment!
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            s_RefreshPending = false;
        }

        private static bool s_RefreshPending;

        [SerializeField] private bool m_IsDirty;
        [SerializeField] private string m_AssetGUID;
        [SerializeField] private string m_AssetPath;
        [SerializeField] private string m_AssetJson;
        [SerializeField] private InputActionAsset m_ImportedAssetObject;
        [SerializeField] private InputActionAsset m_AssetObjectForEditing;
        [SerializeField] private TreeViewState m_TreeViewState;
        [SerializeField] private TreeViewState m_PickerTreeViewState;

        private InputActionListTreeView m_TreeView;
        private SerializedObject m_SerializedObject;
        private InputBindingPropertiesView m_PropertyView;
        private CopyPasteUtility m_CopyPasteUtility;
        private SearchField m_SearchField;
        private string m_SearchText;

        private const string k_FileExtension = ".inputactions";

        private readonly GUIContent m_SaveAssetGUI = EditorGUIUtility.TrTextContent("Save");
        private readonly GUIContent m_AddBindingGUI = EditorGUIUtility.TrTextContent("Binding");
        private readonly GUIContent m_AddBindingContextGUI = EditorGUIUtility.TrTextContent("Add binding");
        private readonly GUIContent m_AddActionGUI = EditorGUIUtility.TrTextContent("Action");
        private readonly GUIContent m_AddActionContextGUI = EditorGUIUtility.TrTextContent("Add action");
        private readonly GUIContent m_AddActionMapGUI = EditorGUIUtility.TrTextContent("Action map");
        private readonly GUIContent m_AddActionMapContextGUI = EditorGUIUtility.TrTextContent("Add action map");

        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoCallback;
            if (m_ImportedAssetObject == null)
                return;

            // Initialize after assembly reload
            InitializeObjectReferences();
            InitializeTrees();
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoCallback;
        }

        private void SetAsset(InputActionAsset referencedObject)
        {
            m_ImportedAssetObject = referencedObject;
            InitializeObjectReferences();
            InitializeTrees();
        }

        private void InitializeObjectReferences()
        {
            // If we have an asset object, grab its path and GUID.
            if (m_ImportedAssetObject != null)
            {
                m_AssetPath = AssetDatabase.GetAssetPath(m_ImportedAssetObject);
                m_AssetGUID = AssetDatabase.AssetPathToGUID(m_AssetPath);
            }
            else
            {
                // Otherwise look it up from its GUID. We're not relying on just
                // the path here as the asset may have been moved.
                InitializeReferenceToImportedAssetObject();
            }

            // Duplicate the asset along 1:1. Unlike calling Clone(), this will also preserve
            // GUIDs.
            m_AssetObjectForEditing = Instantiate(m_ImportedAssetObject);
            m_AssetObjectForEditing.hideFlags = HideFlags.HideAndDontSave;
            m_AssetObjectForEditing.name = m_ImportedAssetObject.name;
            m_AssetJson = null;
            m_SerializedObject = new SerializedObject(m_AssetObjectForEditing);
        }

        private void InitializeReferenceToImportedAssetObject()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID));

            m_AssetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(m_AssetPath))
                throw new Exception("Could not determine asset path for " + m_AssetGUID);

            m_ImportedAssetObject = AssetDatabase.LoadAssetAtPath<InputActionAsset>(m_AssetPath);
            if (m_AssetObjectForEditing != null)
            {
                DestroyImmediate(m_AssetObjectForEditing);
                m_AssetObjectForEditing = null;
            }
        }

        private void InitializeTrees()
        {
            if (m_SearchField == null)
                m_SearchField = new SearchField();

            m_TreeView = InputActionListTreeView.CreateFromSerializedObject(Apply, m_SerializedObject, ref m_TreeViewState);
            m_TreeView.OnSelectionChanged = OnSelectionChanged;
            m_TreeView.OnContextClick = OnContextClick;

            m_CopyPasteUtility = new CopyPasteUtility(Apply, m_TreeView, m_SerializedObject);
            if (m_PickerTreeViewState == null)
                m_PickerTreeViewState = new TreeViewState();

            LoadPropertiesForSelection();
        }

        private void OnUndoRedoCallback()
        {
            if (m_TreeView == null)
                return;

            m_IsDirty = true;
            m_TreeView.Reload();
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            LoadPropertiesForSelection();
        }

        private void LoadPropertiesForSelection()
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

        private void Apply()
        {
            m_IsDirty = true;
            m_SerializedObject.ApplyModifiedProperties();
            m_TreeView.Reload();
        }

        private void Refresh()
        {
            // See if the data has actually changed.
            var newJson = m_AssetObjectForEditing.ToJson();
            if (newJson == m_AssetJson)
            {
                // Still need to refresh reference to imported object in case we had a re-import.
                if (m_ImportedAssetObject == null)
                    InitializeReferenceToImportedAssetObject();

                return;
            }

            // Perform a full refresh.
            InitializeObjectReferences();
            InitializeTrees();
            Repaint();

            m_AssetJson = newJson;
        }

        private void SaveChangesToAsset()
        {
            ////TODO: has to be made to work with version control
            Debug.Assert(!string.IsNullOrEmpty(m_AssetPath));

            // Update JSON.
            var asset = m_AssetObjectForEditing;
            m_AssetJson = asset.ToJson();

            // Write out, if changed.
            var existingJson = File.ReadAllText(m_AssetPath);
            if (m_AssetJson != existingJson)
            {
                File.WriteAllText(m_AssetPath, m_AssetJson);
                AssetDatabase.ImportAsset(m_AssetPath);
            }

            m_IsDirty = false;
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Toolbar.
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginDisabledGroup(!m_IsDirty);
            if (GUILayout.Button(m_SaveAssetGUI, EditorStyles.toolbarButton))
                SaveChangesToAsset();
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

        private void DrawMainTree()
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

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, false);
            menu.ShowAsContext();
        }

        private void AddAddOptionsToMenu(GenericMenu menu, bool isContextMenu)
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

        private void OnContextClick(SerializedProperty property)
        {
            var menu = new GenericMenu();
            AddAddOptionsToMenu(menu, true);
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
            InputActionSerializationHelpers.AddActionMap(m_SerializedObject);
            Apply();
        }

        private ActionTreeItem GetSelectedActionLine()
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

        private ActionMapTreeItem GetSelectedActionMapLine()
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

        private void DrawProperties()
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
