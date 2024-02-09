#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // TODO This editor should react to InputSystem.actions being reassigned from outside

    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        private class ImportDetector : AssetPostprocessor
        {
        }

        public const string kSettingsPath = InputSettingsPath.kSettingsRootPath;

        [SerializeField] InputActionsEditorState m_State;
        VisualElement m_RootVisualElement;
        private bool m_HasEditFocus;
        StateContainer m_StateContainer;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_RootVisualElement = rootElement;
            var asset = InputSystem.actions;
            if (asset != null)
                m_State = new InputActionsEditorState(new SerializedObject(asset));

            CreateUI();
            BuildUI();

            // Monitor focus state of root element
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnEditFocusLost);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnEditFocus);

            // Note that focused element will be set if we are navigating back to
            // an existing instance when switching setting in the left project settings panel since
            // this doesn't recreate the editor.
            if (m_RootVisualElement?.focusController?.focusedElement != null)
                OnEditFocus(null);

            InputSystem.onActionsChange += OnActionsChange;
        }

        public override void OnDeactivate()
        {
            if (m_RootVisualElement != null)
            {
                m_RootVisualElement.UnregisterCallback<FocusOutEvent>(OnEditFocusLost);
                m_RootVisualElement.UnregisterCallback<FocusInEvent>(OnEditFocus);
            }

            // Note that OnDeactivate will also trigger when opening the Project Settings (existing instance).
            // Hence we guard against duplicate OnDeactivate() calls.
            if (m_HasEditFocus)
            {
                OnEditFocusLost(null);
                m_HasEditFocus = false;
            }

            InputSystem.onActionsChange -= OnActionsChange;
        }

        private void OnActionsChange()
        {
            m_State = InputSystem.actions != null ? new InputActionsEditorState(new SerializedObject(InputSystem.actions)) : default;
            // Editor will already be present so we need to update it or remove the old one
            BuildUI();
        }

        private void OnEditFocus(FocusInEvent @event)
        {
            if (!m_HasEditFocus)
            {
                m_HasEditFocus = true;
            }
        }

        private void OnEditFocusLost(FocusOutEvent @event)
        {
            // This can be used to detect focus lost events of container elements, but will not detect window focus.
            // Note that `event.relatedTarget` contains the element that gains focus, which is null if we select
            // elements outside of project settings Editor Window. Also note that @event is null when we call this
            // from OnDeactivate().
            var element = (VisualElement)@event?.relatedTarget;
            if (element == null && m_HasEditFocus)
            {
                m_HasEditFocus = false;

                #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
                if (hasAsset)
                    InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
                #endif
            }
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // No action, auto-saved on edit-focus lost
            #else
            // Project wide input actions always auto save - don't check the asset auto save status
            InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
            #endif
        }

        private void CreateUI()
        {
            var projectSettingsAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.ProjectSettingsUxml);

            projectSettingsAsset.CloneTree(m_RootVisualElement);

            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
        }

        private void RemoveOldEditors()
        {
            VisualElement element;
            do
            {
                element = m_RootVisualElement.Q("action-editor");
                if (element != null)
                    m_RootVisualElement.Remove(element);
            }
            while (element != null);
        }

        private void BuildUI()
        {
            var missingAssetSection = m_RootVisualElement.Q<VisualElement>("missing-asset-section");
            if (missingAssetSection != null)
            {
                missingAssetSection.style.visibility = hasAsset ? Visibility.Hidden : Visibility.Visible;
                missingAssetSection.style.display = hasAsset ? DisplayStyle.None : DisplayStyle.Flex;
            }

            var objectField = m_RootVisualElement.Q<ObjectField>("current-asset");
            if (objectField != null)
            {
                objectField.value = InputSystem.actions;
                objectField.RegisterCallback<ChangeEvent<Object>>((evt) =>
                {
                    InputSystem.actions = evt.newValue as InputActionAsset;

                    // UI updated via OnActionsChange
                });
            }

            var createAssetButton = m_RootVisualElement.Q<Button>("create-asset");
            if (createAssetButton != null)
            {
                createAssetButton.RegisterCallback<ClickEvent>(evt =>
                {
                    // Create a new asset
                    ProjectWideActionsAsset.CreateNewAsset("Assets/InputSystem_Actions.inputactions");

                    // Why doesn't OnActionsChange pick this change up? For some reason we need BuildUI call here :
                    m_State = InputSystem.actions != null ? new InputActionsEditorState(new SerializedObject(InputSystem.actions)) : default;
                    BuildUI();
                });
            }

            // Remove input action editor if already present
            RemoveOldEditors();

            if (hasAsset)
            {
                // Show input action editor
                m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
                m_StateContainer.StateChanged += OnStateChanged;
                var view = new InputActionsEditorView(m_RootVisualElement, m_StateContainer, true);
                view.postResetAction += OnResetAsset;
                m_StateContainer.Initialize();
            }

            // Hide the save / auto save buttons in the project wide input actions
            // Project wide input actions always auto save
            {
                var element = m_RootVisualElement.Q("save-asset-toolbar-container");
                if (element != null)
                {
                    element.style.visibility = Visibility.Hidden;
                    element.style.display = DisplayStyle.None;
                }
            }
        }

        private static void CreateNewActionAsset()
        {
            var result = InputAssetEditorUtils.PromptUserForAsset(
                friendlyName: "Input Actions",
                suggestedAssetFilePathWithoutExtension: InputAssetEditorUtils.MakeProjectFileName("Actions"),
                assetFileExtension: "inputactions");
            if (result.result != InputAssetEditorUtils.DialogResult.Valid)
                return; // Either invalid path selected or cancelled by user

            // Create a new asset
            ProjectWideActionsAsset.CreateNewAsset(result.relativePath);
        }

        private void OnResetAsset(InputActionAsset newAsset)
        {
            var serializedAsset = new SerializedObject(newAsset);
            m_State = new InputActionsEditorState(serializedAsset);
            BuildUI();
        }

        private bool hasAsset => m_State.serializedObject != null;

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            return new InputActionsEditorSettingsProvider(kSettingsPath, SettingsScope.Project);
        }
    }
}

#endif
