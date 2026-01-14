#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Search;

namespace UnityEngine.InputSystem.Editor
{
    internal static class SearchConstants
    {
        // SearchFlags: these flags are used to customize how search is performed and how search
        // results are displayed in the advanced object picker.
        // Note: SearchFlags.Packages is not currently used and hides all results from packages.
        internal static readonly SearchFlags PickerSearchFlags = SearchFlags.OpenPicker;

        // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.
        internal static readonly Search.SearchViewFlags PickerViewFlags = SearchViewFlags.DisableBuilderModeToggle
            | SearchViewFlags.DisableInspectorPreview
            | SearchViewFlags.ListView
            | SearchViewFlags.DisableSavedSearchQuery;
    }

    internal static class InputActionReferenceSearchProviders
    {
        const string k_AssetFolderSearchProviderId = "AssetsInputActionReferenceSearchProvider";
        const string k_ProjectWideActionsSearchProviderId = "ProjectWideInputActionReferenceSearchProvider";

        // Search provider for InputActionReferences for all assets in the project, without project-wide actions.
        internal static SearchProvider CreateInputActionReferenceSearchProviderForAssets()
        {
            return CreateInputActionReferenceSearchProvider(k_AssetFolderSearchProviderId,
                "Asset Input Actions",
                // Show the asset path in the description.
                (obj) => AssetDatabase.GetAssetPath((obj as InputActionReference).asset),
                () => InputActionImporter.LoadInputActionReferencesFromAssetDatabase(skipProjectWide: true));
        }

        // Search provider for InputActionReferences for project-wide actions
        internal static SearchProvider CreateInputActionReferenceSearchProviderForProjectWideActions()
        {
            return CreateInputActionReferenceSearchProvider(k_ProjectWideActionsSearchProviderId,
                "Project-Wide Input Actions",
                (obj) => "(Project-Wide Input Actions)",
                () =>
                {
                    var asset = InputSystem.actions;
                    if (asset == null)
                        return Array.Empty<Object>();
                    var assetPath = AssetDatabase.GetAssetPath(asset);
                    return InputActionImporter.LoadInputActionReferencesFromAsset(assetPath);
                });
        }

        private static SearchProvider CreateInputActionReferenceSearchProvider(string id, string displayName,
            Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets)
        {
            // Match icon used for sub-assets from importer for InputActionReferences.
            // We assign description+label in FilteredSearch but also provide a fetchDescription+fetchLabel below.
            // This is needed to support all zoom-modes for an unknown reason.
            // Also, fetchLabel/fetchDescription and what is provided to CreateItem is playing different
            // roles at different zoom levels.
            var inputActionReferenceIcon = InputActionAssetIconLoader.LoadActionIcon();

            return new SearchProvider(id, displayName)
            {
                priority = 25,
                fetchDescription = FetchLabel,
                fetchItems = (context, items, provider) => FilteredSearch(context, provider, FetchLabel, createItemFetchDescription,
                    fetchAssets, "(Project-Wide Input Actions)"),
                fetchLabel = FetchLabel,
                fetchPreview = (item, context, size, options) => inputActionReferenceIcon,
                fetchThumbnail = (item, context) => inputActionReferenceIcon,
                toObject = (item, type) => item.data as Object,
            };
        }

        // Custom search function with label matching filtering.
        private static IEnumerable<SearchItem> FilteredSearch(SearchContext context, SearchProvider provider,
            Func<Object, string> fetchObjectLabel, Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets, string description)
        {
            foreach (var asset in fetchAssets())
            {
                var label = fetchObjectLabel(asset);
                if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    continue; // Ignore due to filtering

                string itemId;

                // 6.4 deprecated instance ids in favour of entity ids
                #if UNITY_6000_4_OR_NEWER
                itemId = asset.GetEntityId().ToString();
                #else
                itemId = asset.GetInstanceID().ToString();
                #endif

                yield return provider.CreateItem(context, itemId, label, createItemFetchDescription(asset),
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
    }
}
#endif
