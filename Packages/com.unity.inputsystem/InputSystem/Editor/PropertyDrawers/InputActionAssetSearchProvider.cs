#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.Search;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputActionAssetSearchProviders
    {
        const string k_AssetFolderSearchProviderId = "AssetsInputActionAssetSearchProvider";
        const string k_ProjectWideActionsSearchProviderId = "ProjectWideInputActionAssetSearchProvider";

        const string k_ProjectWideAssetIdentificationString = " [Project Wide Input Actions]";

        internal static SearchProvider CreateInputActionAssetSearchProvider()
        {
            return CreateInputActionAssetSearchProvider(k_AssetFolderSearchProviderId,
                "Asset Input Action Assets",
                (obj) => { return obj != null ? AssetDatabase.GetAssetPath(obj) : "Null"; },
                () => LoadInputActionAssetsFromAssetDatabase(skipProjectWide : true));
        }

        internal static SearchProvider CreateInputActionAssetSearchProviderForProjectWideActions()
        {
            return CreateInputActionAssetSearchProvider(k_ProjectWideActionsSearchProviderId,
                "Project-Wide Input Action Asset",
                (obj) => { return obj != null ? AssetDatabase.GetAssetPath(obj) : "Null"; },
                () => LoadInputActionReferencesFromAsset());
        }

        private static IEnumerable<Object> LoadInputActionReferencesFromAsset()
        {
            var asset = InputSystem.actions;
            if (asset == null)
                return Array.Empty<Object>();

            return new List<InputActionAsset>() { asset };
        }


        private static IEnumerable<Object> LoadInputActionAssetsFromAssetDatabase(bool skipProjectWide)
        {
            string[] searchFolders = new string[] { "Assets" };

            var inputActionAssetGUIDs = AssetDatabase.FindAssets($"t:{typeof(InputActionAsset).Name}", searchFolders);

            var inputActionAssetList = new List<InputActionAsset>();
            foreach (var guid in inputActionAssetGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetInputActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

                if (skipProjectWide)
                {
                    if (assetInputActionAsset == InputSystem.actions)
                        continue;
                }

                inputActionAssetList.Add(assetInputActionAsset);
            }

            return inputActionAssetList;
        }

        private static SearchProvider CreateInputActionAssetSearchProvider(string id, string displayName,
            Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets)
        {
            // We assign description+label in FilteredSearch but also provide a fetchDescription+fetchLabel below.
            // This is needed to support all zoom-modes for an unknown reason.
            // Also, fetchLabel/fetchDescription and what is provided to CreateItem is playing different
            // roles at different zoom levels.
            var inputActionAssetIcon = InputActionAssetIconLoader.LoadAssetIcon();
            var inputActionAssetProjectWideIcon = InputActionAssetIconLoader.LoadAssetIcon(projectWide : true);

            return new SearchProvider(id, displayName)
            {
                priority = 25,
                fetchDescription = FetchLabel,
                fetchItems = (context, items, provider) => FilteredSearch(context, provider, FetchLabel, createItemFetchDescription,
                    fetchAssets, inputActionAssetIcon, inputActionAssetProjectWideIcon),
                fetchLabel = FetchLabel,
                fetchPreview = FetchPreview,
                fetchThumbnail = FetchThumbnail,
                toObject = ToObject,
            };
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            // thumnail is lost on scaling so have to reassign it
            return item.thumbnail ? item.thumbnail : FetchObjectThumbnail(item.data as Object, InputActionAssetIconLoader.LoadAssetIcon(), InputActionAssetIconLoader.LoadAssetIcon(projectWide: true));
        }

        private static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            // thumnail is lost on scaling so have to reassign it
            return item.thumbnail ? item.thumbnail : FetchObjectThumbnail(item.data as Object, InputActionAssetIconLoader.LoadAssetIcon(), InputActionAssetIconLoader.LoadAssetIcon(projectWide: true));
        }

        private static Object ToObject(SearchItem item, Type type)
        {
            return item.data as Object;
        }

        // Custom search function with label matching filtering.
        private static IEnumerable<SearchItem> FilteredSearch(SearchContext context, SearchProvider provider,
            Func<Object, string> fetchObjectLabel, Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets, Texture2D inputActionAssetIcon, Texture2D inputActionAssetProjectWideIcon)
        {
            foreach (var asset in fetchAssets())
            {
                var label = fetchObjectLabel(asset);
                var thumbnail = FetchObjectThumbnail(asset, inputActionAssetIcon, inputActionAssetProjectWideIcon);

                if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    continue; // Ignore due to filtering
                yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), label, createItemFetchDescription(asset),
                    thumbnail, asset);
            }
        }

        // Note that this is overloaded to allow utilizing FetchLabel inside fetchItems to keep label formatting
        // consistent between CreateItem and additional fetchLabel calls.
        private static string FetchLabel(Object obj)
        {
            if (obj == InputSystem.actions)
                return $"{obj.name}{k_ProjectWideAssetIdentificationString}";
            return obj.name;
        }

        private static Texture2D FetchObjectThumbnail(Object obj, Texture2D inputActionAssetIcon, Texture2D inputActionAssetProjectWideIcon)
        {
            if (obj == InputSystem.actions)
                return inputActionAssetProjectWideIcon;
            return inputActionAssetIcon;
        }

        private static string FetchLabel(SearchItem item, SearchContext context)
        {
            return FetchLabel((item.data as Object)!);
        }
    }
}
#endif
