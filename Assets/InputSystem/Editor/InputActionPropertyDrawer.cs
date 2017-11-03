#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

////TODO: support actions in action sets (only supports singleton actions ATM)

namespace ISX.Editor
{
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionPropertyDrawer : PropertyDrawer
    {
        private const int kFoldoutHeight = 15;
        private const int kBindingHeight = 20;
        private const int kBindingIndent = 5;

        [SerializeField] private bool m_FoldedOut;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var bindingsArray = property.FindPropertyRelative("m_Bindings");
            var bindingsCount = bindingsArray.arraySize;

            var height = kFoldoutHeight;
            if (m_FoldedOut)
            {
                height += bindingsCount * kBindingHeight;
                height += Contents.iconPlus.image.height + 2;
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

            var nowFoldedOut = EditorGUI.Foldout(foldoutRect, m_FoldedOut, label);
            if (nowFoldedOut != m_FoldedOut)
            {
                m_FoldedOut = nowFoldedOut;

                // Awkwardly force a repaint.
                property.serializedObject.Update();
            }
            else if (m_FoldedOut)
            {
                var bindingsArrayProperty = property.FindPropertyRelative("m_Bindings");
                var bindingsCountProperty = property.FindPropertyRelative("m_BindingsCount");
                var bindingsCount = bindingsArrayProperty.arraySize;

                var rect = position;
                rect.y += kFoldoutHeight + 2;
                rect.x += kBindingIndent;
                rect.height = kBindingHeight;
                rect.width -= kBindingIndent;

                for (var i = 0; i < bindingsCount; ++i)
                {
                    var minusButtonRect = rect;
                    minusButtonRect.width = Contents.iconMinus.image.width;
                    if (GUI.Button(minusButtonRect, Contents.iconMinus, GUIStyle.none))
                    {
                        bindingsArrayProperty.DeleteArrayElementAtIndex(i);
                        bindingsCountProperty.intValue = bindingsCount - 1;
                        bindingsArrayProperty.serializedObject.ApplyModifiedProperties();
                        EditorGUI.EndProperty();
                        return;
                    }

                    var bindingRect = rect;
                    bindingRect.x += minusButtonRect.width + 5;
                    bindingRect.width -= minusButtonRect.width + 5;

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
                }
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

        public static class Contents
        {
            public static GUIContent iconPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add new binding");
            public static GUIContent iconMinus = EditorGUIUtility.IconContent("Toolbar Minus", "Remove binding");
        }

        public static class Styles
        {
            public static GUIStyle box = "Box";
        }
    }
}
#endif
