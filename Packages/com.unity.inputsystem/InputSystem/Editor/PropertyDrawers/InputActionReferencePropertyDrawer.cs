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
            AssetSearchProviders.CreateDefaultProvider(),
            AssetSearchProviders.CreateProjectWideInputActionReferenceSearchProvider(),
        }, string.Empty, SearchConstants.PickerSearchFlags);

        private void OnValidate()
        {
            Debug.Log("OnValidate editor");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField.DoObjectField(position, property, typeof(InputActionReference), label,
                m_Context, SearchConstants.PickerViewFlags);

#if false // TODO Remove this code before final version/merge
            // This is debug code to simplify evaluate desynchronized InputActionReference instances
            var buttonRect = position;
            var popupStyle = Styles.popup;
            buttonRect.yMin += popupStyle.margin.top + 1f;
            buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right + 5f;
            buttonRect.height = EditorGUIUtility.singleLineHeight;
            buttonRect.x += 75;

            if (GUI.Button(buttonRect, "D"))
            {
                var reference = property.objectReferenceValue as InputActionReference;
                if (reference != null)
                    Debug.Log(label + ": " + property.objectReferenceValue + ", " + reference.action);
                else
                    Debug.Log(label + ": null");
            }
#endif
        }

        static class Styles
        {
            public static readonly GUIStyle popup = new GUIStyle("PaneOptions") { imagePosition = ImagePosition.ImageOnly };
        }

        // Advanced Picker in UITk
        // Kept uncommented as a future reference for modernizing property drawers.
        // We can only use this one if we rewrite InputProperty property drawer for UITK as well since it would
        // render UI where InputActionReferecePropertyDrawer is a sub interface.
        /*public override UIElements.VisualElement CreatePropertyGUI(SerializedProperty prop)
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

            // Align width in Inspector - note that ObjectField.alignedFieldUssClassName was made public in 2021.2.7f1
            #if UNITY_2021_3_OR_NEWER
            obj.AddToClassList(ObjectField.alignedFieldUssClassName);
            #else
            obj.AddToClassList(ObjectField.ussClassName + "__aligned");
            #endif

            return obj;
        }*/
    }
}

#endif
