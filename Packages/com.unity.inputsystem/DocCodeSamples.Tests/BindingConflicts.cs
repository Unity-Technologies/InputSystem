using UnityEngine;
using UnityEngine.InputSystem;

class BindingConflictsExample
{
    public void Example()
    {
        #region bindingConflicts
        // Create two actions in the same map.
        var map = new InputActionMap();
        var bAction = map.AddAction("B");
        var shiftbAction = map.AddAction("ShiftB");

        // Bind one of the actions to 'B' and the other to 'SHIFT+B'.
        bAction.AddBinding("<Keyboard>/b");
        shiftbAction.AddCompositeBinding("OneModifier")
            .With("Modifier", "<Keyboard>/shift")
            .With("Binding", "<Keyboard>/b");

        // Print something to the console when the actions are triggered.
        bAction.performed += _ => Debug.Log("B action performed");
        shiftbAction.performed += _ => Debug.Log("SHIFT+B action performed");

        // Start listening to input.
        map.Enable();

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

        // Now, let's assume the left shift key on the keyboard is pressed (here, we manually
        // press it by queueing a state event for the control).
        InputSystem.QueueDeltaStateEvent(keyboard.leftShiftKey, 1f);
        InputSystem.Update();

        // And then the B is pressed. This is a valid input for both
        // bAction as well as shiftbAction.
        //
        // What will happen now is that shiftbAction will do its processing first. In response,
        // it will *perform* the action (That is, we see the `performed` callback being invoked) and
        // thus "consume" the input. bAction will stay silent as it will in turn be skipped over.
        InputSystem.QueueDeltaStateEvent(keyboard.bKey, 1f);
        InputSystem.Update();
        #endregion
    }
}
