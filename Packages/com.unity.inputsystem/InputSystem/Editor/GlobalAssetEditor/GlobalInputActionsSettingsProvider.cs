using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class GlobalInputActionsSettingsProvider : SettingsProvider
	{
		public const string kSettingsPath = "Project/Input System Package/Global Actions";

		public GlobalInputActionsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
			: base(path, scopes, keywords)
		{

		}

		public override void OnActivate(string searchContext, VisualElement rootElement)
		{
			var visualElement = Resources.Load<VisualTreeAsset>("GlobalInputActionsEditor");
			visualElement.CloneTree(rootElement);
		}

		[SettingsProvider]
		public static SettingsProvider CreateGlobalInputActionsEditorProvider()
		{
			var provider = new GlobalInputActionsSettingsProvider(kSettingsPath, SettingsScope.Project)
			{
				label = "Global Input Actions"
			};

			return provider;
		}
	}
}