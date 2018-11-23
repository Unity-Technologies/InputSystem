#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    internal class AssetInspectorWindow : EditorWindow
    {
        public static class Styles
        {
            public static GUIStyle actionTreeBackground = new GUIStyle("Label");
            public static GUIStyle propertiesBackground = new GUIStyle("Label");
            public static GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);
            public static GUIStyle waitingForInputLabel = new GUIStyle("WhiteBoldLabel");

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
                actionTreeBackground.margin = new RectOffset(4, 4, 4, 4);

                propertiesBackground.normal.background = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPath + "propertiesBackground.png");
                propertiesBackground.border = new RectOffset(3, 3, 3, 3);
                propertiesBackground.margin = new RectOffset(4, 4, 4, 4);

                columnHeaderLabel.alignment = TextAnchor.MiddleLeft;
                columnHeaderLabel.fontStyle = FontStyle.Bold;
                columnHeaderLabel.padding.left = 10;

                waitingForInputLabel.fontSize = 40;
            }
        }

        [SerializeField]
        private TreeViewState m_ActionMapsTreeState;
        [SerializeField]
        private TreeViewState m_ActionsTreeState;
        [SerializeField]
        private TreeViewState m_PickerTreeViewState;
        [SerializeField]
        private InputActionAssetManager m_ActionAssetManager;
        [SerializeField]
        internal InputActionWindowToolbar m_InputActionWindowToolbar;
        [SerializeField]
        internal ActionInspectorContextMenu m_ContextMenu;

        private InputBindingPropertiesView m_BindingPropertyView;
        internal ActionMapsTree m_ActionMapsTree;
        internal ActionsTree m_ActionsTree;
        internal CopyPasteUtility m_CopyPasteUtility;

        private static bool s_RefreshPending;
        private static readonly string k_FileExtension = ".inputactions";

        GUIContent m_AddActionIconGUI;
        GUIContent m_AddActionMapIconGUI;
        GUIContent m_AddBindingGUI;
        GUIContent m_ActionMapsHeaderGUI = EditorGUIUtility.TrTextContent("Action Maps");
        GUIContent m_ActionsGUI = EditorGUIUtility.TrTextContent("Actions");
        GUIContent m_WaitingForInputContent = EditorGUIUtility.TrTextContent("Waiting for input...");
        GUIContent m_WaitingForSpecificInputContent = new GUIContent("Waiting for {0}...");// EditorGUIUtility.TrTextContent("Waiting for {0}...");
        [SerializeField]
        GUIContent m_DirtyTitle;
        [SerializeField]
        GUIContent m_Title;
        Vector2 m_PropertiesScroll;
        bool m_ForceQuit;

        private void OnEnable()
        {
            minSize = new Vector2(600, 300);

            if (m_AddActionIconGUI == null)
                m_AddActionIconGUI = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add Action");
            if (m_AddActionMapIconGUI == null)
                m_AddActionMapIconGUI = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add Action Map");
            if (m_AddBindingGUI == null)
                m_AddBindingGUI = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Add Binding");

            Undo.undoRedoPerformed += OnUndoRedoCallback;
            if (m_ActionAssetManager == null)
            {
                return;
            }

            // Initialize after assembly reload
            m_ActionAssetManager.InitializeObjectReferences();
            m_ActionAssetManager.SetReferences(SetTitle);
            m_InputActionWindowToolbar.SetReferences(m_ActionAssetManager, Apply);
            m_InputActionWindowToolbar.RebuildData();
            m_ContextMenu.SetReferences(this, m_ActionAssetManager, m_InputActionWindowToolbar);

            InitializeTrees();
            OnActionMapSelection();
            LoadPropertiesForSelection();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoCallback;
        }

        private void OnDestroy()
        {
            if (!m_ForceQuit && m_ActionAssetManager.dirty)
            {
                var result = EditorUtility.DisplayDialogComplex("Unsaved changes", "Do you want to save the changes you made before quitting?", "Save", "Cancel", "Don't Save");
                switch (result)
                {
                    case 0:
                        // Save
                        m_ActionAssetManager.SaveChangesToAsset();
                        m_ActionAssetManager.CleanupAssets();
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
            m_ActionAssetManager.SetReferences(SetTitle);
            m_ActionAssetManager.InitializeObjectReferences();
            m_InputActionWindowToolbar = new InputActionWindowToolbar(m_ActionAssetManager, Apply);
            m_ContextMenu = new ActionInspectorContextMenu(this, m_ActionAssetManager, m_InputActionWindowToolbar);
            InitializeTrees();

            // Make sure first actions map selected and actions tree expanded
            m_ActionMapsTree.SelectFirstRow();
            OnActionMapSelection();
            m_ActionsTree.ExpandAll();
            LoadPropertiesForSelection();
        }

        private void InitializeTrees()
        {
            m_ActionMapsTree = ActionMapsTree.CreateFromSerializedObject(Apply, m_ActionAssetManager.serializedObject, ref m_ActionMapsTreeState);
            m_ActionMapsTree.OnSelectionChanged = OnActionMapSelection;
            m_ActionMapsTree.OnContextClick = m_ContextMenu.OnActionMapContextClick;

            m_ActionsTree = ActionsTree.CreateFromSerializedObject(Apply, ref m_ActionsTreeState);
            m_ActionsTree.OnSelectionChanged = OnActionSelection;
            m_ActionsTree.OnContextClick = m_ContextMenu.OnActionsContextClick;
            m_ActionsTree.OnRowGUI = OnActionRowGUI;
            m_InputActionWindowToolbar.OnSearchChanged = m_ActionsTree.SetNameFilter;
            m_InputActionWindowToolbar.OnSchemeChanged = a =>
            {
                if (a == null)
                {
                    m_ActionsTree.SetSchemeBindingGroupFilter(null);
                    return;
                }
                var group = m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(a).bindingGroup;
                m_ActionsTree.SetSchemeBindingGroupFilter(group);
            };
            m_InputActionWindowToolbar.OnDeviceChanged = m_ActionsTree.SetDeviceFilter;

            m_ActionsTree.SetNameFilter(m_InputActionWindowToolbar.nameFilter);
            if (m_InputActionWindowToolbar.selectedControlSchemeName != null)
            {
                var group = m_ActionAssetManager.m_AssetObjectForEditing.GetControlScheme(m_InputActionWindowToolbar.selectedControlSchemeName).bindingGroup;
                m_ActionsTree.SetSchemeBindingGroupFilter(group);
            }
            m_ActionsTree.SetDeviceFilter(m_InputActionWindowToolbar.selectedDevice);

            m_CopyPasteUtility = new CopyPasteUtility(Apply, m_ActionMapsTree, m_ActionsTree, m_ActionAssetManager.serializedObject);
            if (m_PickerTreeViewState == null)
                m_PickerTreeViewState = new TreeViewState();
        }

        private void OnUndoRedoCallback()
        {
            if (m_ActionMapsTree == null)
                return;

            m_ActionAssetManager.LoadImportedObjectFromGuid();
            EditorApplication.delayCall += Apply;
            // Since the Undo.undoRedoPerformed callback is global, the callback will be called for any undo/redo action
            // We need to make sure we dirty the state only in case of changes to the asset.
            EditorApplication.delayCall += m_ActionAssetManager.UpdateAssetDirtyState;
        }

        private void OnActionMapSelection()
        {
            if (m_ActionMapsTree.GetSelectedRow() != null)
                m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedRow().elementProperty;
            m_ActionsTree.Reload();
        }

        private void OnActionSelection()
        {
            LoadPropertiesForSelection();
        }

        private void LoadPropertiesForSelection()
        {
            m_BindingPropertyView = null;

            // Column #1: Load selected action map.
            if (m_ActionMapsTree.GetSelectedRow() != null)
            {
                var row = m_ActionMapsTree.GetSelectedRow();
                if (row != null)
                {
                    m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedRow().elementProperty;
                    m_ActionsTree.Reload();
                }
            }

            // Column #2: Load selected action or binding.
            if (m_ActionsTree.HasSelection() && m_ActionsTree.GetSelection().Count == 1)
            {
                var item = m_ActionsTree.GetSelectedRow();
                if (item is BindingTreeItem)
                {
                    // Grab the action for the binding and see if we have an expected control layout
                    // set on it. Pass that on to the control picking machinery.
                    var isCompositeTreeItem = item is CompositeTreeItem;
                    var actionItem = (isCompositeTreeItem ? item.parent.parent : item.parent) as ActionTreeItem;
                    Debug.Assert(actionItem != null);

                    // Show properties for binding.
                    m_BindingPropertyView =
                        new InputBindingPropertiesView(
                            item.elementProperty,
                            () =>
                            {
                                Apply();
                                LoadPropertiesForSelection();
                            },
                            m_PickerTreeViewState,
                            m_InputActionWindowToolbar,
                            item.expectedControlLayout);

                    // For composite groups, don't show the binding path and control scheme section.
                    if (item is CompositeGroupTreeItem)
                        m_BindingPropertyView.showPathAndControlSchemeSection = false;
                }
                ////TODO: properties for actions
            }
        }

        internal void Apply()
        {
            m_ActionAssetManager.SetAssetDirty();
            titleContent = m_DirtyTitle;
            m_ActionAssetManager.ApplyChanges();
            m_ActionMapsTree.Reload();
            m_InputActionWindowToolbar.RebuildData();
            var selectedActionMap = m_ActionMapsTree.GetSelectedActionMap();
            if (selectedActionMap != null)
            {
                m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedActionMap().elementProperty;
            }
            else
            {
                m_ActionsTree.actionMapProperty = null;
            }
            m_ActionsTree.Reload();

            LoadPropertiesForSelection();
        }

        private void OnGUI()
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
            m_InputActionWindowToolbar.OnGUI();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            var isPickingInteractively = m_BindingPropertyView != null && m_BindingPropertyView.isInteractivelyPicking;
            EditorGUI.BeginDisabledGroup(isPickingInteractively);

            // Draw columns.
            var columnsRect = EditorGUILayout.BeginHorizontal();
            var columnAreaWidth = position.width - Styles.actionTreeBackground.margin.left - Styles.actionTreeBackground.margin.left - Styles.propertiesBackground.margin.right;
            DrawActionMapsColumn(columnAreaWidth * 0.22f);
            DrawActionsColumn(columnAreaWidth * 0.38f);
            DrawPropertiesColumn(columnAreaWidth * 0.40f);
            EditorGUILayout.EndHorizontal();

            // If we're currently interactively picking a binding, aside from disabling and dimming the normal UI, display a large text over
            // the window that says we're waiting for input.
            // NOTE: We're not using EditorWindow.ShowNotification() as, aside from having trouble displaying our dynamically generated text
            //       properly without clipping, notifications will automatically disappear after a brief moment. We want the input requester
            //       to stay visible for as long as we're still looking for input.
            EditorGUI.EndDisabledGroup();
            if (isPickingInteractively)
                DrawInteractivePickingOverlay(columnsRect);

            // Bottom margin.
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

        private void DrawActionMapsColumn(float width)
        {
            EditorGUILayout.BeginVertical(Styles.actionTreeBackground, GUILayout.MinWidth(width), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            var columnRect = GUILayoutUtility.GetLastRect();

            var labelRect = new Rect(columnRect);
            labelRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            columnRect.y += labelRect.height;
            columnRect.height -= labelRect.height;

            // Draw header
            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, m_ActionMapsHeaderGUI, Styles.columnHeaderLabel);

            labelRect.x = labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (GUI.Button(labelRect, m_AddActionMapIconGUI, GUIStyle.none))
            {
                m_ContextMenu.OnAddActionMap();
            }

            // Draw border rect
            EditorGUI.LabelField(columnRect, GUIContent.none, Styles.propertiesBackground);
            // Compensate for the border rect
            columnRect.x += 1;
            columnRect.height -= 1;
            columnRect.width -= 2;
            m_ActionMapsTree.OnGUI(columnRect);
        }

        private void DrawActionsColumn(float width)
        {
            EditorGUILayout.BeginVertical(Styles.actionTreeBackground, GUILayout.MaxWidth(width), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            var columnRect = GUILayoutUtility.GetLastRect();

            var labelRect = new Rect(columnRect);
            labelRect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;
            columnRect.y += labelRect.height;
            columnRect.height -= labelRect.height;

            EditorGUI.LabelField(labelRect, GUIContent.none, Styles.actionTreeBackground);
            var headerRect = new Rect(labelRect.x + 1, labelRect.y + 1, labelRect.width - 2, labelRect.height - 2);
            EditorGUI.LabelField(headerRect, m_ActionsGUI, Styles.columnHeaderLabel);

            labelRect.x = labelRect.x + labelRect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            labelRect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (GUI.Button(labelRect, m_AddActionIconGUI, GUIStyle.none))
            {
                m_ContextMenu.OnAddAction();
            }

            // Draw border rect
            EditorGUI.LabelField(columnRect, GUIContent.none, Styles.propertiesBackground);
            // Compensate for the border rect
            columnRect.x += 1;
            columnRect.height -= 1;
            columnRect.width -= 2;
            m_ActionsTree.OnGUI(columnRect);
        }

        private void DrawPropertiesColumn(float width)
        {
            EditorGUILayout.BeginVertical(Styles.propertiesBackground, GUILayout.Width(width));


            var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2, GUILayout.ExpandWidth(true));
            rect.x -= 2;
            rect.y -= 1;
            rect.width += 4;

            EditorGUI.LabelField(rect, GUIContent.none, Styles.propertiesBackground);
            var headerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            EditorGUI.LabelField(headerRect, "Properties", Styles.columnHeaderLabel);

            if (m_BindingPropertyView != null)
            {
                m_PropertiesScroll = EditorGUILayout.BeginScrollView(m_PropertiesScroll);
                m_BindingPropertyView.OnGUI();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
            }


            EditorGUILayout.EndVertical();
        }

        private void DrawInteractivePickingOverlay(Rect rect)
        {
            // If we have an expected control layout, be specific about what kind of input we expect as
            // otherwise it can be quite confusing to hammer an input control and nothing happens.
            var expectedControlLayout = m_BindingPropertyView.expectedControlLayout;
            GUIContent waitingForInputText;
            if (!string.IsNullOrEmpty(expectedControlLayout))
            {
                var text = string.Format(m_WaitingForSpecificInputContent.text, expectedControlLayout);
                waitingForInputText = new GUIContent(text);
            }
            else
            {
                waitingForInputText = m_WaitingForInputContent;
            }

            float minWidth, maxWidth;
            Styles.waitingForInputLabel.CalcMinMaxWidth(waitingForInputText, out minWidth, out maxWidth);

            var waitingForInputTextRect = rect;
            waitingForInputTextRect.width = maxWidth;
            waitingForInputTextRect.height = Styles.waitingForInputLabel.CalcHeight(waitingForInputText, rect.width);
            waitingForInputTextRect.x = rect.width / 2 - maxWidth / 2;
            waitingForInputTextRect.y = rect.height / 2 - waitingForInputTextRect.height / 2;

            EditorGUI.DropShadowLabel(waitingForInputTextRect, waitingForInputText, Styles.waitingForInputLabel);

            var cancelButtonRect = waitingForInputTextRect;
            cancelButtonRect.y += waitingForInputTextRect.height + 3;
            cancelButtonRect.x = waitingForInputTextRect.x + waitingForInputTextRect.width - 50;
            cancelButtonRect.width = 50;
            cancelButtonRect.height = 15;

            if (GUI.Button(cancelButtonRect, "Cancel"))
            {
                m_BindingPropertyView.CancelInteractivePicking();
                Repaint();
            }
        }

        public static void RefreshAllOnAssetReimport()
        {
            if (s_RefreshPending)
                return;

            // We don't want to refresh right away but rather wait for the next editor update
            // to then do one pass of refreshing action editor windows.
            EditorApplication.delayCall += RefreshAllOnAssetReimportCallback;
            s_RefreshPending = true;
        }

        private static void RefreshAllOnAssetReimportCallback()
        {
            s_RefreshPending = false;

            // When the asset is modified outside of the editor
            // and the importer settings are visible in the inspector
            // the asset references in the importer inspector need to be force rebuild
            // (otherwise we gets lots of exceptions)
            ActiveEditorTracker.sharedTracker.ForceRebuild();

            var windows = Resources.FindObjectsOfTypeAll<AssetInspectorWindow>();
            foreach (var window in windows)
                window.ReloadAssetFromFile();
        }

        private void ReloadAssetFromFile()
        {
            if (!m_ActionAssetManager.dirty)
            {
                m_ActionAssetManager.CreateWorkingCopyAsset();
                InitializeTrees();
                LoadPropertiesForSelection();
                Repaint();
            }
        }

        [OnOpenAsset]
        internal static bool OnOpenAsset(int instanceId, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!path.EndsWith(k_FileExtension))
                return false;

            string mapToSelect = null;
            string actionToSelect = null;

            // Grab InputActionAsset.
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as InputActionAsset;
            if (asset == null)
            {
                // Check if the user clicked on an action inside the asset.
                var actionReference = obj as InputActionReference;
                if (actionReference != null)
                {
                    asset = actionReference.asset;
                    mapToSelect = actionReference.action.actionMap.name;
                    actionToSelect = actionReference.action.name;
                }
                else
                    return false;
            }

            // See if we have an existing editor window that has the asset open.
            var inputManagers = Resources.FindObjectsOfTypeAll<AssetInspectorWindow>();
            var window = inputManagers.FirstOrDefault(w => w.m_ActionAssetManager.ImportedAssetObjectEquals(asset));

            if (window != null)
            {
                window.Show();
                window.Focus();
            }
            else
            {
                // No, so create a new window.
                window = CreateInstance<AssetInspectorWindow>();
                window.m_Title = new GUIContent(asset.name + " (Input Manager)");
                window.m_DirtyTitle = new GUIContent("(*) " + window.m_Title.text);
                window.titleContent = window.m_Title;
                window.SetAsset(asset);
                window.Show();
            }

            // If user clicked on an action inside the asset, focus on that action (if we can find it).
            if (actionToSelect != null)
            {
                if (window.m_ActionMapsTree.SetSelection(mapToSelect))
                {
                    window.OnActionMapSelection();
                    window.m_ActionsTree.SetSelection(actionToSelect);
                }
            }

            return true;
        }

        void OnActionRowGUI(TreeViewItem treeViewItem, Rect rect)
        {
            if (treeViewItem is ActionTreeItem)
            {
                rect.x = rect.width - (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                rect.width = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (GUI.Button(rect, m_AddBindingGUI, GUIStyle.none))
                {
                    m_ContextMenu.ShowAddActionsMenu(treeViewItem);
                }
            }
        }

        void SetTitle(bool dirty)
        {
            titleContent = dirty ? m_DirtyTitle : m_Title;
        }

        internal void CloseWithoutSaving()
        {
            m_ForceQuit = true;
            Close();
        }
    }
}
#endif // UNITY_EDITOR
