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
        internal const string defaultSearchQuery = "t:InputActionReference";
        //internal const string displayName = "Input Action";
    }

    static class InputActionReferenceSearchProvider
    {
        [SearchItemProvider]
        internal static SearchProvider CreateProjectSettingsAssetProvider()
        {
            return new SearchProvider(InputActionReferenceSearchProviderConstants.type, "Project Settings")
            {
                priority = 25,
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.DefaultGroup,
                toObject = (item, type) => GetObject(item, type),
                fetchItems = (context, items, provider) => SearchDefaultCustomAssets(context, provider),
            };
        }

        [SearchItemProvider]
        internal static SearchProvider CreateAssetDatabaseProvider()
        {
            return new SearchProvider(InputActionReferenceSearchProviderConstants.type, "Assets")
            {
                priority = 25,
                showDetails = true, // true
                showDetailsOptions = ShowDetailsOptions.Default | ShowDetailsOptions.DefaultGroup,
                toObject = (item, type) => GetObject(item, type),
                fetchItems = (context, items, provider) => SearchStandardAssets(context, provider),
            };
        }

        static IEnumerable<SearchItem> SearchDefaultCustomAssets(SearchContext context, SearchProvider provider)
        {
            // TODO This could in theory be another icon to differentiate a project-wide action
            var icon = AssetDatabase.GetCachedIcon(ProjectWideActionsAsset.kAssetPath) as Texture2D;

            // This yields all accepted assets (InputActionReference) from ProjectWideActionsAsset.kAssetPath.
            var assets = AssetDatabase.LoadAllAssetsAtPath(ProjectWideActionsAsset.kAssetPath); // TODO Why does this return an outdated asset?!
            foreach (var asset in assets)
            {
                // We filter the returned result to only contain InputActionReference types since this
                // otherwise would also pick up InputActionMaps
                //if (AcceptAsset(asset))
                yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), asset.name,
                    "Input Action Reference (Project Settings)", icon, asset);
            }
        }

        static IEnumerable<SearchItem> SearchStandardAssets(SearchContext context, SearchProvider provider)
        {
            // TODO This could in theory be another icone, e.g. action icon, but this seems to be what is registered
            //var icon = AssetDatabase.GetCachedIcon(ProjectWideActionsAsset.kAssetPath) as Texture2D;
            var icon = (Texture2D)EditorGUIUtility.Load(InputActionImporter.kActionIcon);

            // Note that this would just find the first InputActionReference within each InputActionMap
            // so it could be questioned whether we should just look for maps here?
            // However, this might pickup any other asset that may contain InputActionReference which might
            // be better from a future-proofing perspective.
            var guids = AssetDatabase.FindAssets($"t:{typeof(InputActionReference)}");
            foreach (var guid in guids)
            {
                // We use LoadAllAssetsAtPath to load all InputActionReference assets within whatever we found
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var asset in assets)
                    yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), asset.name,
                        $"Input Action Reference ({assetPath})", icon, asset);
            }
        }

        static Object GetObject(SearchItem item, System.Type type)
        {
            return item.data as Object;
        }

        [MenuItem("Test/Load Input Manager")]
        static void LoadInputManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(ProjectWideActionsAsset.kAssetPath);
            foreach (var asset in assets)
            {
                Debug.Log($"asset: {asset.name} {asset.GetType()} {AcceptAsset(asset)}");
            }
        }

        static bool AcceptAsset(Object asset)
        {
            // Only include assets of type InputActionReference
            if (asset.GetType() == typeof(InputActionReference))
                return true;
            return false;
        }
    }

    // Custom property drawer in order to use the Advance picker:
    [CustomPropertyDrawer(typeof(InputActionReference))]
    public class MyCustomAssetPropertyDrawer : PropertyDrawer
    {
        private SearchContext m_Context;
        public MyCustomAssetPropertyDrawer()
        {
            // Create a SearchContext with the normal asset provider which should be used to filter your type.
            // Add to it the DefaultCustomAsset provider to yield all defaults assets.
            //var assetProvider = UnityEditor.Search.SearchService.GetProvider("asset"); // TODO This is what allows us to find InputActionReferences
            var assetProvider = InputActionReferenceSearchProvider.CreateAssetDatabaseProvider();
            var defaultProvider = InputActionReferenceSearchProvider.CreateProjectSettingsAssetProvider();

            // Note that since we use custom providers we can skip using a type-based query
            m_Context = UnityEditor.Search.SearchService.CreateContext(
                new[] { assetProvider, defaultProvider },
                "",
                SearchFlags.Sorted | SearchFlags.OpenPicker); // t:InputActionReference seems to be required to find InputActionReferences in regular assets?
        }

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Pop the picker.
            // IMPORTANT Note: in your case the filterType should be typeof(InputActionReference)
            // If the default asset in InputManager were InputActionReference as well (it doesn't seem to be the case for me) you wouldn't need
            // to specify the custom query above (t:MyCustomAsset)
            // FINDING: .inputactions objects only found if having typeof(Object)
            // FINDING: having proper typeof(InputActionReference) does not include InputActionReference results
            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label, m_Context);
        }
    }
}
