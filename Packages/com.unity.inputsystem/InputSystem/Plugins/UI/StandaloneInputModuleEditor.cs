#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_ENABLE_UI

using UnityEditor;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI.Editor
{
    // The only purpose of the Input System suppying a custom editor for the UI StandaloneInputModule is to guide users to using
    // the Input System's InputSystemUIInputModule instead.
    [CustomEditor(typeof(StandaloneInputModule))]
    internal class StandaloneInputModuleModuleEditor : UnityEditor.Editor
    {
        SerializedProperty enableNativePlatformBackendsForNewInputSystem;
        SerializedProperty disableOldInputManagerSupport;

        public void OnEnable()
        {
            var allPlayerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (allPlayerSettings.Length > 0)
            {
                var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>()[0];
                var so = new SerializedObject(playerSettings);
                enableNativePlatformBackendsForNewInputSystem = so.FindProperty("enableNativePlatformBackendsForNewInputSystem");
                disableOldInputManagerSupport = so.FindProperty("disableOldInputManagerSupport");
            }
        }

        public override void OnInspectorGUI()
        {
            // We assume that if these properties don't exist (ie are null), then that's because the new Input System has become the default.
            if (enableNativePlatformBackendsForNewInputSystem == null || enableNativePlatformBackendsForNewInputSystem.boolValue)
            {
                if (disableOldInputManagerSupport == null || disableOldInputManagerSupport.boolValue)
                    EditorGUILayout.HelpBox("You are using StandaloneInputModule, which uses the old InputManager. You are using the new InputSystem, and have the old InputManager disabled. StandaloneInputModule will not work. Click the button below to replace this component with a InputSystemUIInputModule, which uses the new InputSystem.", MessageType.Error);
                else
                    EditorGUILayout.HelpBox("You are using StandaloneInputModule, which uses the old InputManager. You also have the new InputSystem enabled in your project. Click the button below to replace this component with a InputSystemUIInputModule, which uses the new InputSystem (recommended).", MessageType.Info);
                if (GUILayout.Button("Replace with InputSystemUIInputModule"))
                {
                    var go = ((StandaloneInputModule)target).gameObject;
                    Undo.DestroyObjectImmediate(target);
                    Undo.AddComponent<InputSystemUIInputModule>(go);
                    return;
                }
                GUILayout.Space(10);
            }
            base.OnInspectorGUI();
        }
    }
}
#endif
