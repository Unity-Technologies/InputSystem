#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Property drawer for <see cref="InputActionMap"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionMap))]
    internal class InputActionMapDrawer : InputActionDrawerBase
    {
        protected override TreeViewItem BuildTree(SerializedProperty property)
        {
            return InputActionTreeView.BuildWithJustActionsAndBindingsFromMap(property);
        }

        protected override string GetSuffixToRemoveFromPropertyDisplayName()
        {
            return " Action Map";
        }

        protected override bool IsPropertyAClone(SerializedProperty property)
        {
            // When a new item is added to a collection through the inspector, the default behaviour is
            // to create a clone of the previous item. Here we look at all InputActionMaps that appear before
            // the current one and compare their Ids to determine if we have a clone. We don't look past
            // the current item because Unity will be calling this property drawer for each input action map
            // in the collection in turn. If the user just added a new input action map, and it's a clone, as
            // we work our way down the list, we'd end up thinking that an existing input action map was a clone
            // of the newly added one, instead of the other way around. If we do have a clone, we need to
            // clear out some properties of the InputActionMap (id, name, input actions, and bindings) and
            // recreate the tree view.

            if (property?.GetParentProperty() == null || property.GetParentProperty().isArray == false)
                return false;

            var array = property.GetArrayPropertyFromElement();
            var index = property.GetIndexOfArrayElement();

            for (var i = 0; i < index; i++)
            {
                if (property.FindPropertyRelative(nameof(InputActionMap.m_Id))?.stringValue ==
                    array.GetArrayElementAtIndex(i)?.FindPropertyRelative(nameof(InputActionMap.m_Id))?.stringValue)
                    return true;
            }

            return false;
        }

        protected override void ResetProperty(SerializedProperty property)
        {
            if (property == null) return;

            property.SetStringValue(nameof(InputActionMap.m_Id), "");
            property.SetStringValue(nameof(InputActionMap.m_Name), "Input Action Map");
            property.FindPropertyRelative(nameof(InputActionMap.m_Actions))?.ClearArray();
            property.FindPropertyRelative(nameof(InputActionMap.m_Bindings))?.ClearArray();
            property.serializedObject?.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif // UNITY_EDITOR
