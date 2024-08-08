Overview of operations

| Operation              | Output             | Inputs                | Kind        | Comment                                                                                                                                                 |
|------------------------|--------------------|-----------------------|-------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| DistinctUntilChanged   | InteractionEvent   | Source: T             | Filter      | Discards element if not different from previous element.                                                                                                |
| Press                  | InteractionEvent   | Trigger: bool         | Interaction |                                                                                                                                                         |
| Release                | InteractionEvent   | Trigger: bool         | Interaction |                                                                                                                                                         |
| Chord<T>               | bool               |                       |             | Basically Edge<T1, T2>?                                                                                                                                 |
| CombineLatest<T1, T2>  | ValueTuple<T1, T2> | First: T1, Second: T2 | Processor   | Combines multiple streams into a new composite stream.                                                                                                  |
| Composite2D<T>         | T                  | First: T, Second: T   | Processor   | Might not be required since CombineLatest yields same behavior. Composite is a simplified specialization of CombineLatest with homogeneous input types. |
| Composite3D<T>         | T                  | First: T, Second: T   | Processor   | Might not be required since CombineLatest yields same behavior. Composite is a simplified specialization of CombineLatest with homogeneous input types. |
| Composite3D<T>         | T                  | First: T, Second: T   | Processor   | Might not be required since CombineLatest yields same behavior. Composite is a simplified specialization of CombineLatest with homogeneous input types. |

// Frame-based input
Gamepad.leftStick.DistinctUntilChanged().Last().Subscribe(...);
Gamepad.buttonSouth.Pressed().First().Subscribe(...);
Gamepad.leftTrigger.Pressed(pressThreshold: 0.7f, releaseThreshold: 0.4f).Subscribe(...);
Combine.Chord(Gamepad.buttonSouth, Gamepad.buttonWest).Last().Subscribe(...)
Mouse.delta.Sum().Subscribe(...);

// Aliases
Last() may be aliased OncePerFrame()

TODO:
withLatestFrom
sum/reduce

Rebinding means modifying sources which are leaf streams. Bindings to be rebound may exist anywhere in the dependency graph.

Reducing code bloat

Code bloat may be reduced by relying on code generation based off attributes. E.g.

```cs
[InputOperation(name = "Pressed")]
public partial struct PressedOperation
{
    [InputOperationInput(slot = 0)] private TSource source;
    
    private partial struct State
    {
        bool previousValue;
    }
    
    [InputOperationSink(slot = 0)]
    public void OnNext(bool value, ref State state)
    {
        if (state.previousValue == value) 
            return;
        if (value) 
            state.OnNext(new InputEvent());
        state.previousValue = value;
    }
}
```