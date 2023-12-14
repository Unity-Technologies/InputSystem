#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = InputSettingsPath.kSettingsRootPath;

        [SerializeField] InputActionsEditorState m_State;
        VisualElement m_RootVisualElement;
        private bool m_HasEditFocus;
        StateContainer m_StateContainer;

        private bool m_IsActivated;
        InputAnalytics.InputActionsEditorSession m_ActionEditorAnalytics;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            m_IsActivated = true;

            m_RootVisualElement = rootElement;

            var asset = ProjectWideActionsAsset.GetOrCreate();
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(m_ActionEditorAnalytics, serializedAsset);

            BuildUI();

            // Always begin a session when activated (note that OnActivate isn't called when navigating back
            // to editor from another setting category)
            m_ActionEditorAnalytics = new InputAnalytics.InputActionsEditorSession(
                InputAnalytics.InputActionsEditorKind.EmbeddedInProjectSettings);
            m_ActionEditorAnalytics.Begin();

            // Monitor focus state of root element
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnFocusOut);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnFocusIn);

            // Note that focused element will be set if we are navigating back to
            // an existing instance when switching setting in the left project settings panel since
            // this doesn't recreate the editor.
            if (m_RootVisualElement.focusController.focusedElement != null)
                OnEditFocus();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (!m_IsActivated)
                return; // Observed that when switching back to settings from another setting OnDeactivate is called before OnActivate.
            m_IsActivated = false;

            if (m_RootVisualElement != null)
            {
                m_RootVisualElement.UnregisterCallback<FocusInEvent>(OnFocusIn);
                m_RootVisualElement.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            }

            // Note that OnDeactivate will also trigger when opening the Project Settings (existing instance).
            // Hence we guard against duplicate OnDeactivate() calls.
            if (m_HasEditFocus)
            {
                OnFocusOut();
                m_HasEditFocus = false;
            }

            // Always end a session when deactivated.
            m_ActionEditorAnalytics?.End();
        }

        private void OnFocusIn(FocusInEvent @event = null)
        {
            if (!m_HasEditFocus)
            {
                m_HasEditFocus = true;
                OnEditFocus();
            }
        }

        private void OnFocusOut(FocusOutEvent @event = null)
        {
            // This can be used to detect focus lost events of container elements, but will not detect window focus.
            // Note that `event.relatedTarget` contains the element that gains focus, which is null if we select
            // elements outside of project settings Editor Window. Also note that @event is null when we call this
            // from OnDeactivate().
            var element = (VisualElement)@event?.relatedTarget;
            if (element != null || !m_HasEditFocus)
                return;
            m_HasEditFocus = false;
            OnEditFocusLost();
        }

        private void OnEditFocus()
        {
            m_ActionEditorAnalytics.RegisterEditorFocusIn();
        }

        private void OnEditFocusLost()
        {
#if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
#endif

            m_ActionEditorAnalytics.RegisterEditorFocusOut();
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

        private void BuildUI()
        {
            m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
            m_StateContainer.StateChanged += OnStateChanged;
            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            var view = new InputActionsEditorView(m_RootVisualElement, m_StateContainer);
            view.postResetAction += OnResetAsset;
            m_StateContainer.Initialize();

            // Hide the save / auto save buttons in the project wide input actions
            // Project wide input actions always auto save
            var element = m_RootVisualElement.Q("save-asset-toolbar-container");
            if (element != null)
            {
                element.style.visibility = Visibility.Hidden;
                element.style.display = DisplayStyle.None;
            }
        }

        private void OnResetAsset(InputActionAsset newAsset)
        {
            var serializedAsset = new SerializedObject(newAsset);
            m_State = new InputActionsEditorState(m_ActionEditorAnalytics, serializedAsset);
        }

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            return new InputActionsEditorSettingsProvider(kSettingsPath, SettingsScope.Project);
        }
    }
}

#endif
