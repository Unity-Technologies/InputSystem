namespace UnityEngine.Experimental.Input.Interactions
{
    /// <summary>
    /// An interaction that will perform an action on any value change on any
    /// control.
    /// </summary>
    /// <remarks>
    /// This interaction can be useful when an action simply wants to listen for any
    /// activity on its bound controls.
    /// </remarks>
    public class PassthroughInteraction : IInputInteraction
    {
        public void Process(ref InputInteractionContext context)
        {
            if (context.continuous)
                context.PerformedAndStayPerformed();
            else
                context.PerformedAndGoBackToWaiting();
        }

        public void Reset()
        {
        }
    }
}
