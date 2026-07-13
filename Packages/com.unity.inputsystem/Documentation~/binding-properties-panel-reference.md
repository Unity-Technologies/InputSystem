---
uid: input-system-binding-properties-panel
---

# Binding Properties panel reference

Use the Binding Properties panel to configure [bindings](bindings.md) and [composite bindings](composite-bindings.md), and their associated [interactions](Interactions.md) and [processors](processors.md).


The Binding Properties panel changes depending on whether you are configuring a single [binding](bindings.md) or a [composite binding](composite-bindings.md).

## Binding properties

For each [binding](bindings.md), the Binding Properties panel displays the following properties by default:

|Property|Description|
|-|-|
|**Path**| Select the path of the control to assign this binding to. You can select an [input device](devices.md) from the list available, or use the **Usage** menu to select from a list of [usages](control-usages.md). |
|**Use in control scheme**|Select the [control schemes](control-schemes.md) that you want to apply this binding to. <br/><br/> This section only appears if you have control schemes in your project. |

## Composite binding properties

When you create a [composite binding](composite-bindings.md), the Binding Properties panel displays the following properties by default:

|Property|Description|
|-|-|
|**Composite Type**|Select the [composite type](composite-bindings.md) of the selected binding. The options are: <br/>- **1D Axis**: Create a composite binding made of two buttons: one that pulls a 1D axis in its negative direction, and another that pulls it in its positive direction. <br/>- **2D Vector**: Create a composite binding that represents a 4-way button setup like the D-pad on gamepads. <br/>- **3D Vector**: Create a composite binding that represents a 6-way button where two combinations each control one axis of a 3D vector. <br/>- **One Modifier**: Create a composite binding that requires the user to hold down a "modifier" button in addition to another control from which the actual value of the binding is determined. <br/>- **Two Modifiers**: Create a composite binding that requires the user to hold down a "modifier" button in addition to another control from which the actual value of the binding is determined. |

For more information on the composite types that are available by default, refer to scripting documentation on [`InputSystem.Composites`](../api/UnityEngine.InputSystem.Composites.html). Others might also be available if your project contains custom composite binding types.

### Axis binding reference

When **Composite Type** is set to **1D Axis**, the Binding Properties panel displays the following properties by default:

|Property|Description|
|-|-|
|**Which Side Wins**|Define what happens when both buttons are triggered at the same time. <br/>- **Positive**: Prioritize the Positive binding. <br/>- **Negative**: Prioritize the Negative binding. <br/>- **Neither**: Do nothing when both bindings are triggered at the same time. |

For more details, refer to scripting reference documentation on [`Composites.AxisComposite`](../api/UnityEngine.InputSystem.Composites.AxisComposite.html).

### Vector binding reference

When **Composite Type** is set to **2D Vector** or **3D Vector**, the Binding Properties panel displays the following properties by default:

|Property|Description|
|-|-|
|**Mode**| Select the process that the Input System uses to calculate a Vector2 or Vector3 from the input values provided. <br/>- **Analog**: Accept and use the floating-point values from controls. <br/>- **Digital**: Treat control values as on/off, and do not normalize the resulting vector. <br/>- **Digital Normalized**: Treat control values as on/off, and normalize the resulting vector. |

For more details on each mode, refer to scripting reference documentation on  [`Vector2Composite.Mode`](../api/UnityEngine.InputSystem.Composites.Vector2Composite.Mode.html) and [`Vector3Composite.Mode`](../api/UnityEngine.InputSystem.Composites.Vector3Composite.Mode.html).

### Binding with modifier reference

When **Composite Type** is set to **One Modifier**, **Two Modifiers**, **Button With One Modifier**, or **Button with Two Modifiers**, the Binding Properties panel displays the following properties by default:

|Property|Description|
|-|-|
|**Override Modifiers Need To Be Pressed First**| **Note**: This property is obsolete. Use the **Modifiers Order** property instead.<br/><br/>Override the Input Consumption setting, so that the composite binding still triggers if the modifiers are pressed after the button. |
|**Modifiers order**| Define the order in which buttons and modifiers must be pressed in order to trigger the composite binding. <br/>- **Default**: Apply the Input Consumption setting.  <br/>- **Ordered**: Only trigger the binding if the modifiers are in a pressed state before the button enters a pressed state.   <br/>- **Unordered**: Allow the binding to trigger regardless of the order in which the buttons and modifiers enter a pressed state. |

For more details, refer to scripting reference documentation on [`OneModifierComposite`](../api/UnityEngine.InputSystem.Composites.OneModifierComposite.html), [`TwoModifiersComposite`](../api/UnityEngine.InputSystem.Composites.TwoModifiersComposite.html), [`ButtonWithOneModifier`](../api/UnityEngine.InputSystem.Composites.ButtonWithOneModifier.html), and [`ButtonWithTwoModifiers`](../api/UnityEngine.InputSystem.Composites.ButtonWithTwoModifiers.html).

[!include[Interactions reference](include-interactions-reference.md)]

[!include[Processors reference](include-processors-reference.md)]
