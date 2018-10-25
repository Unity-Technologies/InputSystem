#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    class BindingPropertiesPopup : EditorWindow
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

        void SetProperty(ActionTreeViewItem treeViewLine)
        {
            m_BindingPropertyView = treeViewLine.GetPropertiesView(OnChange, new TreeViewState());
        }

        void OnGUI()
        {
            m_BindingPropertyView.OnGUI();
        }
    }
}
#endif // UNITY_EDITOR
