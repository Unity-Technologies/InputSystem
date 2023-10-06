#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.VersionControl;

namespace UnityEngine.InputSystem.Editor
{
    internal static class SearchConstants
    {
        // SearchFlags : these flags are used to customize how search is performed and how search
        // results are displayed.
        internal static readonly SearchFlags SearchFlags = SearchFlags.Sorted |
            SearchFlags.OpenPicker |
            SearchFlags.Packages;

        // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.
        internal static readonly Search.SearchViewFlags ViewFlags = Search.SearchViewFlags.OpenInBuilderMode |
            Search.SearchViewFlags.DisableBuilderModeToggle |
            Search.SearchViewFlags.DisableInspectorPreview |
            Search.SearchViewFlags.DisableSavedSearchQuery;
    }

    internal static class AssetSearchProviders
    {
        // Note that if this method is annotated with [SearchItemProvider] we would register the provider to
        // the SearchWindow, we currently skip this since we are mainly interested in using SearchItemProvider as
        // a picker.
        private static SearchProvider CreateAssetSearchProvider(string id, string displayName,
            Func<IEnumerable<Object>> assetProvider, Texture2D thumbnail = null)
        {
            return new SearchProvider(id, displayName)
            {
                priority = 25,
                toObject = GetObject,
                fetchItems = (context, items, provider) => Search(context, provider, assetProvider, thumbnail),
            };
        }

        internal static SearchProvider CreateInputActionReferenceSearchProvider()
        {
            // Match icon used for sub-assets from importer, if null will use cached icon from asset database instead.
            // Note that .inputactions has one icon and sub-assets (actions) have another in the importer.
            return CreateAssetSearchProvider(
                "InputActionReferenceSearchProvider",
                "Project-Wide Input Actions",
                GetInputActionReferenceAssets,
                (Texture2D)EditorGUIUtility.Load(InputActionImporter.kActionIcon));
        }

        private static IEnumerable<Object> GetInputActionReferenceAssets()
        {
            // Extract actions from actions maps from ProjectWideActionAsset.
            // Note that we cannot do something like:
            //  return AssetDatabase.LoadAllAssetsAtPath(ProjectWideActionsAsset.kAssetPath)
            //      .Where((asset) => (asset is InputActionReference));
            // Since that would return an outdated asset that is out of sync with ProjectWideActionsAsset.
            // TODO Understand why this is a problem and update this comment
            var asset = ProjectWideActionsAsset.GetOrCreate();
            return (from actionMap in asset.actionMaps from action in actionMap.actions select InputActionReference.Create(action)).ToList();
        }

        static IEnumerable<SearchItem> Search(
            SearchContext context, SearchProvider provider, Func<IEnumerable<Object>> assetProvider, Texture2D thumbnail)
        {
            foreach (var asset in assetProvider())
            {
                var label = asset.name;
                if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    continue; // Ignore: not matching search text filter

                yield return provider.CreateItem(context,
                    asset.GetInstanceID().ToString(),
                    label,
                    $"{AssetDatabase.GetAssetPath(asset)} ({label})",
                    (thumbnail == null) ? AssetDatabase.GetCachedIcon(ProjectWideActionsAsset.kAssetPath) as Texture2D : thumbnail,
                    asset);
            }
        }

        private static Object GetObject(SearchItem item, System.Type type)
        {
            return item.data as Object;
        }
    }
}
#endif
