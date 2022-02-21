using UnityEditor;

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
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("ProjectSettings/InputManager.asset");
            var serializedAsset = new SerializedObject(asset);
            var stateContainer = new StateContainer(rootVisualElement, new GlobalInputActionsEditorState(serializedAsset));

            var view = new GlobalInputActionsEditorView(rootVisualElement, stateContainer);
            stateContainer.Initialize();
        }
    }
}
