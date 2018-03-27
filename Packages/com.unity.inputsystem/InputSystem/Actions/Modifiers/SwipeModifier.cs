////REVIEW: While chaining allows to combine this with an on/off button, I think it
////        would be far better to be able to address a binding such that it covers
////        all relevant controls so that the SwipeModifier itself can work off of
////        both a ButtonControl and a Vector2Control

namespace UnityEngine.Experimental.Input.Modifiers
{
    // Performs the action if the 2D control is pressed and then released within
    // 'minDistance' in the direction between 'minAngle' and 'maxAngle'.
    public class SwipeModifier : IInputBindingModifier
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
