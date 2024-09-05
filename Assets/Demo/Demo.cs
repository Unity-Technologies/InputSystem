using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using InputEvent = UnityEngine.InputSystem.Experimental.InputEvent;
using Usages = UnityEngine.InputSystem.Experimental.Devices.Usages;

// TODO Add support for subscription groups, it can help in making it easy to unregister a group of subscriptions

// TODO Consider SubscriptionMap, it would basically be a function (setting up subscriptions) and being a collection of subscriptions.
// TODO Note however that we can have a group of bindings given their GDC type which must be observable<T>.
// TODO Conceptualize assigning an asset to a system defined action.

// Argument: Why would you setup binding groups in assets? What you are after is contextual enabling/disabling of bound actions.

// Note that its up to the user to track their subscriptions
public class Demo : MonoBehaviour
{
    [SerializeField] public GameObject target;

    [SerializeField] public float movementSpeed = 1.0f;
    // private List<IDisposable> subscriptions = new ();
    //
    // public enum Mode
    // {
    //     All,
    //     KeyboardButtonPressAndRelease,
    //     KeyboardCompositeAxes
    // }
    //
    // private float rotateAroundY;
    // private float rotateAroundZ;
    //
    // [Tooltip("Specifies the current demo mode")]
    // [SerializeField] 
    // public Mode mode;
    //
    // [Tooltip("Specifies the binding to trigger celebration")]
    // //[SerializeField] 
    // //public BindableInput<InputEvent> celebrate; // TODO Make an editor for bindable input, causes editor issues it seems
    //
    //
    //
    // private void ChangeColor(Color color)
    // {
    //     if (target == null)
    //         return;
    //     var meshRenderer = target.GetComponent<MeshRenderer>();
    //     meshRenderer.material.color = color;
    // }
    //
    // private void OnEnable()
    // {
    //     // Bind to celebrate whatever it is triggered from
    //     /*if (celebrate == null)
    //         celebrate = ScriptableObject.CreateInstance<BindableInput<InputEvent>>();
    //     if (celebrate.bindingCount == 0)
    //     {
    //         celebrate.AddBinding(Keyboard.Space.Pressed());       
    //     }
    //     celebrate.Subscribe(static evt => Debug.Log("Celebrating!!!!!!!!!!!"), subscriptions);*/
    //     
    //     Keyboard.C.Pressed()
    //         .Subscribe( evt => ChangeColor(Color.red), subscriptions);
    //     Keyboard.C.Released()
    //         .Subscribe( evt => ChangeColor(Color.white), subscriptions);
    //     Keyboard.LeftCtrl.Released()
    //         .Subscribe( evt => ChangeColor(Color.black), subscriptions);
    //     
    //     // Constructing axes from keyboard keys (D-pad)
    //     Combine.Composite(negative: Keyboard.A, positive: Keyboard.D)
    //         .Subscribe((v) => rotateAroundY = v, subscriptions); // TODO Show-case Continuous to get callback every frame
    //     Combine.Composite(negative: Keyboard.S, positive: Keyboard.W)
    //         .Subscribe((v) => rotateAroundZ = v, subscriptions); // TODO Consider if we want Continuous or not if its an anti-pattern, depends on if aggregated or not?! 
    //     
    //     // Constructing a stick from keyboard keys (Analog) with smoothening filter property on MonoBehavior, e.g.
    //     // Combine.Composite(
    //     //  negativeX: Keyboard.LeftArrow,
    //     //  positiveX: Keyboard.RightArrow,
    //     //  negativeY: Keyboard.DownArrow,
    //     //  positiveY: Keyboard.UpArrow
    //     // ).Smooth(SmootheningAlpha, SmootheningBeta).Subscribe((v) => transform += v * Time.deltaTime);        // TODO: ScaleWithTime is no-op if relative underlying control is absolute?!
    //     
    //     // TODO Show a combined look action with gamepad and mouse that scales mouse by mouse sensitivity and scales gamepad with time
    //     
    //     // Chords
    //     Combine.Chord(Keyboard.LeftCtrl, Keyboard.Space) 
    //         .Subscribe(static evt => Debug.Log("Pressed LeftCtrl and Space simultaneously"), subscriptions);
    //
    //     // Shortcut
    //     Combine.Shortcut(Keyboard.LeftCtrl, Keyboard.C).Pressed()
    //         .Subscribe(static evt => Debug.Log("Ctrl+C"), subscriptions); // TODO Needs deferred invoke
    //
    //     // InputAction<Vector2> action;
    //     // action.Subscribe(x => );
    //
    //     // TODO Showcase switching control layout by switching bindings, either via subscribe or by using conditional bindings
    //     // Option 1: Just have all bindings on and have a condition on them effectively blocking them.
    //     // Option 2: Unsubscribe from current set of bindings, subscribe with alternative setup. This isn't super-clear from an asset perspective since it would require us to to map them all out to e.g. MonoBehavior. At the same time it could be a separate thing for this use-case, e.g.
    //     //
    //     // BindableInput input;
    //     // input.AddBinding(Keyboard.space.Pressed().Tag("Scheme1"));
    //     // input.AddBinding(Gamepad.buttonSouth.Pressed().Tag("Scheme2"));
    //     // input.WithTags("Scheme1").Subscribe(x => );
    //
    //     // TODO Showcase local multiplayer by using bindings that are conditional for an underlying control associated with a certain player
    //     // Note: E.g. assignment may happen from underlying platform or virtually, but basically we may want to use distinct sets of controls per player
    //     // If e.g. Vikings demo, we have a setup where move is e.g. keyboard or gamepad. When a player joins they are assigned a device and player identity.
    //     // The reason why this is needed is that the players share prefab and same logic.
    //     // This is easily solved as such.
    //     //
    //     // // Joining
    //     // ObservableInputEvent join;
    //     // input.AddBinding(Keyboard.space.Pressed());
    //     // input.AddBinding(Gamepad.buttonSouth.Pressed());
    //     // input.WithDevice((device) => device.paired == false).Once().Subscribe((evt, device) => device.Assign(++playerId)); // Note unpaired condition only makes callback relevant for unpaired devices, Once() unsubscribes the first time the callback executes.
    //     //
    //     // TODO For the above, replace with convenience extension:
    //     // input.JoinPlayer().Subscribe((joinEvent) => ...);
    //     //
    //     // // Player specific bindings 
    //     // ObservableInputEvent input;
    //     // input.AddBinding(Keyboard.space.Pressed());
    //     // input.AddBinding(Gamepad.buttonSouth.Pressed());
    //     // input.Player(playerId).Subscribe(evt => Celebrate(evt.playerId));
    //     //
    //     // Note that playerID would be ambiguous if the underlying controls belong to multiple devices that are assigned to different players by the platform.
    //
    //     // TODO Showcase interactive rebinding
    //
    //     // TODO Showcase symbols
    //
    //     // TODO Make an argument for enabling/disabling actions. Its part of the API.
    //
    //     // TODO Singleton scenario being called out, why don't we support that via a menu create singleton
    //
    //     // TODO Make points referring back to Williams messaging about breaking it down, explain what it means to breaking down to binding.
    //     //      Support binding and binding set in the same object.
    //
    //     // TODO Make an example with UI/game switching
    //
    //     // TODO Make an argument for code generators and why they do not really matter down the road. Include what it would mean to register all actions with the system.
    //
    //     // Meaning of any:
    //     // boolean : OR
    //     // axis    : SUM
    //     // vector2 : SUM
    //
    //     // TODO Keyboard.any.Last().Subscribe((v) => v.anyKey());
    //     
    //     // Example #1
    //     Keyboard.Escape.Pressed().Subscribe((_) => Application.Quit()); // TODO any
    //
    //     // Example #2
    //     Keyboard.Space.Pressed().Subscribe((_) => Jump()); // TODO any
    //
    //     // Example #3 
    //     Keyboard.Space.Subscribe(isDown => Fire()); // TODO any
    //     
    //     // Example #4
    //     Gamepad.leftStick.Subscribe((value) => Move(value));
    // }
    //
    // private void OnDisable()
    // {
    //     // Cancel all subscriptions
    //     foreach (var subscription in subscriptions) 
    //         subscription.Dispose();
    //     subscriptions.Clear();
    // }
    //
    // void Update()
    // {
    //     // Classic way of modifying object with sampled input and doing work in Update based on frame-sampled input.
    //     var angles = target.transform.localEulerAngles;
    //     angles.y += rotateAroundY * Time.deltaTime * 100.0f;
    //     angles.z += rotateAroundZ * Time.deltaTime * 100.0f;
    //     target.transform.localEulerAngles = angles;
    // }
    //
    // private void Jump()
    // {
    //     Debug.Log("Jump");
    // }
    //
    // private void Fire()
    // {
    //     Debug.Log("Fire");
    // }
    //
    // private void Move(Vector2 v)
    // {
    //     
    // }
    
    private float rx, ry, rz;

    private void Update()
    {
        rx = Time.deltaTime * 10.0f;
        ry = Time.deltaTime * 20.0f;
        rz = Time.deltaTime * 30.0f;
            
        if (target != null)
            target.transform.Rotate(rx, ry, rz);
    }
}
