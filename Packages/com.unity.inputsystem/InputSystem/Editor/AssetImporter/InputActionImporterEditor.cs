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
            // ScriptedImporterEditor in 2019.2 now requires explicitly updating the SerializedObject
            // like in other types of editors.
            #if UNITY_2019_2_OR_NEWER
            serializedObject.Update();
            #endif

            // Button to pop up window to edit the asset.
            if (GUILayout.Button("Edit asset"))
                InputActionEditorWindow.OnOpenAsset(GetAsset().GetInstanceID(), 0);

            EditorGUILayout.Space();

            // Importer settings UI.
            var generateWrapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");
            EditorGUILayout.PropertyField(generateWrapperCodeProperty, m_GenerateWrapperCodeLabel);
            if (generateWrapperCodeProperty.boolValue)
            {
                var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");
                var wrapperClassNameProperty = serializedObject.FindProperty("m_WrapperClassName");
                var wrapperCodeNamespaceProperty = serializedObject.FindProperty("m_WrapperCodeNamespace");

                EditorGUILayout.BeginHorizontal();
                var assetPath = AssetDatabase.GetAssetPath(GetAsset());
                var defaultFileName = Path.ChangeExtension(assetPath, ".cs");

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

                wrapperClassNameProperty.PropertyFieldWithDefaultText(m_WrapperClassNameLabel, CSharpCodeHelpers.MakeTypeName(GetAsset().name));

                if (!CSharpCodeHelpers.IsEmptyOrProperIdentifier(wrapperClassNameProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# identifier", MessageType.Error);

                wrapperCodeNamespaceProperty.PropertyFieldWithDefaultText(m_WrapperCodeNamespaceLabel, "<Global namespace>");

                if (!CSharpCodeHelpers.IsEmptyOrProperNamespaceName(wrapperCodeNamespaceProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# namespace name", MessageType.Error);
            }

            #if UNITY_2019_2_OR_NEWER
            // Using ApplyRevertGUI requires calling Update and ApplyModifiedProperties around the serializedObject,
            // and will print warning messages otherwise (see warning message in ApplyRevertGUI implementation).
            serializedObject.ApplyModifiedProperties();
            #endif

            ApplyRevertGUI();
        }

        private InputActionAsset GetAsset()
        {
            var asset = (InputActionAsset)assetTarget;
            if (asset == null)
                throw new InvalidOperationException("Asset editor has not been initialized yet");
            return asset;
        }

        private readonly GUIContent m_GenerateWrapperCodeLabel = EditorGUIUtility.TrTextContent("Generate C# Class");
        private readonly GUIContent m_WrapperCodePathLabel = EditorGUIUtility.TrTextContent("C# Class File");
        private readonly GUIContent m_WrapperClassNameLabel = EditorGUIUtility.TrTextContent("C# Class Name");
        private readonly GUIContent m_WrapperCodeNamespaceLabel = EditorGUIUtility.TrTextContent("C# Class Namespace");
    }
}
#endif // UNITY_EDITOR
