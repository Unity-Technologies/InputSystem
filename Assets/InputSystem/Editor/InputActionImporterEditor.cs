#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

////TODO: replace "Apply" text on button with "Save"

namespace ISX.Editor
{
    // Custom editor that allows modifying importer settings for an InputActionImporter.
    //
    // NOTE: This inspector has an unusual setup in that it not only modifies import settings
    //       but actually overwrites the source file on apply if there have been changes made to the
    //       action sets.
    [CustomEditor(typeof(InputActionImporter))]
    public class InputActionImporterEditor : ScriptedImporterEditor
    {
        [SerializeField] private bool m_AssetIsDirty;
        [NonSerialized] private bool m_Initialized;

        protected override void Apply()
        {
            RegenerateJsonSourceFile();
            m_AssetIsDirty = false;
            base.Apply();
        }

        // Re-generate the JSON source file and if it doesn't match what's already in
        // the file, overwrite the source file.
        private void RegenerateJsonSourceFile()
        {
            var editor = assetEditor as InputActionAssetEditor;
            if (editor == null)
                return;

            var asset = (InputActionAsset)editor.target;
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return;

            var newJson = asset.ToJson();
            var existingJson = File.ReadAllText(assetPath);

            if (newJson != existingJson)
                File.WriteAllText(assetPath, newJson);
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            m_AssetIsDirty = false;
            (assetEditor as InputActionAssetEditor)?.Reload();

            ////TODO: need to make a backup copy of the SerializedObject and revert to it here
        }

        public override void OnInspectorGUI()
        {
            // 'assetEditor' is set only after the editor is enabled so do the
            // initialization here.
            if (!m_Initialized)
            {
                var editor = assetEditor as InputActionAssetEditor;
                if (editor != null)
                    editor.m_ApplyAction = OnAssetModified;
                m_Initialized = true;
            }

            // Look up properties on importer object.
            var generateWapperCodeProperty = serializedObject.FindProperty("m_GenerateWrapperCode");
            var wrapperCodePathProperty = serializedObject.FindProperty("m_WrapperCodePath");

            // Add settings UI.
            EditorGUILayout.PropertyField(generateWapperCodeProperty);
            if (generateWapperCodeProperty.boolValue)
            {
                ////REVIEW: any way to make this a file selector of sorts?
                EditorGUILayout.PropertyField(wrapperCodePathProperty);
            }

            ApplyRevertGUI();
        }

        public override bool HasModified()
        {
            return m_AssetIsDirty || base.HasModified();
        }

        private void OnAssetModified()
        {
            m_AssetIsDirty = true;
            Repaint();
        }
    }
}
#endif // UNITY_EDITOR
