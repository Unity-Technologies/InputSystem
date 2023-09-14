#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Enum describing the asset option selected.
    /// </summary>
    enum AssetOptions
    {
        [InspectorName("Project-Wide Actions")]
        ProjectWideActions,
        ActionsAsset
    }

    /// <summary>
    /// Property drawer for <see cref="InputActionAsset"/>.
    /// </summary>
    /// This property drawer allows for choosing the action asset field as either project-wide actions or
    /// a user created actions asset
    [CustomPropertyDrawer(typeof(InputActionAsset))]
    internal class InputActionAssetDrawer : PropertyDrawer
    {
        static readonly string[] k_ActionsTypeOptions = new[] { "Project-Wide Actions", "Actions Asset" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var isAssetProjectWideActions = IsAssetProjectWideActions(property);
            var selectedAssetOptionIndex = isAssetProjectWideActions ? AssetOptions.ProjectWideActions : AssetOptions.ActionsAsset;

            // Draw dropdown menu to select between using project-wide actions or an action asset
            var selected = (AssetOptions)EditorGUILayout.EnumPopup(new GUIContent("Actions"), selectedAssetOptionIndex);

            // Update property in case there's a change in the dropdown popup
            if (selectedAssetOptionIndex != selected)
            {
                UpdatePropertyWithSelectedOption(property, selected);
                selectedAssetOptionIndex = selected;
            }

            // Show relevant UI elements depending on the option selected
            // In case project-wide actions are selected, the object picker is not shown.
            if (selectedAssetOptionIndex == AssetOptions.ActionsAsset)
            {
                EditorGUILayout.PropertyField(property, label, true);
            }

            EditorGUI.EndProperty();
        }

        static void UpdatePropertyWithSelectedOption(SerializedProperty assetProperty, AssetOptions selected)
        {
            if (selected == AssetOptions.ProjectWideActions)
            {
                assetProperty.objectReferenceValue = ProjectWideActionsAsset.GetOrCreate();
            }
            else
            {
                // Reset the actions asset to null if the first time user selects the "Actions Asset" option
                assetProperty.objectReferenceValue = null;
            }

            assetProperty.serializedObject.ApplyModifiedProperties();
        }

        static bool IsAssetProjectWideActions(SerializedProperty property)
        {
            var isAssetProjectWideActions = false;

            // Check if the property InputActionAsset name is the same as project-wide actions to determine if
            // project-wide actions are set
            if (property.objectReferenceValue != null)
            {
                var asset = (InputActionAsset)property.objectReferenceValue;
                isAssetProjectWideActions = asset?.name == ProjectWideActionsAsset.kAssetName;
            }

            return isAssetProjectWideActions;
        }
    }
}

#endif
