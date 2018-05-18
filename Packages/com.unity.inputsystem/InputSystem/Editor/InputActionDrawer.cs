#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    // Custom inspector support for InputActions.
    //
    // NOTE: This only supports singleton actions. It will not work correctly with actions
    //       that are part of InputActionSets. InputActionMap has its own inspector support
    //       that does not use this property drawer.
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionDrawer : PropertyDrawer
    {
        private const int kFoldoutHeight = 15;
        private const int kBindingIndent = 5;

        private InputBindingListView m_BindingListView;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = (float)kFoldoutHeight;
            if (property.isExpanded)
            {
                if (m_BindingListView == null)
                    m_BindingListView = new InputBindingListView(property);
                height += m_BindingListView.GetHeight();
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // If the action has no name, infer it from the name of the action property.
            SetActionNameIfNotSet(property);

            var foldoutRect = position;
            foldoutRect.height = kFoldoutHeight;

            var nowFoldedOut = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
            if (nowFoldedOut != property.isExpanded)
            {
                property.isExpanded = nowFoldedOut;

                // Awkwardly force a repaint.
                property.serializedObject.Update();
            }
            else if (property.isExpanded)
            {
                position.y += kFoldoutHeight + 2;
                position.x += kBindingIndent;
                position.width -= kBindingIndent;

                if (m_BindingListView == null)
                    m_BindingListView = new InputBindingListView(property);

                m_BindingListView.DoList(position);
            }

            EditorGUI.EndProperty();
        }

        private void SetActionNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            var name = actionProperty.displayName;
            if (name.EndsWith(" Action"))
                name = name.Substring(0, name.Length - " Action".Length);

            nameProperty.stringValue = name;
            // Don't apply. Let's apply it as a side-effect whenever something about
            // the action in the UI is changed.
        }
    }
}
#endif // UNITY_EDITOR
