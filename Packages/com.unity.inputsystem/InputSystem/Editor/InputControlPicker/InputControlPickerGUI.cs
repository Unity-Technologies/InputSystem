#if UNITY_EDITOR
namespace UnityEngine.Experimental.Input.Editor
{
    class InputControlPickerGUI : AdvancedDropdownGUI
    {
        internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            if (hasSearch && item is InputControlTreeViewItem)
            {
                name = (item as InputControlTreeViewItem).searchableName;
            }
            base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
        }
    }
}
#endif
