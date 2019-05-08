#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal abstract class AdvancedDropdownDataSource
    {
        private static readonly string kSearchHeader = L10n.Tr("Search");

        public AdvancedDropdownItem mainTree { get; private set; }
        public AdvancedDropdownItem searchTree { get; private set; }
        public List<int> selectedIDs { get; } = new List<int>();

        protected AdvancedDropdownItem root => mainTree;
        protected List<AdvancedDropdownItem> m_SearchableElements;

        public void ReloadData()
        {
            mainTree = FetchData();
        }

        protected abstract AdvancedDropdownItem FetchData();

        public void RebuildSearch(string search)
        {
            searchTree = Search(search);
        }

        protected bool AddMatchItem(AdvancedDropdownItem e, string name, string[] searchWords, List<AdvancedDropdownItem> matchesStart, List<AdvancedDropdownItem> matchesWithin)
        {
            var didMatchAll = true;
            var didMatchStart = false;

            // See if we match ALL the search words.
            for (var w = 0; w < searchWords.Length; w++)
            {
                var search = searchWords[w];
                if (name.Contains(search))
                {
                    // If the start of the item matches the first search word, make a note of that.
                    if (w == 0 && name.StartsWith(search))
                        didMatchStart = true;
                }
                else
                {
                    // As soon as any word is not matched, we disregard this item.
                    didMatchAll = false;
                    break;
                }
            }
            // We always need to match all search words.
            // If we ALSO matched the start, this item gets priority.
            if (didMatchAll)
            {
                if (didMatchStart)
                    matchesStart.Add(e);
                else
                    matchesWithin.Add(e);
            }
            return didMatchAll;
        }

        protected virtual AdvancedDropdownItem PerformCustomSearch(string searchString)
        {
            return null;
        }

        protected virtual AdvancedDropdownItem Search(string searchString)
        {
            if (m_SearchableElements == null)
            {
                BuildSearchableElements();
            }
            if (string.IsNullOrEmpty(searchString))
                return null;

            var searchTree = PerformCustomSearch(searchString);
            if (searchTree == null)
            {
                // Support multiple search words separated by spaces.
                var searchWords = searchString.ToLower().Split(' ');

                // We keep two lists. Matches that matches the start of an item always get first priority.
                var matchesStart = new List<AdvancedDropdownItem>();
                var matchesWithin = new List<AdvancedDropdownItem>();

                foreach (var e in m_SearchableElements)
                {
                    var name = e.searchableName.ToLower().Replace(" ", "");
                    AddMatchItem(e, name, searchWords, matchesStart, matchesWithin);
                }

                searchTree = new AdvancedDropdownItem(kSearchHeader);
                matchesStart.Sort();
                foreach (var element in matchesStart)
                {
                    searchTree.AddChild(element);
                }
                matchesWithin.Sort();
                foreach (var element in matchesWithin)
                {
                    searchTree.AddChild(element);
                }
            }

            return searchTree;
        }

        private void BuildSearchableElements()
        {
            m_SearchableElements = new List<AdvancedDropdownItem>();
            BuildSearchableElements(root);
        }

        private void BuildSearchableElements(AdvancedDropdownItem item)
        {
            if (!item.children.Any())
            {
                if (!item.IsSeparator())
                    m_SearchableElements.Add(item);
                return;
            }
            foreach (var child in item.children)
            {
                BuildSearchableElements(child);
            }
        }
    }
}

#endif // UNITY_EDITOR
