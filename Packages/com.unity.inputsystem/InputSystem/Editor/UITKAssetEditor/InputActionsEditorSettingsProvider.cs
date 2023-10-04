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
        private VisualElement m_MainElement;
        private bool m_HasFocus;
        StateContainer m_StateContainer;
        
        private InputAnalytics.InputActionsEditorSession m_AnalyticsSession;
        
        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_AnalyticsSession = new InputAnalytics.InputActionsEditorSession(
                InputAnalytics.InputActionsEditorType.EmbeddedInProjectSettings);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_RootVisualElement = rootElement;
            var asset = ProjectWideActionsAsset.GetOrCreate();
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(serializedAsset);
            BuildUI();
            
            Debug.Log("OnActivate"); // At this point we now the editor has been opened
            
            // Monitor focus state of root element
            m_RootVisualElement.focusable = true;
            m_RootVisualElement.RegisterCallback<FocusOutEvent>(OnFocusLost);
            m_RootVisualElement.RegisterCallback<FocusInEvent>(OnFocus);
            
            m_AnalyticsSession.Begin();
            
            // Note that focused element will be set if we are navigating back to
            // an existing instance when switching setting in the left project settings panel.
            if (m_RootVisualElement.focusController.focusedElement != null) 
                OnFocus(null); // note: m_HasFocus == true
        }

        public override void OnDeactivate()
        {
            // Note that OnDeactivate will also trigger when opening the Project Settings (old instance?).
            // Hence we guard against duplicate OnDeactivate() calls.
            //Debug.Log("OnDeactivate"); // At this point we know the editor has been dismissed
            
            m_AnalyticsSession.End();
            InputAnalytics.OnInputActionEditorSessionEnd(ref m_AnalyticsSession);
        }

        private void OnFocus(FocusInEvent @event)
        {
            //Debug.Log("Focus gained " + @event.target + " " + m_MainElement.focusController.focusedElement);
            if (!m_HasFocus)
            {
                m_HasFocus = true;
                
                Debug.Log("OnFocus");
                
                m_AnalyticsSession.RegisterEditorFocusIn();
            }
        }
        
        private void OnFocusLost(FocusOutEvent @event)
        {
            // This can be used to detect focus lost events, but will not detect window focus
            var element = (VisualElement)@event.relatedTarget;
            if (element == null && m_HasFocus)
            {
                m_HasFocus = false;
                
                Debug.Log("OnFocusLost");
                
                m_AnalyticsSession.RegisterEditorFocusOut();
            }
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            // Project wide input actions always auto save - don't check the asset auto save status
            InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
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
