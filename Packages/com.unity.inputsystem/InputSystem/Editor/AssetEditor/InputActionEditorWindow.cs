#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;

////TODO: Add "Revert" button

////TODO: add helpers to very quickly set up certain common configs (e.g. "FPS Controls" in add-action context menu;
////      "WASD Control" in add-binding context menu)

////REVIEW: should we listen for Unity project saves and save dirty .inputactions assets along with it?

////FIXME: when saving, processor/interaction selection is cleared

////TODO: persist view state of asset in Library/ folder

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// An editor window to edit .inputactions assets.
    /// </summary>
    /// <remarks>
    /// The .inputactions editor code does not really separate between model and view. Selection state is contained
    /// in the tree views and persistent across domain reloads via <see cref="TreeViewState"/>.
    /// </remarks>
    internal class InputActionEditorWindow : EditorWindow, IDisposable
    {
        /// <summary>
        /// Open window if someone clicks on an .inputactions asset or an action inside of it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "line", Justification = "line parameter required by OnOpenAsset attribute")]
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                return false;

            string mapToSelect = null;
            string actionToSelect = null;

            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
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

            var window = OpenEditor(asset);

            // If user clicked on an action inside the asset, focus on that action (if we can find it).
            if (actionToSelect != null && window.m_ActionMapsTree.TrySelectItem(mapToSelect))
            {
                window.OnActionMapTreeSelectionChanged();
                window.m_ActionsTree.SelectItem(actionToSelect);
            }

            return true;
        }

        /// <summary>
        /// Open the specified <paramref name="asset"/> in an editor window. Used when someone hits the "Edit Asset" button in the
        /// importer inspector.
        /// </summary>
        /// <param name="asset">The InputActionAsset to open.</param>
        /// <returns>The editor window.</returns>
        public static InputActionEditorWindow OpenEditor(InputActionAsset asset)
        {
            ////REVIEW: It'd be great if the window got docked by default but the public EditorWindow API doesn't allow that
            ////        to be done for windows that aren't singletons (GetWindow<T>() will only create one window and it's the
            ////        only way to get programmatic docking with the current API).
            // See if we have an existing editor window that has the asset open.
            var window = FindEditorForAsset(asset);
            if (window == null)
            {
                // No, so create a new window.
                window = CreateInstance<InputActionEditorWindow>();
                window.SetAsset(asset);
            }
            window.Show();
            window.Focus();

            return window;
        }

        public static InputActionEditorWindow FindEditorForAsset(InputActionAsset asset)
        {
            var windows = Resources.FindObjectsOfTypeAll<InputActionEditorWindow>();
            return windows.FirstOrDefault(w => w.m_ActionAssetManager.ImportedAssetObjectEquals(asset));
        }

        public static InputActionEditorWindow FindEditorForAssetWithGUID(string guid)
        {
            var windows = Resources.FindObjectsOfTypeAll<InputActionEditorWindow>();
            return windows.FirstOrDefault(w => w.m_ActionAssetManager.guid == guid);
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

        public void SaveChangesToAsset()
        {
            m_ActionAssetManager.SaveChangesToAsset();
        }

        public void AddNewActionMap()
        {
            m_ActionMapsTree.AddNewActionMap();
        }

        public void AddNewAction()
        {
            // Make sure we have an action map. If we don't have an action map selected,
            // refuse the operation.
            var actionMapItem = m_ActionMapsTree.GetSelectedItems().OfType<ActionMapTreeItem>().FirstOrDefault();
            if (actionMapItem == null)
            {
                EditorApplication.Beep();
                return;
            }

            m_ActionsTree.AddNewAction(actionMapItem.property);
        }

        public void AddNewBinding()
        {
            // Make sure we have an action selected.
            var actionItems = m_ActionsTree.GetSelectedItems().OfType<ActionTreeItem>();
            if (!actionItems.Any())
            {
                EditorApplication.Beep();
                return;
            }

            foreach (var item in actionItems)
                m_ActionsTree.AddNewBinding(item.property, item.actionMapProperty);
        }

        private static void RefreshAllOnAssetReimportCallback()
        {
            s_RefreshPending = false;

            // When the asset is modified outside of the editor
            // and the importer settings are visible in the inspector
            // the asset references in the importer inspector need to be force rebuild
            // (otherwise we gets lots of exceptions)
            ActiveEditorTracker.sharedTracker.ForceRebuild();

            var windows = Resources.FindObjectsOfTypeAll<InputActionEditorWindow>();
            foreach (var window in windows)
                window.ReloadAssetFromFileIfNotDirty();
        }

        private bool ConfirmSaveChangesIfNeeded()
        {
            // Ask for confirmation if we have unsaved changes.
            if (!m_ForceQuit && m_ActionAssetManager.dirty)
            {
                var result = EditorUtility.DisplayDialogComplex("Input Action Asset has been modified",
                    $"Do you want to save the changes you made in:\n{m_ActionAssetManager.path}\n\nYour changes will be lost if you don't save them.", "Save", "Cancel", "Don't Save");
                switch (result)
                {
                    case 0: // Save
                        m_ActionAssetManager.SaveChangesToAsset();
                        m_ActionAssetManager.Cleanup();
                        break;
                    case 1: // Cancel
                        Instantiate(this).Show();
                        // Cancel editor quit.
                        return false;
                    case 2: // Don't save, don't ask again.
                        m_ForceQuit = true;
                        break;
                }
            }
            return true;
        }

        private bool EditorWantsToQuit()
        {
            return ConfirmSaveChangesIfNeeded();
        }

        private void OnEnable()
        {
            minSize = new Vector2(600, 300);

            // Initialize toolbar. We keep the toolbar across domain reloads but we
            // will lose the delegates.
            if (m_Toolbar == null)
                m_Toolbar = new InputActionEditorToolbar();
            m_Toolbar.onSearchChanged = OnToolbarSearchChanged;
            m_Toolbar.onSelectedSchemeChanged = OnControlSchemeSelectionChanged;
            m_Toolbar.onSelectedDeviceChanged = OnControlSchemeSelectionChanged;
            m_Toolbar.onSave = SaveChangesToAsset;
            m_Toolbar.onControlSchemesChanged = OnControlSchemesModified;
            m_Toolbar.onControlSchemeRenamed = OnControlSchemeRenamed;
            m_Toolbar.onControlSchemeDeleted = OnControlSchemeDeleted;
            EditorApplication.wantsToQuit += EditorWantsToQuit;

            // Initialize after assembly reload.
            if (m_ActionAssetManager != null)
            {
                if (!m_ActionAssetManager.Initialize())
                {
                    // The asset we want to edit no longer exists.
                    Close();
                    return;
                }
                m_ActionAssetManager.onDirtyChanged = OnDirtyChanged;

                InitializeTrees();
            }

            InputSystem.onSettingsChange += OnInputSettingsChanged;
        }

        private void OnDestroy()
        {
            ConfirmSaveChangesIfNeeded();
            EditorApplication.wantsToQuit -= EditorWantsToQuit;
            InputSystem.onSettingsChange -= OnInputSettingsChanged;
        }

        private void OnInputSettingsChanged()
        {
            Repaint();
        }

        // Set asset would usually only be called when the window is open
        private void SetAsset(InputActionAsset asset)
        {
            if (asset == null)
                return;

            m_ActionAssetManager = new InputActionAssetManager(asset) {onDirtyChanged = OnDirtyChanged};
            m_ActionAssetManager.Initialize();

            InitializeTrees();
            LoadControlSchemes();

            // Select first action map in asset.
            m_ActionMapsTree.SelectFirstToplevelItem();

            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            var title = m_ActionAssetManager.name + " (Input Actions)";
            m_Title = new GUIContent(title);
            m_DirtyTitle = new GUIContent("(*) " + m_Title.text);
            titleContent = m_Title;
        }

        private void LoadControlSchemes()
        {
            TransferControlSchemes(save: false);
        }

        private void TransferControlSchemes(bool save)
        {
            // The easiest way to load and save control schemes is using SerializedProperties to just transfer the data
            // between the InputControlScheme array in the toolbar and the one in the asset. Doing it this way rather than
            // just overwriting the array in m_AssetManager.m_AssetObjectForEditing directly will make undo work.
            using (var editorWindowObject = new SerializedObject(this))
            using (var controlSchemesArrayPropertyInWindow = editorWindowObject.FindProperty("m_Toolbar.m_ControlSchemes"))
            using (var controlSchemesArrayPropertyInAsset = m_ActionAssetManager.serializedObject.FindProperty("m_ControlSchemes"))
            {
                Debug.Assert(controlSchemesArrayPropertyInWindow != null, $"Cannot find m_ControlSchemes in window");
                Debug.Assert(controlSchemesArrayPropertyInAsset != null, $"Cannot find m_ControlSchemes in asset");

                if (save)
                {
                    var json = controlSchemesArrayPropertyInWindow.CopyToJson();
                    controlSchemesArrayPropertyInAsset.RestoreFromJson(json);
                    editorWindowObject.ApplyModifiedProperties();
                }
                else
                {
                    // Load.
                    var json = controlSchemesArrayPropertyInAsset.CopyToJson();
                    controlSchemesArrayPropertyInWindow.RestoreFromJson(json);
                    editorWindowObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private void OnControlSchemeSelectionChanged()
        {
            OnToolbarSearchChanged();
            LoadPropertiesForSelection();
        }

        private void OnControlSchemesModified()
        {
            TransferControlSchemes(save: true);

            // Control scheme changes may affect the search filter.
            OnToolbarSearchChanged();

            ApplyAndReloadTrees();
        }

        private void OnControlSchemeRenamed(string oldBindingGroup, string newBindingGroup)
        {
            InputActionSerializationHelpers.ReplaceBindingGroup(m_ActionAssetManager.serializedObject,
                oldBindingGroup, newBindingGroup);
            ApplyAndReloadTrees();
        }

        private void OnControlSchemeDeleted(string name, string bindingGroup)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "Control scheme name should not be empty");
            Debug.Assert(!string.IsNullOrEmpty(bindingGroup), "Binding group should not be empty");

            var asset = m_ActionAssetManager.m_AssetObjectForEditing;

            var bindingMask = InputBinding.MaskByGroup(bindingGroup);
            var schemeHasBindings = asset.actionMaps.Any(m => m.bindings.Any(b => bindingMask.Matches(ref b)));
            if (!schemeHasBindings)
                return;

            ////FIXME: this does not delete composites that have bindings in only one control scheme
            ////REVIEW: offer to do nothing and leave all bindings as is?
            var deleteBindings =
                EditorUtility.DisplayDialog("Delete Bindings?",
                    $"Delete bindings for '{name}' as well? If you select 'No', the bindings will only "
                    + $"be unassigned from the '{name}' control scheme but otherwise left as is. Note that bindings "
                    + $"that are assigned to '{name}' but also to other control schemes will be left in place either way.",
                    "Yes", "No");

            InputActionSerializationHelpers.ReplaceBindingGroup(m_ActionAssetManager.serializedObject, bindingGroup, "",
                deleteOrphanedBindings: deleteBindings);

            ApplyAndReloadTrees();
        }

        private void InitializeTrees()
        {
            // We persist tree view states (most importantly, they contain our selection states),
            // so only create those if we don't have any yet.
            if (m_ActionMapsTreeState == null)
                m_ActionMapsTreeState = new TreeViewState();
            if (m_ActionsTreeState == null)
                m_ActionsTreeState = new TreeViewState();

            // Create tree in middle pane showing actions and bindings. We initially
            // leave this tree empty and populate it by selecting an action map in the
            // left pane tree.
            m_ActionsTree = new InputActionTreeView(m_ActionAssetManager.serializedObject, m_ActionsTreeState)
            {
                onSelectionChanged = OnActionTreeSelectionChanged,
                onSerializedObjectModified = ApplyAndReloadTrees,
                drawMinusButton = false,
                title = ("Actions", "A list of InputActions in the InputActionMap selected in the left pane. Also, for each InputAction, the list "
                    + "of bindings that determine the controls that can trigger the action.\n\nThe name of each action must be unique within its InputActionMap."),
            };

            // Create tree in left pane showing action maps.
            m_ActionMapsTree = new InputActionTreeView(m_ActionAssetManager.serializedObject, m_ActionMapsTreeState)
            {
                onBuildTree = () =>
                    InputActionTreeView.BuildWithJustActionMapsFromAsset(m_ActionAssetManager.serializedObject),
                onSelectionChanged = OnActionMapTreeSelectionChanged,
                onSerializedObjectModified = ApplyAndReloadTrees,
                onHandleAddNewAction = m_ActionsTree.AddNewAction,
                drawMinusButton = false,
                title = ("Action Maps", "A list of InputActionMaps in the asset. Each map can be enabled and disabled separately at runtime and holds "
                    + "its own collection of InputActions which are listed in the middle pane (along with their InputBindings).")
            };
            m_ActionMapsTree.Reload();
            m_ActionMapsTree.ExpandAll();

            RebuildActionTree();
            LoadPropertiesForSelection();

            // Sync current search status in toolbar.
            OnToolbarSearchChanged();
        }

        /// <summary>
        /// Synchronize the search filter applied to the trees.
        /// </summary>
        /// <remarks>
        /// Note that only filter the action tree. The action map tree remains unfiltered.
        /// </remarks>
        private void OnToolbarSearchChanged()
        {
            // Rather than adding FilterCriterion instances directly, we go through the
            // string-based format here. This allows typing queries directly into the search bar.

            var searchStringBuffer = new StringBuilder();

            // Plain-text search.
            if (!string.IsNullOrEmpty(m_Toolbar.searchText))
                searchStringBuffer.Append(m_Toolbar.searchText);

            // Filter by binding group of selected control scheme.
            if (m_Toolbar.selectedControlScheme != null)
            {
                searchStringBuffer.Append(" \"");
                searchStringBuffer.Append(InputActionTreeView.FilterCriterion.k_BindingGroupTag);
                searchStringBuffer.Append(m_Toolbar.selectedControlScheme.Value.bindingGroup);
                searchStringBuffer.Append('\"');
            }

            // Filter by device layout.
            if (m_Toolbar.selectedDeviceRequirement != null)
            {
                searchStringBuffer.Append(" \"");
                searchStringBuffer.Append(InputActionTreeView.FilterCriterion.k_DeviceLayoutTag);
                searchStringBuffer.Append(InputControlPath.TryGetDeviceLayout(m_Toolbar.selectedDeviceRequirement.Value.controlPath));
                searchStringBuffer.Append('\"');
            }

            var searchString = searchStringBuffer.ToString();
            if (string.IsNullOrEmpty(searchString))
                m_ActionsTree.ClearItemSearchFilterAndReload();
            else
                m_ActionsTree.SetItemSearchFilterAndReload(searchStringBuffer.ToString());

            // Have trees create new bindings with the right binding group.
            var currentBindingGroup = m_Toolbar.selectedControlScheme?.bindingGroup;
            m_ActionsTree.bindingGroupForNewBindings = currentBindingGroup;
            m_ActionMapsTree.bindingGroupForNewBindings = currentBindingGroup;
        }

        /// <summary>
        /// Synchronize the display state to the currently selected action map.
        /// </summary>
        private void OnActionMapTreeSelectionChanged()
        {
            // Re-configure action tree (middle pane) for currently select action map.
            RebuildActionTree();

            // If there's no actions in the selected action map or if there is no action map
            // selected, make sure we wipe the property pane.
            if (!m_ActionMapsTree.HasSelection() || !m_ActionsTree.rootItem.hasChildren)
            {
                LoadPropertiesForSelection();
            }
            else
            {
                // Otherwise select first action in map.
                m_ActionsTree.SelectFirstToplevelItem();
            }
        }

        private void RebuildActionTree()
        {
            var selectedActionMapItem =
                m_ActionMapsTree.GetSelectedItems().OfType<ActionMapTreeItem>().FirstOrDefault();
            if (selectedActionMapItem == null)
            {
                // Nothing selected. Wipe middle and right pane.
                m_ActionsTree.onBuildTree = null;
            }
            else
            {
                m_ActionsTree.onBuildTree = () =>
                    InputActionTreeView.BuildWithJustActionsAndBindingsFromMap(selectedActionMapItem.property);
            }

            // Rebuild tree.
            m_ActionsTree.Reload();
        }

        private void OnActionTreeSelectionChanged()
        {
            LoadPropertiesForSelection();
        }

        private void LoadPropertiesForSelection()
        {
            m_BindingPropertyView = null;
            m_ActionPropertyView = null;

            ////TODO: preserve interaction/processor selection when reloading

            // Nothing else to do if we don't have a selection in the middle pane or if
            // multiple items are selected (we don't currently have the ability to multi-edit).
            if (!m_ActionsTree.HasSelection() || m_ActionsTree.GetSelection().Count != 1)
                return;

            var item = m_ActionsTree.GetSelectedItems().FirstOrDefault();
            if (item is BindingTreeItem)
            {
                // Grab the action for the binding and see if we have an expected control layout
                // set on it. Pass that on to the control picking machinery.
                var isCompositePartBinding = item is PartOfCompositeBindingTreeItem;
                var actionItem = (isCompositePartBinding ? item.parent.parent : item.parent) as ActionTreeItem;
                Debug.Assert(actionItem != null);

                if (m_ControlPickerViewState == null)
                    m_ControlPickerViewState = new InputControlPickerState();

                // The toolbar may constrain the set of devices we're currently interested in by either
                // having one specific device selected from the current scheme or having at least a control
                // scheme selected.
                IEnumerable<string> controlPathsToMatch;
                if (m_Toolbar.selectedDeviceRequirement != null)
                {
                    // Single device selected from set of devices in control scheme.
                    controlPathsToMatch = new[] {m_Toolbar.selectedDeviceRequirement.Value.controlPath};
                }
                else if (m_Toolbar.selectedControlScheme != null)
                {
                    // Constrain to devices from current control scheme.
                    controlPathsToMatch =
                        m_Toolbar.selectedControlScheme.Value.deviceRequirements.Select(x => x.controlPath);
                }
                else
                {
                    // If there's no device filter coming from a control scheme, filter by supported
                    // devices as given by settings.
                    controlPathsToMatch = InputSystem.settings.supportedDevices.Select(x => $"<{x}>");
                }

                // Show properties for binding.
                m_BindingPropertyView =
                    new InputBindingPropertiesView(
                        item.property,
                        change =>
                        {
                            if (change == InputBindingPropertiesView.k_PathChanged ||
                                change == InputBindingPropertiesView.k_CompositePartAssignmentChanged ||
                                change == InputBindingPropertiesView.k_CompositeTypeChanged ||
                                change == InputBindingPropertiesView.k_GroupsChanged)
                            {
                                ApplyAndReloadTrees();
                            }
                            else
                            {
                                // Simple property change that doesn't affect the rest of the UI.
                                Apply();
                            }
                        },
                        m_ControlPickerViewState,
                        expectedControlLayout: item.expectedControlLayout,
                        controlSchemes: m_Toolbar.controlSchemes,
                        controlPathsToMatch: controlPathsToMatch);
            }
            else if (item is ActionTreeItem actionItem)
            {
                // Show properties for action.
                m_ActionPropertyView =
                    new InputActionPropertiesView(
                        actionItem.property,
                        // Apply without reload is enough here as modifying the properties of an action will
                        // never change the structure of the data.
                        change => Apply());
            }
        }

        private void ApplyAndReloadTrees()
        {
            Apply();

            // This path here is meant to catch *any* edits made to the serialized data. I.e. also
            // any arbitrary undo that may have changed some misc bit not visible in the trees.

            m_ActionMapsTree.Reload();
            RebuildActionTree();
            m_ActionAssetManager.UpdateAssetDirtyState();
            LoadControlSchemes();

            LoadPropertiesForSelection();
        }

        private void Apply()
        {
            m_ActionAssetManager.ApplyChanges();

            // Update dirty count, otherwise ReloadIfSerializedObjectHasBeenChanged will trigger a full ApplyAndReloadTrees
            m_ActionMapsTree.UpdateSerializedObjectDirtyCount();
            m_ActionsTree.UpdateSerializedObjectDirtyCount();

            // If auto-save is active, immediately flush out the changes to disk. Otherwise just
            // put us into dirty state.
            if (InputEditorUserSettings.autoSaveInputActionAssets)
            {
                m_ActionAssetManager.SaveChangesToAsset();
            }
            else
            {
                m_ActionAssetManager.SetAssetDirty();
                titleContent = m_DirtyTitle;
            }
        }

        private void OnGUI()
        {
            // If the actions tree has lost the filters (because they would not match an item it tried to highlight),
            // update the Toolbar UI to remove them.
            if (!m_ActionsTree.hasFilter)
                m_Toolbar.ResetSearchFilters();

            // Allow switching between action map tree and action tree using arrow keys.
            ToggleFocusUsingKeyboard(KeyCode.RightArrow, m_ActionMapsTree, m_ActionsTree);
            ToggleFocusUsingKeyboard(KeyCode.LeftArrow, m_ActionsTree, m_ActionMapsTree);

            // Route copy-paste events to tree views if they have focus.
            if (m_ActionsTree.HasFocus())
                m_ActionsTree.HandleCopyPasteCommandEvent(Event.current);
            else if (m_ActionMapsTree.HasFocus())
                m_ActionMapsTree.HandleCopyPasteCommandEvent(Event.current);

            // Draw toolbar.
            EditorGUILayout.BeginVertical();
            m_Toolbar.OnGUI();
            EditorGUILayout.Space();

            // Draw columns.
            EditorGUILayout.BeginHorizontal();
            var columnAreaWidth = position.width - InputActionTreeView.Styles.backgroundWithBorder.margin.left -
                InputActionTreeView.Styles.backgroundWithBorder.margin.left -
                InputActionTreeView.Styles.backgroundWithBorder.margin.right;

            var oldType = Event.current.type;
            DrawActionMapsColumn(columnAreaWidth * 0.22f);
            if (Event.current.type == EventType.Used && oldType != Event.current.type)
            {
                // When renaming an item, TreeViews will capture all mouse Events, and process any clicks outside the item
                // being renamed to end the renaming process. However, since we have two TreeViews, if the action column is
                // renaming an item, and then you double click on an item in the action map column, the action map column will
                // get to use the mouse event before the action collumn gets to see it, which would cause the action map column
                // to enter rename mode and use the event, before the action column gets a chance to see it and exit rename mode.
                // Then we end up with two active renaming sessions, which does not work correctly.
                // (See https://fogbugz.unity3d.com/f/cases/1140869/).
                // Now, our fix to this problem is to force-end and accept any renaming session on the action column if we see
                // that the action map column had processed the current event. This is not particularly elegant, but I cannot think
                // of a better solution as we are limited by the public APIs exposed by TreeView.
                m_ActionsTree.EndRename(forceAccept: true);
            }
            DrawActionsColumn(columnAreaWidth * 0.38f);
            DrawPropertiesColumn(columnAreaWidth * 0.40f);
            EditorGUILayout.EndHorizontal();

            // Bottom margin.
            GUILayout.Space(3);
            EditorGUILayout.EndVertical();
        }

        private static void ToggleFocusUsingKeyboard(KeyCode key, InputActionTreeView fromTree,
            InputActionTreeView toTree)
        {
            var uiEvent = Event.current;
            if (uiEvent.type == EventType.KeyDown && uiEvent.keyCode == key && fromTree.HasFocus())
            {
                if (!toTree.HasSelection())
                    toTree.SelectFirstToplevelItem();
                toTree.SetFocus();
                uiEvent.Use();
            }
        }

        private void DrawActionMapsColumn(float width)
        {
            DrawColumnWithTreeView(m_ActionMapsTree, width, true);
        }

        private void DrawActionsColumn(float width)
        {
            DrawColumnWithTreeView(m_ActionsTree, width, false);
        }

        private static void DrawColumnWithTreeView(TreeView treeView, float width, bool fixedWidth)
        {
            EditorGUILayout.BeginVertical(InputActionTreeView.Styles.backgroundWithBorder,
                fixedWidth ? GUILayout.MaxWidth(width) : GUILayout.MinWidth(width),
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            var columnRect = GUILayoutUtility.GetLastRect();

            treeView.OnGUI(columnRect);
        }

        private void DrawPropertiesColumn(float width)
        {
            EditorGUILayout.BeginVertical(InputActionTreeView.Styles.backgroundWithBorder, GUILayout.Width(width));

            var rect = GUILayoutUtility.GetRect(0,
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2,
                GUILayout.ExpandWidth(true));
            rect.x -= 2;
            rect.y -= 1;
            rect.width += 4;

            EditorGUI.LabelField(rect, GUIContent.none, InputActionTreeView.Styles.backgroundWithBorder);
            var headerRect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);

            if (m_BindingPropertyView != null)
            {
                if (m_BindingPropertiesTitle == null)
                    m_BindingPropertiesTitle = new GUIContent("Binding Properties", "The properties for the InputBinding selected in the "
                        + "'Actions' pane on the left.");
                EditorGUI.LabelField(headerRect, m_BindingPropertiesTitle, InputActionTreeView.Styles.columnHeaderLabel);
                m_PropertiesScroll = EditorGUILayout.BeginScrollView(m_PropertiesScroll);
                m_BindingPropertyView.OnGUI();
                EditorGUILayout.EndScrollView();
            }
            else if (m_ActionPropertyView != null)
            {
                if (m_ActionPropertiesTitle == null)
                    m_ActionPropertiesTitle = new GUIContent("Action Properties", "The properties for the InputAction selected in the "
                        + "'Actions' pane on the left.");
                EditorGUI.LabelField(headerRect, m_ActionPropertiesTitle, InputActionTreeView.Styles.columnHeaderLabel);
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

        private void ReloadAssetFromFileIfNotDirty()
        {
            if (m_ActionAssetManager.dirty)
                return;

            // If our asset has disappeared from disk, just close the window.
            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m_ActionAssetManager.guid)))
            {
                Close();
                return;
            }

            // Don't touch the UI state if the serialized data is still the same.
            if (!m_ActionAssetManager.ReInitializeIfAssetHasChanged())
                return;

            // Unfortunately, on this path we lose the selection state of the interactions and processors lists
            // in the properties view.

            InitializeTrees();
            LoadPropertiesForSelection();
            Repaint();
        }

        ////TODO: add shortcut to focus search box

        ////TODO: show shortcuts in tooltips
        ////FIXME: the shortcuts seem to have focus problems; often requires clicking away and then back to the window
        [Shortcut("Input Action Editor/Save", typeof(InputActionEditorWindow), KeyCode.S, ShortcutModifiers.Alt)]
        private static void SaveShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionEditorWindow)arguments.context;
            window.SaveChangesToAsset();
        }

        [Shortcut("Input Action Editor/Add Action Map", typeof(InputActionEditorWindow), KeyCode.M, ShortcutModifiers.Alt)]
        private static void AddActionMapShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionEditorWindow)arguments.context;
            window.AddNewActionMap();
        }

        [Shortcut("Input Action Editor/Add Action", typeof(InputActionEditorWindow), KeyCode.A, ShortcutModifiers.Alt)]
        private static void AddActionShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionEditorWindow)arguments.context;
            window.AddNewAction();
        }

        [Shortcut("Input Action Editor/Add Binding", typeof(InputActionEditorWindow), KeyCode.B, ShortcutModifiers.Alt)]
        private static void AddBindingShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionEditorWindow)arguments.context;
            window.AddNewBinding();
        }

        private void OnDirtyChanged(bool dirty)
        {
            titleContent = dirty ? m_DirtyTitle : m_Title;
            m_Toolbar.isDirty = dirty;
        }

        public void Dispose()
        {
            m_BindingPropertyView?.Dispose();
        }

        [SerializeField] private TreeViewState m_ActionMapsTreeState;
        [SerializeField] private TreeViewState m_ActionsTreeState;
        [SerializeField] private InputControlPickerState m_ControlPickerViewState;
        [SerializeField] private InputActionAssetManager m_ActionAssetManager;
        [SerializeField] private InputActionEditorToolbar m_Toolbar;
        [SerializeField] private GUIContent m_DirtyTitle;
        [SerializeField] private GUIContent m_Title;
        [NonSerialized] private GUIContent m_ActionPropertiesTitle;
        [NonSerialized] private GUIContent m_BindingPropertiesTitle;

        private InputBindingPropertiesView m_BindingPropertyView;
        private InputActionPropertiesView m_ActionPropertyView;
        private InputActionTreeView m_ActionMapsTree;
        private InputActionTreeView m_ActionsTree;

        private static bool s_RefreshPending;
        private static readonly string k_FileExtension = "." + InputActionAsset.Extension;

        private Vector2 m_PropertiesScroll;
        private bool m_ForceQuit;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Intantiated through reflection by Unity")]
        private class ProcessAssetModifications : UnityEditor.AssetModificationProcessor
        {
            // Handle .inputactions asset being deleted.
            // ReSharper disable once UnusedMember.Local
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "options", Justification = "options parameter required by Unity API")]
            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                    return default;

                // See if we have an open window.
                var guid = AssetDatabase.AssetPathToGUID(path);
                var window = FindEditorForAssetWithGUID(guid);
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
                            // User canceled. Stop the deletion.
                            return AssetDeleteResult.FailedDelete;
                        }

                        window.m_ForceQuit = true;
                    }

                    window.Close();
                }

                return default;
            }

            #pragma warning disable CA1801 // unused parameters

            // Handle .inputactions asset being moved.
            // ReSharper disable once UnusedMember.Local
            public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
            {
                if (!sourcePath.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                    return default;

                var guid = AssetDatabase.AssetPathToGUID(sourcePath);
                var window = FindEditorForAssetWithGUID(guid);
                if (window != null)
                {
                    window.UpdateWindowTitle();
                }

                return default;
            }

            #pragma warning restore CA1801
        }
    }
}
#endif // UNITY_EDITOR
