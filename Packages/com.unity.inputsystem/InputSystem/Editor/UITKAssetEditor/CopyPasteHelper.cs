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
        private static InputActionsEditorState m_State;

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

        public static void PasteFromClipboard(int[] indicesToInsert, SerializedProperty arrayToInsertInto, InputActionsEditorState state)
        {
            lastAddedElement = null;
            m_State = state;
            PasteData(EditorGUIUtility.systemCopyBuffer, indicesToInsert, arrayToInsertInto);
        }

        private static void PasteData(string copyBufferString, int[] indicesToInsert, SerializedProperty arrayToInsertInto)
        {
            if (!copyBufferString.StartsWith(k_CopyPasteMarker))
                return;
            PasteItems(copyBufferString, indicesToInsert, arrayToInsertInto);
        }

        private static void PasteItems(string copyBufferString, int[] indicesToInsert, SerializedProperty arrayToInsertInto)
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
                PasteElement(arrayToInsertInto, block, indexToInsert, out _, out _);
            else if (copiedType == typeof(InputAction))
                PasteAction(arrayToInsertInto, block, indexToInsert);
            else
                PasteBindingOrComposite(arrayToInsertInto, block, indexToInsert);
        }

        public static SerializedProperty DuplicateElement(SerializedProperty arrayProperty, SerializedProperty toDuplicate, string name, int index, bool changeName = true)
        {
            var json = toDuplicate.CopyToJson(true);
            return PasteElement(arrayProperty, json, index, out _, out _, name, changeName);
        }

        private static SerializedProperty PasteElement(SerializedProperty arrayProperty, string json, int index, out string oldName, out string oldId, string name = "newElement",  bool changeName = true, bool assignUniqueIDs = true)
        {
            var duplicatedProperty = AddElement(arrayProperty, name, index);
            duplicatedProperty.RestoreFromJson(json);
            oldName = duplicatedProperty.FindPropertyRelative("m_Name").stringValue;
            oldId = duplicatedProperty.FindPropertyRelative("m_Id").stringValue;
            if (changeName)
                EnsureUniqueName(duplicatedProperty);
            if (assignUniqueIDs)
                AssignUniqueIDs(duplicatedProperty);
            lastAddedElement = duplicatedProperty;
            return duplicatedProperty;
        }

        public static void DuplicateAction(SerializedProperty arrayProperty, SerializedProperty toDuplicate, InputActionsEditorState state)
        {
            m_State = state;
            var json = toDuplicate.CopyToJson(true);
            PasteAction(arrayProperty, json, toDuplicate.GetIndexOfArrayElement() + 1);
        }

        private static void PasteAction(SerializedProperty arrayProperty, string jsonToInsert, int indexToInsert)
        {
            var property = PasteElement(arrayProperty, jsonToInsert, indexToInsert, out var name, out var id, "");
            var newName = property.FindPropertyRelative("m_Name").stringValue;
            var newId = property.FindPropertyRelative("m_Id").stringValue;
            var actionMapFrom = Selectors.GetActionMapForAction(m_State, id);
            var actionMapTo = Selectors.GetActionMapForAction(m_State, newId);
            var bindingsArray = actionMapFrom.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var bindings = bindingsArray.Where(binding => binding.FindPropertyRelative("m_Action").stringValue.Equals(name)).ToList();
            var bindingArrayToInsertTo = actionMapTo.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var prevActionName = arrayProperty.GetArrayElementAtIndex(indexToInsert - 1).FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
            var index = bindingArrayToInsertTo.Where(b => b.FindPropertyRelative("m_Action").stringValue.Equals(prevActionName)).Last().GetIndexOfArrayElement() + 1;
            foreach (var binding in bindings)
            {
                var newIndex = PasteBindingAsPartOfAction(bindingArrayToInsertTo, binding, newName, index);
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
            return PasteBinding(binding, index, newActionName);
        }

        private static void PasteBindingOrComposite(SerializedProperty arrayProperty, string toDuplicate, int index)
        {
            var property = PasteElement(arrayProperty, toDuplicate, index, out _, out var oldId, "", false);
            var actionName = Selectors.GetSelectedBinding(m_State)?.wrappedProperty.FindPropertyRelative("m_Action").stringValue;
            if (m_State.selectionType == SelectionType.Action)
                actionName = Selectors.GetSelectedAction(m_State)?.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
            if (IsComposite(property))
            {
                PasteComposite(arrayProperty, property, PropertyName(property), actionName, index, oldId);
                return;
            }
            PasteBinding(property, index, actionName);
        }

        private static int PasteBinding(SerializedProperty duplicatedBinding, int index, string actionName)
        {
            duplicatedBinding.FindPropertyRelative("m_Action").stringValue = actionName;
            return index;
        }

        private static void PasteComposite(SerializedProperty bindingsArray, SerializedProperty duplicatedComposite, string name, string actionName, int index, string oldId)
        {
            duplicatedComposite.FindPropertyRelative("m_Name").stringValue = name;
            duplicatedComposite.FindPropertyRelative("m_Action").stringValue = actionName;
            var composite = Selectors.GetBindingForId(m_State, oldId, out var bindingsFrom);
            var bindings = GetBindingsForComposite(bindingsFrom, composite.GetIndexOfArrayElement());
            PasteBindingsForComposite(bindingsArray, bindings, ++index, actionName);
        }

        private static SerializedProperty DuplicateComposite(SerializedProperty bindingsArray, SerializedProperty compositeToDuplicate, string name, string actionName, int index, out int newIndex, bool increaseIndex = true)
        {
            if (increaseIndex)
                index += InputActionSerializationHelpers.GetCompositePartCount(bindingsArray, compositeToDuplicate.GetIndexOfArrayElement());
            var newComposite = DuplicateElement(bindingsArray, compositeToDuplicate, name, index++, false);
            newComposite.FindPropertyRelative("m_Action").stringValue = actionName;
            var bindings = GetBindingsForComposite(bindingsArray, compositeToDuplicate.GetIndexOfArrayElement());
            newIndex = PasteBindingsForComposite(bindingsArray, bindings, index, actionName);
            return newComposite;
        }

        private static int PasteBindingsForComposite(SerializedProperty bindingsToInsertTo, List<SerializedProperty> bindingsOfComposite, int index, string actionName)
        {
            foreach (var binding in bindingsOfComposite)
            {
                var newBinding = DuplicateElement(bindingsToInsertTo, binding, binding.FindPropertyRelative("m_Name").stringValue, index++, false);
                newBinding.FindPropertyRelative("m_Action").stringValue = actionName;
            }

            return index;
        }

        private static List<SerializedProperty> GetBindingsForComposite(SerializedProperty bindingsArray, int indexOfComposite)
        {
            var compositeBindings = new List<SerializedProperty>();
            var compositeStartIndex = InputActionSerializationHelpers.GetCompositeStartIndex(bindingsArray, indexOfComposite);
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
