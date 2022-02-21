using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsProvider : SettingsProvider
    {
        public const string kSettingsPath = "Project/Input";

        public InputSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            label = "Input";
        }

        [SettingsProvider]
        public static SettingsProvider CreateInputSettingsProvider()
        {
            return new InputSettingsProvider(kSettingsPath, SettingsScope.Project);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var visualElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                GlobalInputActionsConstants.PackagePath +
                GlobalInputActionsConstants.ResourcesPath +
                GlobalInputActionsConstants.MainEditorViewName);
            visualElement.CloneTree(rootElement);
        }
    }
}
