#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public static string SettingsPath => InputSettingsPath.kSettingsRootPath;

        [SerializeField] InputActionsEditorState m_State;
        VisualElement m_RootVisualElement;
        private bool m_HasEditFocus;
        private bool m_IgnoreActionChangedCallback;
        private bool m_IsActivated;
        StateContainer m_StateContainer;
        private static InputActionsEditorSettingsProvider m_ActiveSettingsProvider;

        private InputActionsEditorView m_View;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // There is an editor bug UUM-55238 that may cause OnActivate and OnDeactivate to be called in unexpected order.
            // This flag avoids making assumptions and executing logic twice.
            if (m_IsActivated)
                return;

            // Setup root element with focus monitoring
            m_RootVisualElement = rootElement;
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnEditFocusLost);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnEditFocus);

            CreateUI();

            // Monitor any changes to InputSystem.actions for as long as this editor is active
            InputSystem.onActionsChange += BuildUI;

            // Set the asset assigned with the editor which indirectly builds the UI based on setting
            BuildUI();

            // Note that focused element will be set if we are navigating back to an existing instance when switching
            // setting in the left project settings panel since this doesn't recreate the editor.
            if (m_RootVisualElement?.focusController?.focusedElement != null)
                OnEditFocus(null);

            m_IsActivated = true;
        }

        public override void OnDeactivate()
        {
            // There is an editor bug UUM-55238 that may cause OnActivate and OnDeactivate to be called in unexpected order.
            // This flag avoids making assumptions and executing logic twice.
            if (!m_IsActivated)
                return;

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

            InputSystem.onActionsChange -= BuildUI;

            m_IsActivated = false;

            m_View?.DestroyView();
        }

        private void OnEditFocus(FocusInEvent @event)
        {
            if (!m_HasEditFocus)
            {
                m_HasEditFocus = true;
                m_ActiveSettingsProvider = this;
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
                var asset = GetAsset();
                if (asset != null)
                    ValidateAndSaveAsset(asset);
                #endif
            }
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // No action, auto-saved on edit-focus lost
            #else
            // Project wide input actions always auto save - don't check the asset auto save status
            var asset = GetAsset();
            if (asset != null)
                ValidateAndSaveAsset(asset);
            #endif
        }

        private void ValidateAndSaveAsset(InputActionAsset asset)
        {
            ProjectWideActionsAsset.Validate(asset); // Ignore validation result for save
            EditorHelpers.SaveAsset(AssetDatabase.GetAssetPath(asset), asset.ToJson());
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
            // Construct from InputSystem.actions asset
            var asset = InputSystem.actions;
            var hasAsset = asset != null;
            m_State = (asset != null) ? new InputActionsEditorState(new SerializedObject(asset)) : default;

            // Dynamically show a section indicating that an asset is missing if not currently having an associated asset
            var missingAssetSection = m_RootVisualElement.Q<VisualElement>("missing-asset-section");
            if (missingAssetSection != null)
            {
                missingAssetSection.style.visibility = hasAsset ? Visibility.Hidden : Visibility.Visible;
                missingAssetSection.style.display = hasAsset ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Allow the user to select an asset out of the assets available in the project via picker.
            // Note that we show "None" (null) even if InputSystem.actions is currently a broken/missing reference.
            var objectField = m_RootVisualElement.Q<ObjectField>("current-asset");
            if (objectField != null)
            {
                objectField.value = (asset == null) ? null : asset;
                objectField.RegisterCallback<ChangeEvent<Object>>((evt) =>
                {
                    if (evt.newValue != asset)
                        InputSystem.actions = evt.newValue as InputActionAsset;
                });
            }

            // Configure a button to allow the user to create and assign a new project-wide asset based on default template
            var createAssetButton = m_RootVisualElement.Q<Button>("create-asset");
            createAssetButton?.RegisterCallback<ClickEvent>(evt =>
            {
                var assetPath = ProjectWideActionsAsset.defaultAssetPath;
                Dialog.Result result = Dialog.Result.Discard;
                if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
                    result = Dialog.InputActionAsset.ShowCreateAndOverwriteExistingAsset(assetPath);
                if (result == Dialog.Result.Discard)
                    InputSystem.actions = ProjectWideActionsAsset.CreateDefaultAssetAtPath(assetPath);
            });

            // Remove input action editor if already present
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

            // If the editor is associated with an asset we show input action editor
            if (hasAsset)
            {
                m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
                m_StateContainer.StateChanged += OnStateChanged;
                m_View = new InputActionsEditorView(m_RootVisualElement, m_StateContainer, true, null);
                m_StateContainer.Initialize();
            }
        }

        private InputActionAsset GetAsset()
        {
            return m_State.serializedObject?.targetObject as InputActionAsset;
        }

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            return new InputActionsEditorSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        #region Shortcuts
        [Shortcut("Input Action Editor/Project Settings/Add Action Map", null, KeyCode.M, ShortcutModifiers.Alt)]
        private static void AddActionMapShortcut(ShortcutArguments arguments)
        {
            if (m_ActiveSettingsProvider is { m_HasEditFocus : true })
                m_ActiveSettingsProvider.m_StateContainer.Dispatch(Commands.AddActionMap());
        }

        [Shortcut("Input Action Editor/Project Settings/Add Action", null, KeyCode.A, ShortcutModifiers.Alt)]
        private static void AddActionShortcut(ShortcutArguments arguments)
        {
            if (m_ActiveSettingsProvider is { m_HasEditFocus : true })
                m_ActiveSettingsProvider.m_StateContainer.Dispatch(Commands.AddAction());
        }

        [Shortcut("Input Action Editor/Project Settings/Add Binding", null, KeyCode.B, ShortcutModifiers.Alt)]
        private static void AddBindingShortcut(ShortcutArguments arguments)
        {
            if (m_ActiveSettingsProvider is { m_HasEditFocus : true })
                m_ActiveSettingsProvider.m_StateContainer.Dispatch(Commands.AddBinding());
        }

        #endregion
    }
}

#endif
