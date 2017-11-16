#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;

namespace ISX.Editor
{
    // A ReorderableList that displays an editable list of bindings for an action.
    internal class InputBindingListView : ReorderableList
    {
        public InputBindingListView(SerializedProperty actionProperty, SerializedProperty actionSetProperty = null, bool displayHeader = true)
            : base(actionProperty.serializedObject,
                   actionSetProperty != null
                   ? actionSetProperty.FindPropertyRelative("m_Bindings")
                   : actionProperty.FindPropertyRelative("m_Bindings"))
        {
            if (!displayHeader)
                headerHeight = 2;

            drawElementCallback =
                (rect, index, isActive, isFocused) =>
                {
                    var binding = serializedProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, binding);
                };

            drawHeaderCallback =
                (rect) =>
                {
                    if (displayHeader)
                        EditorGUI.LabelField(rect, "Bindings");
                };

            onAddCallback =
                (list) => InputActionSerializationHelpers.AppendBinding(actionProperty, actionSetProperty);

            onRemoveCallback =
                (list) => InputActionSerializationHelpers.RemoveBinding(actionProperty, list.index, actionSetProperty);
        }
    }
}
#endif // UNITY_EDITOR
