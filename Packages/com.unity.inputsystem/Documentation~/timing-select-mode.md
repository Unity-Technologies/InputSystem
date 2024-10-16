# Select an appropriate input processing mode

The Input System **Update Mode** controls when the input system processes queued input events.

You can find and change the Update Mode by going to **Project Settings** \> **Input System Package** \> **Input Settings** \> **Update Mode**.

The choice of Update Mode that best suits your project relates to whether you're using Update or FixedUpdate to respond to input events. You should choose this based on the specifics of the game you're making. You can read more about Update and FixedUpdate in [Time and Framerate Management](https://docs.unity3d.com/Manual/TimeFrameManagement.html).

## When a small amount of latency is not an issue

In cases where a small amount of input latency (a few frames) isn't an issue, set the update mode to match where you read your input. If your input code is in `Update` (usually non-physics-based scenarios), use **Process Events in Dynamic Update**. If your input code is in `FixedUpdate` (usually physics-based scenarios), use **Process Events in Fixed Update**.

## When minimum latency is a necessity

In cases where minimum latency is a necessity, set the update mode to **Process Events in Dynamic Update**, even if you're using code in FixedUpdate to apply physics forces based on input. This strategy comes with some additional issues that you must be aware of. Refer to the section [Optimizing for fixed-timestep scenarios](timing-optimize-fixed-update.md) for more information.
