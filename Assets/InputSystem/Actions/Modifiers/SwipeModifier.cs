namespace ISX
{
    // Requires a directional swipe from the initial contact point in order
    // to go to consider the action to be performed.
    public class SwipeModifier : IInputActionModifier
    {
        public float angle;
        public float minDistance;

        public void Process(ref InputAction.Context context)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
