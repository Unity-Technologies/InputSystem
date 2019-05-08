    ////WIP

# Future / Planned Features

This page details features that are not yet implemented but have been planned for future versions of Unity.

## More Extensive Device Support

>STATUS: Not started.

The currently support set of devices is fairly minimal. Post-1.0 we are planning to significantly broaden our support for devices.

## Multi-Mouse and Multi-Display Support

>STATUS: Not started.

...

## Player Loop Throttling Based On Input Activity

>STATUS: Under investigation.

>Forum thread: https://forum.unity.com/threads/how-far-are-we-away-from-framerate-renderloop-independent-input.544499/

For mobile applications with mostly static UIs that change only in response to user interaction (e.g. user touching UI elements), it can very desirable to run applications at very low frame-rates and only temporarily throttle up while user interaction is processed. This feature will require being able to initiate player loop updates based on input events.

## Profiler Integration

>STATUS: Waiting for Unity core API extensions.

At the moment, there is no way to see input activity in Unity's profiler window. There is work under way to allow the window to be extended from packages. The plan is to add a new "Input" track to the profiler window once the new APIs are available.
