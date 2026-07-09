# Interactions Editor reference

The Interactions foldout contains properties for [interactions](Interactions.md) that you can assign to actions and bindings.

All properties in the Interactions foldout correspond to the [`InputSystem.Interactions`](../api/UnityEngine.InputSystem.Interactions.html) API.

By default, all Interactions have the following properties:

|Property|Description|
|-|-|
|**Press Point**| Set the amount of control actuation required for the action to trigger. |
|**Default**| Use the default value for the associated property. Un-check this checkbox to enter a custom value.|
|**Open Input Settings**| Open the [project-wide Input System settings](input-settings.md) in the Project Settings. |

Additionally, each Interaction type has unique properties.

## Hold

|Property|Description|
|-|-|
|**Hold Time**| Set the duration in seconds that the control must be pressed for the hold to register.|


## MultiTap

|Property|Description|
|-|-|
|**Tap Count**| Set the number of taps required to perform the action.|
|**Max Tap Spacing**| Set the maximum amount of time (in seconds) that can pass between taps.|
|**Max Tap Duration**| Set the maximum time (in seconds) within which the control needs to be pressed and released to perform the interaction.|

## Press

|Property|Description|
|-|-|
|**Trigger behavior**|Define when the interaction triggers. The options available by default are: <br/>- **Press Only**: Trigger the action when the button enters a pressed state. <br/> **Release Only**: Trigger the action or binding when the button exits a pressed state.  <br/>- **Press and Release**: Trigger the action when the button enters a pressed state, and again when it exits a pressed state.  |

## Slow Tap

|Property|Description|
|-|-|
|**Min Tap Duration**| Set the minimum time (in seconds) within which the control needs to be pressed and released to perform the interaction.|

## Tap
|Property|Description|
|-|-|
|**Max Tap Duration**| Set the maximum time (in seconds) within which the control needs to be pressed and released to perform the interaction.|
