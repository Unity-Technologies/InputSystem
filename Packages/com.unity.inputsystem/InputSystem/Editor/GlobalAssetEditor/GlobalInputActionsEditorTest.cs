using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class GlobalInputActionsEditorTest : EditorWindow
	{
		[MenuItem("Tests/Input System Editor/Global Input Action Editor")]
		static void ShowWindow()
		{
			var window = GetWindow<GlobalInputActionsEditorTest>();
			window.titleContent = new GUIContent("GlobalInputActionsEditorTest");
			window.Show();
		}

		void OnEnable()
		{
			var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/GlobalInputActionsTest.inputactions");
			var serializedAsset = new SerializedObject(asset);
			var stateContainer = new StateContainer(rootVisualElement, new GlobalInputActionsEditorState(serializedAsset));
		
			var theme = EditorGUIUtility.isProSkin
				? AssetDatabase.LoadAssetAtPath<StyleSheet>(GlobalInputActionsConstants.PackagePath + GlobalInputActionsConstants.ResourcesPath + "/InputAssetEditorDark.uss")
				: AssetDatabase.LoadAssetAtPath<StyleSheet>(GlobalInputActionsConstants.PackagePath + GlobalInputActionsConstants.ResourcesPath + "/InputAssetEditorLight.uss");
			
			rootVisualElement.styleSheets.Add(theme);

			var view = new GlobalInputActionsEditorView(rootVisualElement, stateContainer);
			stateContainer.Initialize();
		}
	}
}