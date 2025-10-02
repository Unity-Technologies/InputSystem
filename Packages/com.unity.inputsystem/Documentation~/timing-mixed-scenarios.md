---
uid: input-system-timing-mixed
---
# Mixed timing scenarios with fixed and dynamic input

There are some situations where you might set the Update Mode **process in Dynamic Update** even when using input code in `FixedUpdate`, to minimize input latency, as described in the [previous section](xref:input-system-timing-optimize-fixed).

In this situation, for discrete events you must ensure that you use  [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) or [`WasReleasedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasReleasedThisFrame*) in `Update`, and pass through a variable to your `FixedUpdate` code to indicate the event happened. There may still be some latency between the frame in which the event occurred, and the next `FixedUpdate` call.

For example:

```c#
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleScript : MonoBehaviour
{
    InputAction jumpAction;
    bool jumpPressed;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void Update()
    {
        // read discrete jump pressed event here:
        if (jumpAction.WasPressedThisFrame())
        {
            // set this variable to true, for use in FixedUpdate
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        if (jumpPressed)
        {
            // apply jump physics here

            // set the variable to false so that the jump pressed physics are only applied once
            jumpPressed = false;
        }
    }
}
```

## Minimum latency in mixed timing scenarios

A technique to give the user the feel of absolute minimum latency while still using `FixedUpdate` is to respond as fast as possible in `Update` giving some visual feedback, but also respond to that same input in `FixedUpdate` for your physics system code. For example, you could display the start of a "jump" animation immediately in `Update`, while applying physics to correspond with the "jump" animation in the next available `FixedUpdate` which might come slightly later.

In this scenario, set your Update Mode **Process events in Dynamic Update** which gives you the fastest response in your `Update` call. However for the reasons mentioned in the previous section, this might mean you miss discrete events if you use methods like [`WasPressedThisFrame`](xref:UnityEngine.InputSystem.InputAction.WasPressedThisFrame*) in your `FixedUpdate` call. To avoid this problem, use a variable to pass through the pressed/released state of the discrete event from the event handler to your FixedUpdate call, and then clear it once your FixedUpdate code has acted on it. For example:

```c#
using UnityEngine;
using UnityEngine.InputSystem;

public class ExampleScript : MonoBehaviour
{
    InputAction jumpAction;
    bool jumpPressed;

    private void Start()
    {
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void Update()
    {
        // at high FPS, it’s fastest to read actions here:

        // read discrete jump pressed event here:
        if (jumpAction.WasPressedThisFrame())
        {
            // start jump animation here

            // set this variable to true, for use in FixedUpdate
            jumpPressed = true;

        }
    }

    void FixedUpdate()
    {
        if (jumpPressed)
        {
            // apply jump physics here

            // set the variable to false so that the jump pressed physics are only applied once
            jumpPressed = false;
        }
    }
}
```
