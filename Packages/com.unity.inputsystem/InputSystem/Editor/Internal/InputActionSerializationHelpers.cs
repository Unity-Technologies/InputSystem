#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////TODO: resolving bindings to actions needs to take "{id}" form into account

namespace UnityEngine.InputSystem.Editor
{
    // Helpers for doctoring around in InputActions using SerializedProperties.
    internal static class InputActionSerializationHelpers
    {
        public static string GetName(SerializedProperty element)
        {
            using (var nameProperty = element.FindPropertyRelative("m_Name"))
            {
                Debug.Assert(nameProperty != null, $"Cannot find m_Name property in {element.propertyPath}");
                return nameProperty.stringValue;
            }
        }

        public static Guid GetId(SerializedProperty element)
        {
            using (var idProperty = element.FindPropertyRelative("m_Id"))
            {
                Debug.Assert(idProperty != null, $"Cannot find m_Id property in {element.propertyPath}");
                return new Guid(idProperty.stringValue);
            }
        }

        public static int GetIndex(SerializedProperty arrayProperty, Guid id)
        {
            Debug.Assert(arrayProperty.isArray, $"Property {arrayProperty.propertyPath} is not an array");
            for (var i = 0; i < arrayProperty.arraySize; ++i)
            {
                using (var element = arrayProperty.GetArrayElementAtIndex(i))
                    if (GetId(element) == id)
                        return i;
            }
            return -1;
        }

        public static int GetIndex(SerializedProperty arrayProperty, SerializedProperty arrayElement)
        {
            return GetIndex(arrayProperty, GetId(arrayElement));
        }

        public static int GetIndex(SerializedProperty arrayElement)
        {
            var arrayProperty = arrayElement.GetArrayPropertyFromElement();
            return GetIndex(arrayProperty, arrayElement);
        }

        /// <summary>
        /// Starting with the given binding, find the composite that the binding belongs to. The given binding
        /// must either be the composite or be part of a composite.
        /// </summary>
        public static int GetCompositeStartIndex(SerializedProperty bindingArrayProperty, int bindingIndex)
        {
            for (var i = bindingIndex; i >= 0; --i)
            {
                var bindingProperty = bindingArrayProperty.GetArrayElementAtIndex(i);
                var bindingFlags = (InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue;
                if ((bindingFlags & InputBinding.Flags.Composite) != 0)
                    return i;
                Debug.Assert((bindingFlags & InputBinding.Flags.PartOfComposite) != 0,
                    "Binding is neither a composite nor part of a composite");
            }
            return -1;
        }

        public static int GetCompositePartCount(SerializedProperty bindingArrayProperty, int bindingIndex)
        {
            var compositeStartIndex = GetCompositeStartIndex(bindingArrayProperty, bindingIndex);
            if (compositeStartIndex == -1)
                return 0;

            var numParts = 0;
            for (var i = compositeStartIndex + 1; i < bindingArrayProperty.arraySize; ++i, ++numParts)
            {
                var bindingProperty = bindingArrayProperty.GetArrayElementAtIndex(i);
                var bindingFlags = (InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue;
                if ((bindingFlags & InputBinding.Flags.PartOfComposite) == 0)
                    break;
            }

            return numParts;
        }

        public static int ConvertBindingIndexOnActionToBindingIndexInArray(SerializedProperty bindingArrayProperty, string actionName,
            int bindingIndexOnAction)
        {
            var bindingCount = bindingArrayProperty.arraySize;
            var indexOnAction = -1;
            var indexInArray = 0;
            for (; indexInArray < bindingCount; ++indexInArray)
            {
                var bindingActionName = bindingArrayProperty.GetArrayElementAtIndex(indexInArray).FindPropertyRelative("m_Action")
                    .stringValue;
                if (actionName.Equals(bindingActionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    ++indexOnAction;
                    if (indexOnAction == bindingIndexOnAction)
                        return indexInArray;
                }
            }
            return indexInArray;
        }

        public static SerializedProperty AddElement(SerializedProperty arrayProperty, string name, int index = -1)
        {
            var uniqueName = FindUniqueName(arrayProperty, name);
            if (index < 0)
                index = arrayProperty.arraySize;

            arrayProperty.InsertArrayElementAtIndex(index);
            var elementProperty = arrayProperty.GetArrayElementAtIndex(index);
            elementProperty.ResetValuesToDefault();

            elementProperty.FindPropertyRelative("m_Name").stringValue = uniqueName;
            elementProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            return elementProperty;
        }

        public static SerializedProperty AddActionMap(SerializedObject asset, int index = -1)
        {
            if (!(asset.targetObject is InputActionAsset))
                throw new InvalidOperationException(
                    $"Can only add action maps to InputActionAsset objects (actual object is {asset.targetObject}");

            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var name = FindUniqueName(mapArrayProperty, "New action map");
            if (index < 0)
                index = mapArrayProperty.arraySize;

            mapArrayProperty.InsertArrayElementAtIndex(index);
            var mapProperty = mapArrayProperty.GetArrayElementAtIndex(index);

            mapProperty.FindPropertyRelative("m_Name").stringValue = name;
            mapProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            mapProperty.FindPropertyRelative("m_Actions").ClearArray();
            mapProperty.FindPropertyRelative("m_Bindings").ClearArray();

            return mapProperty;
        }

        public static void DeleteActionMap(SerializedObject asset, Guid id)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var mapIndex = GetIndex(mapArrayProperty, id);
            if (mapIndex == -1)
                throw new ArgumentException($"No map with id {id} in {asset}", nameof(id));
            mapArrayProperty.DeleteArrayElementAtIndex(mapIndex);
        }

        // Append a new action to the end of the set.
        public static SerializedProperty AddAction(SerializedProperty actionMap, int index = -1)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            if (index < 0)
                index = actionsArrayProperty.arraySize;

            var actionName = FindUniqueName(actionsArrayProperty, "New action");

            actionsArrayProperty.InsertArrayElementAtIndex(index);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(index);

            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
            actionProperty.FindPropertyRelative("m_Type").intValue = (int)InputActionType.Button;  // Default to creating button actions.
            actionProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();
            actionProperty.FindPropertyRelative("m_ExpectedControlType").stringValue = "Button";
            actionProperty.FindPropertyRelative("m_Flags").intValue = 0;
            actionProperty.FindPropertyRelative("m_Interactions").stringValue = "";
            actionProperty.FindPropertyRelative("m_Processors").stringValue = "";

            return actionProperty;
        }

        public static void DeleteActionAndBindings(SerializedProperty actionMap, Guid actionId)
        {
            using (var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions"))
            using (var bindingsArrayProperty = actionMap.FindPropertyRelative("m_Bindings"))
            {
                // Find index of action.
                var actionIndex = GetIndex(actionsArrayProperty, actionId);
                if (actionIndex == -1)
                    throw new ArgumentException($"No action with ID {actionId} in {actionMap.propertyPath}",
                        nameof(actionId));

                using (var actionsProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex))
                {
                    var actionName = GetName(actionsProperty);
                    var actionIdString = actionId.ToString();

                    // Delete all bindings that refer to the action by ID or name.
                    for (var i = 0; i < bindingsArrayProperty.arraySize; ++i)
                    {
                        using (var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i))
                        using (var bindingActionProperty = bindingProperty.FindPropertyRelative("m_Action"))
                        {
                            var targetAction = bindingActionProperty.stringValue;
                            if (targetAction.Equals(actionName, StringComparison.InvariantCultureIgnoreCase) ||
                                targetAction == actionIdString)
                            {
                                bindingsArrayProperty.DeleteArrayElementAtIndex(i);
                                --i;
                            }
                        }
                    }
                }

                actionsArrayProperty.DeleteArrayElementAtIndex(actionIndex);
            }
        }

        // Equivalent to InputAction.AddBinding().
        public static SerializedProperty AddBinding(SerializedProperty actionProperty,
            SerializedProperty actionMapProperty = null, SerializedProperty afterBinding = null,
            string groups = "", string path = "", string name = "",
            string interactions = "", string processors = "",
            InputBinding.Flags flags = InputBinding.Flags.None)
        {
            var bindingsArrayProperty = actionMapProperty != null
                ? actionMapProperty.FindPropertyRelative("m_Bindings")
                : actionProperty.FindPropertyRelative("m_SingletonActionBindings");
            var bindingsCount = bindingsArrayProperty.arraySize;
            var actionName = actionProperty.FindPropertyRelative("m_Name").stringValue;

            int bindingIndex;
            if (afterBinding != null)
            {
                // If we're supposed to put the binding right after another binding, find the
                // binding's index. Also, if it's a composite, skip past all its parts.
                bindingIndex = GetIndex(bindingsArrayProperty, afterBinding);
                if (IsCompositeBinding(afterBinding))
                    bindingIndex += GetCompositePartCount(bindingsArrayProperty, bindingIndex);
                ++bindingIndex; // Put it *after* the binding.
            }
            else
            {
                // Find the index of the last binding for the action in the array.
                var indexOfLastBindingForAction = -1;
                for (var i = 0; i < bindingsCount; ++i)
                {
                    var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);
                    var bindingActionName = bindingProperty.FindPropertyRelative("m_Action").stringValue;
                    if (actionName.Equals(bindingActionName, StringComparison.InvariantCultureIgnoreCase))
                        indexOfLastBindingForAction = i;
                }

                // Insert after last binding or at end of array.
                bindingIndex = indexOfLastBindingForAction != -1 ? indexOfLastBindingForAction + 1 : bindingsCount;
            }

            ////TODO: bind using {id} rather than action name
            return AddBindingToBindingArray(bindingsArrayProperty,
                bindingIndex: bindingIndex,
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
            newBindingProperty.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString();

            ////FIXME: this likely leaves m_Bindings in the map for singleton actions unsync'd in some cases

            return newBindingProperty;
        }

        public static void ChangeBinding(SerializedProperty bindingProperty, string path = null, string groups = null,
            string interactions = null, string processors = null, string action = null)
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

            // Action.
            if (!string.IsNullOrEmpty(action))
            {
                var actionProperty = bindingProperty.FindPropertyRelative("m_Action");
                actionProperty.stringValue = action;
            }
        }

        public static void DeleteBinding(SerializedProperty bindingArrayProperty, Guid id)
        {
            // If it's a composite, delete all its parts first.
            var bindingIndex = GetIndex(bindingArrayProperty, id);
            var bindingProperty = bindingArrayProperty.GetArrayElementAtIndex(bindingIndex);
            var bindingFlags = (InputBinding.Flags)bindingProperty.FindPropertyRelative("m_Flags").intValue;
            if ((bindingFlags & InputBinding.Flags.Composite) != 0)
            {
                for (var partIndex = bindingIndex + 1; partIndex < bindingArrayProperty.arraySize;)
                {
                    var part = bindingArrayProperty.GetArrayElementAtIndex(partIndex);
                    var flags = (InputBinding.Flags)part.FindPropertyRelative("m_Flags").intValue;
                    if ((flags & InputBinding.Flags.PartOfComposite) == 0)
                        break;
                    bindingArrayProperty.DeleteArrayElementAtIndex(partIndex);
                }
            }

            bindingArrayProperty.DeleteArrayElementAtIndex(bindingIndex);
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

        public static void AssignUniqueID(SerializedProperty property)
        {
            var idProperty = property.FindPropertyRelative("m_Id");
            idProperty.stringValue = Guid.NewGuid().ToString();
        }

        public static void EnsureUniqueName(SerializedProperty arrayElement)
        {
            var arrayProperty = arrayElement.GetArrayPropertyFromElement();
            var arrayIndexOfElement = arrayElement.GetIndexOfArrayElement();
            var nameProperty = arrayElement.FindPropertyRelative("m_Name");
            var baseName = nameProperty.stringValue;
            nameProperty.stringValue = FindUniqueName(arrayProperty, baseName, ignoreIndex: arrayIndexOfElement);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "False positive (possibly caused by lambda expression?).")]
        public static string FindUniqueName(SerializedProperty arrayProperty, string baseName, int ignoreIndex = -1)
        {
            return StringHelpers.MakeUniqueName(baseName,
                Enumerable.Range(0, arrayProperty.arraySize),
                index =>
                {
                    if (index == ignoreIndex)
                        return string.Empty;
                    var elementProperty = arrayProperty.GetArrayElementAtIndex(index);
                    var nameProperty = elementProperty.FindPropertyRelative("m_Name");
                    if (nameProperty == null)
                        throw new ArgumentException($"Cannot find m_Name property in elements of array",
                            nameof(arrayProperty));
                    return nameProperty.stringValue;
                });
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
                if (actionNameProperty.stringValue.Equals(oldName, StringComparison.InvariantCultureIgnoreCase))
                    actionNameProperty.stringValue = uniqueName;
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

                    var partProperty = AddBinding(actionProperty, actionMapProperty, groups: groups);
                    partProperty.FindPropertyRelative("m_Name").stringValue = field.Name;
                    partProperty.FindPropertyRelative("m_Flags").intValue = (int)InputBinding.Flags.PartOfComposite;
                }
            }

            return newProperty;
        }

        public static bool IsCompositeBinding(SerializedProperty bindingProperty)
        {
            using (var flagsProperty = bindingProperty.FindPropertyRelative("m_Flags"))
            {
                var flags = (InputBinding.Flags)flagsProperty.intValue;
                return (flags & InputBinding.Flags.Composite) != 0;
            }
        }

        public static SerializedProperty ChangeCompositeBindingType(SerializedProperty bindingProperty,
            NameAndParameters nameAndParameters)
        {
            var bindingsArrayProperty = bindingProperty.GetArrayPropertyFromElement();
            Debug.Assert(bindingsArrayProperty != null, "SerializedProperty is not an array of bindings");
            var bindingIndex = bindingProperty.GetIndexOfArrayElement();

            Debug.Assert(IsCompositeBinding(bindingProperty),
                $"Binding {bindingProperty.propertyPath} is not a composite");

            // If the composite still has the default name, change it to the default
            // one for the new composite type.
            var pathProperty = bindingProperty.FindPropertyRelative("m_Path");
            var nameProperty = bindingProperty.FindPropertyRelative("m_Name");
            if (nameProperty.stringValue ==
                ObjectNames.NicifyVariableName(NameAndParameters.Parse(pathProperty.stringValue).name))
                nameProperty.stringValue = ObjectNames.NicifyVariableName(nameAndParameters.name);

            pathProperty.stringValue = nameAndParameters.ToString();

            // Adjust part bindings if we have information on the registered composite. If we don't have
            // a type, we don't know about the parts. In that case, leave part bindings untouched.
            var compositeType = InputBindingComposite.s_Composites.LookupTypeRegistration(nameAndParameters.name);
            if (compositeType != null)
            {
                var actionName = bindingProperty.FindPropertyRelative("m_Action").stringValue;

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

        public static void ReplaceBindingGroup(SerializedObject asset, string oldBindingGroup, string newBindingGroup, bool deleteOrphanedBindings = false)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            var mapCount = mapArrayProperty.arraySize;

            for (var k = 0; k < mapCount; ++k)
            {
                var actionMapProperty = mapArrayProperty.GetArrayElementAtIndex(k);
                var bindingsArrayProperty = actionMapProperty.FindPropertyRelative("m_Bindings");
                var bindingsCount = bindingsArrayProperty.arraySize;

                for (var i = 0; i < bindingsCount; ++i)
                {
                    var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);
                    var groupsProperty = bindingProperty.FindPropertyRelative("m_Groups");
                    var groups = groupsProperty.stringValue;

                    // Ignore bindings not belonging to any control scheme.
                    if (string.IsNullOrEmpty(groups))
                        continue;

                    var groupsArray = groups.Split(InputBinding.Separator);
                    var numGroups = groupsArray.LengthSafe();
                    var didRename = false;
                    for (var n = 0; n < numGroups; ++n)
                    {
                        if (string.Compare(groupsArray[n], oldBindingGroup, StringComparison.InvariantCultureIgnoreCase) != 0)
                            continue;
                        if (string.IsNullOrEmpty(newBindingGroup))
                        {
                            ArrayHelpers.EraseAt(ref groupsArray, n);
                            --n;
                            --numGroups;
                        }
                        else
                            groupsArray[n] = newBindingGroup;
                        didRename = true;
                    }
                    if (!didRename)
                        continue;

                    if (groupsArray != null)
                        groupsProperty.stringValue = string.Join(InputBinding.kSeparatorString, groupsArray);
                    else
                    {
                        if (deleteOrphanedBindings)
                        {
                            // Binding no long belongs to any binding group. Delete it.
                            bindingsArrayProperty.DeleteArrayElementAtIndex(i);
                            --i;
                            --bindingsCount;
                        }
                        else
                        {
                            groupsProperty.stringValue = string.Empty;
                        }
                    }
                }
            }
        }

        public static void RemoveUnusedBindingGroups(SerializedProperty binding, ReadOnlyArray<InputControlScheme> controlSchemes)
        {
            var groupsProperty = binding.FindPropertyRelative(nameof(InputBinding.m_Groups));
            groupsProperty.stringValue = string.Join(InputBinding.kSeparatorString,
                groupsProperty.stringValue
                    .Split(InputBinding.Separator)
                    .Where(g => controlSchemes.Any(c => c.bindingGroup.Equals(g, StringComparison.InvariantCultureIgnoreCase))));
        }
    }
}
#endif // UNITY_EDITOR
