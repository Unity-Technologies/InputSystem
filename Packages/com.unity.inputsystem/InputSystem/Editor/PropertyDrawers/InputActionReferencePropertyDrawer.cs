// Note: If not UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS we do not use a custom property drawer and
//       picker for InputActionReferences but rather rely on default (classic) object picker.
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using UnityEditor.Search;
using ObjectField = UnityEditor.Search.ObjectField;
#endif // UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Custom property drawer in order to use the "Advanced Picker" from UnityEditor.Search.
    /// </summary>
    [CustomPropertyDrawer(typeof(InputActionReference))]
    internal sealed class InputActionReferencePropertyDrawer : PropertyDrawer
    {
        #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        // Custom search providers for asset-based, and project-wide actions input action reference sub-assets.
        private readonly SearchContext m_Context = UnityEditor.Search.SearchService.CreateContext(new[]
        {
            InputActionReferenceSearchProviders.CreateInputActionReferenceSearchProviderForAssets(),
            InputActionReferenceSearchProviders.CreateInputActionReferenceSearchProviderForProjectWideActions(),
        }, string.Empty, SearchConstants.PickerSearchFlags);
        #endif

        public void OnGUI(Rect position, SerializedProperty property, GUIContent label,
            System.Func<Rect, Object, System.Type, GUIContent, Object> doField = null)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var current = property.objectReferenceValue;

            // If the reference is set but cannot be resolved, we want to be consistent with standard unity objects
            // which resolve to display "Missing (Type)". This isn't really feasible since our SO isn't destroyed
            // directly if referenced from other assets and we have two-layer indirection (InputAction isn't an asset).
            // Hence, the next best thing we can do is pass null to object field to show "None (Type)" to indicate
            // a broken reference that need to be replaced. If however, other code would manage to delete the SO,
            // "Missing (Type)" should be the result.
            var obj = current;
            var currentReference = current as InputActionReference;
            if (currentReference && currentReference.action == null)
                obj = null; // none/missing

            #if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
            // Pick an InputActionReference using custom picker. We need to use this overload taking an object
            // in order to be in control of the actual assignment to the serialized property so we can substitute.
            // We allow substituting the object field here to at least enable some test coverage.
            var candidate = doField != null ? doField(position, obj, typeof(InputActionReference), label)
                : ObjectField.DoObjectField(position, obj, typeof(InputActionReference),
                label, m_Context, SearchConstants.PickerViewFlags);
            #else
            // Before Search API was introduced we need to do this differently to be able to use different
            // presentation and modification object as well as to use the same assignment based on a new reference
            // object to guarantee that there are no side-effects.
            var candidate = doField != null ? doField(position, obj, typeof(InputActionReference), label)
                : EditorGUI.ObjectField(position, new GUIContent(label), obj, typeof(InputActionReference), true);
            #endif

            // Only assign the value was actually changed by the user.
            if (EditorGUI.EndChangeCheck() && !Equals(candidate, current))
            {
                var reference = candidate as InputActionReference;
                property.objectReferenceValue = reference ?
                    InputActionReference.Create(reference.action) : null;
            }

            EditorGUI.EndProperty();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OnGUI(position, property, label);
        }
    }
}

#endif
