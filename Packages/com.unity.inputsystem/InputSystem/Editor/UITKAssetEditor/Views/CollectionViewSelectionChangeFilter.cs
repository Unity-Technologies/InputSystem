#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A helper class that provides a workaround to prevent deselection in a UI-Toolkit list or tree-view,
    /// e.g. when the user is pressing the ESC key while the view has focus.
    /// </summary>
    /// <remarks>
    ///The workaround is based on reassigning the selected index based on last selection, constrained
    /// to the available range items. The motivation behind this workaround is that there is no built-in support
    /// in UI-Toolkit list or tree-view to prevent deselection via ESC key.
    ///
    /// This workaround should be removed if this changes in the future and the functionality is provided by
    /// the UI framework.
    /// </remarks>>
    internal class CollectionViewSelectionChangeFilter
    {
        /// <summary>
        /// Currently left as a constant to allow turning deselection on/off to be able to remove issues
        /// related to not having selections more quickly. Also it might be desirable to either support or
        /// not support deselection (E.g. pressing ESC while having a current selection).
        ///
        /// Could be removed if there is no use-case for quickly switching between modes during development.
        /// </summary>
        private const bool kAllowDeselection = false;

        private readonly BaseVerticalCollectionView m_View_;
        private List<int> m_SelectedIndices_;

        /// <summary>
        /// Event
        /// </summary>
        public event Action<IEnumerable<int>> selectedIndicesChanged;

        public CollectionViewSelectionChangeFilter(BaseVerticalCollectionView view)
        {
            m_SelectedIndices_ = new List<int>();

            m_View_ = view;
            if (kAllowDeselection)
                m_View_.selectedIndicesChanged += OnSelectedIndicesChanged;
            else
                m_View_.selectedIndicesChanged += FilterSelectedIndicesChanged;
        }

        private void FilterSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            // Convert IEnumerable to a list to allow for multiple-iteration
            var currentlySelectedIndices = selectedIndices.ToList();

            // If the selection change is a deselection (transition from having a selection to having none)
            if (currentlySelectedIndices.Count == 0)
            {
                if (m_SelectedIndices_.Count > 0)
                {
                    // Remove any stored selection indices that are no longer within valid range
                    var count = m_View_.itemsSource.Count;
                    for (var i = m_SelectedIndices_.Count - 1; i >= 0; --i)
                    {
                        if (m_SelectedIndices_[i] >= count)
                            m_SelectedIndices_.RemoveAt(i);
                    }

                    // Restore selection based on last known selection and return immediately since
                    // assignment will retrigger selection change.
                    m_View_.SetSelection(m_SelectedIndices_);
                    return;
                }
            }
            else
            {
                // Store indices to allow preventing future deselect
                m_SelectedIndices_ = currentlySelectedIndices;
            }

            OnSelectedIndicesChanged(this.m_SelectedIndices_);
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            var selectedIndicesChanged = this.selectedIndicesChanged;
            selectedIndicesChanged?.Invoke(selectedIndices);
        }
    }
}

#endif
