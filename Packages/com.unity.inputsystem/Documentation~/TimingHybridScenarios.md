# Hybrid scenarios requiring fixed and variable rate input

In some scenarios where the frame rate is running faster than the physics system, you might want to respond to some types of user input as fast as possible (in Update) but also have a need to respond to discrete events in FixedUpdate. For example displaying a “punch” animation immediately, while applying physics to correspond with those graphics in the next available FixedUpdate which might come slightly later.

In this scenario, set your Update Mode **Process events in Dynamic Update** which gives you the fastest response in your `Update` call. However for the reasons mentioned in the previous section, this might mean you miss discrete events if you use methods like `WasPressedThisFrame` in your `FixedUpdate` call. To avoid this problem, use a variable to pass through the pressed/released state of the discrete event from the event handler to your FixedUpdate call, and then clear it once your FixedUpdate code has acted on it. For example:

```
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