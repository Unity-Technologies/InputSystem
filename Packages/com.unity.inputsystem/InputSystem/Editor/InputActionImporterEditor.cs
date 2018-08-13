#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Experimental.Input.Utilities;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

////TODO: replace "Apply" text on button with "Save"

////TODO: create custom editor for InputActionReference which prevents modifying the references

////TODO: support for multi-editing

////FIXME: because of how .inputactions are structured, an asset with just a set and no actions in it will come out
////       as no set at all when deserialized and then cause exception in InputActionTreeView

namespace UnityEngine.Experimental.Input.Editor
{
    // Custom editor that allows modifying importer settings for an InputActionImporter.
    //
    // NOTE: This inspector has an unusual setup in that it not only modifies import settings
    //       but actually overwrites the source file on apply if there have been changes made to the
    //       action sets.
    //
    // NOTE: Depends on InputActionAssetEditor as the chosen editor for the imported asset.
    [CustomEditor(typeof(InputActionImporter))]
    public class InputActionImporterEditor : ScriptedImporterEditor
    {
        [SerializeField] private bool m_AssetIsDirty;
        // We need to be able to revert edits. We support that by simply keeping a copy of
        // the last JSON version of the asset around.
        [SerializeField] private string m_Backup;
        [NonSerialized] private bool m_Initialized;

        private static List<InputActionImporterEditor> s_EnabledEditors;

        public static InputActionImporterEditor FindFor(SerializedObject obj)
        {
            if (s_EnabledEditors != null)
            {
                foreach (var editor in s_EnabledEditors)
                    if (editor.assetSerializedObject == obj)
                        return editor;
            }

            return null;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (s_EnabledEditors == null)
                s_EnabledEditors = new List<InputActionImporterEditor>();
            s_EnabledEditors.Add(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (s_EnabledEditors != null)
                s_EnabledEditors.Remove(this);
        }

        protected InputActionAsset GetAsset()
        {
            var asset = (InputActionAsset)assetTarget;
            if (asset == null)
                throw new InvalidOperationException("Asset editor has not been initialized yet");
            return asset;
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

            ////REVIEW: can we somehow get pretty-printed JSON instead of the compact form that JsonUtility writes?
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

            // ResetValues() also gets called from the apply logic at a time
            // when the asset editor isn't set up yet.
            var assetObject = (InputActionAsset)assetTarget;
            if (assetObject != null)
            {
                if (m_Backup != null)
                    assetObject.LoadFromJson(m_Backup);
            }
        }

        public override void OnInspectorGUI()
        {
            // 'assetEditor' is set only after the editor is enabled so do the
            // initialization here.
            if (!m_Initialized)
            {
                // Read current asset as backup.
                if (m_Backup == null)
                    m_Backup = GetAsset().ToJson();

                m_Initialized = true;
            }

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

        public override bool HasModified()
        {
            return m_AssetIsDirty || base.HasModified();
        }

        public void OnAssetModified()
        {
            m_AssetIsDirty = true;
            Repaint();
        }

        private static class Contents
        {
            public static GUIContent generateWrapperCode = new GUIContent("Generate C# Wrapper Class");
        }
    }
}
#endif // UNITY_EDITOR
