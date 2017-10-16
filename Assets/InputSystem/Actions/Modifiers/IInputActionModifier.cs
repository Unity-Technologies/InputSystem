////TODO: allow variant to be attached to modifiers

namespace ISX
{
    // By default, actions will start when a source control leaves its default state
    // and will be completed when the control goes back to that state. Modifiers can customize
    // this and also implement logic that signals cancellations (which the default logic never
    // triggers).
    // Modifiers can be stateful and mutate state over time.
    public interface IInputActionModifier
    {
        void Process(ref InputAction.Context context);
        void Reset();
    }
}
