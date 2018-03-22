////REVIEW: rename to IInputActionGesture? (and "modifier" to "gesture" in general)
////        or rename processors to "control processors" and modifiers to "action processors"?

////TODO: modifiers should be able to not just control phase flow but also what value is reported through the action

////REVIEW: what about putting an instance of one of these on every resolved control instead of sharing it between all controls resolved from a binding?

namespace ISX
{
    // By default, actions will start when a source control leaves its default state
    // and will be completed when the control goes back to that state. Modifiers can customize
    // this and also implement logic that signals cancellations (which the default logic never
    // triggers).
    // Modifiers can be stateful and mutate state over time.
    public interface IInputActionModifier
    {
        void Process(ref InputAction.ModifierContext context);
        void Reset();
    }
}
