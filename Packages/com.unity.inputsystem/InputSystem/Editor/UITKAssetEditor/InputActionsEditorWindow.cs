#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // TODO: Remove when UIToolkit editor is complete and set as the default editor
    [InitializeOnLoad]
    internal static class EnableUITKEditor
    {
        static EnableUITKEditor()
        {
            // set this feature flag to true to enable the UITK editor
            InputSystem.settings.SetInternalFeatureFlag(InputFeatureNames.kUseUIToolkitEditor, false);
        }
    }

    internal class InputActionsEditorWindow : EditorWindow
    {
        private static readonly string k_FileExtension = "." + InputActionAsset.Extension;

        [OnOpenAsset]
        public static bool OpenAsset(int instanceId, int line)
        {
            if (!InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseUIToolkitEditor))
                return false;

            var path = AssetDatabase.GetAssetPath(instanceId);
            if (!path.EndsWith(k_FileExtension, StringComparison.InvariantCultureIgnoreCase))
                return false;

            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as InputActionAsset;
            if (asset == null)
                return false;

            var window = GetWindow<InputActionsEditorWindow>();
            window.titleContent = new GUIContent("Input Actions Editor");
            window.SetAsset(asset);
            window.Show();

            return true;
        }

        private void SetAsset(InputActionAsset asset)
        {
            m_state = new InputActionsEditorState(asset);
            BuildUI(m_state);
        }

        private void CreateGUI()
        {
            // When opening the window for the first time there will be no state or asset yet.
            // In that case, SetAsset() will be called after this and at that point the UI can be created.
            // Here we only recreate the UI e.g. after a domain reload.
            if (m_state.asset != null)
            {
                // After domain reloads the state will be in a invalid state as some of the fields
                // cannot be serialized and will become null.
                // Therefore we recreate the state here using the fields which were saved.
                if (m_state.serializedObject == null)
                    m_state = new InputActionsEditorState(m_state);

                BuildUI(m_state);
            }
        }

        private void BuildUI(InputActionsEditorState state)
        {
            var stateContainer = new StateContainer(rootVisualElement, state);

            var theme = EditorGUIUtility.isProSkin
                ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
                : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

            rootVisualElement.styleSheets.Add(theme);
            var view = new InputActionsEditorView(rootVisualElement, stateContainer);
            stateContainer.Initialize();
        }

        [SerializeField] private InputActionsEditorState m_state;
    }
}

#endif
