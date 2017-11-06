#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX.Editor
{
    // Similar to EditorGUI, this is a static class implementing routines to draw and work
    // with common GUI elements related to InputActions.
    internal static class InputActionGUI
    {
        private const int kBindingHeight = 20;

        public static float GetBindingsArrayHeight(SerializedProperty actionProperty)
        {
            var bindingsArray = actionProperty.FindPropertyRelative("m_Bindings");
            var bindingsCount = bindingsArray.arraySize;

            return bindingsCount * kBindingHeight
                + Contents.iconPlus.image.height + 2;
        }

        ////REVIEW: can we make drag&drop working with this? (to re-order bindings)

        // Draws a GUI of stacked bindings. A minus icon on the left side of each binding
        // allows to remove the respective binding. A plus icon after the last binding
        // allows adding new bindings.
        // Returns true if the plus or the minus button was pressed.
        public static bool BindingsArray(Rect rect, SerializedProperty actionProperty, SerializedProperty actionSetProperty = null)
        {
            var bindingsArrayProperty = actionProperty.FindPropertyRelative("m_Bindings");
            var bindingsCountProperty = actionProperty.FindPropertyRelative("m_BindingsCount");
            var bindingsStartIndexProperty = actionProperty.FindPropertyRelative("m_BindingsStartIndex");

            var bindingsCount = bindingsCountProperty.intValue;
            var bindingsStartIndex = bindingsStartIndexProperty.intValue;

            rect.height = kBindingHeight;
            for (var i = 0; i < bindingsCount; ++i)
            {
                // Button to remove binding.
                var minusButtonRect = rect;
                minusButtonRect.width = Contents.iconMinus.image.width;
                if (GUI.Button(minusButtonRect, Contents.iconMinus, GUIStyle.none))
                {
                    InputActionSerializationHelpers.RemoveBinding(actionProperty, i, actionSetProperty);
                    return true;
                }

                var bindingRect = rect;
                bindingRect.x += minusButtonRect.width + 5;
                bindingRect.width -= minusButtonRect.width + 5;

                // UI for binding itself.
                var currentBinding = bindingsArrayProperty.GetArrayElementAtIndex(bindingsStartIndex + i);
                EditorGUI.PropertyField(bindingRect, currentBinding);

                rect.y += kBindingHeight;
            }

            rect.height = Contents.iconPlus.image.height;
            rect.width = Contents.iconPlus.image.width;

            // Button to add new binding.
            if (GUI.Button(rect, Contents.iconPlus, GUIStyle.none))
            {
                InputActionSerializationHelpers.AppendBinding(actionProperty, actionSetProperty);
                return true;
            }

            return false;
        }

        private static class Contents
        {
            public static GUIContent iconPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add new binding");
            public static GUIContent iconMinus = EditorGUIUtility.IconContent("Toolbar Minus", "Remove binding");
        }
    }
}

#endif // UNITY_EDITOR
