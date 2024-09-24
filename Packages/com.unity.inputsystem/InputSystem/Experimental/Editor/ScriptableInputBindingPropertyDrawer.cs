using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.Search;
using Object = UnityEngine.Object;

namespace UnityEditor.InputSystem.Experimental
{
    // Default selector allows "Assets" and "Scene"
    // https://discussions.unity.com/t/custompropertydrawer-for-a-class-with-a-generic-type/498538/4
    //[CustomPropertyDrawer(typeof(ScriptableInputBinding<>), useForChildren: true)]
    internal sealed class ScriptableInputBindingPropertyDrawer : PropertyDrawer
    {
        const string kAssetSearchProviderId = "InputBindingAssetSearchProvider";
        const string kPresetSearchProviderId = "InputBindingPresetSearchProvider";

        private static class SearchProviders
        {
            private static SearchProvider CreateSearchProvider(string id, string displayName,
                Func<Object, string> createItemFetchDescription, Func<IEnumerable<Object>> fetchAssets)
            {
                // Match icon used for sub-assets from importer for InputActionReferences.
                // We assign description+label in FilteredSearch but also provide a fetchDescription+fetchLabel below.
                // This is needed to support all zoom-modes for an unknown reason.
                // Also, fetchLabel/fetchDescription and what is provided to CreateItem is playing different
                // roles at different zoom levels.
                var icon = Resources.LoadIcon(Resources.Icon.InteractiveBinding);

                return new SearchProvider(id, displayName)
                {
                    priority = 25,
                    fetchDescription = FetchDescription,
                    fetchItems = (context, items, provider) => FilteredSearch(context, provider, FetchLabel, createItemFetchDescription,
                        fetchAssets, "(Presets)"),
                    fetchLabel = FetchLabel,
                    fetchPreview = (item, context, size, options) => icon,
                    fetchThumbnail = (item, context) => icon,
                    toObject = (item, type) => item.data as Object,
                };
            }
            
            // Custom search function with label matching filtering.
            private static IEnumerable<SearchItem> FilteredSearch(SearchContext context, SearchProvider provider,
                Func<Object, string> fetchObjectLabel, 
                Func<Object, string> createItemFetchDescription, 
                Func<IEnumerable<UnityEngine.Object>> fetchAssets, 
                string description)
            {
                foreach (var asset in fetchAssets())
                {
                    var label = fetchObjectLabel(asset);
                    if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                        continue; // Ignore due to filtering
                    var id = asset.GetInstanceID().ToString();
                    var item = createItemFetchDescription(asset);
                    yield return provider.CreateItem(context, id, label, item, null, asset);
                }
            }
            
            private static string FetchLabel(UnityEngine.Object obj)
            {
                return obj.name;
            }

            private static string FetchLabel(SearchItem item, SearchContext context)
            {
                return FetchLabel((item.data as Object) !);
            }

            private static string FetchDescription(SearchItem item, SearchContext context)
            {
                var binding = item.data as ScriptableInputBinding;
                if (binding == null)
                    return FetchLabel(binding);
                return "Generic cross-platform configuration for moving in a FPS game.";
                //return $"Input binding of type {binding.GetBindingType()}"; // TODO Could construct description from actual object programmatically
            }

            private static IEnumerable<T> LoadAssetsOfType<T>() where T : UnityEngine.Object
            {
                return AssetDatabase
                    .FindAssets($"t:{typeof(T).Name}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<T>);
            }
            
            public static SearchProvider CreateForAssets()
            {
                return CreateSearchProvider(
                    id: kAssetSearchProviderId,
                    displayName: "Assets",
                    createItemFetchDescription: AssetDatabase.GetAssetPath,
                    fetchAssets: LoadAssetsOfType<ScriptableInputBinding>);
            }

            public static SearchProvider CreateForPresets()
            {
                var icon = Resources.LoadIcon(Resources.Icon.Asset);
                
                return new SearchProvider(kPresetSearchProviderId, "Presets")
                {
                    priority = 25,
                    fetchDescription = (item, context) => "A",
                    fetchItems = (context, items, provider) => FetchPresets(),
                    fetchLabel = FetchLabel,
                    fetchPreview = (item, context, size, options) => icon,
                    fetchThumbnail = (item, context) => icon,
                    toObject = (item, type) => item.data as UnityEngine.Object,
                };
            }

            private static IEnumerable<Object> FetchPresets()
            {
                yield break;
            }
        }
        
        // SearchFlags: these flags are used to customize how search is performed and how search
        // results are displayed in the advanced object picker.
        // Note: SearchFlags.Packages is not currently used and hides all results from packages.
        internal static readonly SearchFlags PickerSearchFlags = SearchFlags.Sorted | SearchFlags.OpenPicker;

        // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.
        internal static readonly SearchViewFlags PickerViewFlags = SearchViewFlags.DisableBuilderModeToggle
                                                                          | SearchViewFlags.DisableInspectorPreview
                                                                          | SearchViewFlags.ListView
                                                                          | SearchViewFlags.DisableSavedSearchQuery;
        
        private readonly SearchContext m_Context = UnityEditor.Search.SearchService.CreateContext(new[]
        {
            SearchProviders.CreateForAssets(),
            SearchProviders.CreateForPresets()
        }, string.Empty, PickerSearchFlags);
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField.DoObjectField(
                position: position, 
                property: property, 
                objType: typeof(ScriptableInputBinding), 
                label: label,
                context: m_Context, 
                searchViewFlags: PickerViewFlags);
        }
    }
}