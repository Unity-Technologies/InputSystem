#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR // We use some UITK controls that are only available in 2022.2 or later (MultiColumnListView for example)
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.InputSystem.Editor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = "Project/Input System Package/Actions";
        [SerializeField] private InputActionsEditorState m_State;
        private VisualElement m_RootVisualElement;
        private StateContainer m_StateContainer;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_RootVisualElement = rootElement;
            var asset = GlobalActionsAsset.GetOrCreateGlobalActionsAsset();
            var serializedAsset = new SerializedObject(asset);
            m_State = new InputActionsEditorState(serializedAsset);
            BuildUI();
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            if (InputEditorUserSettings.autoSaveInputActionAssets)
                InputActionsEditorWindowUtils.SaveAsset(m_State.serializedObject);
        }

        private void BuildUI()
        {
            m_RootVisualElement.Unbind();
            m_StateContainer = new StateContainer(m_RootVisualElement, m_State);
            m_StateContainer.StateChanged += OnStateChanged;
            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            new InputActionsEditorView(m_RootVisualElement, m_StateContainer);
            m_StateContainer.Initialize();
        }

        [SettingsProvider]
        public static SettingsProvider CreateGlobalInputActionsEditorProvider()
        {
            if (!InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseUIToolkitEditor))
                return null;

            var provider = new InputActionsEditorSettingsProvider(kSettingsPath, SettingsScope.Project)
            {
                label = "Input Actions"
            };

            return provider;
        }
    }
}

#endif
