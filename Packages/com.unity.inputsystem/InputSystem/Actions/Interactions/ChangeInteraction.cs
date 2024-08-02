using System.ComponentModel;

namespace UnityEngine.InputSystem.Interactions
{
    [DisplayName("Value Change")]
    public class ChangeInteraction : IInputInteraction
    {
        internal static bool IsActuated(float magnitude, float threshold = 0)
        {
            if (magnitude < 0)
                return true;
            if (Mathf.Approximately(threshold, 0))
                return magnitude > 0;
            return magnitude >= threshold;
        }

        public void Process(ref InputInteractionContext context)
        {
            if (context.action.type == InputActionType.PassThrough)
            {
                // Don't check for actuation on pass-through actions and perform for every value changed
                context.PerformedAndStayStarted();
                return;
            }

            var magnitude = context.ComputeMagnitude();
            if (IsActuated(magnitude))
            {
                if (context.isWaiting)
                {
                    context.Started();
                    context.PerformedAndStayPerformed();
                }
                else
                {
                    context.PerformedAndStayPerformed();
                }
            }
            else
            {
                if (context.isStarted || context.isPerformed)
                {
                    // Cancel the action if it was started/performed and put it back to Waiting
                    context.Canceled();
                }
            }
        }

        public void Reset()
        {
        }
    }
}
