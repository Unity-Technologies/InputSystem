#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Property drawer for <see cref="InputAction"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputAction))]
    internal class InputActionDrawer : InputActionDrawerBase
    {
        protected override TreeViewItem BuildTree(SerializedProperty property)
        {
            return InputActionTreeView.BuildWithJustBindingsFromAction(property);
        }

        protected override string GetSuffixToRemoveFromPropertyDisplayName()
        {
            return " Action";
        }

        protected override bool IsPropertyAClone(SerializedProperty property)
        {
            // When a new item is added to a collection through the inspector, the default behaviour is
            // to create a clone of the previous item. Here we look at all InputActions that appear before
            // the current one and compare their Ids to determine if we have a clone. We don't look past
            // the current item because Unity will be calling this property drawer for each input action
            // in the collection in turn. If the user just added a new input action, and it's a clone, as
            // we work our way down the list, we'd end up thinking that an existing input action was a clone
            // of the newly added one, instead of the other way around. If we do have a clone, we need to
            // clear out some properties of the InputAction (id, name, and singleton action bindings) and
            // recreate the tree view.

            if (property?.GetParentProperty() == null || property.GetParentProperty().isArray == false)
                return false;

            var array = property.GetArrayPropertyFromElement();
            var index = property.GetIndexOfArrayElement();

            for (var i = 0; i < index; i++)
            {
                if (property.FindPropertyRelative(nameof(InputAction.m_Id))?.stringValue ==
                    array.GetArrayElementAtIndex(i)?.FindPropertyRelative(nameof(InputAction.m_Id))?.stringValue)
                    return true;
            }

            return false;
        }

        protected override void ResetProperty(SerializedProperty property)
        {
            if (property == null) return;

            property.SetStringValue(nameof(InputAction.m_Id), "");
            property.SetStringValue(nameof(InputAction.m_Name), "Input Action");
            property.FindPropertyRelative(nameof(InputAction.m_SingletonActionBindings))?.ClearArray();
            property.serializedObject?.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif // UNITY_EDITOR
