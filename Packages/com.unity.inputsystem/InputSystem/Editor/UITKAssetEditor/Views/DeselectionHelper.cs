using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class DeselectionHelper
    {
        /// <summary>
        /// Currently left as a constant to allow turning deselection on/off to be able to remove issues
        /// related to not having selections more quickly. Also it might be desirable to either support or
        /// not support deselection (E.g. pressing ESC while having a current selection).
        /// </summary>
        private const bool kAllowDeselection = false;

        private int m_SelectedIndex = -1;

        public bool Select(BaseVerticalCollectionView view, int selectedIndex)
        {
            if (!kAllowDeselection)
            {
                // This is a workaround to prevent deselection in the list or tree-view, e.g. pressing ESC key.
                // Note that we prevent this by reassigning selected index based on last selection, constrained
                // to available range of items. The motivation behind this part if that there is no built-in
                // support in UITK list or tree-view to prevent deselection via ESC key.
                // TODO: Remove this workaround and m_PrevSelectedIndex when support exists.
                if (selectedIndex < 0)
                {
                    m_SelectedIndex = Math.Min(m_SelectedIndex, view.itemsSource.Count - 1);
                    if (m_SelectedIndex >= 0)
                    {
                        view.selectedIndex = this.m_SelectedIndex;
                        return false;
                    }
                }

                // If its a valid selection store index to allow preventing future deselect
                if (selectedIndex >= 0)
                    this.m_SelectedIndex = selectedIndex;
            }

            return true;
        }

        public bool Select(BaseVerticalCollectionView view, IEnumerable<int> selectedIndices)
        {
            return Select(view, selectedIndices.FirstOrFallback(-1));
        }
    }
}
