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
    }
}
#endif // UNITY_EDITOR
