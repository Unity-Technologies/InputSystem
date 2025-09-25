#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A custom property drawer for <see cref="InputActionProperty"/>.
    /// </summary>
    /// <remarks>
    /// Selectively draws the <see cref="InputActionReference"/> and/or the <see cref="InputAction"/>
    /// underlying the property, based on whether the property is set to use a reference or not.
    /// The drawer also allows for drawing in different layouts, chosen through Project Settings.
    /// </remarks>
    /// <seealso cref="InputSettings.InputActionPropertyDrawerMode"/>
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
                    return GetCompactHeight(property);

                case InputSettings.InputActionPropertyDrawerMode.MultilineEffective:
                case InputSettings.InputActionPropertyDrawerMode.MultilineBoth:
                    return GetMultilineHeight(property, drawerMode == InputSettings.InputActionPropertyDrawerMode.MultilineEffective);
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

                case InputSettings.InputActionPropertyDrawerMode.MultilineEffective:
                case InputSettings.InputActionPropertyDrawerMode.MultilineBoth:
                    DrawMultilineGUI(position, property, label, drawerMode == InputSettings.InputActionPropertyDrawerMode.MultilineEffective);
                    break;
            }

            EditorGUI.EndProperty();
        }

        static float GetCompactHeight(SerializedProperty property)
        {
            var useReference = property.FindPropertyRelative("m_UseReference");
            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");
            return EditorGUI.GetPropertyHeight(effectiveProperty);
        }

        static float GetMultilineHeight(SerializedProperty property, bool showEffectiveOnly)
        {
            var height = 0f;

            // Field label.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // "Use Reference" toggle.
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // We show either the InputAction property drawer or InputActionReference drawer (default object field).
            var useReference = property.FindPropertyRelative("m_UseReference");
            var reference = property.FindPropertyRelative("m_Reference");
            var action = property.FindPropertyRelative("m_Action");
            if (showEffectiveOnly)
            {
                var effectiveProperty = useReference.boolValue ? reference : action;
                height += EditorGUI.GetPropertyHeight(effectiveProperty);
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(reference) + EditorGUI.GetPropertyHeight(action);
            }

            return height;
        }

        static void DrawCompactGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            var useReference = property.FindPropertyRelative("m_UseReference");
            var effectiveProperty = useReference.boolValue ? property.FindPropertyRelative("m_Reference") : property.FindPropertyRelative("m_Action");

            // Calculate rect for configuration button
            var buttonRect = position;
            var popupStyle = Styles.popup;
            buttonRect.yMin += popupStyle.margin.top + 1f;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.height = EditorGUIUtility.singleLineHeight;
            position.xMin = buttonRect.xMax;

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Using BeginProperty / EndProperty on the popup button allows the user to
            // revert prefab overrides to Use Reference by right-clicking the configuration button.
            EditorGUI.BeginProperty(buttonRect, GUIContent.none, useReference);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newPopupIndex = EditorGUI.Popup(buttonRect, GetCompactPopupIndex(useReference), Contents.compactPopupOptions, popupStyle);
                if (check.changed)
                    useReference.boolValue = IsUseReference(newPopupIndex);
            }
            EditorGUI.EndProperty();

            EditorGUI.PropertyField(position, effectiveProperty, GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
        }

        static void DrawMultilineGUI(Rect position, SerializedProperty property, GUIContent label, bool showEffectiveOnly)
        {
            const float kIndent = 15f;

            var titleRect = position;
            titleRect.height = EditorGUIUtility.singleLineHeight;

            var useReference = property.FindPropertyRelative("m_UseReference");
            var useReferenceToggleRect = position;
            useReferenceToggleRect.x += kIndent;
            useReferenceToggleRect.width -= kIndent;
            useReferenceToggleRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            useReferenceToggleRect.height = EditorGUI.GetPropertyHeight(useReference);

            var firstValueRect = position;
            firstValueRect.x += kIndent;
            firstValueRect.width -= kIndent;
            firstValueRect.y += (titleRect.height + useReferenceToggleRect.height) + (EditorGUIUtility.standardVerticalSpacing * 2f);

            var reference = property.FindPropertyRelative("m_Reference");
            var action = property.FindPropertyRelative("m_Action");

            // Draw the Use Reference toggle.
            EditorGUI.LabelField(titleRect, label);
            EditorGUI.PropertyField(useReferenceToggleRect, useReference);

            if (showEffectiveOnly)
            {
                // Draw only the Reference or Action underlying the property, whichever is the effective one.
                var effectiveProperty = useReference.boolValue ? reference : action;
                firstValueRect.height = EditorGUI.GetPropertyHeight(effectiveProperty);

                EditorGUI.PropertyField(firstValueRect, effectiveProperty);
            }
            else
            {
                // Draw the Action followed by the Reference.
                firstValueRect.height = EditorGUI.GetPropertyHeight(action);

                var referenceRect = firstValueRect;
                referenceRect.y += firstValueRect.height + EditorGUIUtility.standardVerticalSpacing;
                referenceRect.height = EditorGUI.GetPropertyHeight(reference);

                EditorGUI.PropertyField(firstValueRect, action);
                EditorGUI.PropertyField(referenceRect, reference);
            }
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
            public static readonly GUIStyle popup = new GUIStyle("PaneOptions") { imagePosition = ImagePosition.ImageOnly };
        }
    }
}
#endif // UNITY_EDITOR
