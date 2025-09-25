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
        private readonly SearchContext m_Context = UnityEditor.Search.SearchService.CreateContext(new[]
        {
            InputActionReferenceSearchProviders.CreateInputActionReferenceSearchProviderForAssets(),
            InputActionReferenceSearchProviders.CreateInputActionReferenceSearchProviderForProjectWideActions(),
        }, string.Empty, SearchConstants.PickerSearchFlags);


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Sets the property to null if the action is not found in the asset.
            ValidatePropertyWithDanglingInputActionReferences(property);

            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label,
                m_Context, SearchConstants.PickerViewFlags);
        }

        static void ValidatePropertyWithDanglingInputActionReferences(SerializedProperty property)
        {
            if (property?.objectReferenceValue is InputActionReference reference)
            {
                // Check only if the reference is a project-wide action.
                if (reference?.asset == InputSystem.actions)
                {
                    var action = reference?.asset?.FindAction(reference.action.id);
                    if (action is null)
                    {
                        property.objectReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }
    }
}

#endif
