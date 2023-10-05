using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    [CustomPropertyDrawer(typeof(InputActionReference))]
    public class InputActionReferencePropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            public static readonly GUIStyle popup = new GUIStyle("PaneOptions") { imagePosition = ImagePosition.ImageOnly };
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // // prefab override logic works on the entire property.
            label = EditorGUI.BeginProperty(position, label, property);
            
            DrawProperty(position, property, label);
            
            EditorGUI.EndProperty();            
        }

        private void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);
            EditorGUI.PropertyField(position, property, GUIContent.none);
        }

        private void DrawAltProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);
            
            /*var assetProperty = property.FindPropertyRelative("m_Asset");
            var actionIdProperty = property.FindPropertyRelative("m_ActionId");*/

            /*var popupStyle = Styles.popup;
            var buttonRect = position;
            buttonRect.yMin += popupStyle.margin.top + 1f;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
            buttonRect.height = EditorGUIUtility.singleLineHeight;
            buttonRect.x = position.x - buttonRect.height;*/
            //position.xMin = buttonRect.xMax;

            // Don't make child fields be indented
            //var indent = EditorGUI.indentLevel;
            //EditorGUI.indentLevel = 0;
            
            // Using BeginProperty / EndProperty on the popup button allows the user to
            // revert prefab overrides to Use Reference by right-clicking the configuration button.            
            //EditorGUI.BeginProperty(buttonRect, GUIContent.none, property);
            
            /*EditorGUI.BeginProperty(buttonRect, GUIContent.none, useReference);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newPopupIndex = EditorGUI.Popup(buttonRect, GetCompactPopupIndex(useReference), Contents.compactPopupOptions, popupStyle);
                if (check.changed)
                    useReference.boolValue = IsUseReference(newPopupIndex);
            }
            EditorGUI.EndProperty();*/
            
            //EditorGUI.EndProperty();
            
            EditorGUI.PropertyField(position, property, GUIContent.none);
            //label = EditorGUILayout.PropertyField(property, label, true);

            //EditorGUI.indentLevel = indent;
        }
    }
}