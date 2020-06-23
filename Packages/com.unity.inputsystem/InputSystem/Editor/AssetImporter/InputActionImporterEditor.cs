#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine.InputSystem.Utilities;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

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
            serializedObject.Update();

            // Button to pop up window to edit the asset.
            if (GUILayout.Button("Edit asset"))
                InputActionEditorWindow.OnOpenAsset(GetAsset().GetInstanceID(), 0);

            EditorGUILayout.Space();

            // Importer settings UI.
            var generateWrapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");
            var generateECSComponentProperty = serializedObject.FindProperty("m_GenerateECSComponent");
            var selectedBefore = generateWrapperCodeProperty.boolValue
                ? 1
                : (generateECSComponentProperty.boolValue ? 2 : 0);
            var selectedAfter = EditorGUILayout.IntPopup(m_GenerateCodeLabel, selectedBefore, m_GenerateCodeOptionLabels,
                m_GenerateCodeOptionValues);
            if (selectedAfter != selectedBefore)
            {
                switch (selectedAfter)
                {
                    case 0:
                        generateWrapperCodeProperty.boolValue = false;
                        generateECSComponentProperty.boolValue = false;
                        break;

                    case 1:
                        generateWrapperCodeProperty.boolValue = true;
                        generateECSComponentProperty.boolValue = false;
                        break;

                    case 2:
                        generateWrapperCodeProperty.boolValue = false;
                        generateECSComponentProperty.boolValue = true;
                        break;
                }
            }
            if (selectedAfter != 0)
            {
                var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");
                var wrapperClassNameProperty = serializedObject.FindProperty("m_WrapperClassName");
                var wrapperCodeNamespaceProperty = serializedObject.FindProperty("m_WrapperCodeNamespace");

                EditorGUILayout.BeginHorizontal();
                var assetPath = AssetDatabase.GetAssetPath(GetAsset());
                var defaultFileName = Path.ChangeExtension(assetPath, ".cs");

                wrapperCodePathProperty.PropertyFieldWithDefaultText(m_WrapperCodePathLabel, defaultFileName);

                ////TODO: for ECS path, this needs to point to a directory

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

                wrapperClassNameProperty.PropertyFieldWithDefaultText(selectedAfter == 1 ? m_WrapperClassNameLabel : m_ECSComponentNameLabel, CSharpCodeHelpers.MakeTypeName(GetAsset().name));

                if (!CSharpCodeHelpers.IsEmptyOrProperIdentifier(wrapperClassNameProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# identifier", MessageType.Error);

                wrapperCodeNamespaceProperty.PropertyFieldWithDefaultText(selectedAfter == 1 ? m_WrapperCodeNamespaceLabel : m_ECSComponentNamespaceLabel, "<Global namespace>");

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
            var asset = (InputActionAsset)assetTarget;
            if (asset == null)
                throw new InvalidOperationException("Asset editor has not been initialized yet");
            return asset;
        }

        private readonly GUIContent m_GenerateCodeLabel = EditorGUIUtility.TrTextContent("Generate Code");
        private readonly GUIContent m_WrapperCodePathLabel = EditorGUIUtility.TrTextContent("Code Output File");
        private readonly GUIContent m_WrapperClassNameLabel = EditorGUIUtility.TrTextContent("C# Class Name");
        private readonly GUIContent m_ECSComponentNameLabel = EditorGUIUtility.TrTextContent("Component Name Prefix");
        private readonly GUIContent m_WrapperCodeNamespaceLabel = EditorGUIUtility.TrTextContent("C# Class Namespace");
        private readonly GUIContent m_ECSComponentNamespaceLabel = EditorGUIUtility.TrTextContent("Component Namespace");

        private readonly GUIContent[] m_GenerateCodeOptionLabels = new[]
        {
            new GUIContent("None"),
            new GUIContent("C# Wrapper Class"),
            new GUIContent("ECS Components"),
        };
        private readonly int[] m_GenerateCodeOptionValues = new[] { 0, 1, 2 };
    }
}
#endif // UNITY_EDITOR
