#if UNITY_EDITOR
using System;
using UnityEditor;

////TODO: this needs to also have an interactive pick button

namespace UnityEngine.Experimental.Input.Editor
{
    internal class BindingPropertiesPopup : EditorWindow
    {
        InputBindingPropertiesView m_BindingPropertyView;
        Action OnChange;

        public static void Show(Rect btnRect, ActionTreeViewItem treeViewLine, Action reload)
        {
            var w = CreateInstance<BindingPropertiesPopup>();
            w.OnChange = reload;
            w.SetProperty(treeViewLine);
            w.ShowPopup();
            w.ShowAsDropDown(btnRect, new Vector2(250, 350));
        }

        private void SetProperty(ActionTreeViewItem treeViewLine)
        {
            m_BindingPropertyView = new InputBindingPropertiesView(treeViewLine.elementProperty,
                change => OnChange(),
                new InputControlPickerState(), null);
        }

        private void OnGUI()
        {
            m_BindingPropertyView.OnGUI();
        }
    }
}
#endif // UNITY_EDITOR
