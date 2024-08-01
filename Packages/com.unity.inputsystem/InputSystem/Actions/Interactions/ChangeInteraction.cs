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
            var magnitude = context.ComputeMagnitude();
            if (IsActuated(magnitude))
            {
                if (context.isWaiting)
                {
                    context.Started();
                }
                else
                {
                    context.PerformedAndStayPerformed();
                }
            }
            else
            {
                context.Canceled();
            }
        }

        public void Reset()
        {
        }
    }
}
