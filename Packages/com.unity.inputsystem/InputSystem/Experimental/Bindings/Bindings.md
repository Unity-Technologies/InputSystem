# Bindings

Input bindings involves forwarding, routing or transforming input data. 
Input bindings may be defined directly in code in the player or defined in 
the editor and stored in assets.

## Binding objects

- Any type implementing `IObservable<T>` and `IObservableInput<T>` may be used as an input binding. See table XXX for a comprehensive list of all 
supported operations.

## Referencing bindings

- Bindings may be serialized and configured via Inspector properties for `MonoBehaviors`, `ScriptableObject` and other 
- types derived from `Unity.Object` by declaring a serialized field of type 
`InputBinding<T>`. `InputBinding<T>` is capable of referencing `Unity.Object` objects that implement `IObservable<T>`
as well as regular objects implementing `IObservable<T>`.

## Extra work driven by limitations with serialization system

- Unity requires 