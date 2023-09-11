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
        static readonly string[] k_ActionsTypeOptions = new[] { "Project-Wide Actions", "Actions Asset" };
        [SerializeField] int m_SelectedActionsTypeIndex;
        SerializedProperty m_IsAssetProjectWideActionsProperty;
        SerializedProperty m_ActionsAssetProperty;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            m_IsAssetProjectWideActionsProperty = property.FindPropertyRelative("m_IsAssetProjectWideActions");
            m_ActionsAssetProperty = property.FindPropertyRelative("m_ActionsAsset");
            m_SelectedActionsTypeIndex = m_IsAssetProjectWideActionsProperty.boolValue ? 0 : 1;

            var selected = EditorGUILayout.Popup(new GUIContent("Actions"), m_SelectedActionsTypeIndex, k_ActionsTypeOptions);

            // Update property in case there's a change in the dropdown popup
            if (m_SelectedActionsTypeIndex != selected)
            {
                UpdatePropertiesWithSelectedOption(selected);
            }

            // Show UI elements depending on the option selected
            if (m_SelectedActionsTypeIndex == 1)
            {
                EditorGUILayout.PropertyField(m_ActionsAssetProperty, label, true);
            }

            EditorGUI.EndProperty();
        }

        void UpdatePropertiesWithSelectedOption(int selected)
        {
            if (selected == 0)
            {
                m_IsAssetProjectWideActionsProperty.boolValue = true;
                m_ActionsAssetProperty.objectReferenceValue = ProjectWideActionsAsset.GetOrCreate();
            }
            else
            {
                m_IsAssetProjectWideActionsProperty.boolValue = false;
                // Reset the actions asset to null if the first time user selects the "Actions Asset" option
                m_ActionsAssetProperty.objectReferenceValue = null;
            }
            m_SelectedActionsTypeIndex = selected;
        }
    }
}
