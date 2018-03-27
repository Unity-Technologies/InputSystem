#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.Experimental.Input.Editor
{
    // Helpers for doctoring around in InputActions using SerializedProperties.
    internal static class InputActionSerializationHelpers
    {
        public static void AddActionSet(SerializedObject asset)
        {
            var setArrayProperty = asset.FindProperty("m_ActionSets");
            var setCount = setArrayProperty.arraySize;
            var index = setCount;
            var name = FindUniqueName(setArrayProperty, "default");

            setArrayProperty.InsertArrayElementAtIndex(index);
            var setProperty = setArrayProperty.GetArrayElementAtIndex(index);

            setProperty.FindPropertyRelative("m_Name").stringValue = name;
            setProperty.FindPropertyRelative("m_Actions").ClearArray();
            setProperty.FindPropertyRelative("m_Bindings").ClearArray();
        }

        public static void DeleteActionSet(SerializedObject asset, int index)
        {
            var setArrayProperty = asset.FindProperty("m_ActionSets");
            setArrayProperty.DeleteArrayElementAtIndex(index);
        }

        // Append a new action to the end of the set.
        public static void AddAction(SerializedProperty actionSet)
        {
            var actionsArrayProperty = actionSet.FindPropertyRelative("m_Actions");
            var actionsCount = actionsArrayProperty.arraySize;
            var actionIndex = actionsCount;

            var actionName = FindUniqueName(actionsArrayProperty, "action");

            actionsArrayProperty.InsertArrayElementAtIndex(actionIndex);
            var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(actionIndex);
            actionProperty.FindPropertyRelative("m_Name").stringValue = actionName;
            actionProperty.FindPropertyRelative("m_BindingsCount").intValue = 0;
            actionProperty.FindPropertyRelative("m_BindingsStartIndex").intValue = 0;
        }

        public static void DeleteAction(SerializedProperty actionSet, int actionIndex)
        {
            var actionsArrayProperty = actionSet.FindPropertyRelative("m_Actions");
            actionsArrayProperty.DeleteArrayElementAtIndex(actionIndex);
        }

        public static void RemoveBinding(SerializedProperty actionProperty, int bindingIndex, SerializedProperty actionSetProperty = null)
        {
            var bindingsArrayProperty = actionSetProperty != null
                ? actionSetProperty.FindPropertyRelative("m_Bindings")
                : actionProperty.FindPropertyRelative("m_Bindings");
            var bindingsCountProperty = actionProperty.FindPropertyRelative("m_BindingsCount");
            var bindingsStartIndexProperty = actionProperty.FindPropertyRelative("m_BindingsStartIndex");

            var bindingsStartIndex = bindingsStartIndexProperty.intValue;
            var bindingsCount = bindingsCountProperty.intValue;
            bindingIndex += bindingsStartIndex;

            bindingsArrayProperty.DeleteArrayElementAtIndex(bindingIndex);

            // Update count.
            --bindingsCount;
            bindingsCountProperty.intValue = bindingsCount;
            if (bindingsCount == 0)
            {
                // We've removed the last binding on this action. Reset start index just
                // to be safe.
                bindingsStartIndexProperty.intValue = 0;
            }

            if (actionSetProperty != null)
            {
                // Action is part of a set. Need to adjust the binding array such that
                // other actions are updated accordingly.
                AdjustBindingStartOffsets(actionSetProperty, bindingIndex, -1);
            }
        }

        // Equivalent to InputAction.AddBinding().
        public static void AppendBinding(SerializedProperty actionProperty, SerializedProperty actionSetProperty = null)
        {
            var bindingsArrayProperty = actionSetProperty != null
                ? actionSetProperty.FindPropertyRelative("m_Bindings")
                : actionProperty.FindPropertyRelative("m_Bindings");
            var bindingsCountProperty = actionProperty.FindPropertyRelative("m_BindingsCount");
            var bindingsStartIndexProperty = actionProperty.FindPropertyRelative("m_BindingsStartIndex");

            var bindingsStartIndex = bindingsStartIndexProperty.intValue;
            var bindingsCount = bindingsCountProperty.intValue;
            var bindingIndex = bindingsStartIndex + bindingsCount;

            // If this is the first binding, start appending at end of bindings array.
            var appendToArray = bindingsCount == 0;
            if (appendToArray)
            {
                bindingsStartIndex = bindingsArrayProperty.arraySize;
                bindingsStartIndexProperty.intValue = bindingsStartIndex;
                bindingIndex = bindingsStartIndex;
            }

            bindingsArrayProperty.InsertArrayElementAtIndex(bindingIndex);
            bindingsCountProperty.intValue = bindingsCount + 1;

            var newActionProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            newActionProperty.FindPropertyRelative("path").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("group").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("modifiers").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("flags").intValue = 0;

            if (actionSetProperty != null && !appendToArray) // Nothing to adjust if we appended.
            {
                // Adjust binding start indices of actions coming after us.
                AdjustBindingStartOffsets(actionSetProperty, bindingIndex, 1);
            }
        }

        private static void AdjustBindingStartOffsets(SerializedProperty actionSetProperty, int indexAfterWhichToAdjust, int adjust)
        {
            var actionsArray = actionSetProperty.FindPropertyRelative("m_Actions");
            var actionsCount = actionsArray.arraySize;

            for (var i = 0; i < actionsCount; ++i)
            {
                var property = actionsArray.GetArrayElementAtIndex(i);

                // Skip actions with no bindings.
                var bindingsCount = property.FindPropertyRelative("m_BindingsCount").intValue;
                if (bindingsCount == 0)
                    continue;

                // Adjust start index.
                var startIndexProperty = property.FindPropertyRelative("m_BindingsStartIndex");
                var startIndex = startIndexProperty.intValue;

                if (startIndex >= indexAfterWhichToAdjust)
                    startIndexProperty.intValue = startIndex + adjust;
            }
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
