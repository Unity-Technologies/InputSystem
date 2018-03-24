#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    [CustomPropertyDrawer(typeof(InputActionWrapper), useForChildren: true)]
    public class InputActionWrapperDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var assetProperty = property.FindPropertyRelative("m_Asset");
            EditorGUI.PropertyField(position, assetProperty, label);
        }
    }
}
#endif // UNITY_EDITOR
