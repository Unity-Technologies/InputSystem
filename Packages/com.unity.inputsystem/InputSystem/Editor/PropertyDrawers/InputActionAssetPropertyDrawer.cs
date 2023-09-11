using System.Linq;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    // [CustomPropertyDrawer(typeof(InputActionAsset))]
    [CustomPropertyDrawer(typeof(InputActionAssetProperty))]
    public class InputActionAssetPropertyDrawer : PropertyDrawer
    {
        //TODO: refactor into enum
        static readonly string[] k_ActionsTypeOptions = new[] { "Project-Wide Actions", "Actions Asset" };
        [SerializeField] int m_SelectedActionsTypeIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var isAssetProjectWideActions = property.FindPropertyRelative("m_IsAssetProjectWideActions").boolValue;
            m_SelectedActionsTypeIndex = isAssetProjectWideActions ? 0 : 1;

            var selected = EditorGUILayout.Popup(new GUIContent("Actions"), m_SelectedActionsTypeIndex, k_ActionsTypeOptions);

            // Update property in case there's a change in the dropdown popup
            if (m_SelectedActionsTypeIndex != selected)
            {
                if (selected == 0)
                {
                    //TODO: check if we can cache this
                    property.FindPropertyRelative("m_IsAssetProjectWideActions").boolValue = true;
                    property.FindPropertyRelative("m_ActionsAsset").objectReferenceValue = ProjectWideActionsAsset.GetOrCreate();
                }
                else
                {
                    property.FindPropertyRelative("m_IsAssetProjectWideActions").boolValue = false;
                    // Reset the actions asset to null if the first time user selects the "Actions Asset" option
                    property.FindPropertyRelative("m_ActionsAsset").objectReferenceValue = null;
                }

                m_SelectedActionsTypeIndex = selected;
            }

            // Show UI elements depending on the option selected
            if (m_SelectedActionsTypeIndex == 1)
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("m_ActionsAsset"), label, true);
            }

            EditorGUI.EndProperty();
        }
    }
}
