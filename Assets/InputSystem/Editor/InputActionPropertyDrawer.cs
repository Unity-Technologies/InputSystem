#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX.Editor
{
    // Custom inspector support for InputActions.
    //
    // NOTE: This only supports singleton actions. It will not work correctly with actions
    //       that are part of InputActionSets. InputActionSet has its own inspector support
    //       that does not use this property drawer.
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionPropertyDrawer : PropertyDrawer
    {
        private const int kFoldoutHeight = 15;
        private const int kBindingIndent = 5;

        ////FIXME: this doesn't work correctly; folding state doesn't survive domain reloads
        [SerializeField] private bool m_FoldedOut;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = (float)kFoldoutHeight;
            if (m_FoldedOut)
                height += InputActionGUI.GetBindingsArrayHeight(property);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // If the action has no name, infer it from the name of the action property.
            SetActionNameIfNotSet(property);

            var foldoutRect = position;
            foldoutRect.height = kFoldoutHeight;

            var nowFoldedOut = EditorGUI.Foldout(foldoutRect, m_FoldedOut, label);
            if (nowFoldedOut != m_FoldedOut)
            {
                m_FoldedOut = nowFoldedOut;

                // Awkwardly force a repaint.
                property.serializedObject.Update();
            }
            else if (m_FoldedOut)
            {
                position.y += kFoldoutHeight + 2;
                position.x += kBindingIndent;
                position.width -= kBindingIndent;

                InputActionGUI.BindingsArray(position, property);
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
