#if UNITY_EDITOR
using System;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

////TODO: create custom editor for InputActionReference which prevents modifying the references

////TODO: support for multi-editing

namespace UnityEngine.Experimental.Input.Editor
{
    // Custom editor that allows modifying importer settings for an InputActionImporter.
    [CustomEditor(typeof(InputActionImporter))]
    internal class InputActionImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Edit asset"))
            {
                ActionInspectorWindow.OnOpenAsset(GetAsset().GetInstanceID(), 0);
            }

            EditorGUILayout.Space();

            // Look up properties on importer object.
            var generateWapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");

            // Add settings UI.
            EditorGUILayout.PropertyField(generateWapperCodeProperty, Contents.generateWrapperCode);
            if (generateWapperCodeProperty.boolValue)
            {
                var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");
                var wrapperClassNameProperty = serializedObject.FindProperty("m_WrapperClassName");
                var wrapperCodeNamespaceProperty = serializedObject.FindProperty("m_WrapperCodeNamespace");

                ////TODO: tie a file selector to this
                EditorGUILayout.PropertyField(wrapperCodePathProperty);

                EditorGUILayout.PropertyField(wrapperClassNameProperty);
                if (!CSharpCodeHelpers.IsEmptyOrProperIdentifier(wrapperClassNameProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# identifier", MessageType.Error);

                EditorGUILayout.PropertyField(wrapperCodeNamespaceProperty);
                if (!CSharpCodeHelpers.IsEmptyOrProperNamespaceName(wrapperCodeNamespaceProperty.stringValue))
                    EditorGUILayout.HelpBox("Must be a valid C# namespace name", MessageType.Error);
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

        private static class Contents
        {
            public static GUIContent generateWrapperCode = new GUIContent("Generate C# Wrapper Class");
        }
    }
}
#endif // UNITY_EDITOR
