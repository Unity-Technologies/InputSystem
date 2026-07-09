---
uid: input-system-bindings-intro
---

# Introduction to bindings

![A flowchart showing the general workflow of the Input System, with icons representing the different concepts. It starts with the User icon, which then leads into the Input Device and its Controls icon. This then leads into the Action Map and Actions concept. The Input Device and Action Map and Actions icons are collectively grouped under the Binding header. This leads into the final icon representing your action code.](Images/ConceptsOverview.png)

A **binding** represents a connection between an [Action](actions.md) and one or more [Controls](controls.md) identified by a [Control path](./control-paths.md). For example, the right trigger of a gamepad (a control) might be bound to an an action named "accelerate", so that pulling the right trigger causes a car to accelerate in your game.

You can add multiple bindings to an action, which is generally useful for supporting multiple types of input device. For example, in the default set of actions, the "Move" action has a binding to the left gamepad stick and the WASD keys, which means input through any of these bindings will perform the action.

You can also bind multiple controls from the same device to an action. For example, both the left and right trigger of a gamepad could be mapped to the same action, so that pulling either trigger has the same result in your game.

![The default "move" action with its multiple bindings highlighted](./Images/ActionWithMultipleBindings.png)<br/>
_The default "Move" action in the Actions Editor window, displaying the multiple bindings associated with it._

You can also set up [Composite](composite-bindings.md) bindings, which don't bind to the controls themselves, but receive their input from **Part Bindings** and then return a value representing a composition of those inputs. For example, the right trigger on the gamepad can act as a strength multiplier on the value of the left stick.
