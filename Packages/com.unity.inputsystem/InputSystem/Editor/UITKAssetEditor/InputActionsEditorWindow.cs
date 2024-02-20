// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // TODO: Remove when UIToolkit editor is complete and set as the default editor
    [InitializeOnLoad]
    internal static class EnableUITKEditor
    {
        static EnableUITKEditor()
        {
        }
    }

    internal class InputActionsEditorWindow : EditorWindow, IInputActionsEditor
    {
        // TODO Consider moving state into its own struct so it can just be assigned or reset

        static readonly Vector2 k_MinWindowSize = new Vector2(650, 450);

        [SerializeField] private InputActionsEditorState m_State;
        [SerializeField] private string m_AssetGUID;

        private int m_AssetId;
        private string m_AssetJson;
        private bool m_IsDirty;

        [OnOpenAsset]
        public static bool OpenAsset(int instanceId, int line)
        {
            if (InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets))
                return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!InputActionImporter.IsInputActionAssetPath(path))
                return false;

            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as InputActionAsset;

            string actionMapToSelect = null;
            string actionToSelect = null;

            // Means we're dealing with an InputActionReference, e.g. when expanding the an .input action asset
            // on the Asset window and selecting an Action.
            if (asset == null)
            {
                var actionReference = obj as InputActionReference;
                if (actionReference != null && actionReference.asset != null)
                {
                    asset = actionReference.asset;
                    actionMapToSelect = actionReference.action.actionMap?.name;
                    actionToSelect = actionReference.action?.name;
                }
                else
                {
                    return false;
                }
            }

            OpenWindow(asset, actionMapToSelect, actionToSelect);
            return true;
        }

        private static InputActionsEditorWindow OpenWindow(InputActionAsset asset, string actionMapToSelect = null, string actionToSelect = null)
        {
            int instanceId = asset.GetInstanceID();

            ////REVIEW: It'd be great if the window got docked by default but the public EditorWindow API doesn't allow that
            ////        to be done for windows that aren't singletons (GetWindow<T>() will only create one window and it's the
            ////        only way to get programmatic docking with the current API).
            // See if we have an existing editor window that has the asset open.
            var window = GetOrCreateWindow(instanceId, out var isAlreadyOpened);
            if (isAlreadyOpened)
            {
                window.Focus();
                return window;
            }

            window.m_IsDirty = false;
            window.m_AssetId = instanceId;
            window.minSize = k_MinWindowSize;
            window.SetAsset(asset, actionToSelect, actionMapToSelect);
            window.Show();

            return window;
        }

        private static GUIContent GetEditorTitle(InputActionAsset asset, bool isDirty)
        {
            var text = asset.name + " (Input Actions Editor)";
            if (isDirty)
                text = "(*) " + text;
            return new GUIContent(text);
        }

        /// <summary>
        /// Open the specified <paramref name="asset"/> in an editor window. Used when someone hits the "Edit Asset" button in the
        /// importer inspector.
        /// </summary>
        /// <param name="asset">The InputActionAsset to open.</param>
        /// <returns>The editor window.</returns>
        public static InputActionsEditorWindow OpenEditor(InputActionAsset asset)
        {
            return OpenWindow(asset, null, null);
        }

        private static InputActionsEditorWindow GetOrCreateWindow(int id, out bool isAlreadyOpened)
        {
            isAlreadyOpened = false;
            if (HasOpenInstances<InputActionsEditorWindow>())
            {
                var openWindows = Resources.FindObjectsOfTypeAll(typeof(InputActionsEditorWindow)) as InputActionsEditorWindow[];
                var alreadyOpenWindow = openWindows?.ToList().FirstOrDefault(window => window.m_AssetId.Equals(id));
                isAlreadyOpened = alreadyOpenWindow != null;
                return isAlreadyOpened ? alreadyOpenWindow : CreateWindow<InputActionsEditorWindow>();
            }
            return GetWindow<InputActionsEditorWindow>();
        }

        /*private void SetAsset(string assetPath, string actionToSelect = null, string actionMapToSelect = null)
        {
            SetAsset(AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath), actionToSelect, actionMapToSelect);
        }

        private void SetAsset(string assetPath)
        {
            var actionToSelect = m_State.
            SetAsset(AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath), actionToSelect, actionMapToSelect);
        }*/

        private void SetAsset(InputActionAsset asset, string actionToSelect = null, string actionMapToSelect = null)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(serializedAsset);

            // Select the action that was selected on the Asset window.
            if (actionMapToSelect != null && actionToSelect != null)
            {
                m_State = m_State.SelectActionMap(actionMapToSelect);
                m_State = m_State.SelectAction(actionToSelect);
            }

            // Read and cache the asset content into m_AssetJson
            m_AssetJson = File.ReadAllText(assetPath);
            bool isGUIDObtained = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGUID, out long _);
            Debug.Assert(isGUIDObtained, $"Failed to get asset {asset.name} GUID");

            // Update window title based on associated asset
            titleContent = GetEditorTitle(asset, m_IsDirty);

            BuildUI();
        }

        private void CreateGUI()
        {
            // When opening the window for the first time there will be no state or asset yet.
            // In that case, we don't do anything as SetAsset() will be called later and at that point the UI can be created.
            // Here we only recreate the UI e.g. after a domain reload.
            if (!string.IsNullOrEmpty(m_AssetGUID))
            {
                // After domain reloads the state will be in a invalid state as some of the fields
                // cannot be serialized and will become null.
                // Therefore we recreate the state here using the fields which were saved.
                if (m_State.serializedObject == null)
                {
                    var asset = GetAssetFromDatabase();
                    if (asset != null)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(asset);
                        m_AssetJson = File.ReadAllText(assetPath);
                        var serializedAsset = new SerializedObject(asset);
                        m_State = new InputActionsEditorState(m_State, serializedAsset);
                    }
                    else
                    {
                        // Asset cannot be retrieved or doesn't exist anymore; abort opening the Window.
                        Debug.LogWarning($"Failed to open InputActionAsset with GUID {m_AssetGUID}. The asset might have been deleted.");
                        this.Close();
                        return;
                    }
                }

                BuildUI();
            }
        }

        private void BuildUI()
        {
            var stateContainer = new StateContainer(rootVisualElement, m_State);
            stateContainer.StateChanged += OnStateChanged;

            rootVisualElement.Clear();
            if (!rootVisualElement.styleSheets.Contains(InputActionsEditorWindowUtils.theme))
                rootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            var view = new InputActionsEditorView(rootVisualElement, stateContainer, false);
            view.postSaveAction += PostSaveAction;
            stateContainer.Initialize();
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            DirtyInputActionsEditorWindow(newState);
            m_State = newState;

            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // No action taken apart from setting dirty flag, auto-save triggered as part of having a dirty asset
            // and editor loosing focus instead.
            #else
            if (InputEditorUserSettings.autoSaveInputActionAssets)
                Save();
            #endif
        }

        private void UpdateWindowTitle()
        {
            titleContent = GetEditorTitle(m_State.serializedObject.targetObject as InputActionAsset, m_IsDirty);
        }

        private void Save()
        {
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);


            // TODO Check if valid to save asset
            // TODO Should really detect if editing project wide asset here and run validation on it if editing in free-floating editor
            var asset = m_State.serializedObject.targetObject as InputActionAsset;
            InputActionAssetManager.SaveAsset(asset);
            PostSaveAction();
        }

        private void PostSaveAction()
        {
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);

            //Debug.Assert(File.Exists(path));
            m_IsDirty = false;
            m_AssetJson = File.ReadAllText(path);
            UpdateWindowTitle();
        }

        private void DirtyInputActionsEditorWindow(InputActionsEditorState newState)
        {
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // Window is dirty is equivalent to if asset has changed
            var isWindowDirty = HasAssetChanged(newState.serializedObject);
            #else
            // Window is dirty is never true since every change is auto-saved
            var isWindowDirty = !InputEditorUserSettings.autoSaveInputActionAssets && HasAssetChanged(newState.serializedObject);
            #endif

            if (m_IsDirty == isWindowDirty)
                return;
            m_IsDirty = isWindowDirty;
            UpdateWindowTitle();
        }

        private bool HasAssetChanged(SerializedObject serializedAsset)
        {
            var editedAsset = serializedAsset.targetObject as InputActionAsset;
            return editedAsset.HasChanged(m_AssetJson);
        }

        private void OnLostFocus()
        {
            // Auto-save triggers on focus-lost instead of on every change
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            if (InputEditorUserSettings.autoSaveInputActionAssets && m_IsDirty)
                Save();
            #endif
        }

        private void OnDestroy()
        {
            ConfirmSaveChangesIfNeeded();
        }

        private void ConfirmSaveChangesIfNeeded()
        {
            // Do we have unsaved changes?
            if (!m_IsDirty)
                return;

            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(assetPath))
                return; // TODO

            var result = InputActionsEditorWindowUtils.ConfirmSaveChanges(assetPath);
            switch (result)
            {
                case InputActionsEditorWindowUtils.DialogResult.Save:
                    Save();
                    break;
                case InputActionsEditorWindowUtils.DialogResult.Cancel:
                    // Cancel editor quit. (open new editor window with the edited asset)
                    ReshowEditorWindowWithUnsavedChanges();
                    break;
                case InputActionsEditorWindowUtils.DialogResult.DontSave:
                    // Don't save, quit - reload the old asset from the json to prevent the asset from being dirtied
                    AssetDatabase.ImportAsset(assetPath);
                    break;
            }
        }

        private void ReshowEditorWindowWithUnsavedChanges()
        {
            var window = CreateWindow<InputActionsEditorWindow>();
            CopyOldStatsToNewWindow(window);
            window.BuildUI();
            window.Show();
        }

        private void CopyOldStatsToNewWindow(InputActionsEditorWindow window)
        {
            window.m_AssetId = m_AssetId;
            window.m_State = m_State;
            window.m_AssetJson = m_AssetJson;
            window.m_IsDirty = true;
        }

        private InputActionAsset GetAssetFromDatabase()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }

        /*public static InputActionsEditorWindow FindEditorForAsset(InputActionAsset asset)
        {
            var windows = Resources.FindObjectsOfTypeAll<InputActionsEditorWindow>();
            return windows.FirstOrDefault(w => w.ImportedAssetObjectEquals(asset));
        }*/

        public static InputActionsEditorWindow FindEditorForAssetWithGUID(string guid)
        {
            var windows = Resources.FindObjectsOfTypeAll<InputActionsEditorWindow>();
            return windows.FirstOrDefault(w => w.m_AssetGUID == guid);
        }

        #region IInputActionEditorWindow

        public string assetGUID => m_AssetGUID;
        public bool isDirty => m_IsDirty;
        public void Dismiss(bool forceQuit = false)
        {
            if (forceQuit)
                m_ForceQuit = true;
            Close();
        }

        public void OnMove()
        {
            // Remap GUID to asset path
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(path))
            {
                // Associated asset do not exist
                Close(); // TODO Better to revert to empty window
            }

            UpdateWindowTitle();
        }

        public void Refresh()
        {
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(path))
                Close();

            // Don't touch the UI state if the serialized data is still the same.
            //if (!m_ActionAssetManager.ReInitializeIfAssetHasChanged())
            //    return;

            // Unfortunately, on this path we lose the selection state of the interactions and processors lists
            // in the properties view.

            //InitializeTrees();
            //LoadPropertiesForSelection();
            //Repaint();
            //SetAsset(m_AssetPath, null, null); // TODO Preserve selection (if possible)

            //var guid = AssetDatabase.AssetPathToGUID(m_AssetPath);

            //OnStateChanged(m_State);
        }

        private bool m_ForceQuit = false;

        #endregion
    }
}

#endif
