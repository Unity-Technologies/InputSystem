using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
	internal class InputActionsEditorView : UIToolkitView<InputActionsEditorState>
	{
		private VisualTreeAsset m_MainEditorAsset;

		public InputActionsEditorView(VisualElement root, StateContainer stateContainer)
			: base(stateContainer)
		{
			m_MainEditorAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
				InputActionsEditorConstants.PackagePath + 
				InputActionsEditorConstants.ResourcesPath + 
				InputActionsEditorConstants.MainEditorViewNameUxml);

			m_MainEditorAsset.CloneTree(root);
			CreateChildView(new ActionMapsView(root, stateContainer));
			CreateChildView(new ActionsListView(root, stateContainer));
			CreateChildView(new BindingsListView(root, stateContainer));
			CreateChildView(new PropertiesView(root, stateContainer));

			// only register the state changed event here in the parent. Changes will be cascaded
			// into child views.
			stateContainer.StateChanged += OnStateChanged;

			// this is unnecessary for this view since it doesn't render anything in RedrawUI, but
			// it avoids a warning in the debug log about a missing selector.
			CreateSelector(state => state);
		}
	}
}