#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ISX
{
    #if false
    [CustomPropertyDrawer(typeof(InputAction))]
    public class InputActionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(position, "Yeah!");
        }
    }
    #endif
}
#endif
