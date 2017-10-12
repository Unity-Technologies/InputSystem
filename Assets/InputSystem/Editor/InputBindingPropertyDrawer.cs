#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

//make the selector for controls a popup window with a search function

namespace ISX
{
    // Instead of letting users fiddle around with strings in the inspector, this
    // presents an interface that allows to automatically construct the path
    // strings. The user can still enter a plain string manually in the popup
    // window we display.
    [CustomPropertyDrawer(typeof(InputBinding))]
    public class InputBindingPropertyDrawer : PropertyDrawer
    {
        private const int kPathLabelWidth = 200;
        private const int kPathButtonWidth = 50;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            ////TODO: modifiers

            var pathProperty = property.FindPropertyRelative("path");
            var path = pathProperty.stringValue;
            var pathContent = GetContentForPath(path);

            var pathRect = new Rect(position.x, position.y, kPathLabelWidth, position.height);
            var pathButtonRect = new Rect(position.x + kPathLabelWidth + 4, position.y, kPathButtonWidth, position.height);

            EditorGUI.LabelField(pathRect, pathContent);
            if (EditorGUI.DropdownButton(pathButtonRect, new GUIContent("Edit"), FocusType.Keyboard))
            {
                PopupWindow.Show(pathButtonRect, new InputBindingPathSelector(pathProperty));
            }

            EditorGUI.EndProperty();
        }

        private GUIContent GetContentForPath(string path)
        {
            ////TODO
            return new GUIContent("path");
        }
    }
}
#endif // UNITY_EDITOR
