#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_UI_TK_ASSET_EDITOR // We use some UITK controls that are only available in 2022.2 or later (MultiColumnListView for example)
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = "Project/Input System Package/Actions";

        public InputActionsEditorSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var visualElement = Resources.Load<VisualTreeAsset>("InputActionsEditor");
            visualElement.CloneTree(rootElement);
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
