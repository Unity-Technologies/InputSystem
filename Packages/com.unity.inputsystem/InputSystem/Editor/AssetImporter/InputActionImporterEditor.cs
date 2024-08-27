#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine.InputSystem.Utilities;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

////TODO: support for multi-editing

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom editor that allows modifying importer settings for an <see cref="InputActionImporter"/>.
    /// </summary>
    [CustomEditor(typeof(InputActionImporter))]
    internal class InputActionImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            var inputActionAsset = GetAsset();

            // ScriptedImporterEditor in 2019.2 now requires explicitly updating the SerializedObject
            // like in other types of editors.
            serializedObject.Update();

            EditorGUILayout.Space();

            if (inputActionAsset == null)
                EditorGUILayout.HelpBox("The currently selected object is not an editable input action asset.",
                    MessageType.Info);

            // Button to pop up window to edit the asset.
            using (new EditorGUI.DisabledScope(inputActionAsset == null))
            {
                if (GUILayout.Button(GetOpenEditorButtonText(inputActionAsset), GUILayout.Height(30)))
                    OpenEditor(inputActionAsset);
            }

            EditorGUILayout.Space();

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            // Project-wide Input Actions Asset UI.
            InputAssetEditorUtils.DrawMakeActiveGui(InputSystem.actions, inputActionAsset,
                inputActionAsset ? inputActionAsset.name : "Null", "Project-wide Input Actions",
                (value) => InputSystem.actions = value, !EditorApplication.isPlayingOrWillChangePlaymode);

            EditorGUILayout.Space();
#endif

            // Importer settings UI.
            var generateWrapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");
            EditorGUILayout.PropertyField(generateWrapperCodeProperty, m_GenerateWrapperCodeLabel);
            if (generateWrapperCodeProperty.boolValue)
            {
                var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");
                var wrapperClassNameProperty = serializedObject.FindProperty("m_WrapperClassName");
                var wrapperCodeNamespaceProperty = serializedObject.FindProperty("m_WrapperCodeNamespace");

                EditorGUILayout.BeginHorizontal();

                string defaultFileName = "";
                if (inputActionAsset != null)
                {
                    var assetPath = AssetDatabase.GetAssetPath(inputActionAsset);
                    defaultFileName = Path.ChangeExtension(assetPath, ".cs");
                }

                wrapperCodePathProperty.PropertyFieldWithDefaultText(m_WrapperCodePathLabel, defaultFileName);

                if (GUILayout.Button("…", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
                {
                    var fileName = EditorUtility.SaveFilePanel("Location for generated C# file",
                        Path.GetDirectoryName(defaultFileName),
                        Path.GetFileName(defaultFileName), "cs");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        if (fileName.StartsWith(Application.dataPath))
                            fileName = "Assets/" + fileName.Substring(Application.dataPath.Length + 1);

                        wrapperCodePathProperty.stringValue = fileName;
                    }
                }
                EditorGUILayout.EndHorizontal();

                string typeName = null;
                if (inputActionAsset != null)
                    typeName = CSharpCodeHelpers.MakeTypeName(inputActionAsset?.name);
                wrapperClassNameProperty.PropertyFieldWithDefaultText(m_WrapperClassNameLabel, typeName ?? "<Class name>");

                if (!CSharpCodeHelpers.IsEmptyOrProperIdentifier(wrapperClassNameProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# identifier", MessageType.Error);

                wrapperCodeNamespaceProperty.PropertyFieldWithDefaultText(m_WrapperCodeNamespaceLabel, "<Global namespace>");

                if (!CSharpCodeHelpers.IsEmptyOrProperNamespaceName(wrapperCodeNamespaceProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# namespace name", MessageType.Error);
            }

            // Using ApplyRevertGUI requires calling Update and ApplyModifiedProperties around the serializedObject,
            // and will print warning messages otherwise (see warning message in ApplyRevertGUI implementation).
            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        private InputActionAsset GetAsset()
        {
            return assetTarget as InputActionAsset;
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        protected override bool ShouldHideOpenButton()
        {
            return IsProjectWideActionsAsset();
        }

        private bool IsProjectWideActionsAsset()
        {
            return IsProjectWideActionsAsset(GetAsset());
        }

        private static bool IsProjectWideActionsAsset(InputActionAsset asset)
        {
            return !ReferenceEquals(asset, null) && InputSystem.actions == asset;
        }

#endif

        private string GetOpenEditorButtonText(InputActionAsset asset)
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            if (IsProjectWideActionsAsset(asset))
                return "Edit in Project Settings Window";
#endif
            return "Edit Asset";
        }

        private static void OpenEditor(InputActionAsset asset)
        {
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            // Redirect to Project-settings Input Actions editor if this is the project-wide actions asset
            if (IsProjectWideActionsAsset(asset))
            {
                SettingsService.OpenProjectSettings(InputSettingsPath.kSettingsRootPath);
                return;
            }

            // Redirect to UI-Toolkit window editor if not configured to use IMGUI explicitly
            if (!InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets))
                InputActionsEditorWindow.OpenEditor(asset);
            else
                InputActionEditorWindow.OpenEditor(asset);
#else
            // Redirect to IMGUI editor
            InputActionEditorWindow.OpenEditor(asset);
#endif
        }

        private readonly GUIContent m_GenerateWrapperCodeLabel = EditorGUIUtility.TrTextContent("Generate C# Class");
        private readonly GUIContent m_WrapperCodePathLabel = EditorGUIUtility.TrTextContent("C# Class File");
        private readonly GUIContent m_WrapperClassNameLabel = EditorGUIUtility.TrTextContent("C# Class Name");
        private readonly GUIContent m_WrapperCodeNamespaceLabel = EditorGUIUtility.TrTextContent("C# Class Namespace");
    }
}
#endif // UNITY_EDITOR
