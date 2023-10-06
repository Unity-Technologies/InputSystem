// Note: If not UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS we do not use a custom property drawer and
//       picker for InputActionReferences but rather rely on default (classic) object picker.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using UnityEditor;
using UnityEditor.Search;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom property drawer in order to use the "Advanced Picker" from UnityEditor.Search.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionReference))]
    internal sealed class InputActionReferencePropertyDrawer : PropertyDrawer
    {
        // By default ADB search provider yields ALL assets even if the search query is empty.
        // AssetProvider ("asset") will NOT yield anything if searchQuery is empty.
        private readonly SearchContext m_Context = UnityEditor.Search.SearchService.CreateContext(new[]
        {
            UnityEditor.Search.SearchService.GetProvider("adb"),
            AssetSearchProviders.CreateInputActionReferenceSearchProvider()
        }, string.Empty, SearchConstants.SearchFlags);

        // Advanced Picker in UITk
        public override UIElements.VisualElement CreatePropertyGUI(SerializedProperty prop)
        {
            ObjectField obj = new ObjectField()
            {
                name = "InputActionReferenceProperty",
                label = preferredLabel,
                bindingPath = prop.propertyPath,
                objectType = fieldInfo.FieldType,
                searchViewFlags = SearchConstants.ViewFlags,
                searchContext = m_Context
            };
            obj.AddToClassList(ObjectField.alignedFieldUssClassName); // align width
            return obj;
        }

        // Advanced Picker in IMGUI, keeping for reference
        /*public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label,
                m_Context, SearchConstants.ViewFlags);
        }*/
    }
}

#endif
