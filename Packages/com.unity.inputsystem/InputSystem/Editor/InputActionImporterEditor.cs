#if UNITY_EDITOR
using System;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

////TODO: support for multi-editing

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Custom editor that allows modifying importer settings for an <see cref="InputActionImporter"/>.
    /// </summary>
    [CustomEditor(typeof(InputActionImporter))]
    internal class InputActionImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            // Button to pop up window to edit the asset.
            if (GUILayout.Button("Edit asset"))
                AssetInspectorWindow.OnOpenAsset(GetAsset().GetInstanceID(), 0);

            EditorGUILayout.Space();

            // Importer settings UI.
            var generateWapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");
            EditorGUILayout.PropertyField(generateWapperCodeProperty, m_GenerateWrapperCodeLabel);
            if (generateWapperCodeProperty.boolValue)
            {
                var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");
                var wrapperClassNameProperty = serializedObject.FindProperty("m_WrapperClassName");
                var wrapperCodeNamespaceProperty = serializedObject.FindProperty("m_WrapperCodeNamespace");
                var generateActionEventsProperty = serializedObject.FindProperty("m_GenerateActionEvents");

                ////TODO: tie a file selector to this
                EditorGUILayout.PropertyField(wrapperCodePathProperty);

                EditorGUILayout.PropertyField(wrapperClassNameProperty);
                if (!CSharpCodeHelpers.IsEmptyOrProperIdentifier(wrapperClassNameProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# identifier", MessageType.Error);

                EditorGUILayout.PropertyField(wrapperCodeNamespaceProperty);
                if (!CSharpCodeHelpers.IsEmptyOrProperNamespaceName(wrapperCodeNamespaceProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# namespace name", MessageType.Error);

                EditorGUILayout.PropertyField(generateActionEventsProperty);
            }

            ApplyRevertGUI();
        }

        private InputActionAsset GetAsset()
        {
            var asset = (InputActionAsset)assetTarget;
            if (asset == null)
                throw new InvalidOperationException("Asset editor has not been initialized yet");
            return asset;
        }

        private GUIContent m_GenerateWrapperCodeLabel = EditorGUIUtility.TrTextContent("Generate C# Wrapper Class");
    }
}
#endif // UNITY_EDITOR
