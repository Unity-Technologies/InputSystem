using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Editor
{
    internal class CopyPasteHelper : MonoBehaviour
    {
        internal const string k_CopyPasteMarker = "INPUTASSET ";
        private const string k_StartOfText = "\u0002";
        private const string k_EndOfTransmission = "\u0004";
        private static readonly Dictionary<Type, string> k_TypeMarker = new Dictionary<Type, string>()
        {
            {typeof(InputActionMap), "InputActionMap"},
            {typeof(InputAction), "InputAction"},
            {typeof(InputBinding), "InputBinding"},
        };

        public static SerializedProperty lastAddedElement;
        private static SerializedProperty currentActionMap;
        private static string currentActionName;

        public static void CopySelectedTreeViewItemsToClipboard(List<SerializedProperty> items, Type type)
        {
            var copyBuffer = new StringBuilder();
            CopyItems(items, copyBuffer, type); // TODO: sort out contained children
            EditorGUIUtility.systemCopyBuffer = copyBuffer.ToString();
        }

        private static void CopyItems(List<SerializedProperty> items, StringBuilder buffer, Type type)
        {
            buffer.Append(k_CopyPasteMarker);
            buffer.Append(k_TypeMarker[type]);
            foreach (var item in items)
            {
                CopyItemData(item, buffer);
                buffer.Append(k_EndOfTransmission);
            }
        }

        private static void CopyItemData(SerializedProperty item, StringBuilder buffer)
        {
            buffer.Append(k_StartOfText);
            buffer.Append(item.CopyToJson(true));
        }

        public static bool HavePastableClipboardData(Type selectedType)
        {
            var clipboard = EditorGUIUtility.systemCopyBuffer;
            if (clipboard.Length < k_CopyPasteMarker.Length)
                return false;
            var isInputAssetData = clipboard.StartsWith(k_CopyPasteMarker);
            return isInputAssetData && IsMatchingType(selectedType, GetCopiedClipboardType());
        }

        private static bool IsMatchingType(Type selectedType, Type copiedType)
        {
            if (selectedType == typeof(InputActionMap))
                return copiedType == typeof(InputActionMap) || copiedType == typeof(InputAction);
            if (selectedType == typeof(InputAction))
                return copiedType == typeof(InputAction) || copiedType == typeof(InputBinding);
            //bindings and composites
            return copiedType == typeof(InputBinding);
        }

        public static Type GetCopiedClipboardType()
        {
            if (!EditorGUIUtility.systemCopyBuffer.StartsWith(k_CopyPasteMarker))
                return null;
            foreach (var typePair in k_TypeMarker)
            {
                if (EditorGUIUtility.systemCopyBuffer.Substring(k_CopyPasteMarker.Length).StartsWith(typePair.Value))
                    return typePair.Key;
            }
            return null;
        }

        public static void PasteFromClipboard(int[] indicesToInsert, SerializedProperty arrayToInsertInto, SerializedProperty actionMap = null, string actionName = "")
        {
            lastAddedElement = null;
            currentActionMap = actionMap;
            currentActionName = actionName;
            PasteData(EditorGUIUtility.systemCopyBuffer, indicesToInsert, arrayToInsertInto);
        }

        private static void PasteData(string copyBufferString, int[] indicesToInsert, SerializedProperty arrayToInsertInto)
        {
            if (!copyBufferString.StartsWith(k_CopyPasteMarker))
                return;
            PasteItems(copyBufferString, indicesToInsert, arrayToInsertInto);
        }

        private static void PasteItems(string copyBufferString, int[] indicesToInsert, SerializedProperty arrayToInsertInto, bool assignNewIDs = true, bool selectNewItems = true)
        {
            // Split buffer into transmissions and then into transmission blocks
            int indexOffset = 0;
            foreach (var transmission in copyBufferString.Substring(k_CopyPasteMarker.Length + k_TypeMarker[GetCopiedClipboardType()].Length)
                     .Split(new[] {k_EndOfTransmission}, StringSplitOptions.RemoveEmptyEntries))
            {
                indexOffset += 1;
                foreach (var index in indicesToInsert)
                    PasteBlocks(transmission, index + indexOffset, arrayToInsertInto);
            }
        }

        private static void PasteBlocks(string transmission, int indexToInsert, SerializedProperty arrayToInsertInto)
        {
            var block = transmission.Substring(transmission.IndexOf(k_StartOfText, StringComparison.Ordinal) + 1);
            var copiedType = GetCopiedClipboardType();
            if (copiedType == typeof(InputActionMap))
                PasteElement(arrayToInsertInto, block, indexToInsert);
            else if (copiedType == typeof(InputAction))
                PasteAction(arrayToInsertInto, block, indexToInsert);
            else
            {
                PasteBindingOrComposite(arrayToInsertInto, block, currentActionName, indexToInsert);
            }
        }

        public static SerializedProperty DuplicateElement(SerializedProperty arrayProperty, SerializedProperty toDuplicate, string name, int index, bool changeName = true)
        {
            var json = toDuplicate.CopyToJson(true);
            return PasteElement(arrayProperty, json, index, name, changeName);
        }

        private static SerializedProperty PasteElement(SerializedProperty arrayProperty, string json, int index, string name = "newElement",  bool changeName = true, bool assignUniqueIDs = true)
        {
            var duplicatedProperty = AddElement(arrayProperty, name, index);
            duplicatedProperty.RestoreFromJson(json);
            if (changeName)
                EnsureUniqueName(duplicatedProperty);
            if (assignUniqueIDs)
                AssignUniqueIDs(duplicatedProperty);
            lastAddedElement = duplicatedProperty;
            return duplicatedProperty;
        }

        public static void DuplicateAction(SerializedProperty actionMap, SerializedProperty arrayProperty, SerializedProperty toDuplicate, string name)
        {
            currentActionMap = actionMap;
            var json = toDuplicate.CopyToJson(true);
            PasteAction(arrayProperty, json, toDuplicate.GetIndexOfArrayElement() + 1);
        }

        private static void PasteAction(SerializedProperty arrayProperty, string jsonToInsert, int indexToInsert)
        {
            var property = PasteElement(arrayProperty, jsonToInsert, indexToInsert, "", false);
            var name = property.FindPropertyRelative("m_Name").stringValue;
            EnsureUniqueName(property);
            var newName = property.FindPropertyRelative("m_Name").stringValue;
            var actionMap = property.FindPropertyRelative("m_ActionMap");
            var bindingsArray = actionMap.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var bindings = bindingsArray.Where(binding => binding.FindPropertyRelative("m_Action").stringValue.Equals(name)).ToList();
            var index = bindings.Select(b => b.GetIndexOfArrayElement()).Max() + 1;
            var bindingArrayToInsertTo = arrayProperty.FirstOrDefault()?.FindPropertyRelative("m_ActionMap")?.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            foreach (var binding in bindings)
            {
                var newIndex = PasteBindingAsPartOfAction(bindingArrayToInsertTo, binding, newName, index);
                Debug.Log("do");
                index = newIndex;
            }
        }

        private static int PasteBindingAsPartOfAction(SerializedProperty arrayProperty, SerializedProperty toDuplicate, string newActionName, int index)
        {
            if (IsComposite(toDuplicate))
            {
                DuplicateComposite(arrayProperty, toDuplicate, PropertyName(toDuplicate), newActionName, index, out var newIndex, false);
                return newIndex;
            }
            if (IsPartComposite(toDuplicate))
                return index;
            var duplicatedBinding = DuplicateElement(arrayProperty, toDuplicate, PropertyName(toDuplicate), index++, false);
            duplicatedBinding.FindPropertyRelative("m_Action").stringValue = newActionName;
            return index;
        }

        private static bool IsComposite(SerializedProperty property) => property.FindPropertyRelative("m_Flags").intValue == (int)InputBinding.Flags.Composite;
        private static bool IsPartComposite(SerializedProperty property) => property.FindPropertyRelative("m_Flags").intValue == (int)InputBinding.Flags.PartOfComposite;
        private static string PropertyName(SerializedProperty property) => property.FindPropertyRelative("m_Name").stringValue;

        public static int DuplicateBinding(SerializedProperty arrayProperty, SerializedProperty toDuplicate, string newActionName, int index)
        {
            if (IsComposite(toDuplicate))
                return DuplicateComposite(arrayProperty, toDuplicate, PropertyName(toDuplicate), newActionName, index, out _).GetIndexOfArrayElement();
            var binding = DuplicateElement(arrayProperty, toDuplicate, newActionName, index, false);
            return PasteBinding(binding, newActionName, index);
        }

        private static void PasteBindingOrComposite(SerializedProperty arrayProperty, string toDuplicate, string newActionName, int index)
        {
            var property = PasteElement(arrayProperty, toDuplicate, index, "", false);
            if (IsComposite(property))
            {
                PasteComposite(arrayProperty, property, PropertyName(property), newActionName, index);
                return;
            }
            PasteBinding(property, newActionName, index);
        }

        private static int PasteBinding(SerializedProperty duplicatedBinding, string newActionName, int index)
        {
            duplicatedBinding.FindPropertyRelative("m_Action").stringValue = newActionName;
            return index;
        }

        private static void PasteComposite(SerializedProperty bindingsArray, SerializedProperty duplicatedComposite, string name, string actionName, int index, bool increaseIndex = true)
        {
            index += InputActionSerializationHelpers.GetCompositePartCount(bindingsArray, duplicatedComposite.GetIndexOfArrayElement());
            duplicatedComposite.FindPropertyRelative("m_Name").stringValue = name;
            duplicatedComposite.FindPropertyRelative("m_Action").stringValue = actionName;
            PasteBindingsForComposite(bindingsArray, duplicatedComposite, index, actionName);
        }

        private static SerializedProperty DuplicateComposite(SerializedProperty bindingsArray, SerializedProperty compositeToDuplicate, string name, string actionName, int index, out int newIndex, bool increaseIndex = true)
        {
            if (increaseIndex)
                index += InputActionSerializationHelpers.GetCompositePartCount(bindingsArray, compositeToDuplicate.GetIndexOfArrayElement());
            var newComposite = DuplicateElement(bindingsArray, compositeToDuplicate, name, index++, false);
            newComposite.FindPropertyRelative("m_Action").stringValue = actionName;

            PasteBindingsForComposite(bindingsArray, compositeToDuplicate, index, actionName);
            newIndex = index;
            return newComposite;
        }

        private static void PasteBindingsForComposite(SerializedProperty bindingsArray, SerializedProperty compositeToDuplicate, int index, string actionName)
        {
            var bindings = GetBindingsForComposite(bindingsArray, compositeToDuplicate);
            foreach (var binding in bindings)
            {
                var newBinding = DuplicateElement(bindingsArray, binding, binding.FindPropertyRelative("m_Name").stringValue, index++, false);
                newBinding.FindPropertyRelative("m_Action").stringValue = actionName;
            }
        }

        private static List<SerializedProperty> GetBindingsForComposite(SerializedProperty bindingsArray, SerializedProperty compositeToDuplicate)
        {
            var compositeBindings = new List<SerializedProperty>();
            var compositeStartIndex = InputActionSerializationHelpers.GetCompositeStartIndex(bindingsArray, compositeToDuplicate.GetIndexOfArrayElement());
            if (compositeStartIndex == -1)
                return compositeBindings;

            for (var i = compositeStartIndex + 1; i < bindingsArray.arraySize; ++i)
            {
                var bindingProperty = bindingsArray.GetArrayElementAtIndex(i);
                var bindingFlags = (InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue;
                if ((bindingFlags & InputBinding.Flags.PartOfComposite) == 0)
                    break;
                compositeBindings.Add(bindingProperty);
            }
            return compositeBindings;
        }

        public static SerializedProperty AddElement(SerializedProperty arrayProperty, string name, int index = -1)
        {
            var uniqueName = InputActionSerializationHelpers.FindUniqueName(arrayProperty, name);
            if (index < 0)
                index = arrayProperty.arraySize;

            arrayProperty.InsertArrayElementAtIndex(index);
            var elementProperty = arrayProperty.GetArrayElementAtIndex(index);
            elementProperty.ResetValuesToDefault();

            elementProperty.FindPropertyRelative("m_Name").stringValue = uniqueName;
            elementProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            return elementProperty;
        }

        public static void EnsureUniqueName(SerializedProperty arrayElement)
        {
            var arrayProperty = arrayElement.GetArrayPropertyFromElement();
            var arrayIndexOfElement = arrayElement.GetIndexOfArrayElement();
            var nameProperty = arrayElement.FindPropertyRelative("m_Name");
            var baseName = nameProperty.stringValue;
            nameProperty.stringValue = InputActionSerializationHelpers.FindUniqueName(arrayProperty, baseName, ignoreIndex: arrayIndexOfElement);
        }

        public static void AssignUniqueIDs(SerializedProperty element)
        {
            // Assign new ID to map.
            AssignUniqueID(element);

            //
            foreach (var child in element.GetChildren())
            {
                if (!child.isArray)
                    continue;

                var fieldType = child.GetFieldType();
                if (fieldType == typeof(InputBinding[]) || fieldType == typeof(InputAction[]) ||
                    fieldType == typeof(InputActionMap))
                {
                    ////TODO: update bindings that refer to actions by {id}
                    for (var i = 0; i < child.arraySize; ++i)
                        using (var childElement = child.GetArrayElementAtIndex(i))
                            AssignUniqueIDs(childElement);
                }
            }
        }

        private static void AssignUniqueID(SerializedProperty property)
        {
            var idProperty = property.FindPropertyRelative("m_Id");
            idProperty.stringValue = Guid.NewGuid().ToString();
        }
    }
}
