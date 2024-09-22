# Bindings

Input bindings involves forwarding, routing or transforming input data. 
Input bindings may be defined directly in code in the player or defined in 
the editor and stored in assets.

## Binding objects

- Any type implementing `IObservable<T>` and `IObservableInput<T>` may be used as an input binding. See table XXX for a comprehensive list of all 
supported operations.

## Referencing bindings

- Bindings may be serialized with `MonoBehaviors`, `ScriptableObject` and other `Unity.Object` derivatives via 
`InputBinding<T>`. `InputBinding<T>` is capable of referencing `Unity.Object` objects that implement `IObservable<T>`
as well as regular objects implementing `IObservable<T>`.

## Extra work driven by limitations with serialization system

- Unity requires 