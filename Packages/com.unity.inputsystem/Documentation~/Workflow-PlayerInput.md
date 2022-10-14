# Workflow Overview - Using an Actions Asset and PlayerInput Component

![image alt text](./Images/Workflow-PlayerInput.svg)


The highest level of abstraction provided by the Input System is when you use the [Actions Asset](ActionAssets.html) and the **Player Input component** together. The Player Input component takes a reference to an Actions Asset, and provides a way to make connections between the Actions defined in that asset, and C# methods in your own MonoBehaviour scripts, so that your desired C# methods are called when the user performs an input action.

It allows you to set up these connections using a UI in the inspector, instead of requiring you to write code to make those connections, as well as letting you choose how those methods are called.

![image alt text](./Images/PlayerInputWithGameplayEvents.png)

In the above image, you can see the PlayerInput component set up to map "fire", "move" and "look" actions to `OnFire`, `OnMove` and `OnLook` methods in a script, via Unity Events.

You may find that this level of abstraction and configuration in the UI makes things more convenient, or you may prefer to implement these connections between the actions and your action methods in your own code. This is again a trade-off between simplicity and flexibility.

Using the Player Input component provides the flexibility of being able to connect any action to any public method on a GameObject’s component without writing code. However you may find that coding the connections in your own script is simpler than setting up and keeping track of these connections in a Player Input component on a GameObject.

