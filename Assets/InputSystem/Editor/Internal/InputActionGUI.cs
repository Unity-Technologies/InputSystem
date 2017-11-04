#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX.Editor
{
    // Similar to EditorGUI, this is a static class implementation routines to draw and work
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

        ////TODO: make this work with non-singleton actions

        // Draws a GUI of stacked bindings. A minus icon on the left side of each binding
        // allows to remove the respective binding. A plus icon after the last binding
        // allows adding new bindings.
        // Returns true if the plus or the minus button was pressed.
        public static bool BindingsArray(Rect rect, SerializedProperty actionProperty)
        {
            var bindingsArrayProperty = actionProperty.FindPropertyRelative("m_Bindings");
            var bindingsCountProperty = actionProperty.FindPropertyRelative("m_BindingsCount");
            var bindingsCount = bindingsArrayProperty.arraySize;

            rect.height = kBindingHeight;
            for (var i = 0; i < bindingsCount; ++i)
            {
                // Draw minus icon.
                var minusButtonRect = rect;
                minusButtonRect.width = Contents.iconMinus.image.width;
                if (GUI.Button(minusButtonRect, Contents.iconMinus, GUIStyle.none))
                {
                    bindingsArrayProperty.DeleteArrayElementAtIndex(i);
                    bindingsCountProperty.intValue = bindingsCount - 1;
                    bindingsArrayProperty.serializedObject.ApplyModifiedProperties();
                    return true;
                }

                var bindingRect = rect;
                bindingRect.x += minusButtonRect.width + 5;
                bindingRect.width -= minusButtonRect.width + 5;

                // Draw binding UI.
                var currentBinding = bindingsArrayProperty.GetArrayElementAtIndex(i);
                EditorGUI.PropertyField(bindingRect, currentBinding);

                rect.y += kBindingHeight;
            }

            rect.height = Contents.iconPlus.image.height;
            rect.width = Contents.iconPlus.image.width;

            if (GUI.Button(rect, Contents.iconPlus, GUIStyle.none))
            {
                bindingsArrayProperty.InsertArrayElementAtIndex(bindingsCount);
                bindingsCountProperty.intValue = bindingsCount + 1;
                bindingsArrayProperty.serializedObject.ApplyModifiedProperties();
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
