using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;

// TODO Why is project wide input asset (InputManager.asset) not up to date when loaded?
// TODO Fix asset icons to make sense
// TODO Fix filter so that search is possible, what is default method to fallback to it?
// TODO Why is grouping not working as expected?
// TODO Possible to remove/eliminate options in search?
// TODO Why is picker flag not making a difference?
// TODO Does it make sense to fetch assets as currently done

namespace UnityEngine.InputSystem.Editor
{
    static class InputActionReferenceSearchProviderConstants
    {
        internal const string type = "InputActionReference"; // This allows picking up also assets
    }

    static class InputActionReferenceSearchProvider
    {
        // No need to use the SearchItemProvider -> this attribute is used to register the provider to the SearchWindow.
        // [SearchItemProvider]
        internal static SearchProvider CreateProjectSettingsAssetProvider()
        {
            return new SearchProvider(InputActionReferenceSearchProviderConstants.type, "Project Settings")
            {
                priority = 25,
                toObject = (item, type) => GetObject(item, type),
                fetchItems = (context, items, provider) => SearchProjectSettingsInputReferenceActionAssets(context, provider),
            };
        }

        static IEnumerable<SearchItem> SearchProjectSettingsInputReferenceActionAssets(SearchContext context, SearchProvider provider)
        {
            // TODO This could in theory be another icon to differentiate a project-wide action
            var icon = AssetDatabase.GetCachedIcon(ProjectWideActionsAsset.kAssetPath) as Texture2D;

            // This yields all accepted assets (InputActionReference) from ProjectWideActionsAsset.kAssetPath.
            var assets = AssetDatabase.LoadAllAssetsAtPath(ProjectWideActionsAsset.kAssetPath); // TODO Why does this return an outdated asset?!
            foreach (var asset in assets)
            {
                var label = asset.name;
                if (asset is InputActionReference && label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), label,
                        "Input Action Reference (Project Settings)", icon, asset);
            }
        }        

        static Object GetObject(SearchItem item, System.Type type)
        {
            return item.data as Object;
        }
    }

    // Custom property drawer in order to use the Advance picker:
    [CustomPropertyDrawer(typeof(InputActionReference))]
    public class AdvanceInputActionReferencePropertyDrawer : PropertyDrawer
    {
        private SearchContext m_Context;
        public AdvanceInputActionReferencePropertyDrawer()
        {
            // By default ADB search provider yields ALL assets even if the search query is empty. AssetProvider will NOT yield anything if searchQuery is empty
            var adbProvider = UnityEditor.Search.SearchService.GetProvider("adb");
            var defaultProvider = InputActionReferenceSearchProvider.CreateProjectSettingsAssetProvider();
            m_Context = UnityEditor.Search.SearchService.CreateContext(
                new[] { adbProvider, defaultProvider },
                "",
                // SearchFlags : these flags are used to customize how search is performed and how search results are displayed.
                SearchFlags.Sorted | SearchFlags.OpenPicker | SearchFlags.Packages);

        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.

            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label, m_Context,
                Search.SearchViewFlags.OpenInBuilderMode | Search.SearchViewFlags.DisableBuilderModeToggle | Search.SearchViewFlags.DisableInspectorPreview | Search.SearchViewFlags.DisableSavedSearchQuery);
        }
    }
}
