#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.Experimental.Input.Layouts;

namespace UnityEngine.Experimental.Input.Editor
{
    // Helpers for doctoring around in InputActions using SerializedProperties.
    internal static class InputActionSerializationHelpers
    {
        public static int GetBindingCount(SerializedProperty bindingArrayProperty, string actionName)
        {
            Debug.Assert(bindingArrayProperty != null, "Binding array property is null");
            Debug.Assert(bindingArrayProperty.isArray, "Binding array property is not an array");

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
                nameof(index),
                $"Binding index {index} on action '{actionName}' with {bindingCountForAction} bindings is out of range");
        }

        public static void AddActionMap(SerializedObject asset)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var mapCount = mapArrayProperty.arraySize;
            var index = mapCount;
            var name = FindUniqueName(mapArrayProperty, "New action map");

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

            var actionName = FindUniqueName(actionsArrayProperty, "New action");

            actionsArrayProperty.InsertArrayElementAtIndex(actionIndex);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex);
            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
            actionProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            actionProperty.FindPropertyRelative("m_ExpectedControlLayout").stringValue = string.Empty;

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
        public static SerializedProperty AddBinding(SerializedProperty actionProperty,
            SerializedProperty actionMapProperty = null, string groups = "", string path = "", string name = "",
            string interactions = "", string processors = "",
            InputBinding.Flags flags = InputBinding.Flags.None)
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
            return AddBindingToBindingArray(bindingsArrayProperty, bindingIndex,
                actionName: actionName,
                groups: groups,
                path: path,
                name: name,
                interactions: interactions,
                processors: processors,
                flags: flags);
        }

        public static SerializedProperty AddBindingToBindingArray(SerializedProperty bindingsArrayProperty, int bindingIndex = -1,
            string actionName = "", string groups = "", string path = "", string name = "", string interactions = "", string processors = "",
            InputBinding.Flags flags = InputBinding.Flags.None)
        {
            Debug.Assert(bindingsArrayProperty != null);
            Debug.Assert(bindingsArrayProperty.isArray, "SerializedProperty is not an array of bindings");
            Debug.Assert(bindingIndex == -1 || (bindingIndex >= 0 && bindingIndex <= bindingsArrayProperty.arraySize));

            if (bindingIndex == -1)
                bindingIndex = bindingsArrayProperty.arraySize;

            bindingsArrayProperty.InsertArrayElementAtIndex(bindingIndex);

            var newBindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            newBindingProperty.FindPropertyRelative("m_Path").stringValue = path;
            newBindingProperty.FindPropertyRelative("m_Groups").stringValue = groups;
            newBindingProperty.FindPropertyRelative("m_Interactions").stringValue = interactions;
            newBindingProperty.FindPropertyRelative("m_Processors").stringValue = processors;
            newBindingProperty.FindPropertyRelative("m_Flags").intValue = (int)flags;
            newBindingProperty.FindPropertyRelative("m_Action").stringValue = actionName;
            newBindingProperty.FindPropertyRelative("m_Name").stringValue = name;

            ////FIXME: this likely leaves m_Bindings in the map for singleton actions unsync'd in some cases

            return newBindingProperty;
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

        public static void ChangeBinding(SerializedProperty bindingProperty, string path = null, string groups = null,
            string interactions = null, string processors = null)
        {
            // Path.
            if (!string.IsNullOrEmpty(path))
            {
                var pathProperty = bindingProperty.FindPropertyRelative("m_Path");
                pathProperty.stringValue = path;
            }

            // Groups.
            if (!string.IsNullOrEmpty(groups))
            {
                var groupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
                groupsProperty.stringValue = groups;
            }

            // Interactions.
            if (!string.IsNullOrEmpty(interactions))
            {
                var interactionsProperty = bindingProperty.FindPropertyRelative("m_Interactions");
                interactionsProperty.stringValue = interactions;
            }

            // Processors.
            if (!string.IsNullOrEmpty(processors))
            {
                var processorsProperty = bindingProperty.FindPropertyRelative("m_Processors");
                processorsProperty.stringValue = processors;
            }
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
                nameof(bindingIndex),
                $"Binding index {bindingIndex} on action {actionName} is out of range");
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
                        result = $"{baseName}{namesTried}";
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

        public static SerializedProperty AddCompositeBinding(SerializedProperty actionProperty, SerializedProperty actionMapProperty,
            string compositeName, Type compositeType = null, string groups = "", bool addPartBindings = true)
        {
            var newProperty = AddBinding(actionProperty, actionMapProperty);
            newProperty.FindPropertyRelative("m_Name").stringValue = ObjectNames.NicifyVariableName(compositeName);
            newProperty.FindPropertyRelative("m_Path").stringValue = compositeName;
            newProperty.FindPropertyRelative("m_Flags").intValue = (int)InputBinding.Flags.Composite;

            if (addPartBindings)
            {
                var fields = compositeType.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    // Skip fields that aren't marked with [InputControl] attribute.
                    if (field.GetCustomAttribute<InputControlAttribute>(false) == null)
                        continue;

                    var partProperty = AddBinding(actionProperty, actionMapProperty, groups);
                    partProperty.FindPropertyRelative("m_Name").stringValue = ObjectNames.NicifyVariableName(field.Name);
                    partProperty.FindPropertyRelative("m_Flags").intValue = (int)InputBinding.Flags.PartOfComposite;
                }
            }

            return newProperty;
        }

        public static SerializedProperty ChangeCompositeType(SerializedProperty bindingsArrayProperty, int bindingIndex,
            string compositeName, Type compositeType, string actionName)
        {
            Debug.Assert(bindingsArrayProperty != null);
            Debug.Assert(bindingsArrayProperty.isArray, "SerializedProperty is not an array of bindings");
            Debug.Assert(bindingIndex >= 0 && bindingIndex < bindingsArrayProperty.arraySize);

            var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            Debug.Assert(((InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue &
                InputBinding.Flags.Composite) != 0);

            // Only change the name if the current name is still the default one.
            var pathProperty = bindingProperty.FindPropertyRelative("m_Path");
            var nameProperty = bindingProperty.FindPropertyRelative("m_Name");
            if (nameProperty.stringValue == ObjectNames.NicifyVariableName(pathProperty.stringValue))
                nameProperty.stringValue = ObjectNames.NicifyVariableName(compositeName);
            pathProperty.stringValue = compositeName;

            // Adjust part bindings if we have information on the registered composite. If we don't have
            // a type, we don't know about the parts. In that case, leave part bindings untouched.
            if (compositeType != null)
            {
                // Repurpose existing part bindings for the new composite or add any part bindings that
                // we're missing.
                var fields = compositeType.GetFields(BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
                var partIndex = 0;
                var partBindingsStartIndex = bindingIndex + 1;
                foreach (var field in fields)
                {
                    // Skip fields that aren't marked with [InputControl] attribute.
                    if (field.GetCustomAttribute<InputControlAttribute>(false) == null)
                        continue;

                    // See if we can reuse an existing part binding.
                    SerializedProperty partProperty = null;
                    if (partBindingsStartIndex + partIndex < bindingsArrayProperty.arraySize)
                    {
                        ////REVIEW: this should probably look up part bindings by name rather than going sequentially
                        var element = bindingsArrayProperty.GetArrayElementAtIndex(partBindingsStartIndex + partIndex);
                        if (((InputBinding.Flags)element.FindPropertyRelative("m_Flags").intValue & InputBinding.Flags.PartOfComposite) != 0)
                            partProperty = element;
                    }

                    // If not, insert a new binding.
                    if (partProperty == null)
                    {
                        partProperty = AddBindingToBindingArray(bindingsArrayProperty, partBindingsStartIndex + partIndex,
                            flags: InputBinding.Flags.PartOfComposite);
                    }

                    // Initialize.
                    partProperty.FindPropertyRelative("m_Name").stringValue = ObjectNames.NicifyVariableName(field.Name);
                    partProperty.FindPropertyRelative("m_Action").stringValue = actionName;
                    ++partIndex;
                }

                ////REVIEW: when we allow adding the same part multiple times, we may want to do something smarter here
                // Delete extraneous part bindings.
                while (partBindingsStartIndex + partIndex < bindingsArrayProperty.arraySize)
                {
                    var element = bindingsArrayProperty.GetArrayElementAtIndex(partBindingsStartIndex + partIndex);
                    if (((InputBinding.Flags)element.FindPropertyRelative("m_Flags").intValue & InputBinding.Flags.PartOfComposite) == 0)
                        break;

                    bindingsArrayProperty.DeleteArrayElementAtIndex(partBindingsStartIndex + partIndex);
                    // No incrementing of partIndex.
                }
            }

            return bindingProperty;
        }
    }
}
#endif // UNITY_EDITOR
