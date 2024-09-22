using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Experimental.Editor
{
    // https://github.com/Thundernerd/Unity3D-SerializableInterface/blob/main/Runtime/Extensions.cs
    
    [CustomPropertyDrawer(typeof(InputBinding<>), useForChildren: true)]
    public class InputBindingPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create property container element.
            var container = new VisualElement();

            // Create property fields.
            var objectField = new PropertyField(GetObjectProperty(property));
            var objField = new PropertyField(GetValueProperty(property));
            var modeField = new PropertyField(GetModeProperty(property));
            
            var createObject = new Button();
            createObject.text = "Create Unity Object";
            createObject.clicked += () =>
            {
                SetPropertyValue(property, InputBindingMode.Reference, ScriptableInputBinding.Create(Devices.Gamepad.leftStick));
            };

            var createObj = new Button();
            createObj.text = "Create Object";
            createObj.clicked += () =>
            {
                SetPropertyValue(property, InputBindingMode.Value, Devices.Gamepad.leftStick);
            };
            
            var reset = new Button();
            reset.text = "Reset";
            reset.clicked += () =>
            {
                SetPropertyValue(property, InputBindingMode.Value, null);
            };
            
            // Add fields to the container.
            container.Add(objectField);
            container.Add(objField);
            container.Add(modeField);
            container.Add(createObject);
            container.Add(createObj);
            container.Add(reset);

            return container;
        }

        private static SerializedProperty GetModeProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Mode");
        }

        private static InputBindingMode GetModeValue(SerializedProperty property)
        {
            return (InputBindingMode)GetModeProperty(property).enumValueIndex;
        }

        private static void SetMode(SerializedProperty property, InputBindingMode value)
        {
            GetModeProperty(property).enumValueIndex = (int)value;
        }

        private static SerializedProperty GetObjectProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Object");
        }

        private static Object GetObjectReference(SerializedProperty property)
        {
            return GetObjectProperty(property).objectReferenceValue;
        }

        private static void SetObjectReference(SerializedProperty property, Object value)
        {
            GetObjectProperty(property).objectReferenceValue = value;
        }

        private static SerializedProperty GetValueProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_Value");
        }
        
        private static object GetValue(SerializedProperty property)
        {
            return GetValueProperty(property).managedReferenceValue;
        }

        private static void SetValue(SerializedProperty property, object value)
        {
            GetValueProperty(property).managedReferenceValue = value;
        }
        
        private static void SetPropertyValue(SerializedProperty property, InputBindingMode mode, object value)
        {
            switch (mode)
            {
                case InputBindingMode.Reference:
                    SetObjectReference(property, (Object)value);
                    SetValue(property, null);
                    SetMode(property, InputBindingMode.Reference);
                    break;
                case InputBindingMode.Value:
                    SetObjectReference(property, null);
                    SetValue(property, value);
                    SetMode(property, InputBindingMode.Value);
                    break;
                case InputBindingMode.Undefined:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}