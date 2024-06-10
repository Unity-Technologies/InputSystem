# Experimental

## Design goals
- No fragile string-based or name convention APIs that leads to run-time failures.
- Strong typing, also for abstract input (actions).
- Better or equal performance to current Input System for key use cases.
- No enforced frame concept to simplify concurrent processing.

## Design decisions
- Provide fluent lazily evaluated binding API inspired by Reactive Extensions to express abstract actions in code.
- Make all user-facing types strongly typed and use this type information to provide guidance and prevent human error at compile-time or in edit-mode by utilizing IntelliSense and editing constraints.
- Separate events and signals.
- Adapt design for integration with DOTS Job System and Burst compiler.

## Use-cases in scope for this proposal
- Consume input from a value control of a specific device directly.
- Consume input from a value control of any device of specific device-family directly.
- Consume input from a an abstract value control 

## Not in scope
- Backend implementation. Multiple proof-of-concepts exist TBD and TBD and a separate RFC is needed to adress this area.

## Reactive patterns and DOTS
Reactive patterns and DOTS do not fit well together:
- Job system do not allow managed non-blittable types.
- Reactive pattern defers decision to directly invoke or defer intermediate data.

Hence the following compromise is suggested:
- Use a custom interface instead of IObservable/IObserver that allow for batch processing.
- 

## Stream design

Alternative 1:
- Local (thread specific) data is reported without synchronization into simple array buffers.
- Shared asynchronous data is reported into SPSC queues per registered consumer context. This implies async processing cost but reduces synchronization overhead compared to MPMC setup. 
- Segment is allocated with a capacity reflecting historical observations with outer loop control.
- Linked segments containing data.
- Writers lock segment for writing but associated stream may be concurrently read.
- Consumers do not deque but report back on consumed sample ids. 
```cs
// Reactive
public IDisposable Subscribe(Context context, IObserver<T> observer)
    
// Directed    
public Subscription Subscribe(Context context, Stream<T> target)
 ```

## TODO
- Implement proper emulation of stream concept:
  - Lock free semantics
  - Time-based view
  - Serial/Timestamp stream
- Adapt streams on managed side to utilize Burst and Job systems.
- Consider if there are any benefits from potential caching of derivatives and how we may safely cache them. This is essential to avoid duplicated processing.

```cs
// Hard-coded bindings directly accessing specific device
BindableInput<T> jump = Gamepad.buttonSouth.pressed() | Keyboard.keys[space].pressed();
```

Relevant references:
Microsoft .NET Unsafe docs on function pointers: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#function-pointers
Simple approach to reference managed types via handle https://www.jacksondunstan.com/articles/5397
Old comparison of interfaces and delegates: https://www.jacksondunstan.com/articles/3043
Another old comparison of interfaces and delegates: https://www.codeproject.com/Articles/468967/Interfaces-vs-Delegates
Caching delegates: https://www.mattgibson.dev/blog/csharp-delegates-memory-summary#:~:text=You%20can%20manually%20cache%20a,time%20you%20use%20the%20delegate.&text=Having%20a%20private%20member%20(static,times%20without%20needing%20new%20allocations.
Reactive .NET https://introtorx.com/chapters/key-types