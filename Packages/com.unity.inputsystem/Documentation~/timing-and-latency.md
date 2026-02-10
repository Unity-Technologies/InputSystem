---
uid: timing-latency
---
# Timing and Latency

Input Timing refers to the topic of exactly when the Input System receives and processes input from devices.

Latency is the amount of time between the user providing some input, and the user receiving a response to that input. For example, the time between a button press and your game’s character moving on-screen. In fast-paced input scenarios such as action games, even tiny delays between the user's input and your game responding can be noticeable and affect the feel of your gameplay.

In addition to the effects of latency, timing can affect one-off discrete events such as when a button press starts or finishes. Checking for these at the wrong time can result in missed or duplicate events.

To minimize input latency, and to avoid missed or duplicate events, it helps to understand how the Input System processes events in relation to Unity's frame updates, physics updates, and fixed updates. This will help you make decisions about how to read and respond to input in your game or app.

| **Topic**                       | **Description**                  |
| :------------------------------ | :------------------------------- |
| **[Input events queue](xref:input-system-timing-queue)** | Understand how and when the Input System receives and processes input from devices. |
| **[Select an input processing mode](xref:input-system-timing-select)** | How to select an appropriate **Update Mode** which controls when the Input System processes queued input events. |
| **[Optimize for dynamic update](xref:input-system-timing-optimize-dynamic)** | How to optimize input for use in `Update` calls. |
| **[Optimize for fixed update](xref:input-system-timing-optimize-fixed)** | How to optimize input for use in `FixedUpdate` calls. |
| **[Avoid missed or duplicate events](xref:input-system-timing-missed)** | How to avoid missing or duplicated discrete input events like when a button was pressed or released. |
| **[Mixed timing scenarios](xref:input-system-timing-mixed)** | How to optimize and avoid problems when using input in both `Update` and `FixedUpdate` calls. |
