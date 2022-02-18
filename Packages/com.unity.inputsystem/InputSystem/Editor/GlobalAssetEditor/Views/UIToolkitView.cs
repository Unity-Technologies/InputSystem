namespace UnityEngine.InputSystem.Editor
{
	internal abstract class UIToolkitView
	{
		protected readonly StateContainer m_StateContainer;

		protected UIToolkitView(StateContainer stateContainer)
		{
			m_StateContainer = stateContainer;
			m_StateContainer.StateChanged += OnStateChanged;
		}

		public void OnStateChanged(GlobalInputActionsEditorState state)
		{
			// TODO: Implement selectors on the base view so that views only re-render when some state they're
			// interested in has changed.
			CreateUI(state);
		}

		public void Dispatch(Command command)
		{
			m_StateContainer.Dispatch(command);
		}

		public abstract void CreateUI(GlobalInputActionsEditorState state);

		public virtual void ClearUI()
		{

		}

		public void Dispose()
		{
			m_StateContainer.StateChanged -= OnStateChanged;
		}
	}
}