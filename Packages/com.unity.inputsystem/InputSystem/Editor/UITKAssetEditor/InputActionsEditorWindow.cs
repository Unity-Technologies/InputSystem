// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // TODO: Remove when UIToolkit editor is complete and set as the default editor
    [InitializeOnLoad]
    internal static class EnableUITKEditor
    {
        static EnableUITKEditor()
        {
            // set this feature flag to true to enable the UITK editor
            InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseUIToolkitEditor, false);
        }
    }

    internal class InputActionsEditorWindow : EditorWindow
    {
        private static readonly string k_FileExtension = "." + InputActionAsset.Extension;
        private int m_AssetId;
        private string m_AssetPath;
        private string m_AssetJson;
        private bool m_IsDirty;

        [OnOpenAsset]
        public static bool OpenAsset(int instanceId, int line)
        {
            if (!InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseUIToolkitEditor))
                return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                return false;

            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as InputActionAsset;
            if (asset == null)
                return false;


            var window = GetOrCreateWindow(instanceId, out var isAlreadyOpened);
            if (isAlreadyOpened)
            {
                window.Focus();
                return true;
            }
            window.m_IsDirty = false;
            window.m_AssetId = instanceId;
            window.titleContent = new GUIContent("Input Actions Editor");
            window.SetAsset(asset);
            window.Show();

            return true;
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

        private void SetAsset(InputActionAsset asset)
        {
            m_AssetPath = AssetDatabase.GetAssetPath(asset);
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(serializedAsset);
            m_AssetJson = File.ReadAllText(m_AssetPath);
            bool isGUIDObtained = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGUID, out long _);
            Debug.Assert(isGUIDObtained, $"Failed to get asset {asset.name} GUID");

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
                    m_AssetPath = AssetDatabase.GetAssetPath(asset);
                    m_AssetJson = File.ReadAllText(m_AssetPath);
                    var serializedAsset = new SerializedObject(asset);
                    m_State = new InputActionsEditorState(m_State, serializedAsset);
                }

                BuildUI();
            }
        }

        private void BuildUI()
        {
            var stateContainer = new StateContainer(rootVisualElement, m_State);
            stateContainer.StateChanged += OnStateChanged;

            var theme = EditorGUIUtility.isProSkin
                ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

            rootVisualElement.styleSheets.Add(theme);
            var view = new InputActionsEditorView(rootVisualElement, stateContainer);
            stateContainer.Initialize();
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            DirtyInputActionsEditorWindow(newState);
            if (InputEditorUserSettings.autoSaveInputActionAssets)
                SaveAsset(m_State.serializedObject);
        }

        private void DirtyInputActionsEditorWindow(InputActionsEditorState newState)
        {
            var isWindowDirty = !InputEditorUserSettings.autoSaveInputActionAssets && HasAssetChanged(newState.serializedObject);
            if (m_IsDirty == isWindowDirty)
                return;
            m_IsDirty = isWindowDirty;
            titleContent = m_IsDirty ? new GUIContent("(*) Input Actions Editor") : new GUIContent("Input Actions Editor");
        }

        private bool HasAssetChanged(SerializedObject serializedAsset)
        {
            var asset = (InputActionAsset)serializedAsset.targetObject;
            var newAssetJson = asset.ToJson();
            return newAssetJson != m_AssetJson;
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

            var result = EditorUtility.DisplayDialogComplex("Input Action Asset has been modified", $"Do you want to save the changes you made in:\n{m_AssetPath}\n\nYour changes will be lost if you don't save them.", "Save", "Cancel", "Don't Save");
            switch (result)
            {
                case 0:     // Save
                    SaveAsset(m_State.serializedObject, this);
                    break;
                case 1:    // Cancel editor quit. (open new editor window with the edited asset)
                    ReshowEditorWindowWithUnsavedChanges();
                    break;
                case 2:     // Don't save, quit - reload the old asset from the json to prevent the asset from being dirtied
                    AssetDatabase.ImportAsset(m_AssetPath);
                    break;
            }
        }

        private void ReshowEditorWindowWithUnsavedChanges()
        {
            var window = CreateWindow<InputActionsEditorWindow>();
            window.m_AssetId = m_AssetId;
            window.m_State = m_State;
            window.BuildUI();
            window.Show();
        }

        private InputActionAsset GetAssetFromDatabase()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }

        [SerializeField] private InputActionsEditorState m_State;
        [SerializeField] private string m_AssetGUID;

        public static void SaveAsset(SerializedObject serializedAsset, InputActionsEditorWindow currentWindow = null)
        {
            if ((focusedWindow == null || focusedWindow is not InputActionsEditorWindow) && currentWindow == null)
                return;
            currentWindow = currentWindow ? currentWindow : (InputActionsEditorWindow)focusedWindow;
            var asset = (InputActionAsset)serializedAsset.targetObject;
            var assetJson = asset.ToJson();

            var existingJson = File.ReadAllText(currentWindow.m_AssetPath);
            if (assetJson != existingJson)
            {
                EditorHelpers.CheckOut(currentWindow.m_AssetPath);
                File.WriteAllText(currentWindow.m_AssetPath, assetJson);
                AssetDatabase.ImportAsset(currentWindow.m_AssetPath);
                currentWindow.m_AssetJson = assetJson;
            }
        }
    }
}

#endif
