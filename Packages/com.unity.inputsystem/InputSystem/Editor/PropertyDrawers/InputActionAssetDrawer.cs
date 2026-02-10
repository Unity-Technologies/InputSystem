#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom property drawer in order to use the "Advanced Picker" from UnityEditor.Search.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionAsset))]
    internal sealed class InputActionAssetDrawer : PropertyDrawer
    {
        private readonly SearchContext m_Context = UnityEditor.Search.SearchService.CreateContext(new[]
        {
            InputActionAssetSearchProviders.CreateInputActionAssetSearchProvider(),
            InputActionAssetSearchProviders.CreateInputActionAssetSearchProviderForProjectWideActions(),
        }, string.Empty, SearchConstants.PickerSearchFlags);


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField.DoObjectField(position, property, typeof(InputActionAsset), label,
                m_Context, SearchConstants.PickerViewFlags);
        }
    }
}

#endif
