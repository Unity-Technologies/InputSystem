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
        // We need to be able to revert edits. We support that by simply keeping a copy of
        // the last JSON version of the asset around.
        [SerializeField] private string m_Backup;
        [NonSerialized] private bool m_Initialized;

        protected InputActionAssetEditor GetAssetEditor()
        {
            if (assetEditor == null)
                throw new InvalidOperationException("Asset editor has not yet been initialized");

            var editor = assetEditor as InputActionAssetEditor;
            if (editor == null)
                throw new InvalidOperationException("Asset editor is not an InputActionAssetEditor");

            return editor;
        }

        protected InputActionAsset GetAsset()
        {
            return (InputActionAsset)GetAssetEditor().target;
        }

        protected string GetAssetPath()
        {
            return AssetDatabase.GetAssetPath(GetAsset());
        }

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
            var assetPath = GetAssetPath();
            if (string.IsNullOrEmpty(assetPath))
                return;

            var newJson = GetAsset().ToJson();
            var existingJson = File.ReadAllText(assetPath);

            if (newJson != existingJson)
                File.WriteAllText(assetPath, newJson);

            // Becomes our new backup copy.
            m_Backup = newJson;
        }

        // NOTE: This is called during Awake() when nothing of the asset editing
        //       structure has been initialized yet.
        protected override void ResetValues()
        {
            base.ResetValues();
            m_AssetIsDirty = false;
            if (m_Backup != null)
                GetAsset().FromJson(m_Backup);
            (assetEditor as InputActionAssetEditor)?.Reload();
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

                // Read current asset as backup.
                if (m_Backup == null)
                    m_Backup = GetAsset().ToJson();

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
