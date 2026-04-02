#if UNITY_EDITOR

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
    ///
    /// Define UNITY_INPUT_SYSTEM_INPUT_ASSET_EDITOR_ALLOWS_DESELECTION to disable this feature if desired
    /// during development.
    /// </remarks>>
    internal class CollectionViewSelectionChangeFilter
    {
        private readonly BaseVerticalCollectionView m_View;
        private List<int> m_SelectedIndices;

        /// <summary>
        /// Event triggered as an output to filtering the selected indices reported by the view.
        /// </summary>
        public event Action<IEnumerable<int>> selectedIndicesChanged;

        public CollectionViewSelectionChangeFilter(BaseVerticalCollectionView view)
        {
            m_SelectedIndices = new List<int>();

            m_View = view;
            #if UNITY_INPUT_SYSTEM_INPUT_ASSET_EDITOR_ALLOWS_DESELECTION
            m_View_.selectedIndicesChanged += OnSelectedIndicesChanged;
            #else
            m_View.selectedIndicesChanged += FilterSelectedIndicesChanged;
            #endif
        }

        #if !UNITY_INPUT_SYSTEM_INPUT_ASSET_EDITOR_ALLOWS_DESELECTION
        private void FilterSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            // Convert IEnumerable to a list to allow for multiple-iteration
            var currentlySelectedIndices = selectedIndices.ToList();

            // If the selection change is a deselection (transition from having a selection to having none)
            if (currentlySelectedIndices.Count == 0)
            {
                if (m_SelectedIndices.Count > 0)
                {
                    // Remove any stored selection indices that are no longer within valid range
                    var count = m_View.itemsSource.Count;
                    for (var i = m_SelectedIndices.Count - 1; i >= 0; --i)
                    {
                        if (m_SelectedIndices[i] >= count)
                            m_SelectedIndices.RemoveAt(i);
                    }

                    // Restore selection based on last known selection and return immediately since
                    // assignment will retrigger selection change.
                    m_View.SetSelection(m_SelectedIndices);
                    return;
                }
            }
            else
            {
                // Store indices to allow preventing future deselect
                m_SelectedIndices = currentlySelectedIndices;
            }

            OnSelectedIndicesChanged(this.m_SelectedIndices);
        }

        #endif

        private void OnSelectedIndicesChanged(IEnumerable<int> selectedIndices)
        {
            var selectedIndicesChanged = this.selectedIndicesChanged;
            selectedIndicesChanged?.Invoke(selectedIndices);
        }
    }
}

#endif
