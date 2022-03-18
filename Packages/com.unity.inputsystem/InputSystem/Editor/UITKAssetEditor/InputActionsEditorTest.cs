using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class InputActionsEditorTest : EditorWindow
	{
		[MenuItem("Tests/Input System Editor/Input Action Editor")]
		static void ShowWindow()
		{
			var window = GetWindow<InputActionsEditorTest>();
			window.titleContent = new GUIContent("InputActionsEditorTest");
			window.Show();
		}

		void OnEnable()
		{
			var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/GlobalInputActionsTest.inputactions");
			var serializedAsset = new SerializedObject(asset);
			var stateContainer = new StateContainer(rootVisualElement, new InputActionsEditorState(serializedAsset));
		
			var theme = EditorGUIUtility.isProSkin
				? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
				: AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");
			
			rootVisualElement.styleSheets.Add(theme);

			var view = new InputActionsEditorView(rootVisualElement, stateContainer);
			stateContainer.Initialize();
		}
	}
}