#if UNITY_EDITOR
using System;
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
                var bindingActionName = bindingArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative("action")
                    .stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    ++bindingCountForAction;
            }

            return bindingCountForAction;
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
                var bindingActionName = bindingProperty.FindPropertyRelative("action").stringValue;
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
            mapProperty.FindPropertyRelative("m_Actions").ClearArray();
            mapProperty.FindPropertyRelative("m_Bindings").ClearArray();
        }

        public static void DeleteActionMap(SerializedObject asset, int index)
        {
            var mapArrayProperty = asset.FindProperty("m_ActionMaps");
            mapArrayProperty.DeleteArrayElementAtIndex(index);
        }

        // Append a new action to the end of the set.
        public static void AddAction(SerializedProperty actionMap)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            var actionsCount = actionsArrayProperty.arraySize;
            var actionIndex = actionsCount;

            var actionName = FindUniqueName(actionsArrayProperty, "action");

            actionsArrayProperty.InsertArrayElementAtIndex(actionIndex);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex);
            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
        }

        public static void DeleteAction(SerializedProperty actionMap, int actionIndex)
        {
            var actionsArrayProperty = actionMap.FindPropertyRelative("m_Actions");
            actionsArrayProperty.DeleteArrayElementAtIndex(actionIndex);
        }

        // Equivalent to InputAction.AppendBinding().
        public static void AppendBinding(SerializedProperty actionProperty, SerializedProperty actionMapProperty = null)
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
                var bindingActionName = bindingProperty.FindPropertyRelative("action").stringValue;
                if (string.Compare(actionName, bindingActionName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    indexOfLastBindingForAction = i;
            }

            // Insert after last binding or at end of array.
            var bindingIndex = indexOfLastBindingForAction != -1 ? indexOfLastBindingForAction + 1 : bindingsCount;
            bindingsArrayProperty.InsertArrayElementAtIndex(bindingIndex);

            var newActionProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            newActionProperty.FindPropertyRelative("path").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("groups").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("interactions").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("flags").intValue = 0;
            newActionProperty.FindPropertyRelative("action").stringValue = actionName;

            ////FIXME: this likely leaves m_Bindings in the map for singleton actions unsync'd in some cases
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
                var bindingActionName = bindingProperty.FindPropertyRelative("action").stringValue;
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

        private static string FindUniqueName(SerializedProperty arrayProperty, string baseName)
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
    }
}
#endif // UNITY_EDITOR
