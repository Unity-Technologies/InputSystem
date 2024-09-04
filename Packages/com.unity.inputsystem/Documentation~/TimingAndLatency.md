---
uid: timing-latency
---
# Timing and Latency

Input Timing refers to the topic of exactly when the Input System receives and processes input from devices.

Latency is the amount of time between the user providing some input, and the user receiving a response to that input. For example, the time between a button press and your game’s character moving on-screen. In fast-paced input scenarios such as action games, even tiny delays between the user's input and your game responding can be noticeable and affect the feel of your gameplay.

In addition to the effects of latency, timing can affect one-off discrete events such as when a button press starts or finishes. Checking for these at the wrong time can result in [missed or duplicate events](TimingAvoidMissedOrDuplicateEvents.md).

To minimize input latency, and to avoid missed or duplicate events, it helps to understand how the Input System processes events in relation to Unity's frame updates, physics updates, and fixed updates. This will help you make decisions about how to read and respond to input in your game or app.

Usually, to achieve minimum latency, set the Input System **Update Mode** to **Process Events in Dynamic Update**, even if you're using code in FixedUpdate to apply physics forces based on input. In physics-based or FixedUpdate scenarios in particular however, there are details to be aware of to avoid issues described above, or which might cause you to choose a different update mode, explained in the following sections.
