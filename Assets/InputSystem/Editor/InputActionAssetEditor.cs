#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX.Editor
{
    // Custom inspector that allows modifying action sets in InputActionAssets.
    [CustomEditor(typeof(InputActionAsset))]
    public class InputActionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            //one set after the other
            //can add and remove sets (reorder too?)
            //each set shows list of actions
            //bindings can be filtered by their group
            //new groups can be added


            ////FIXME: toolbar doesn't work well visually; switch to something else
            // Toolbar.
            DrawToolbarGUI();

            ////REVIEW: draw as tree?
            //// name column, binding column, group column?

            // UI for each set.
            EditorGUILayout.BeginVertical();
            var setArrayProperty = serializedObject.FindProperty("m_ActionSets");
            var setCount = setArrayProperty.arraySize;
            for (var i = 0; i < setCount; ++i)
            {
                var setProperty = setArrayProperty.GetArrayElementAtIndex(i);
                DrawSetGUI(setProperty);
            }
            EditorGUILayout.EndVertical();
        }

        protected void DrawToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(Contents.addNewSet, EditorStyles.toolbarButton))
                AddActionSet();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawSetGUI(SerializedProperty setProperty)
        {
            var nameProperty = setProperty.FindPropertyRelative("m_Name");

            EditorGUILayout.BeginVertical(Styles.box, GUILayout.ExpandWidth(true));
            EditorGUILayout.PropertyField(nameProperty);
            EditorGUILayout.EndVertical();
        }

        protected void AddActionSet()
        {
            var setArrayProperty = serializedObject.FindProperty("m_ActionSets");
            var setCount = setArrayProperty.arraySize;
            setArrayProperty.InsertArrayElementAtIndex(setCount);
            serializedObject.ApplyModifiedProperties();
        }

        private static class Styles
        {
            public static GUIStyle box = "Box";
        }

        private static class Contents
        {
            public static GUIContent addNewSet = new GUIContent("Add New Set");
        }
    }
}
#endif // UNITY_EDITOR
