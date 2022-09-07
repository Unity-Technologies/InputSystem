# Input System Concepts

This page introduces the concepts that relate to working with the Input System.

When you become familiar with these concepts, you will be able to understand the difference between the workflows available within the Input System, and choose which workflow best suits your project.

## Basic concepts

These basic concepts and terms refer to the steps in the sequence of events that occur when a user sends input to your game or app. The Input System provides features which implement these steps, or you can choose to implement some of them yourself.

![image alt text](Images/ConceptsOverview.png)

**User**: The person playing your game or using your app, by holding or touching the input device and providing input.

**Input Device**: Often referred to just as a "**device**" within the context of input. A physical piece of hardware, such as a keyboard, gamepad, mouse, or touchscreen which allows the user to send input into Unity.

**Controls:** The separate individual parts of an input device which each send input values into Unity. For example, a gamepad’s **controls** comprise multiple buttons, sticks and triggers. For example, a mouse’s controls include the two X and Y sensors on the underside, and the various buttons and scroll wheels on the top side.

**Interactions:** These describe different ways of using the controls on a device. For example, pressing a button down, releasing a button, a long press, or a double tap. Interactions can be thought of as "patterns of input". The Input System provides ways of identifying and responding to different types of interaction.

**Actions**: These are things a user can do in your game or app as a result of input, regardless of what device or control they use to perform it. Actions generally have conceptual names that you choose to suit your project, and should usually be verbs. For example "Run", "Jump" "Crouch", "Use", "Start", "Quit". The Input System can help you manage and edit your actions, or you can implement them yourself.

**Action Asset:** An asset type which allows you to define and configure groups of actions as a set. The Action Asset UI allows you to bind controls, group related actions into **Action Maps**, and specify which controls belong to different **Control Schemes**. 

**Embedded Actions:** Actions defined directly as fields in your scripts (as opposed to in an Action Asset). These types of action are the same as those defined in an Action Asset, and their inspector UI allows you to bind controls. However, because they’re defined as individual fields in your script, you do not benefit from the Action Asset’s ability to group Actions together into Action Maps and Control Schemes.

**Binding**: A connection defined between an **Action** and one or more **Controls**. For example, in a car racing game, pressing the right shoulder button on a controller might be bound to the action "Change Gear Up". The **Action Asset** and **Embedded Actions** both provide a similar UI to create and edit bindings.

**Action Method**: The C# method you want to call when an action is performed. Usually when a user performs an action, you want the Input System to call a method in your C# code to do something in response, and it’s common for the action method to have the same name as the action itself. For example, you might want to add some upwards force to a player character when the user presses a button that represents the "Jump" action. Your code which does this might be **player.OnJump()**. In this case, **Jump **is the name of the action, and **OnJump **is the name of the corresponding Action Method.There is nothing special in implementation about an Action Method compared with other methods in your scripts, however it is defined here because it is useful to be able to refer to it later in the documentation.
