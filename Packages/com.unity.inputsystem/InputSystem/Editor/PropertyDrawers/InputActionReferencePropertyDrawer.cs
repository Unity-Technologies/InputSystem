// Note: If not UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS we do not use a custom property drawer and
//       picker for InputActionReferences but rather rely on default (classic) object picker.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.UIElements;

// TODO Why is project wide input asset (InputManager.asset) not up to date when loaded?
// TODO Fix asset icons to make sense

namespace UnityEngine.InputSystem.Editor
{
    static class ProjectWideInputActionReferenceSearchProvider
    {
        // No need to use the SearchItemProvider -> this attribute is used to register the provider to
        // the SearchWindow and here we are mainly interested in using SearchItemProvider as a picker.
        // [SearchItemProvider]
        internal static SearchProvider CreateProjectSettingsAssetProvider()
        {
            return new SearchProvider("InputActionReferenceSearchProvider", "Project-Wide Actions")
            {
                priority = 25,
                toObject = (item, type) => GetObject(item, type),
                fetchItems = (context, items, provider) => SearchProjectSettingsInputReferenceActionAssets(context, provider),
            };
        }

        static Object[] GetInputActionReferenceAssets()
        {
            // TODO Why does this return outdated assets?! Must be a problem unrelated to this search
            // TODO Why does this return duplicates with different GUIDs
            return AssetDatabase.LoadAllAssetsAtPath(ProjectWideActionsAsset.kAssetPath);
        }

        static IEnumerable<SearchItem> SearchProjectSettingsInputReferenceActionAssets(SearchContext context, SearchProvider provider)
        {
            // TODO This could in theory be another icon to differentiate a project-wide action
            var icon = AssetDatabase.GetCachedIcon(ProjectWideActionsAsset.kAssetPath) as Texture2D;

            // This yields all accepted assets (InputActionReference) from ProjectWideActionsAsset.kAssetPath.
            foreach (var asset in GetInputActionReferenceAssets())
            {
                var inputActionReference = asset as InputActionReference;
                if (inputActionReference == null)
                    continue; // Ignore item since not an InputActionReference

                var label = asset.name;
                if (!label.Contains(context.searchText, System.StringComparison.InvariantCultureIgnoreCase))
                    continue; // Ignore time since not matching search text filter

                //Debug.Log($"name:{asset.name} instance:{asset.GetInstanceID()} type:{asset.GetType()} subAsset:{AssetDatabase.IsSubAsset(asset)} actionId:{inputActionReference.m_ActionId}, map:{inputActionReference.m_Asset}");

                var description = $"{AssetDatabase.GetAssetPath(asset)} ({label})";

                yield return provider.CreateItem(context, asset.GetInstanceID().ToString(), label,
                    description, icon, asset);
            }
        }

        private static Object GetObject(SearchItem item, System.Type type)
        {
            return item.data as Object;
        }
    }

    // Custom property drawer in order to use the Advance picker:
    [CustomPropertyDrawer(typeof(InputActionReference))]
    public sealed class InputActionReferencePropertyDrawer : PropertyDrawer
    {
        private readonly SearchContext m_Context;

        // Search.SearchViewFlags : these flags are used to customize the appearance of the PickerWindow.
        private readonly Search.SearchViewFlags m_ViewFlags = Search.SearchViewFlags.OpenInBuilderMode |
            Search.SearchViewFlags.DisableBuilderModeToggle |
            Search.SearchViewFlags.DisableInspectorPreview |
            Search.SearchViewFlags.DisableSavedSearchQuery;

        public InputActionReferencePropertyDrawer()
        {
            // By default ADB search provider yields ALL assets even if the search query is empty.
            // AssetProvider ("asset") will NOT yield anything if searchQuery is empty.
            var adbProvider = UnityEditor.Search.SearchService.GetProvider("adb");
            var projectSettingsProvider = ProjectWideInputActionReferenceSearchProvider.CreateProjectSettingsAssetProvider();
            m_Context = UnityEditor.Search.SearchService.CreateContext(
                new[] { adbProvider, projectSettingsProvider },
                "",
                // SearchFlags : these flags are used to customize how search is performed and how search
                // results are displayed.
                SearchFlags.Sorted | SearchFlags.OpenPicker | SearchFlags.Packages);
        }

        // TODO: Enable UITk picker as part of modernizing all pickers since sizing policies are different.
        /*
        // Advance Picker in UITk
        public override VisualElement CreatePropertyGUI(SerializedProperty prop)
        {

            ObjectField obj = new ObjectField()
            {
                name = "InputActionReferenceProperty",
                label = preferredLabel,
                bindingPath = prop.propertyPath,
                objectType = fieldInfo.FieldType,
                searchViewFlags = m_ViewFlags,
                searchContext = m_Context
            };
            return obj;
        }
        */

        // Advance Picker in IMGUI
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label, m_Context, m_ViewFlags);
        }
    }
}

#endif
