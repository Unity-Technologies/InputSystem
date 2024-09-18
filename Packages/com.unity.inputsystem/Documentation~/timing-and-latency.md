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
| **[Subpage A](link-to-page-A)** | Summary sentence from subpage A. |
| **[Subpage B](link-to-page-B)** | Summary sentence from subpage A. |
| **...**                         | ...                              |
