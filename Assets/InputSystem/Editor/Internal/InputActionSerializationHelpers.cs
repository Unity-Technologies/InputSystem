#if UNITY_EDITOR
using UnityEditor;

namespace ISX.Editor
{
    // Helpers for doctoring around in InputActions using SerializedProperties.
    internal static class InputActionSerializationHelpers
    {
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
            bindingsCountProperty.intValue = bindingsCount - 1;

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

            bindingsArrayProperty.InsertArrayElementAtIndex(bindingIndex);
            bindingsCountProperty.intValue = bindingsCount + 1;

            var newActionProperty = bindingsArrayProperty.GetArrayElementAtIndex(bindingIndex);
            newActionProperty.FindPropertyRelative("path").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("group").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("modifiers").stringValue = string.Empty;
            newActionProperty.FindPropertyRelative("flags").intValue = 0;

            if (actionSetProperty != null)
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
                var startIndexProperty = property.FindPropertyRelative("m_BindingsStartIndex");
                var startIndex = startIndexProperty.intValue;

                if (startIndex >= indexAfterWhichToAdjust)
                    startIndexProperty.intValue = startIndex + adjust;
            }
        }
    }
}
#endif // UNITY_EDITOR
