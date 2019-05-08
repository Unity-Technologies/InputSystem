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
    }
}
#endif // UNITY_EDITOR
