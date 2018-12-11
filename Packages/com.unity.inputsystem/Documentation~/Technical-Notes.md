This page is for collecting notes on specific technical issues.

# Action Polling vs Callbacks

```C#
    if (fireAction.wasPerformed)
        Fire();

    // vs.

    fireAction.performed += ctx => Fire();
```

Polling has two big drawbacks:

* Storing up all the state in actions relevant to when a control was triggered (when was it triggered? which control triggered it? which modifier was involved? etc.) gets complicated quickly and is prone to storing state that is never needed. The callback-based approach can lazily fetch information from the callback context when needed and can thus cheaply add all kinds of context-dependent information.
* Actions are able to observe and perform based on every single state change in the system -- even if those state changes fall into the same frame. A polling-based API will only be able to observe the very latest state change.

However, polling has one huge advantage:

* It gives a natural sync point to allow the system to work asynchronously. I.e. if the user has to do `.wasPressed` we know that this is exactly the point by which we have to have the current input system update C# job completing. With callbacks, the question of when to sync becomes much harder. In the synchronous version, callbacks fire immediately. Moving it to async will unavoidably delay calls but then the questions becomes "to when?"

Also, callbacks are harder to manage for users than polling. You have to unregister and they are prone to keeping objects alive that really should have been killed. Overall, they require more advanced programming than a polling-based approach. And not only that, they also give no control to the user over when processing happens.

## Solution

There is a third alternative: event-based delivery. So, it seems that the ideal solution is an API that has a simple polling-based front-end and then in addition has a front-end that is event-based and delivers all activity on an action but at a point where the user deems it right to do so. This also brings back a natural sync point for job activity.

# Delta Controls

Delta controls (like e.g. mouse motion deltas) turn out to be unexpectedly tricky beasts. The two unusual aspects about them is that a) they want to *accumulate* (if the app gets sent two mouse deltas from the system and they end up in the same input update, you want them to add up and not just see only the second delta) and b) they want to reset back to zero between input updates. Since mice are usually sampled at higher frequencies than GFX refreshes and since deltas are commonly used to drive sensitive things such as mouse look, handling this correctly actually matters.

My first thought was: let's not complicate the state system and deal with the problem in the code that generates mouse state events (i.e. the platform layer). However, this turns out to be surprisingly tricky to implement correctly. The problem is that knowing when to accumulate and when to reset is intricately tied to which mouse state events go into which input updates -- something that is hard for the OS layer to know. Simply accumulating during a frame and resetting between frames will lead to correct behavior for dynamic updates (which span an entire frame) but will not lead to correct behavior for other update types.

This led me to conclude that these controls are best supported directly in the state and control system -- which is unfortunate as accumulation and resetting not only make state updates more complicated but also complicate state change detection which actions rely on.

# Haptics/Output

There's two principal designs allowed for by the system:

1. A haptics interface on a device creates state events against output controls and queues them. When the system processes those events, they result in an IOCTL on the device.
2. A haptics interface on a device immediately issues an IOCTL on the device.

Both approaches have their pros and cons.

Approach 1 has the advantage of being visible in the control system and in the event stream. This means that actions will work with output controls, too, and that remoting will work out of the box (i.e. haptic events in the player will be visible in the input debugger and it will be possible to trigger haptics remotely). However, it has the principal disadvantage of introducing a delay between a haptics interface call and an actual IOCTL being issued on the device.

Approach 2 has the advantage of being immediate. Also, no extra state is kept in the system for output. Haptics won't be visible to actions but that does not seem like a big deal as monitoring output values seems of limited use (there's no way the system can guarantee an output value *actually* reflects the value on the device). Haptics not being visible in the event stream and in remoting is a bigger deal.
