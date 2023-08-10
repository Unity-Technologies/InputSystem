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

            var drawerMode = InputSystem.settings.inputActionPropertyDrawerMode;
            switch (drawerMode)
            {
                case InputSettings.InputActionPropertyDrawerMode.Compact:
                default:
                    return GetCompactHeight(property, label);

                case InputSettings.InputActionPropertyDrawerMode.Multiline:
                    return GetMultilineHeight(property, label);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property == null)
                throw new System.ArgumentNullException(nameof(property));

            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            label = EditorGUI.BeginProperty(position, label, property);

            var drawerMode = InputSystem.settings.inputActionPropertyDrawerMode;
            switch (drawerMode)
            {
                case InputSettings.InputActionPropertyDrawerMode.Compact:
                default:
                    DrawCompactGUI(position, property, label);
                    break;

                case InputSettings.InputActionPropertyDrawerMode.Multiline:
                    DrawMultilineGUI(position, property, label);
                    break;
            }

            EditorGUI.EndProperty();

        }

        static float GetCompactHeight(SerializedProperty property, GUIContent label)
        {
            var useReference = property.FindPropertyRelative("m_UseReference");
            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");
            return EditorGUI.GetPropertyHeight(effectiveProperty);
        }

        static float GetMultilineHeight(SerializedProperty property, GUIContent label)
        {
            var height = 0f;

            // Field label.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // "Use Reference" toggle.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // We show either the InputAction property drawer or InputActionReference drawer (default object field).
            var useReference = property.FindPropertyRelative("m_UseReference");
            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");
            height += EditorGUI.GetPropertyHeight(effectiveProperty);

            return height;
        }

        static void DrawCompactGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            var useReference = property.FindPropertyRelative("m_UseReference");
            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");

            // Calculate rect for configuration button
            var buttonRect = position;
            var popupStyle = Styles.GetPopupStyle();
            buttonRect.yMin += popupStyle.margin.top + 1f;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.height = EditorGUIUtility.singleLineHeight;
            position.xMin = buttonRect.xMax;

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newPopupIndex = EditorGUI.Popup(buttonRect, GetCompactPopupIndex(useReference), Contents.compactPopupOptions, popupStyle);
                if (check.changed)
                    useReference.boolValue = IsUseReference(newPopupIndex);
            }

            EditorGUI.PropertyField(position, effectiveProperty, GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
        }

        static void DrawMultilineGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            const float kIndent = 16f;

            var titleRect = position;
            titleRect.height = EditorGUIUtility.singleLineHeight;

            var useReference = property.FindPropertyRelative("m_UseReference");
            var useReferenceToggleRect = position;
            useReferenceToggleRect.x += kIndent;
            useReferenceToggleRect.width -= kIndent;
            useReferenceToggleRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            useReferenceToggleRect.height = EditorGUI.GetPropertyHeight(useReference);

            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");
            var effectiveRect = position;
            effectiveRect.x += kIndent;
            effectiveRect.width -= kIndent;
            effectiveRect.y += (useReferenceToggleRect.height + EditorGUIUtility.standardVerticalSpacing) * 2;
            effectiveRect.height = EditorGUI.GetPropertyHeight(effectiveProperty);

            EditorGUI.LabelField(titleRect, label, Styles.header);
            EditorGUI.PropertyField(useReferenceToggleRect, useReference);
            EditorGUI.PropertyField(effectiveRect, effectiveProperty);
        }

        // 0 == Use Reference, 1 == Use Action
        // Keep synced with Contents.compactPopupOptions.
        static int GetCompactPopupIndex(SerializedProperty useReference) => useReference.boolValue ? 0 : 1;
        static bool IsUseReference(int index) => index == 0;

        static class Contents
        {
            static readonly GUIContent s_UseReference = EditorGUIUtility.TrTextContent("Use Reference");
            static readonly GUIContent s_UseAction = EditorGUIUtility.TrTextContent("Use Action");
            public static readonly GUIContent[] compactPopupOptions = { s_UseReference, s_UseAction };
        }

        static class Styles
        {
            static GUIStyle s_PopupStyle;

            public static readonly GUIStyle header = new GUIStyle("Label").WithFontStyle(FontStyle.Bold);

            public static GUIStyle GetPopupStyle()
            {
                if (s_PopupStyle == null)
                    s_PopupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions")) { imagePosition = ImagePosition.ImageOnly };

                return s_PopupStyle;
            }
        }
    }
}
#endif // UNITY_EDITOR
