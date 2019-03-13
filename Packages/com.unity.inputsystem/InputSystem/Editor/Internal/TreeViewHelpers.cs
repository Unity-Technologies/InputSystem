#if UNITY_EDITOR
using System;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.Experimental.Input.Editor
{
    /// <summary>
    /// Extension methods for working with tree views.
    /// </summary>
    /// <seealso cref="TreeView"/>
    internal static class TreeViewHelpers
    {
        public static TItem TryFindItemInHierarchy<TItem>(this TreeViewItem item)
            where TItem : TreeViewItem
        {
            while (item != null)
            {
                if (item is TItem result)
                    return result;
                item = item.parent;
            }

            return null;
        }

        public static bool IsParentOf(this TreeViewItem parent, TreeViewItem child)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (child == null)
                throw new ArgumentNullException(nameof(child));

            do
            {
                child = child.parent;
            }
            while (child != null && child != parent);
            return child != null;
        }
    }
}
#endif // UNITY_EDITOR
