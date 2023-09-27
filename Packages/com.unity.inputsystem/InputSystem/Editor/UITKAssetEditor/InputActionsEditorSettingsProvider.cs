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
        }

        public override void OnDeactivate()
        {
            // Debug.Log("Project settings Editor destroyed, trying to save asset");
            TrySaveAsset();
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            // Project wide input actions always auto save - don't check the asset auto save status
            InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
        }

        private void TrySaveAsset()
        {
            if (m_State.serializedObject != null)
            {
                InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
            }
        }

        private void BuildUI()
        {
            m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
            //TODO: remove this
            // m_StateContainer.StateChanged += OnStateChanged;
            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            new InputActionsEditorView(m_RootVisualElement, m_StateContainer, null);
            m_StateContainer.Initialize();

            var mainElement = m_RootVisualElement.Q("actions-split-view");
            mainElement.focusable = true;
            mainElement.RegisterCallback<FocusOutEvent>(OnFocusLost);

            // Hide the save / auto save buttons in the project wide input actions
            // Project wide input actions always auto save
            var element = m_RootVisualElement.Q("save-asset-toolbar-container");
            if (element != null)
            {
                element.style.visibility = Visibility.Hidden;
                element.style.display = DisplayStyle.None;
            }
        }

        void OnFocusLost(FocusOutEvent evt)
        {
            // Workaround to find if the "main window" is out of focus.
            // `relatedTarget` contains the element that gains focus, which seems to be null if we select
            // elements outside of project settings Editor Window.
            var element = (VisualElement)evt.relatedTarget;
            if (element == null)
            {
                // Debug.Log("Focus lost, saving asset");
                //TODO Validate if something else needs to happen
                //TODO I think the state is not in a good condition if we just save here
                TrySaveAsset();
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
