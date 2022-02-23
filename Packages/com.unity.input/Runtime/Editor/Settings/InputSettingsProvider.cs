#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputSettingsProvider : SettingsProvider
    {
        private StateContainer m_StateContainer;
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
            var serializedAsset = new SerializedObject(InputSystem.settings.actions);
            var path = AssetDatabase.GetAssetPath(InputSystem.settings.actions);
            m_StateContainer = new StateContainer(rootElement, new GlobalInputActionsEditorState(serializedAsset));

            var view = new GlobalInputActionsEditorView(rootElement, m_StateContainer);
            m_StateContainer.Initialize();
        }

        public override void OnDeactivate()
        {
            m_StateContainer?.Dispose();
            m_StateContainer = null;

            base.OnDeactivate();
        }
    }
}
#endif
