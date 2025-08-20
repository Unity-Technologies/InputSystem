#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        private static InputActionsEditorSettingsProvider s_Provider;

        public static string SettingsPath => InputSettingsPath.kSettingsRootPath;

        [SerializeField] InputActionsEditorState m_State;
        VisualElement m_RootVisualElement;
        private bool m_HasEditFocus;
        private bool m_IsActivated;
        private static bool m_IMGUIDropdownVisible;
        StateContainer m_StateContainer;
        private static InputActionsEditorSettingsProvider m_ActiveSettingsProvider;

        private InputActionsEditorView m_View;

        private InputActionsEditorSessionAnalytic m_ActionEditorAnalytics;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // There is an editor bug UUM-55238 that may cause OnActivate and OnDeactivate to be called in unexpected order.
            // This flag avoids making assumptions and executing logic twice.
            if (m_IsActivated)
                return;

            // Monitor play mode state changes
            EditorApplication.playModeStateChanged += ModeChanged;

            // Setup root element with focus monitoring
            m_RootVisualElement = rootElement;
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnFocusOut);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnFocusIn);

            // Always begin a session when activated (note that OnActivate isn't called when navigating back
            // to editor from another setting category)
            m_ActionEditorAnalytics = new InputActionsEditorSessionAnalytic(
                InputActionsEditorSessionAnalytic.Data.Kind.EmbeddedInProjectSettings);
            m_ActionEditorAnalytics.Begin();

            CreateUI();

            // Monitor any changes to InputSystem.actions for as long as this editor is active
            InputSystem.onActionsChange += BuildUI;

            // Set the asset assigned with the editor which indirectly builds the UI based on setting
            BuildUI();

            // Note that focused element will be set if we are navigating back to an existing instance when switching
            // setting in the left project settings panel since this doesn't recreate the editor.
            if (m_RootVisualElement?.focusController?.focusedElement != null)
                OnFocusIn();

            m_IsActivated = true;
        }

        public override void OnDeactivate()
        {
            // There is an editor bug UUM-55238 that may cause OnActivate and OnDeactivate to be called in unexpected order.
            // This flag avoids making assumptions and executing logic twice.
            if (!m_IsActivated)
                return;

            // Stop monitoring play mode state changes
            EditorApplication.playModeStateChanged -= ModeChanged;

            if (m_RootVisualElement != null)
            {
                m_RootVisualElement.UnregisterCallback<FocusInEvent>(OnFocusIn);
                m_RootVisualElement.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            }

            // Make sure any remaining changes are actually saved
            SaveAssetOnFocusLost();

            // Note that OnDeactivate will also trigger when opening the Project Settings (existing instance).
            // Hence we guard against duplicate OnDeactivate() calls.
            if (m_HasEditFocus)
            {
                OnFocusOut();
                m_HasEditFocus = false;
            }

            InputSystem.onActionsChange -= BuildUI;

            m_IsActivated = false;

            // Always end a session when deactivated.
            m_ActionEditorAnalytics?.End();

            m_View?.DestroyView();
        }

        private void OnFocusIn(FocusInEvent @event = null)
        {
            if (!m_HasEditFocus)
            {
                m_HasEditFocus = true;
                m_ActionEditorAnalytics.RegisterEditorFocusIn();
                m_ActiveSettingsProvider = this;
                SetIMGUIDropdownVisible(false, false);
            }
        }

        void SaveAssetOnFocusLost()
        {
#if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            var asset = GetAsset();
            if (asset != null)
                ValidateAndSaveAsset(asset);
#endif
        }

        public static void SetIMGUIDropdownVisible(bool visible, bool optionWasSelected)
        {
            if (m_ActiveSettingsProvider == null)
                return;

            // If we selected an item from the dropdown, we *should* still be focused on this settings window - but
            // since the IMGUI dropdown is technically a separate window, we have to refocus manually.
            //
            // If we didn't select a dropdown option, there's not a simple way to know where the focus has gone,
            // so assume we lost focus and save if appropriate. ISXB-801
            if (!visible && m_IMGUIDropdownVisible)
            {
                if (optionWasSelected)
                    m_ActiveSettingsProvider.m_RootVisualElement.Focus();
                else
                    m_ActiveSettingsProvider.SaveAssetOnFocusLost();
            }
            else if (visible && !m_IMGUIDropdownVisible)
            {
                m_ActiveSettingsProvider.m_HasEditFocus = false;
            }

            m_IMGUIDropdownVisible = visible;
        }

        private async void DelayFocusLost(bool relatedTargetWasNull)
        {
            await Task.Delay(120);

            // We delay this call to ensure that the IMGUI flag has a chance to change first.
            if (relatedTargetWasNull && m_HasEditFocus && !m_IMGUIDropdownVisible)
            {
                m_HasEditFocus = false;
                SaveAssetOnFocusLost();
            }
        }

        private void OnFocusOut(FocusOutEvent @event = null)
        {
            // This can be used to detect focus lost events of container elements, but will not detect window focus.
            // Note that `event.relatedTarget` contains the element that gains focus, which is null if we select
            // elements outside of project settings Editor Window. Also note that @event is null when we call this
            // from OnDeactivate().
            var element = (VisualElement)@event?.relatedTarget;

            m_ActionEditorAnalytics.RegisterEditorFocusOut();

            DelayFocusLost(element == null);
        }

        private void OnStateChanged(InputActionsEditorState newState, UIRebuildMode editorRebuildMode)
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
            ProjectWideActionsAsset.Verify(asset); // Ignore verification result for save
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
            m_State = (asset != null) ? new InputActionsEditorState(m_ActionEditorAnalytics, new SerializedObject(asset)) : default;

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

                // Prevent reassignment in in editor which would result in exception during play-mode
                objectField.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
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
                VisualElement element = m_RootVisualElement.Q("action-editor");
                if (element != null)
                    m_RootVisualElement.Remove(element);
            }

            // If the editor is associated with an asset we show input action editor
            if (hasAsset)
            {
                m_StateContainer = new StateContainer(m_State, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
                m_StateContainer.StateChanged += OnStateChanged;
                m_View = new InputActionsEditorView(m_RootVisualElement, m_StateContainer, true, null);
                m_StateContainer.Initialize(m_RootVisualElement.Q("action-editor"));
            }
        }

        private InputActionAsset GetAsset()
        {
            return m_State.serializedObject?.targetObject as InputActionAsset;
        }

        private void SetObjectFieldEnabled(bool enabled)
        {
            // Update object picker enabled state based off editor play mode
            if (m_RootVisualElement != null)
                UQueryExtensions.Q<ObjectField>(m_RootVisualElement, "current-asset")?.SetEnabled(enabled);
        }

        private void ModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredEditMode:
                    SetObjectFieldEnabled(true);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    // Ensure any changes are saved to the asset; FocusLost isn't always triggered when entering PlayMode.
                    SaveAssetOnFocusLost();
                    SetObjectFieldEnabled(false);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingPlayMode:
                default:
                    break;
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            if (s_Provider == null)
                s_Provider = new InputActionsEditorSettingsProvider(SettingsPath, SettingsScope.Project);

            return s_Provider;
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
