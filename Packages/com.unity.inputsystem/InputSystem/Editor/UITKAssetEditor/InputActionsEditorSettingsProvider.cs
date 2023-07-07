#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR // We use some UITK controls that are only available in 2022.2 or later (MultiColumnListView for example)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = "Project/Input System Package/Actions";
        internal const string kGlobalActionsAssetPath = "ProjectSettings/InputManager.asset";
        internal const string kDefaultGlobalActionsPath = "Packages/com.unity.inputsystem/InputSystem/API/GlobalInputActions.inputactions";
        [SerializeField] private InputActionsEditorState m_State;
        private VisualElement m_RootVisualElement;

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_RootVisualElement = rootElement;
            var asset = LoadGlobalActionAsset();
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
            var stateContainer = new StateContainer(m_RootVisualElement, m_State);
            stateContainer.StateChanged += OnStateChanged;

            m_RootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            new InputActionsEditorView(m_RootVisualElement, stateContainer);
            stateContainer.Initialize();
        }

        private InputActionAsset LoadGlobalActionAsset()
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(kGlobalActionsAssetPath);
            if (objects == null)
                throw new InvalidOperationException("Couldn't load global input system actions because the InputManager.asset file is missing or corrupt.");

            var globalInputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == "GlobalInputActions") as InputActionAsset;
            if (globalInputActionsAsset != null)
                return globalInputActionsAsset;

            var json = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, kDefaultGlobalActionsPath));

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.LoadFromJson(json);
            return asset;
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
