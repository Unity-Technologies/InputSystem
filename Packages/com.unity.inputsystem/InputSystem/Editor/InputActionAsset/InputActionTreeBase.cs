using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    abstract class InputActionTreeBase : TreeView
    {
        ////TODO: move to a better place
        public static string SharedResourcesPath = "Packages/com.unity.inputsystem/InputSystem/Editor/InputActionAsset/Resources/";
        public static string ResourcesPath
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                    return SharedResourcesPath + "pro/";
                return SharedResourcesPath + "personal/";
            }
        }
        
        protected InputActionTreeBase(TreeViewState state)
            : base(state) { }
        
        public ActionTreeViewItem GetSelectedRow()
        {
            if (!HasSelection())
                return null;

            return (ActionTreeViewItem)FindItem(GetSelection().First(), rootItem);
        }

        public IEnumerable<ActionTreeViewItem> GetSelectedRows()
        {
            return FindRows(GetSelection()).Cast<ActionTreeViewItem>();
        }

        public bool CanRenameCurrentSelection()
        {
            var selection = GetSelectedRows();
            if (selection.Count() != 1)
                return false;
            return CanRename(selection.Single());
        }

        public ActionTreeItem GetSelectedAction()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionTreeItem) && item != null && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionTreeItem;
        }

        public ActionMapTreeItem GetSelectedActionMap()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            while (!(item is ActionMapTreeItem) && item != null && item.parent != null)
            {
                item = item.parent;
            }

            return item as ActionMapTreeItem;
        }

        public SerializedProperty GetSelectedProperty()
        {
            if (!HasSelection())
                return null;

            var item = FindItem(GetSelection().First(), rootItem);

            if (item == null)
                return null;

            return ((ActionTreeViewItem)item).elementProperty;
        }
    }
}
