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
        //static readonly string[] k_ActionsTypeOptions = new[] { "Project-Wide Actions", "Actions Asset" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var actions = InputSystem.actions;
            var hasProjectWideActions = !ReferenceEquals(actions, null);
            var current = property.objectReferenceValue as InputActionAsset;
            var currentIsProjectWideActions = (hasProjectWideActions) && current == InputSystem.actions;
            var currentSelectedAssetOptionIndex = (currentIsProjectWideActions) ? AssetOptions.ProjectWideActions : AssetOptions.ActionsAsset;

            var selected = AssetOptions.ActionsAsset;
            if (hasProjectWideActions)
            {
                EditorGUILayout.BeginHorizontal();

                // Draw dropdown menu to select between using project-wide actions or an action asset
                selected = (AssetOptions)EditorGUILayout.EnumPopup(label, currentSelectedAssetOptionIndex);

                // Draw button to edit the asset
                DoOpenAssetButtonUI(property, selected);

                EditorGUILayout.EndHorizontal();
            }

            // Update property in case there's a change in the dropdown popup
            if (currentSelectedAssetOptionIndex != selected)
            {
                if (selected == AssetOptions.ProjectWideActions)
                    property.objectReferenceValue = actions;
                else
                    property.objectReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            }

            // Show relevant UI elements depending on the option selected
            // In case project-wide actions are selected, the object picker is not shown.
            if (selected == AssetOptions.ActionsAsset)
            {
                if (hasProjectWideActions)
                    ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(property, new GUIContent("Actions Asset") , true);
                if (hasProjectWideActions)
                    --EditorGUI.indentLevel;
            }

            EditorGUI.EndProperty();
        }

        static void DoOpenAssetButtonUI(SerializedProperty property, AssetOptions selected)
        {
            if (selected == AssetOptions.ProjectWideActions)
            {
                GUIContent buttonText = new GUIContent("Open");
                Vector2 buttonSize = GUI.skin.button.CalcSize(buttonText);
                // Create a new Rect with the calculated size
                // Rect buttonRect = new Rect(position.x, position.y, buttonSize.x, buttonSize.y);
                if (GUILayout.Button(buttonText, GUILayout.Width(buttonSize.x)))
                    SettingsService.OpenProjectSettings(InputActionsEditorSettingsProvider.kSettingsPath);
            }
        }
    }
}

#endif
