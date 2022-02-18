namespace UnityEngine.InputSystem.Editor
{
	internal class GlobalInputActionsConstants
	{
		public const string PackagePath = "Packages/com.unity.inputsystem";
		public const string ResourcesPath = "/InputSystem/Editor/GlobalAssetEditor/Resources";

		public const string MainEditorViewName = "/GlobalInputActionsEditor.uxml";
		public const string ActionsPanelViewName = "/ActionPanelRowTemplate.uxml";
		public const string BindingsPanelRowTemplate = "/BindingPanelRowTemplate.uxml";
		public const string NameAndParametersListViewItem = "/NameAndParameterListViewItemTemplate.uxml";

		public const string CompositePartAssignmentTooltip =
			"The named part of the composite that the binding is assigned to. Multiple bindings may be assigned the same part. All controls from "
			+ "all bindings that are assigned the same part will collectively feed values into that part of the composite.";

		public const string CompositeTypeTooltip =
			"Type of composite. Allows changing the composite type retroactively. Doing so will modify the bindings that are part of the composite.";

		public const string InitialStateCheckTooltip =
			"Whether in the next input update after the action was enabled, the action should "
			+ "immediately trigger if any of its bound controls are currently in a non-default state. "
			+ "This check happens implicitly for Value actions but can be explicitly enabled for Button and Pass-Through actions.";
	}
}
