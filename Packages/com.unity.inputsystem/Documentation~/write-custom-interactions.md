---
uid: input-system-custom-interactions
---

# Write custom interactions

You can write a custom Interaction and use it the same way you use built-in Interactions. 

To write a custom Interaction:

**1.** Add a class that implements the [`IInputInteraction`](../api/UnityEngine.InputSystem.IInputInteraction.html) interface. For example:

```CSharp
// Interaction which performs when you quickly move an
// axis all the way from extreme to the other.
public class MyExampleInteraction : IInputInteraction
{
    public float duration = 0.2;

    void Process(ref InputInteractionContext context)
    {
        if (context.timerHasExpired)
        {
            context.Canceled();
            return;
        }

        switch (context.phase)
        {
            case InputActionPhase.Waiting:
                if (context.Control.ReadValue<float>() == 1)
                {
                    context.Started();
                    context.SetTimeout(duration);
                }
                break;

            case InputActionPhase.Started:
                if (context.Control.ReadValue<float>() == -1)
                    context.Performed();
                break;
        }
    }

    // Unlike processors, Interactions can be stateful, meaning that you can keep a
    // local state that changes over time as input is received. The system might
    // invoke the Reset() method to ask Interactions to reset to the local state
    // at certain points.
    void Reset()
    {
    }
}
```

**2.** Add your Interaction to the Input System. Call the following method in your initialization code:

```CSharp
InputSystem.RegisterInteraction<MyExampleInteraction>();
```

Your new Interaction is now available in the [Input Action Asset Editor window](action-assets.md).

Alternatively, you can add it this way:

```CSharp
var Action = new InputAction(Interactions: "MyExample(duration=0.5)");
```
