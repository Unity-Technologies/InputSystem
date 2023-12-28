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
        private const string k_CopyPasteMarker = "INPUTASSET ";
        private const string k_StartOfText = "\u0002";
        private const string k_EndOfTransmission = "\u0004";
        private const string k_BindingData = "bindingData";
        private const string k_EndOfBinding = "+++";
        private static readonly Dictionary<Type, string> k_TypeMarker = new Dictionary<Type, string>()
        {
            {typeof(InputActionMap), "InputActionMap"},
            {typeof(InputAction), "InputAction"},
            {typeof(InputBinding), "InputBinding"},
        };

        private static SerializedProperty lastAddedElement;
        private static InputActionsEditorState s_State;

        public static void CopySelectedTreeViewItemsToClipboard(List<SerializedProperty> items, Type type, SerializedProperty actionMap = null)
        {
            var copyBuffer = new StringBuilder();
            CopyItems(items, copyBuffer, type, actionMap); // TODO: sort out contained children
            EditorGUIUtility.systemCopyBuffer = copyBuffer.ToString();
        }

        private static void CopyItems(List<SerializedProperty> items, StringBuilder buffer, Type type, SerializedProperty actionMap)
        {
            buffer.Append(k_CopyPasteMarker);
            buffer.Append(k_TypeMarker[type]);
            foreach (var item in items)
            {
                CopyItemData(item, buffer, type, actionMap);
                buffer.Append(k_EndOfTransmission);
            }
        }

        private static void CopyItemData(SerializedProperty item, StringBuilder buffer, Type type, SerializedProperty actionMap)
        {
            buffer.Append(k_StartOfText);
            buffer.Append(item.CopyToJson(true));
            if (type != typeof(InputAction)) return;
            buffer.Append(k_BindingData);
            foreach (var binding in GetBindingsForActionInMap(actionMap, item))
            {
                buffer.Append(binding.CopyToJson(true));
                buffer.Append(k_EndOfBinding);
            }
        }

        private static List<SerializedProperty> GetBindingsForActionInMap(SerializedProperty actionMap, SerializedProperty action)
        {
            var actionName = action.FindPropertyRelative("m_Name").stringValue;
            var bindingsArray = actionMap.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var bindings = bindingsArray.Where(binding => binding.FindPropertyRelative("m_Action").stringValue.Equals(actionName)).ToList();
            return bindings;
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

        public static SerializedProperty PasteFromClipboard(int[] indicesToInsert, SerializedProperty arrayToInsertInto, InputActionsEditorState state)
        {
            lastAddedElement = null;
            s_State = state;
            PasteData(EditorGUIUtility.systemCopyBuffer, indicesToInsert, arrayToInsertInto);
            return lastAddedElement;
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
            {
                var actionName = Selectors.GetSelectedBinding(s_State)?.wrappedProperty.FindPropertyRelative("m_Action")
                    .stringValue;
                if (s_State.selectionType == SelectionType.Action)
                    actionName = Selectors.GetSelectedAction(s_State)?.wrappedProperty.FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
                PasteBindingOrComposite(arrayToInsertInto, block, indexToInsert, actionName);
            }
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
            s_State = state;
            var json = toDuplicate.CopyToJson(true);
            PasteAction(arrayProperty, json, toDuplicate.GetIndexOfArrayElement() + 1);
        }

        private static void PasteAction(SerializedProperty arrayProperty, string jsonToInsert, int indexToInsert)
        {
            var json = jsonToInsert.Split(k_BindingData, StringSplitOptions.RemoveEmptyEntries);
            var bindingJsons = json.Last().Split(k_EndOfBinding, StringSplitOptions.RemoveEmptyEntries);
            var property = PasteElement(arrayProperty, json.First(), indexToInsert, out _, out _, "");
            var newName = property.FindPropertyRelative("m_Name").stringValue;
            var newId = property.FindPropertyRelative("m_Id").stringValue;
            var actionMapTo = Selectors.GetActionMapForAction(s_State, newId);
            var bindingArrayToInsertTo = actionMapTo.FindPropertyRelative(nameof(InputActionMap.m_Bindings));
            var prevActionName = arrayProperty.GetArrayElementAtIndex(indexToInsert - 1).FindPropertyRelative(nameof(InputAction.m_Name)).stringValue;
            var index = bindingArrayToInsertTo.Where(b => b.FindPropertyRelative("m_Action").stringValue.Equals(prevActionName)).Last().GetIndexOfArrayElement() + 1;
            foreach (var bindingJson in bindingJsons)
            {
                Debug.Log(index);
                var newIndex = PasteBindingOrComposite(bindingArrayToInsertTo, bindingJson, index, newName);
                index = newIndex;
            }
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

        private static int PasteBindingOrComposite(SerializedProperty arrayProperty, string json, int index, string actionName)
        {
            var property = PasteElement(arrayProperty, json, index, out _, out var oldId, "", false);
            if (IsComposite(property))
                return PasteComposite(arrayProperty, property, PropertyName(property), actionName, index, oldId);
            PasteBinding(property, index, actionName);
            return index + 1;
        }

        private static int PasteBinding(SerializedProperty duplicatedBinding, int index, string actionName)
        {
            duplicatedBinding.FindPropertyRelative("m_Action").stringValue = actionName;
            return index;
        }

        private static int PasteComposite(SerializedProperty bindingsArray, SerializedProperty duplicatedComposite, string name, string actionName, int index, string oldId)
        {
            duplicatedComposite.FindPropertyRelative("m_Name").stringValue = name;
            duplicatedComposite.FindPropertyRelative("m_Action").stringValue = actionName;
            return index + 1;
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
