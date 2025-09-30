// Note: If not UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS we do not use a custom property drawer and
//       picker for InputActionReferences but rather rely on default (classic) object picker.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using UnityEditor;
using UnityEditor.Search;
using ObjectField = UnityEditor.Search.ObjectField;

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
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            // Reassign null if property is a dangling project-wide input action
            // var explicitlyReassigned = false;
            var current = property.objectReferenceValue;
            // if (reference != null && reference.asset == InputSystem.actions)
            // {
            //     var action = reference?.asset?.FindAction(reference.action.id);
            //     if (action is null)
            //         property.objectReferenceValue = null; // TODO This cannot be right?!
            // }

            // Pick an InputActionReference using custom picker. We need to use this overload taking an object
            // in order to be in control of the actual assignment to the serialized property.
            // This is important since we should NEVER assign a direct reference to a ScriptableObject residing
            // in an asset. Instead, we should instantiate a new ScriptableObject instance to prevent destructive
            // operations that would mutate the asset.
            var candidate = ObjectField.DoObjectField(position, current, typeof(InputActionReference),
                label, m_Context, SearchConstants.PickerViewFlags);

            // Only assign the value if it was actually changed by the user.
            if (EditorGUI.EndChangeCheck() &&  !Equals(candidate, current))
            {
                var reference = candidate as InputActionReference;
                property.objectReferenceValue = reference ?
                    InputActionReference.Create(reference.action) : null;
            }

            EditorGUI.EndProperty();
        }
    }
}

#endif
