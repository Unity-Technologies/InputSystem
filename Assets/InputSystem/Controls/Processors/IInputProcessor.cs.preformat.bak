namespace ISX
{
    // A processor that conditions values.
    // InputControls can have stacks of processors assigned to them.
    // IMPORTANT: Processors can NOT be stateful. If you need processing that requires keeping
    //            mutating state over time, use InputActions. All mutable state needs to be
    //            kept in the central state buffers.
    public interface IInputProcessor<TValue>
    {
        TValue Process(TValue value);
    }
}