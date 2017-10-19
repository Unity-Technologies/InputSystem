namespace ISX
{
    // Performs the action if the 2D control is pressed and then released within
    // 'minDistance' in the direction between 'minAngle' and 'maxAngle'.
    public class SwipeModifier : IInputActionModifier
    {
        public float minAngle;
        public float maxAngle;
        public float minDistance;

        public void Process(ref InputAction.ModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public void Reset()
        {
            throw new System.NotImplementedException();
        }
    }
}
