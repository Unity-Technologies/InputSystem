#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine.UIElements;

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

        private void BuildUI()
        {
            {
                var element = m_RootVisualElement.Q<VisualElement>("missing-asset-section");
                if (element != null)
                {
                    element.style.visibility = hasAsset ? Visibility.Hidden : Visibility.Visible;
                    element.style.display = hasAsset ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }

            var button = m_RootVisualElement.Q<Button>("create-asset");
            if (button != null)
            {
                button.RegisterCallback<ClickEvent>(evt =>
                {
                    var result = InputAssetEditorUtils.PromptUserForAsset(
                        friendlyName: "Input Actions",
                        suggestedAssetFilePathWithoutExtension: InputAssetEditorUtils.MakeProjectFileName("Actions"),
                        assetFileExtension: "inputactions");
                    if (result.result != InputAssetEditorUtils.DialogResult.Valid)
                        return; // Either invalid path selected or cancelled by user

                    // Create a new asset
                    ProjectWideActionsAsset.CreateNewAsset(result.relativePath);

                    // Refresh asset database to allow for importer to recognize the asset
                    AssetDatabase.Refresh();

                    // Load the asset we just created and assign it as the Project-wide actions
                    var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(result.relativePath);
                    if (asset != null)
                        InputSystem.actions = asset;

                    // TODO This is not how this should be done, it should instead be triggered by InputSystem.actions being assigned since this might also happen from user code
                    m_State = new InputActionsEditorState(new SerializedObject(asset));
                    BuildUI();
                });
            }

            if (hasAsset)
            {
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

        private void OnResetAsset(InputActionAsset newAsset)
        {
            var serializedAsset = new SerializedObject(newAsset);
            m_State = new InputActionsEditorState(serializedAsset);
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
