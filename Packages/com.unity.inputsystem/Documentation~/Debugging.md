    ////WIP

# Debugging

* [Input Debugger](#input-debugger)
  * [Debugging Remotely](#debugging-remotely)
  * [Debugging Devices](#debugging-devices)
  * [Debugging Actions](#debugging-actions)
  * [Debugging Users/PlayerInput](#debugging-usersplayerinput)
* [Input Visualizers](#input-visualizers)
  * [Visualizing Controls](#visualizing-controls)
  * [Visualizing Actions](#visualizing-actions)
* [Event Diagnostics](#event-diagnostics)

When something isn't working as expected, the best first stop is usually the "Input Debugger" in the Unity editor. This is a Unity editor window that is designed to provide access to the activity of the input system in both the editor and in connected players.

To open the input debugger, go to `Window >> Analysis >> Input Debugger` in Unity's main menu in the editor.

## Input Debugger

![Input Debugger](Images/InputDebugger.png)

The debugger shows a tree breakdown of the state of the input system.

|Item|Description|
|----|-----------|
|Devices|[Input devices](Devices.md) that are currently added to the system as well as a list of unsupported/unrecognized devices.|
|Layouts|A breakdown of all registered control and device layouts. This is essentially the database of supported hardware and the knowledge of how to represent a given piece of input hardware.|
|Actions|Only visible in play mode and only if there are [actions](Actions.md) that are currently enabled.<br><br>Shows the list of all currently enabled actions and the controls they are bound to.<br><br>See [Debugging Actions](#debugging-actions).|
|Users|Only visible when there is one or more `InputUser` instances. See [documentation](UserManagement.md).<br><br>Lists all currently active uers along with their active control schemes and devices, all their associated actions as well as the controls they are bound to.<br><br>Note that `PlayerInput` uses `InputUser` underneath. This means that when using `PlayerInput` components, each player will have an entry here.<br><br>See [Debugging Users/PlayerInput](#debugging-usersplayerinput).|
|Settings||
|Metrics||

### Debugging Remotely

You can connect input debugger to a player running on a device. This makes input activity from the player observable in the editor. The mechanism used by this is Unity's `PlayerConnection`, i.e. the same mechanism by which the Unity profiler can be connected to a player.

>NOTE: At the moment, debugging input in players is restricted to seeing devices and events from connected players. There is no support yet for seeing other input-related data from players.

    ////TODO: explain "Remote Devices"

### Debugging Devices

>NOTE: For an alternative way to debug/visualize controls and/or devices, see [Visualizing Controls](#visualizing-controls).

### Debugging Actions

>NOTE: For an alternative way to debug/visualize actions, see [Visualizing Actions](#visualizing-actions).

### Debugging Users/PlayerInput

When there are multiple `InputUser` instances &ndash; each `PlayerInput` will implicitly create one &ndash;, the debugger will list each user along with its paired devices and active actions.

IMAGE

## Input Visualizers

To debug specific problems it is often helpful to see activity over time and presented in graphical form.

>NOTE: The input visualizer components are only available in the editor and in development players.

### Visualizing Controls

### Visualizing Actions

## Event Diagnostics

In normal operation, the input system will silently discard any anomalous input it finds in the event stream. TODO
