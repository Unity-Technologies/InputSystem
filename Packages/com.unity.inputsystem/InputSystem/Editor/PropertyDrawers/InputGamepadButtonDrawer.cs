using UnityEditor;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Editor
{
    [CustomPropertyDrawer(typeof(GamepadButton))]
    public class InputGamepadButtonDrawer : InputEnumDrawerBase<GamepadButton>
    {
        protected override bool TryGetPopupOptions(SerializedProperty property, out string[] popupOptions)
        {

            controllerType = (ControllerTypes)EditorGUILayout.EnumPopup("Controller type", controllerType);
            string[] options = property.enumDisplayNames;
            switch (controllerType)
            {
                case ControllerTypes.Switch:
                    options[4] = "X";
                    options[5] = "A";
                    options[6] = "B";
                    options[7] = "Y";
                    break;
                case ControllerTypes.PlayStation:
                    options[4] = "Triangle";
                    options[5] = "Circle";
                    options[6] = "Cross";
                    options[7] = "Square";
                    break;
                case ControllerTypes.Xbox:
                    options[4] = "Y";
                    options[5] = "B";
                    options[6] = "A";
                    options[7] = "X";
                    break;
                default:
                    popupOptions = null;
                    return false;
            }
            popupOptions = options;
            return true;
        }
        protected override void DisplayDefaultEnum(SerializedProperty property, GUIContent label)
        {
            property.intValue = (int)(GamepadButton)EditorGUILayout.EnumPopup(label, (GamepadButton)property.intValue);
        }

        private enum ControllerTypes
        {
            Switch,
            PlayStation,
            Xbox,
            Other
        }
        private ControllerTypes controllerType;
    }
}