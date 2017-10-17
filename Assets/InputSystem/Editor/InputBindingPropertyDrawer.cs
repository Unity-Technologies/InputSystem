#if UNITY_EDITOR
using System.Text.RegularExpressions;
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
            if (EditorGUI.DropdownButton(pathButtonRect, Contents.pickContent, FocusType.Keyboard))
            {
                PopupWindow.Show(pathButtonRect, new InputBindingPathSelector(pathProperty));
            }

            EditorGUI.EndProperty();
        }

        private GUIContent GetContentForPath(string path)
        {
            if (s_UsageRegex == null)
                s_UsageRegex = new Regex("\\*/{([A-Za-z0-9]+)}");
            if (s_ControlRegex == null)
                s_ControlRegex = new Regex("<([A-Za-z0-9]+)>/([A-Za-z0-9]+)");

            var usageMatch = s_UsageRegex.Match(path);
            if (usageMatch.Success)
            {
                return new GUIContent(usageMatch.Groups[1].Value);
            }

            var controlMatch = s_ControlRegex.Match(path);
            if (controlMatch.Success)
            {
                var device = controlMatch.Groups[1].Value;
                var control = controlMatch.Groups[2].Value;

                ////TODO: would be nice to print something like "Gamepad: A Button" instead of "Gamepad: A" (or whatever)

                return new GUIContent($"{device} {control}");
            }

            return new GUIContent(path);
        }

        private static Regex s_UsageRegex;
        private static Regex s_ControlRegex;

        private static class Contents
        {
            public static GUIContent pickContent = new GUIContent("Pick");
        }
    }
}
#endif // UNITY_EDITOR
