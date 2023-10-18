#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;

namespace UnityEngine.InputSystem.Editor
{
    internal static class SearchConstants
    {
        // SearchFlags : these flags are used to customize how search is performed and how search
        // results are displayed.
        // Note that SearchFlags.Packages is not currently used and hides all results from packages.
        internal static readonly SearchFlags PickerSearchFlags = SearchFlags.Sorted | SearchFlags.OpenPicker;

        // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.
        internal static readonly Search.SearchViewFlags PickerViewFlags = Search.SearchViewFlags.OpenInBuilderMode |
            Search.SearchViewFlags.DisableBuilderModeToggle |
            Search.SearchViewFlags.DisableInspectorPreview |
            Search.SearchViewFlags.DisableSavedSearchQuery;
    }

    internal static class AssetSearchProviders
    {
        internal static SearchProvider CreateDefaultProvider()
        {
            // Known issues with current ADB implementation:
            // - Asset icon reverts to file-icon on minimum zoom level.
            // - AssetDatabase.CachedIcon fails to retrieve custom package type icon.
            return UnityEditor.Search.SearchService.GetProvider("adb");
        }

        private static SearchProvider CreateInputActionReferenceSearchProvider(string id, string displayName,
            Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets)
        {
            // Match icon used for sub-assets from importer for InputActionReferences.
            // Note that we assign description+label in FilteredSearch but also provide a fetchDescription+fetchLabel below.
            // This is needed to support all zoom-modes for unknown reason.
            // ALso not that fetchLabel/fetchDescription and what is provided to CreateItem is playing different
            // roles at different zoom levels.
            var inputActionReferenceIcon = InputActionAssetIconProvider.LoadActionIcon();
            return new SearchProvider(id, displayName)
            {
                priority = 25,
                fetchDescription = FetchLabel,
                fetchItems = (context, items, provider) => FilteredSearch(context, provider, FetchLabel, createItemFetchDescription,
                    fetchAssets, "(Project-Wide Input Actions)"),
                fetchLabel = FetchLabel,
                fetchPreview = (item, context, size, options) => inputActionReferenceIcon,
                fetchThumbnail = (item, context) => inputActionReferenceIcon,
                toObject = (item, type) => item.data as Object
            };
        }

        internal static SearchProvider CreateProjectWideInputActionReferenceSearchProvider()
        {
            return CreateInputActionReferenceSearchProvider("InputActionReferencePickerSearchProvider",
                "Project-Wide Input Actions",
                (obj) => "(Project-Wide Input Actions)",
                () => InputActionImporter.LoadInputActionReferencesFromAsset(ProjectWideActionsAsset.GetOrCreate()));
        }

        // Custom search function with label matching filtering.
        private static IEnumerable<SearchItem> FilteredSearch(SearchContext context, SearchProvider provider,
            Func<Object, string> fetchObjectLabel, Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets, string description)
        {
            foreach (var asset in  fetchAssets())
            {
                var label = fetchObjectLabel(asset);
                if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    continue; // Ignore due to filtering
                yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), label, createItemFetchDescription(asset),
                    null, asset);
            }
        }

        // Note that this is overloaded to allow utilizing FetchLabel inside fetchItems to keep label formatting
        // consistent between CreateItem and additional fetchLabel calls.

        private static string FetchLabel(Object obj)
        {
            return obj.name;
        }

        private static string FetchLabel(SearchItem item, SearchContext context)
        {
            return FetchLabel((item.data as Object) !);
        }

        private static IEnumerable<InputActionReference> GetInputActionReferences(InputActionAsset asset)
        {
            return (from actionMap in asset.actionMaps from action in actionMap.actions select InputActionReference.Create(action));
        }
    }
}
#endif