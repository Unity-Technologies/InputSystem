#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;

////TODO: Add "Revert" button

////REVIEW: should we listen for Unity project saves and save dirty .inputactions assets along with it?

////TODO: add helpers to very quickly set up certai common configs (e.g. "FPS Controls" in add-action context menu;
////      "WASD Control" in add-binding context menu)

////FIXME: when saving, processor/interaction selection is cleared

namespace UnityEngine.Experimental.Input.Editor
{
    internal class AssetInspectorWindow : EditorWindow
    {
        public static class Styles
        {
            public static readonly GUIStyle actionTreeBackground = new GUIStyle("Label");
            public static readonly GUIStyle propertiesBackground = new GUIStyle("Label");
            public static readonly GUIStyle columnHeaderLabel = new GUIStyle(EditorStyles.toolbar);
            public static readonly GUIStyle waitingForInputLabel = new GUIStyle("WhiteBoldLabel");

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

        [SerializeField] private TreeViewState m_ActionMapsTreeState;
        [SerializeField] private TreeViewState m_ActionsTreeState;
        [SerializeField] private InputControlPickerState m_PickerTreeViewState;
        [SerializeField] private InputActionAssetManager m_ActionAssetManager;
        [SerializeField] internal InputActionWindowToolbar m_InputActionWindowToolbar;
        [SerializeField] internal ActionInspectorContextMenu m_ContextMenu;
        [SerializeField] List<string> m_SelectedActionMaps = new List<string>();

        private InputBindingPropertiesView m_BindingPropertyView;
        private InputActionPropertiesView m_ActionPropertyView;

        internal ActionMapsTree m_ActionMapsTree;
        internal ActionsTree m_ActionsTree;
        internal InputActionCopyPasteUtility m_CopyPasteUtility;

        private static bool s_RefreshPending;
        private static readonly string k_FileExtension = "." + InputActionAsset.kExtension;

        private GUIContent m_AddActionIconGUI;
        private GUIContent m_AddActionMapIconGUI;
        private GUIContent m_AddBindingGUI;
        private readonly GUIContent m_ActionMapsHeaderGUI = EditorGUIUtility.TrTextContent("Action Maps");
        private readonly GUIContent m_ActionsGUI = EditorGUIUtility.TrTextContent("Actions");
        private readonly GUIContent m_WaitingForInputContent = EditorGUIUtility.TrTextContent("Waiting for input...");
        private readonly GUIContent m_WaitingForSpecificInputContent = new GUIContent("Waiting for {0}...");// EditorGUIUtility.TrTextContent("Waiting for {0}...");
        [SerializeField] private GUIContent m_DirtyTitle;
        [SerializeField] private GUIContent m_Title;
        private Vector2 m_PropertiesScroll;
        private bool m_ForceQuit;

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
                return;

            // Initialize after assembly reload
            m_ActionAssetManager.InitializeObjectReferences();
            m_ActionAssetManager.onDirtyChanged = OnDirtyChanged;
            m_InputActionWindowToolbar.SetReferences(m_ActionAssetManager, ApplyAndReload);
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
                var result = EditorUtility.DisplayDialogComplex("Unsaved changes",
                    "Do you want to save the changes you made before quitting?", "Save", "Cancel", "Don't Save");
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
        private void SetAsset(InputActionAsset asset)
        {
            m_ActionAssetManager = new InputActionAssetManager(asset) {onDirtyChanged = OnDirtyChanged};
            m_ActionAssetManager.InitializeObjectReferences();
            m_InputActionWindowToolbar = new InputActionWindowToolbar(m_ActionAssetManager, ApplyAndReload);
            m_ContextMenu = new ActionInspectorContextMenu(this, m_ActionAssetManager, m_InputActionWindowToolbar);
            InitializeTrees();

            // Make sure first actions map selected and actions tree expanded
            m_ActionMapsTree.SelectFirstRow();
            OnActionMapSelection();
            m_ActionsTree.ExpandAll();
            LoadPropertiesForSelection();

            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            var title = m_ActionAssetManager.name + " (Input Actions)";
            m_Title = new GUIContent(title);
            m_DirtyTitle = new GUIContent("(*) " + m_Title.text);
            titleContent = m_Title;
        }

        private void InitializeTrees()
        {
            m_ActionMapsTree = ActionMapsTree.CreateFromSerializedObject(ApplyAndReload, m_ActionAssetManager.serializedObject, ref m_ActionMapsTreeState);
            m_ActionMapsTree.OnSelectionChanged = OnActionMapSelection;
            m_ActionMapsTree.OnContextClick = m_ContextMenu.OnActionMapContextClick;

            m_ActionsTree = ActionsTree.CreateFromSerializedObject(ApplyAndReload, ref m_ActionsTreeState);
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

            m_CopyPasteUtility = new InputActionCopyPasteUtility(ApplyAndReload, m_ActionMapsTree, m_ActionsTree, m_ActionAssetManager.serializedObject);
            if (m_PickerTreeViewState == null)
                m_PickerTreeViewState = new InputControlPickerState();
        }

        private void OnUndoRedoCallback()
        {
            if (m_ActionMapsTree == null)
                return;

            m_ActionAssetManager.LoadImportedObjectFromGuid();
            EditorApplication.delayCall += ApplyAndReload;
            // Since the Undo.undoRedoPerformed callback is global, the callback will be called for any undo/redo action
            // We need to make sure we dirty the state only in case of changes to the asset.
            EditorApplication.delayCall += m_ActionAssetManager.UpdateAssetDirtyState;
        }

        private void OnActionMapSelection()
        {
            if (m_ActionMapsTree.GetSelectedRow() != null)
                m_ActionsTree.actionMapProperty = m_ActionMapsTree.GetSelectedRow().elementProperty;
            m_ActionsTree.Reload();
            if (m_ActionMapsTree.GetSelectedRow() != null)
            {
                var row = m_ActionMapsTree.GetSelectedRow();
                if (!m_SelectedActionMaps.Contains(row.displayName))
                {
                    m_ActionsTree.SetExpandedRecursive(m_ActionsTree.GetRootElement().id, true);
                    m_SelectedActionMaps.Add(row.displayName);
                }
            }
        }

        private void OnActionSelection()
        {
            LoadPropertiesForSelection();
        }

        private void LoadPropertiesForSelection()
        {
            m_BindingPropertyView = null;
            m_ActionPropertyView = null;

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
                    var isCompositePartBinding = item is CompositeTreeItem;
                    var isCompositeBinding = item is CompositeGroupTreeItem;
                    var actionItem = (isCompositePartBinding ? item.parent.parent : item.parent) as ActionTreeItem;
                    Debug.Assert(actionItem != null);

                    // Show properties for binding.
                    m_BindingPropertyView =
                        new InputBindingPropertiesView(
                            item.elementProperty,
                            change =>
                            {
                                if (change == InputBindingPropertiesView.k_CompositeTypeChanged)
                                {
                                    Debug.Assert(isCompositeBinding, "Binding is expected to be a composite");

                                    // This is a pretty complex change. We basically tear out part of the binding tree
                                    // and replace it with a different structure.
                                    var actionMapRow = (ActionMapTreeItem)m_ActionMapsTree.GetSelectedRow();
                                    Debug.Assert(actionMapRow != null);

                                    var compositeName = m_BindingPropertyView.compositeType;
                                    var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(compositeName);

                                    InputActionSerializationHelpers.ChangeCompositeType(actionMapRow.bindingsProperty,
                                        actionItem.bindingsStartIndex + item.index, compositeName, compositeType,
                                        actionItem.actionName);

                                    ApplyAndReload();
                                }
                                else if (change == InputBindingPropertiesView.k_PathChanged)
                                {
                                    // If path changed, perform a full reload as it affects the action tree.
                                    // Otherwise just do a "soft" apply. This is important so as to not lose
                                    // edit state while editing parameters on interactions or processors.
                                    ApplyAndReload();
                                }
                                else
                                {
                                    // Simple property change that doesn't affect the rest of the UI.
                                    Apply();
                                }
                            },
                            m_PickerTreeViewState,
                            m_InputActionWindowToolbar,
                            isCompositeBinding: isCompositeBinding,
                            expectedControlLayout: item.expectedControlLayout);
                }

                if (item is ActionTreeItem actionItem1)
                {
                    // Show properties for binding.
                    m_ActionPropertyView =
                        new InputActionPropertiesView(
                            actionItem1.elementProperty,
                            // Apply without reload is enough here.
                            change => Apply());
                }
            }
        }

        internal void ApplyAndReload()
        {
            Apply();

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

        private void Apply()
        {
            m_ActionAssetManager.serializedObject.ApplyModifiedProperties();

            if (InputEditorUserSettings.autoSaveInputActionAssets)
            {
                m_ActionAssetManager.SaveChangesToAsset();
            }
            else
            {
                m_ActionAssetManager.SetAssetDirty();
                titleContent = m_DirtyTitle;
            }

            m_ActionAssetManager.ApplyChanges();
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
            m_InputActionWindowToolbar.OnGUI();
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
                if (InputActionCopyPasteUtility.IsValidCommand(Event.current.commandName))
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

            var labelRect = new Rect(columnRect)
            {
                height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2
            };

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

            var labelRect = new Rect(columnRect)
            {
                height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2
            };

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
                m_ContextMenu.OnAddBinding(m_ActionsTree.GetSelectedAction());
                m_ActionsTree.SelectNewActionRow();
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
            else if (m_ActionPropertyView != null)
            {
                m_PropertiesScroll = EditorGUILayout.BeginScrollView(m_PropertiesScroll);
                m_ActionPropertyView.OnGUI();
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

            Styles.waitingForInputLabel.CalcMinMaxWidth(waitingForInputText, out _, out var maxWidth);

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

        #if UNITY_2019_1_OR_NEWER
        ////FIXME: the shortcuts seem to have focus problems; often requires clicking away and then back to the window
        [Shortcut("Input Action Editor/Save", typeof(AssetInspectorWindow), KeyCode.S, ShortcutModifiers.Alt)]
        private static void SaveShortcut(ShortcutArguments arguments)
        {
            var window = (AssetInspectorWindow)arguments.context;
            window.m_ActionAssetManager.SaveChangesToAsset();
        }

        [Shortcut("Input Action Editor/Add Action Map", typeof(AssetInspectorWindow), KeyCode.M, ShortcutModifiers.Alt)]
        private static void AddActionMapShortcut(ShortcutArguments arguments)
        {
            var window = (AssetInspectorWindow)arguments.context;
            window.m_ContextMenu.OnAddActionMap();
        }

        [Shortcut("Input Action Editor/Add Action", typeof(AssetInspectorWindow), KeyCode.A, ShortcutModifiers.Alt)]
        private static void AddActionShortcut(ShortcutArguments arguments)
        {
            var window = (AssetInspectorWindow)arguments.context;
            window.m_ContextMenu.OnAddAction();
            window.m_ContextMenu.OnAddBinding(window.m_ActionsTree.GetSelectedAction());
            window.m_ActionsTree.SelectNewActionRow();
        }

        [Shortcut("Input Action Editor/Add Binding", typeof(AssetInspectorWindow), KeyCode.B, ShortcutModifiers.Alt)]
        private static void AddBindingShortcut(ShortcutArguments arguments)
        {
            var window = (AssetInspectorWindow)arguments.context;
            window.m_ContextMenu.OnAddBinding(window.m_ActionsTree.GetSelectedAction());
        }

        #endif

        internal static AssetInspectorWindow FindEditorFor(InputActionAsset asset)
        {
            var windows = Resources.FindObjectsOfTypeAll<AssetInspectorWindow>();
            return windows.FirstOrDefault(w => w.m_ActionAssetManager.ImportedAssetObjectEquals(asset));
        }

        internal static AssetInspectorWindow FindEditorFor(string guid)
        {
            var windows = Resources.FindObjectsOfTypeAll<AssetInspectorWindow>();
            return windows.FirstOrDefault(w => w.m_ActionAssetManager.guid == guid);
        }

        [OnOpenAsset]
        internal static bool OnOpenAsset(int instanceId, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
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

            ////REVIEW: It'd be great if the window got docked by default but the public EditorWindow API doesn't allow that
            ////        to be done for windows that aren't singletons (GetWindow<T>() will only create one window and it's the
            ////        only way to get programmatic docking with the current API).
            // See if we have an existing editor window that has the asset open.
            var window = FindEditorFor(asset);
            if (window == null)
            {
                // No, so create a new window.
                window = CreateInstance<AssetInspectorWindow>();
                window.SetAsset(asset);
            }
            window.Show();
            window.Focus();

            // If user clicked on an action inside the asset, focus on that action (if we can find it).
            if (actionToSelect != null && window.m_ActionMapsTree.SetSelection(mapToSelect))
            {
                window.OnActionMapSelection();
                window.m_ActionsTree.SetSelection(actionToSelect);
            }

            return true;
        }

        private void OnActionRowGUI(TreeViewItem treeViewItem, Rect rect)
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

        private void OnDirtyChanged(bool dirty)
        {
            titleContent = dirty ? m_DirtyTitle : m_Title;
        }

        internal void CloseWithoutSaving()
        {
            m_ForceQuit = true;
            Close();
        }

        private class ProcessAssetModifications : UnityEditor.AssetModificationProcessor
        {
            // Handle .inputactions asset being deleted.
            // ReSharper disable once UnusedMember.Local
            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                    return default;

                // See if we have an open window.
                var guid = AssetDatabase.AssetPathToGUID(path);
                var window = FindEditorFor(guid);
                if (window != null)
                {
                    // If there's unsaved changes, ask for confirmation.
                    if (window.m_ActionAssetManager.dirty)
                    {
                        var result = EditorUtility.DisplayDialog("Unsaved changes",
                            $"You have unsaved changes for '{path}'. Do you want to discard the changes and delete the asset?",
                            "Yes, Delete", "No, Cancel");
                        if (!result)
                        {
                            // User cancelled. Stop the deletion.
                            return AssetDeleteResult.FailedDelete;
                        }

                        window.m_ForceQuit = true;
                    }

                    window.Close();
                }

                return default;
            }

            // Handle .inputactions asset being moved.
            // ReSharper disable once UnusedMember.Local
            public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
            {
                if (!sourcePath.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                    return default;

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var window = FindEditorFor(guid);
                if (window != null)
                {
                    window.m_ActionAssetManager.path = destinationPath;
                    window.UpdateWindowTitle();
                }

                return default;
            }
        }
    }
}
#endif // UNITY_EDITOR
