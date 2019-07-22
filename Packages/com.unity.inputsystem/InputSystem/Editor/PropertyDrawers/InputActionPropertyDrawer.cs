#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A custom property drawer for <see cref="InputActionProperty"/>.
    /// </summary>
    /// <remarks>
    /// This is basically a toggle between the editor for <see cref="InputActionReference"/>
    /// and the editor for <see cref="InputAction"/>.
    /// </remarks>
    [CustomPropertyDrawer(typeof(InputActionProperty))]
    internal class InputActionPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                throw new System.ArgumentNullException(nameof(property));

            var height = 0f;

            // Field label.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // "Use Reference" toggle.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // We show either the InputAction property drawer or InputAssetReferenceDrawer.
            var useReference = property.FindPropertyRelative("m_UseReference");
            if (!useReference.boolValue)
            {
                var actionProperty = property.FindPropertyRelative("m_Action");
                height += EditorGUI.GetPropertyHeight(actionProperty);
            }
            else
            {
                var referenceProperty = property.FindPropertyRelative("m_Reference");
                height += EditorGUI.GetPropertyHeight(referenceProperty);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
                throw new System.ArgumentNullException(nameof(property));

            EditorGUI.BeginProperty(position, label, property);

            const int kIndent = 16;

            var titleRect = position;
            titleRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(titleRect, label, Styles.header);

            var useReferenceToggleRect = position;
            useReferenceToggleRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            useReferenceToggleRect.height = EditorGUIUtility.singleLineHeight;
            useReferenceToggleRect.x += kIndent;
            useReferenceToggleRect.width -= kIndent;

            var useReference = property.FindPropertyRelative("m_UseReference");
            EditorGUI.PropertyField(useReferenceToggleRect, useReference);
            if (!useReference.boolValue)
            {
                var actionProperty = property.FindPropertyRelative("m_Action");

                var actionDrawerRect = position;
                actionDrawerRect.x += kIndent;
                actionDrawerRect.width -= kIndent;
                actionDrawerRect.y += (useReferenceToggleRect.height + EditorGUIUtility.standardVerticalSpacing) * 2;
                actionDrawerRect.height = EditorGUI.GetPropertyHeight(actionProperty);

                EditorGUI.PropertyField(actionDrawerRect, actionProperty);
            }
            else
            {
                var referenceProperty = property.FindPropertyRelative("m_Reference");

                var referenceRect = position;
                referenceRect.x += kIndent;
                referenceRect.width -= kIndent;
                referenceRect.y += (useReferenceToggleRect.height + EditorGUIUtility.standardVerticalSpacing) * 2;
                referenceRect.height = EditorGUI.GetPropertyHeight(referenceProperty);

                EditorGUI.PropertyField(referenceRect, referenceProperty);
            }

            EditorGUI.EndProperty();
        }

        private static class Styles
        {
            public static readonly GUIStyle header = new GUIStyle("Label").WithFontStyle(FontStyle.Bold);
        }
    }
}
#endif // UNITY_EDITOR
