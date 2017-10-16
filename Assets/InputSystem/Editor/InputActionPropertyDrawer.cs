#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX
{
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionPropertyDrawer : PropertyDrawer
    {
        private const int kFoldoutHeight = 15;
        private const int kBindingHeight = 20;
        private const int kBindingIndent = 10;

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
                var bindingsArray = property.FindPropertyRelative("m_Bindings");
                var bindingsCount = bindingsArray.arraySize;

                var rect = position;
                rect.y += kFoldoutHeight + 2;
                rect.x += kBindingIndent;
                rect.height = kBindingHeight;

                for (var i = 0; i < bindingsCount; ++i)
                {
                    var minusButtonRect = rect;
                    minusButtonRect.width = Contents.iconMinus.image.width;
                    if (GUI.Button(minusButtonRect, Contents.iconMinus, GUIStyle.none))
                    {
                        bindingsArray.DeleteArrayElementAtIndex(i);
                        bindingsArray.serializedObject.ApplyModifiedProperties();
                        EditorGUI.EndProperty();
                        return;
                    }

                    var bindingRect = rect;
                    bindingRect.x += minusButtonRect.width + 2;
                    bindingRect.width -= minusButtonRect.width + 2;

                    EditorGUI.PropertyField(bindingRect, bindingsArray.GetArrayElementAtIndex(i));

                    rect.y += kBindingHeight;
                }

                rect.height = Contents.iconPlus.image.height;
                if (GUI.Button(rect, Contents.iconPlus, GUIStyle.none))
                {
                    bindingsArray.InsertArrayElementAtIndex(bindingsCount);
                    bindingsArray.serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }

        public static class Contents
        {
            public static GUIContent iconPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add new binding");
            public static GUIContent iconMinus = EditorGUIUtility.IconContent("Toolbar Minus", "Remove binding");
        }
    }
}
#endif
