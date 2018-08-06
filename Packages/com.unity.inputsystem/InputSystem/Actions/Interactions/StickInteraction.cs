////TODO: add ability to filter out "bounce"; when user lets go of stick, on some gamepads the stick
////      will bounce beyond the deadzone in the opposite direction; in many games this is noticeable with
////      problems of the characters ending up facing the wrong way and such; we should be able to filter
////      bounces out automatically by tracing value histories

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
            var value = context.ReadValue<Vector2>();
            if (value.sqrMagnitude > 0)
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
