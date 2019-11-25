#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Helpers for working with <see cref="SerializedProperty"/> in the editor.
    /// </summary>
    internal static class SerializedPropertyHelpers
    {
        // Show a PropertyField with a greyed-out default text if the field is empty and not being edited.
        // This is meant to communicate the fact that filling these properties is optional and that Unity will
        // use reasonable defaults if left empty.
        public static void PropertyFieldWithDefaultText(this SerializedProperty prop, GUIContent label, string defaultText)
        {
            GUI.SetNextControlName(label.text);
            var rt = GUILayoutUtility.GetRect(label, GUI.skin.textField);

            EditorGUI.PropertyField(rt, prop, label);
            if (string.IsNullOrEmpty(prop.stringValue) && GUI.GetNameOfFocusedControl() != label.text && Event.current.type == EventType.Repaint)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    rt.xMin += EditorGUIUtility.labelWidth;
                    GUI.skin.textField.Draw(rt, new GUIContent(defaultText), false, false, false, false);
                }
            }
        }

        public static SerializedProperty GetParentProperty(this SerializedProperty property)
        {
            var path = property.propertyPath;
            var lastDot = path.LastIndexOf('.');
            if (lastDot == -1)
                return null;
            var parentPath = path.Substring(0, lastDot);
            return property.serializedObject.FindProperty(parentPath);
        }

        public static SerializedProperty GetArrayPropertyFromElement(this SerializedProperty property)
        {
            // Arrays have a structure of 'arrayName.Array.data[index]'.
            // Given property should be element and thus 'data[index]'.
            var arrayProperty = property.GetParentProperty();
            Debug.Assert(arrayProperty.name == "Array", "Expecting 'Array' property");
            return arrayProperty.GetParentProperty();
        }

        public static int GetIndexOfArrayElement(this SerializedProperty property)
        {
            var propertyPath = property.propertyPath;
            if (propertyPath[propertyPath.Length - 1] != ']')
                return -1;
            var lastIndexOfLeftBracket = propertyPath.LastIndexOf('[');
            if (int.TryParse(
                propertyPath.Substring(lastIndexOfLeftBracket + 1, propertyPath.Length - lastIndexOfLeftBracket - 2),
                out var index))
                return index;
            return -1;
        }

        public static Type GetArrayElementType(this SerializedProperty property)
        {
            Debug.Assert(property.isArray, $"Property {property.propertyPath} is not an array");

            var fieldType = property.GetFieldType();
            if (fieldType == null)
                throw new ArgumentException($"Cannot determine managed field type of {property.propertyPath}",
                    nameof(property));

            return fieldType.GetElementType();
        }

        public static void ResetValuesToDefault(this SerializedProperty property)
        {
            var isString = property.propertyType == SerializedPropertyType.String;

            if (property.isArray && !isString)
            {
                property.ClearArray();
            }
            else if (property.hasChildren && !isString)
            {
                foreach (var child in property.GetChildren())
                    ResetValuesToDefault(child);
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                        property.floatValue = default(float);
                        break;

                    case SerializedPropertyType.Boolean:
                        property.boolValue = default(bool);
                        break;

                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.Integer:
                        property.intValue = default(int);
                        break;

                    case SerializedPropertyType.String:
                        property.stringValue = string.Empty;
                        break;

                    case SerializedPropertyType.ObjectReference:
                        property.objectReferenceValue = null;
                        break;
                }
            }
        }

        public static string ToJson(this SerializedObject serializedObject)
        {
            return JsonUtility.ToJson(serializedObject, prettyPrint: true);
        }

        // The following is functionality that allows turning Unity data into text and text
        // back into Unity data. Given that this is essential functionality for any kind of
        // copypaste support, I'm not sure why the Unity editor API isn't providing this out
        // of the box. Internally, we do have support for this on a whole-object kind of level
        // but not for parts of serialized objects.

        /// <summary>
        ///
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        /// <remarks>
        /// Converting entire objects to JSON is easy using Unity's serialization system but we cannot
        /// easily convert just a part of the serialized graph to JSON (or any text format for that matter)
        /// and then recreate the same data from text through SerializedProperties. This method helps by manually
        /// turning an arbitrary part of a graph into JSON which can then be used with <see cref="RestoreFromJson"/>
        /// to write the data back into an existing property.
        ///
        /// The primary use for this is copy-paste where serialized data needs to be stored in
        /// <see cref="EditorGUIUtility.systemCopyBuffer"/>.
        /// </remarks>
        public static string CopyToJson(this SerializedProperty property, bool ignoreObjectReferences = false)
        {
            var buffer = new StringBuilder();
            CopyToJson(property, buffer, ignoreObjectReferences);
            return buffer.ToString();
        }

        public static void CopyToJson(this SerializedProperty property, StringBuilder buffer, bool ignoreObjectReferences = false)
        {
            CopyToJson(property, buffer, noPropertyName: true, ignoreObjectReferences: ignoreObjectReferences);
        }

        private static void CopyToJson(this SerializedProperty property, StringBuilder buffer, bool noPropertyName, bool ignoreObjectReferences)
        {
            var propertyType = property.propertyType;
            if (ignoreObjectReferences && propertyType == SerializedPropertyType.ObjectReference)
                return;

            // Property name.
            if (!noPropertyName)
            {
                buffer.Append('"');
                buffer.Append(property.name);
                buffer.Append('"');
                buffer.Append(':');
            }

            // Strings are classified as arrays and have children.
            var isString = propertyType == SerializedPropertyType.String;

            // Property value.
            if (property.isArray && !isString)
            {
                buffer.Append('[');
                var arraySize = property.arraySize;
                var isFirst = true;
                for (var i = 0; i < arraySize; ++i)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    if (ignoreObjectReferences && element.propertyType == SerializedPropertyType.ObjectReference)
                        continue;
                    if (!isFirst)
                        buffer.Append(',');
                    CopyToJson(element, buffer, true, ignoreObjectReferences);
                    isFirst = false;
                }
                buffer.Append(']');
            }
            else if (property.hasChildren && !isString)
            {
                // Any structured data we represent as a JSON object.

                buffer.Append('{');
                var isFirst = true;
                foreach (var child in property.GetChildren())
                {
                    if (ignoreObjectReferences && child.propertyType == SerializedPropertyType.ObjectReference)
                        continue;
                    if (!isFirst)
                        buffer.Append(',');
                    CopyToJson(child, buffer, false, ignoreObjectReferences);
                    isFirst = false;
                }
                buffer.Append('}');
            }
            else
            {
                switch (propertyType)
                {
                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.Integer:
                        buffer.Append(property.intValue);
                        break;

                    case SerializedPropertyType.Float:
                        buffer.Append(property.floatValue);
                        break;

                    case SerializedPropertyType.String:
                        buffer.Append('"');
                        buffer.Append(property.stringValue.Escape());
                        buffer.Append('"');
                        break;

                    case SerializedPropertyType.Boolean:
                        if (property.boolValue)
                            buffer.Append("true");
                        else
                            buffer.Append("false");
                        break;

                    ////TODO: other property types
                    default:
                        throw new NotImplementedException($"Support for {property.propertyType} property type");
                }
            }
        }

        public static void RestoreFromJson(this SerializedProperty property, string json)
        {
            var parser = new JsonParser(json);
            RestoreFromJson(property, ref parser);
        }

        public static void RestoreFromJson(this SerializedProperty property, ref JsonParser parser)
        {
            var isString = property.propertyType == SerializedPropertyType.String;

            if (property.isArray && !isString)
            {
                property.ClearArray();
                parser.ParseToken('[');
                while (!parser.ParseToken(']') && !parser.isAtEnd)
                {
                    var index = property.arraySize;
                    property.InsertArrayElementAtIndex(index);
                    var elementProperty = property.GetArrayElementAtIndex(index);
                    RestoreFromJson(elementProperty, ref parser);
                    parser.ParseToken(',');
                }
            }
            else if (property.hasChildren && !isString)
            {
                parser.ParseToken('{');
                while (!parser.ParseToken('}') && !parser.isAtEnd)
                {
                    parser.ParseStringValue(out var propertyName);
                    parser.ParseToken(':');

                    var childProperty = property.FindPropertyRelative(propertyName.ToString());
                    if (childProperty == null)
                        throw new ArgumentException($"Cannot find property '{propertyName}' in {property}", nameof(property));

                    RestoreFromJson(childProperty, ref parser);
                    parser.ParseToken(',');
                }
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float:
                    {
                        parser.ParseNumber(out var num);
                        property.floatValue = (float)num.ToDouble();
                        break;
                    }

                    case SerializedPropertyType.String:
                    {
                        parser.ParseStringValue(out var str);
                        property.stringValue = str.ToString();
                        break;
                    }

                    case SerializedPropertyType.Boolean:
                    {
                        parser.ParseBooleanValue(out var b);
                        property.boolValue = b.ToBoolean();
                        break;
                    }

                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.Integer:
                    {
                        parser.ParseNumber(out var num);
                        property.intValue = (int)num.ToInteger();
                        break;
                    }

                    default:
                        throw new NotImplementedException(
                            $"Restoring property value of type {property.propertyType} (property: {property})");
                }
            }
        }

        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
        {
            if (!property.hasChildren)
                yield break;

            using (var iter = property.Copy())
            {
                var end = iter.GetEndProperty(true);

                // Go to first child.
                if (!iter.Next(true))
                    yield break; // Shouldn't happen; we've already established we have children.

                // Iterate over children.
                while (!SerializedProperty.EqualContents(iter, end))
                {
                    yield return iter;
                    if (!iter.Next(false))
                        break;
                }
            }
        }

        public static FieldInfo GetField(this SerializedProperty property)
        {
            var objectType = property.serializedObject.targetObject.GetType();
            var currentSerializableType = objectType;
            var pathComponents = property.propertyPath.Split('.');

            FieldInfo result = null;
            foreach (var component in pathComponents)
            {
                // Handle arrays. They are followed by "Array" and "data[N]" elements.
                if (result != null && currentSerializableType.IsArray)
                {
                    if (component == "Array")
                        continue;

                    if (component.StartsWith("data["))
                    {
                        currentSerializableType = currentSerializableType.GetElementType();
                        continue;
                    }
                }

                result = currentSerializableType.GetField(component,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (result == null)
                    return null;
                currentSerializableType = result.FieldType;
            }

            return result;
        }

        public static Type GetFieldType(this SerializedProperty property)
        {
            return GetField(property)?.FieldType;
        }
    }
}
#endif // UNITY_EDITOR
