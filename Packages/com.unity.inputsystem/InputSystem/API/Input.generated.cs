namespace UnityEngine.InputSystem.HighLevelAPI
{
	// this class to be auto-generated, or better yet, source generated. Provides strongly typed access to
	// input action values
	// NOTE: Obviously these property names have to be unique. If there is a collision, the name could be prepended
	// with the action map name.
	public static partial class Input
	{
		public static Input<Vector2> move => new Input<Vector2>(globalActions.FindAction("Gameplay/Move"));

		public static Input<float> join => new Input<float>(globalActions.FindAction("Player/Join"));

		public static Input<Vector2> navigate => new Input<Vector2>(globalActions.FindAction("UI/Navigate"));
	}
}