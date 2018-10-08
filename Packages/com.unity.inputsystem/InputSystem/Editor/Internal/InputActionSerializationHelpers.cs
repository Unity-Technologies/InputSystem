#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    // Helpers for doctoring around in InputActions using SerializedProperties.
    internal static class InputActionSerializationHelpers
    {
        public static int GetBindingCount(SerializedProperty bindingArrayProperty, string actionName)
        {
            Debug.Assert(bindingArrayProperty != null);
            Debug.Assert(bindingArrayProperty.isArray);

            var bindingCount = bindingArrayProperty.arraySize;
            var bindingCountForAction = 0;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingActionName = bindingArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Action")
                    .stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    ++bindingCountForAction;
            }

            return bindingCountForAction;
        }

        public static int GetBindingsStartIndex(SerializedProperty bindingArrayProperty, string actionName)
        {
            Debug.Assert(bindingArrayProperty != null);
            Debug.Assert(bindingArrayProperty.isArray);

            var bindingCount = bindingArrayProperty.arraySize;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingActionName = bindingArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Action")
                    .stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return i;
            }

            return -1;
        }

        public static SerializedProperty GetBinding(SerializedProperty bindingArrayProperty, string actionName, int index)
        {
            Debug.Assert(bindingArrayProperty != null);
            Debug.Assert(bindingArrayProperty.isArray);

            var bindingCount = bindingArrayProperty.arraySize;
            var bindingCountForAction = 0;
            for (var i = 0; i < bindingCount; ++i)
            {
                var bindingProperty = bindingArrayProperty.GetArrayElementAtIndex(i);
                var bindingActionName = bindingProperty.FindPropertyRelative("m_Action").stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                if (bindingCountForAction == index)
                    return bindingProperty;
                ++bindingCountForAction;
            }

            throw new ArgumentOutOfRangeException(
                string.Format("Binding index {0} on action '{1}' with {2} bindings is out of range", index, actionName,
                    bindingCountForAction), "index");
        }

        public static void AddActionMap(SerializedObject asset)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var mapCount = mapArrayProperty.arraySize;
            var index = mapCount;
            var name = FindUniqueName(mapArrayProperty, "default");

            mapArrayProperty.InsertArrayElementAtIndex(index);
            var mapProperty = mapArrayProperty.GetArrayElementAtIndex(index);

            mapProperty.FindPropertyRelative("m_Name").stringValue = name;
            mapProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            mapProperty.FindPropertyRelative("m_Actions").ClearArray();
            mapProperty.FindPropertyRelative("m_Bindings").ClearArray();
        }

        public static SerializedProperty AddActionMapFromSavedProperties(SerializedObject asset, Dictionary<string, string> parameters)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var mapCount = mapArrayProperty.arraySize;
            var index = mapCount;
            var name = FindUniqueName(mapArrayProperty, parameters["m_Name"]);

            mapArrayProperty.InsertArrayElementAtIndex(index);
            var mapProperty = mapArrayProperty.GetArrayElementAtIndex(index);
            mapProperty.FindPropertyRelative("m_Actions").ClearArray();
            mapProperty.FindPropertyRelative("m_Bindings").ClearArray();
            mapProperty.FindPropertyRelative("m_Name").stringValue = name;
            mapProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            return mapProperty;
        }

        public static void DeleteActionMap(SerializedObject asset, int index)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            mapArrayProperty.DeleteArrayElementAtIndex(index);
        }

        // Append a new action to the end of the set.
        public static SerializedProperty AddAction(SerializedProperty actionMap)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            var actionsCount = actionsArrayProperty.arraySize;
            var actionIndex = actionsCount;

            var actionName = FindUniqueName(actionsArrayProperty, "action");

            actionsArrayProperty.InsertArrayElementAtIndex(actionIndex);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex);
            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
            actionProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            return actionProperty;
        }

        public static SerializedProperty AddActionFromSavedProperties(Dictionary<string, string> parameters, SerializedProperty actionMap)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            var actionsCount = actionsArrayProperty.arraySize;
            var actionIndex = actionsCount;

            actionsArrayProperty.InsertArrayElementAtIndex(actionIndex);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex);

            var actionName = FindUniqueName(actionsArrayProperty, parameters["m_Name"]);
            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
            return actionProperty;
        }

        public static void MoveBinding(SerializedProperty actionMapProperty, int srcIndex, int dstIndex)
        {
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            actionsArrayProperty.MoveArrayElement(srcIndex, dstIndex);
        }

        public static void MoveActionMap(SerializedObject serializedObject, int srcIndex, int dstIndex)
        {
            var actionMapsProperty = serializedObject.FindProperty("m_ActionMaps");
            actionMapsProperty.MoveArrayElement(srcIndex, dstIndex);
        }

        public static void MoveAction(SerializedProperty actionMapProperty, int srcIndex, int dstIndex)
        {
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            actionsArrayProperty.MoveArrayElement(srcIndex, dstIndex);
        }

        public static void DeleteAction(SerializedProperty actionMap, int actionIndex)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            actionsArrayProperty.DeleteArrayElementAtIndex(actionIndex);
        }

        // Equivalent to InputAction.AddBinding().
        public static SerializedProperty AddBinding(SerializedProperty actionProperty, SerializedProperty actionMapProperty = null)
        {
            var bindingsArrayProperty = actionMapProperty != null
                ? actionMapProperty.FindPropertyRelative("m_Bindings")
                : actionProperty.FindPropertyRelative("m_SingletonActionBindings");
            var bindingsCount = bindingsArrayProperty.arraySize;

            // Find the index of the last binding for the action in the array.
            var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
            var indexOfLastBindingForAction = -1;
            for (var i = 0; i < bindingsCount; ++i)
            {
                var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);
                var bindingActionName = bindingProperty.FindPropertyRelative("m_Action").stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    indexOfLastBindingForAction = i;
            }

            // Insert after last binding or at end of array.
            var bindingIndex = indexOfLastBindingForAction != -1 ? indexOfLastBindingForAction + 1 : bindingsCount;
            bindingsArrayProperty.InsertArrayElementAtIndex(bindingIndex);

            var newActionProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            newActionProperty.FindPropertyRelative("m_Path").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("m_Groups").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("m_Interactions").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("m_Flags").intValue = 0;
            newActionProperty.FindPropertyRelative("m_Action").stringValue = actionName;

            return newActionProperty;

            ////FIXME: this likely leaves m_Bindings in the map for singleton actions unsync'd in some cases
        }

        public static void AddBindingFromSavedProperties(Dictionary<string, string> values, SerializedProperty actionProperty, SerializedProperty actionMapProperty = null)
        {
            var newBindingProperty = AddBinding(actionProperty, actionMapProperty);
            newBindingProperty.FindPropertyRelative("m_Path").stringValue = values["path"];
            newBindingProperty.FindPropertyRelative("m_Name").stringValue = values["name"];
            newBindingProperty.FindPropertyRelative("m_Groups").stringValue = values["groups"];
            newBindingProperty.FindPropertyRelative("m_Interactions").stringValue = values["interactions"];
            newBindingProperty.FindPropertyRelative("m_Flags").intValue = int.Parse(values["flags"]);
        }

        public static void RemoveBinding(SerializedProperty actionProperty, int bindingIndex, SerializedProperty actionMapProperty = null)
        {
            var bindingsArrayProperty = actionMapProperty != null
                ? actionMapProperty.FindPropertyRelative("m_Bindings")
                : actionProperty.FindPropertyRelative("m_SingletonActionBindings");
            var bindingsCount = bindingsArrayProperty.arraySize;

            // Find the index of the binding in the action map.
            var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;
            var currentBindingIndexInAction = -1;
            for (var i = 0; i < bindingsCount; ++i)
            {
                var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);
                var bindingActionName = bindingProperty.FindPropertyRelative("m_Action").stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                ++currentBindingIndexInAction;
                if (bindingIndex != currentBindingIndexInAction)
                    continue;

                bindingsArrayProperty.DeleteArrayElementAtIndex(i);
                ////FIXME: this likely leaves m_Bindings in the map for singleton actions unsync'd in some cases
                return;
            }

            throw new ArgumentOutOfRangeException(
                string.Format("Binding index {0} on action {1} is out of range", bindingIndex, actionName),
                "bindingIndex");
        }

        public static string FindUniqueName(SerializedProperty arrayProperty, string baseName)
        {
            var result = baseName;
            var lowerCase = baseName.ToLower();
            var nameIsUnique = false;
            var namesTried = 0;
            var actionCount = arrayProperty.arraySize;

            while (!nameIsUnique)
            {
                nameIsUnique = true;

                for (var i = 0; i < actionCount; ++i)
                {
                    var elementProperty = arrayProperty.GetArrayElementAtIndex(i);
                    var nameProperty = elementProperty.FindPropertyRelative("m_Name");
                    var elementName = nameProperty.stringValue;

                    if (elementName.ToLower() == lowerCase)
                    {
                        ++namesTried;
                        result = string.Format("{0}{1}", baseName, namesTried);
                        lowerCase = result.ToLower();
                        nameIsUnique = false;
                        break;
                    }
                }
            }

            return result;
        }

        public static void RenameAction(SerializedProperty actionProperty, SerializedProperty actionMapProperty, string newName)
        {
            // Make sure name is unique.
            var actionsArrayProperty = actionMapProperty.FindPropertyRelative("m_Actions");
            var uniqueName = FindUniqueName(actionsArrayProperty, newName);

            // Update all bindings that refer to the action.
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            var oldName = nameProperty.stringValue;
            var bindingsProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
            for (var i = 0; i < bindingsProperty.arraySize; i++)
            {
                var element = bindingsProperty.GetArrayElementAtIndex(i);
                var actionNameProperty = element.FindPropertyRelative("m_Action");
                if (actionNameProperty.stringValue == oldName)
                {
                    actionNameProperty.stringValue = uniqueName;
                }
            }

            // Update name.
            nameProperty.stringValue = uniqueName;
        }

        public static void RenameActionMap(SerializedProperty actionMapProperty, string newName)
        {
            // Make sure name is unique in InputActionAsset.
            var assetObject = actionMapProperty.serializedObject;
            var mapsArrayProperty = assetObject.FindProperty("m_ActionMaps");
            var uniqueName = FindUniqueName(mapsArrayProperty, newName);

            // Assign to map.
            var nameProperty = actionMapProperty.FindPropertyRelative("m_Name");
            nameProperty.stringValue = uniqueName;
        }

        public static void RenameComposite(SerializedProperty compositeGroupProperty, string newName)
        {
            var nameProperty = compositeGroupProperty.FindPropertyRelative("m_Name");
            nameProperty.stringValue = newName;
        }

        public static void AddCompositeBinding(SerializedProperty actionProperty, SerializedProperty actionMapProperty, string compositeName, Type type)
        {
            var newProperty = AddBinding(actionProperty, actionMapProperty);
            newProperty.FindPropertyRelative("m_Name").stringValue = compositeName;
            newProperty.FindPropertyRelative("m_Path").stringValue = compositeName;
            newProperty.FindPropertyRelative("m_Flags").intValue = (int)InputBinding.Flags.Composite;

            var fields = type.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Skip fields that aren't InputControls.
                if (!typeof(InputControl).IsAssignableFrom(field.FieldType))
                    continue;

                newProperty = AddBinding(actionProperty, actionMapProperty);
                newProperty.FindPropertyRelative("m_Name").stringValue = field.Name;
                newProperty.FindPropertyRelative("m_Flags").intValue = (int)InputBinding.Flags.PartOfComposite;
            }
        }
    }
}
#endif // UNITY_EDITOR
