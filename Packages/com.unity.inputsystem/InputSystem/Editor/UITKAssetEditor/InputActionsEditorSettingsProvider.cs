#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = "Project/Input System Package/Actions";

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
            var asset = ProjectWideActionsAsset.GetOrCreate();
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(serializedAsset);
            BuildUI();

            // Monitor focus state of root element
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnEditFocusLost);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnEditFocus);

            // Note that focused element will be set if we are navigating back to
            // an existing instance when switching setting in the left project settings panel since
            // this doesn't recreate the editor.
            if (m_RootVisualElement.focusController.focusedElement != null)
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
            // This can be used to detect focus lost events of container elements, but will not detect window focus
            var element = (VisualElement)@event?.relatedTarget;
            if (element == null && m_HasEditFocus)
            {
                m_HasEditFocus = false;

                #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
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

        private void BuildUI()
        {
            m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
            m_StateContainer.StateChanged += OnStateChanged;
            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            new InputActionsEditorView(m_RootVisualElement, m_StateContainer, null);
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

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            var provider = new InputActionsEditorSettingsProvider(kSettingsPath, SettingsScope.Project)
            {
                label = "Input Actions"
            };

            return provider;
        }
    }
}

#endif
