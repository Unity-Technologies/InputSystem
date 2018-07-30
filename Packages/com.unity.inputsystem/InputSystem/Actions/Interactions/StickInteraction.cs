////REVIEW: this should not have to cast to InputControl<Vector2>; interactions should have the same
////        ReadValue<TValue>() API available to them that action callbacks do; this way this interaction
////        here, for example, will also work with composites

namespace UnityEngine.Experimental.Input.Interactions
{
    /// <summary>
    /// Starts when stick leaves deadzone, performs while stick moves outside
    /// of deadzone, cancels when stick goes back into deadzone.
    /// </summary>
    public class StickInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            var stick = context.control as InputControl<Vector2>;
            if (stick == null)
                return;

            var value = stick.ReadValue();
            if (value.x > 0 && value.y > 0)
            {
                if (!context.isStarted)
                    context.Started();
                else
                    context.PerformedAndStayStarted();
            }
            else if (context.isStarted)
            {
                // Went back to below deadzone.
                context.Cancelled();
            }
        }

        public void Reset()
        {
        }
    }
}
