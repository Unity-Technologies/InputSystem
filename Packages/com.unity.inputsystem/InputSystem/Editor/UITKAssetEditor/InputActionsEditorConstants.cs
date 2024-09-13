#if UNITY_EDITOR

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputActionsEditorConstants
    {
        // Paths
        public const string PackagePath = "Packages/com.unity.inputsystem";
        public const string ResourcesPath = "/InputSystem/Editor/UITKAssetEditor/PackageResources";

        /// Template names
        public const string ProjectSettingsUxml = "/InputActionsProjectSettings.uxml";
        public const string MainEditorViewNameUxml = "/InputActionsEditor.uxml";
        public const string BindingsPanelRowTemplateUxml = "/BindingPanelRowTemplate.uxml";
        public const string NameAndParametersListViewItemUxml = "/NameAndParameterListViewItemTemplate.uxml";
        public const string CompositeBindingPropertiesViewUxml = "/CompositeBindingPropertiesEditor.uxml";
        public const string CompositePartBindingPropertiesViewUxml = "/CompositePartBindingPropertiesEditor.uxml";
        public const string ControlSchemeEditorViewUxml = "/ControlSchemeEditor.uxml";
        public const string InputActionMapsTreeViewItemUxml = "/InputActionMapsTreeViewItem.uxml";
        public const string InputActionsTreeViewItemUxml = "/InputActionsTreeViewItem.uxml";

        /// Classes
        public static readonly string HiddenStyleClassName = "unity-input-actions-editor-hidden";

        public const string CompositePartAssignmentTooltip =
            "The named part of the composite that the binding is assigned to. Multiple bindings may be assigned the same part. All controls from "
            + "all bindings that are assigned the same part will collectively feed values into that part of the composite.";

        public const string CompositeTypeTooltip =
            "Type of composite. Allows changing the composite type retroactively. Doing so will modify the bindings that are part of the composite.";

        public const string InitialStateCheckTooltip =
            "Whether in the next input update after the action was enabled, the action should "
            + "immediately trigger if any of its bound controls are currently in a non-default state. "
            + "This check happens implicitly for Value actions but can be explicitly enabled for Button and Pass-Through actions.";

        public struct CommandEvents
        {
            public const string Rename = "Rename";
            public const string Delete = "Delete";
            public const string SoftDelete = "SoftDelete";
            public const string Duplicate = "Duplicate";
            public const string Copy = "Copy";
            public const string Cut = "Cut";
            public const string Paste = "Paste";
        }
    }
}

#endif
