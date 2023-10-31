---
uid: basic-concepts
---
# Basic Concepts

This page introduces the basic concepts that relate to working with the Input System. They relate to the steps in the sequence of events that occur when a user sends input to your game or app. The Input System provides features which implement these steps, or you can choose to implement some of them yourself.

![](Images/ConceptsOverview.png)

|Concept|Description|
|-------|-----------|
|[**User**](UserManagement.html)| The person playing your game or using your app, by holding or touching the input device and providing input.|
|[**Input Device**](SupportedDevices.html)| Often referred to just as a "**device**" within the context of input. A physical piece of hardware, such as a keyboard, gamepad, mouse, or touchscreen which allows the user to send input into Unity.|
|[**Control**](Controls.html)|The separate individual parts of an input device which each send input values into Unity. For example, a gamepad’s **controls** comprise multiple buttons, sticks and triggers, and a mouse’s controls include the two X and Y sensors on the underside, and the various buttons and scroll wheels on the top side.|
|**Action Map**| A collection of Actions which all relate to the same situation. You can simultaneously enable or disable all Actions in an action map, so it is useful to group Actions in Action Maps by the context in which they are relevant. For example, you might have one action map for controlling a player, and another for interacting with your game's UI.|
|[**Action**](Actions.html)| These are things a user can do in your game or app as a result of input, regardless of what device or control they use to perform it. Actions generally have conceptual names that you choose to suit your project, and should usually be verbs. For example "Run", "Jump" "Crouch", "Use", "Start", "Quit". The Input System can help you manage and edit your actions, or you can implement them yourself.|
|[**Binding**](ActionBindings.html)| A connection defined between an **Action** and one or more **Controls**. For example, in a car racing game, pressing the right shoulder button on a controller might be bound to the action "Change Gear Up".|
|**Action Reference**| A reference in your script to an Input Action. Once you have a reference to an action, you can either read the current value or state of the action (also known as "polling"), or set up a callback to call your own method when actions are performed.|