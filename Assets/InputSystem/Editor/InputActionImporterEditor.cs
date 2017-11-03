#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

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
        protected override void Apply()
        {
            // Re-generate the JSON source file and if it doesn't match what's already in
            // the file, overwrite the source file.
            ////TODO

            base.Apply();
        }

        public override void OnInspectorGUI()
        {
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
    }
}
#endif // UNITY_EDITOR
