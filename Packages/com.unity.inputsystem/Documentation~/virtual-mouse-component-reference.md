---
uid: input-system-virtual-mouse-comp
---

# Virtual Mouse component reference

Use the Virtual Mouse component to create a simulated mouse device in your application.

## Cursor

Use the **Cursor** properties to configure a virtual software cursor, and define when to use it.

|**Property**|**Description**|
|--------|-----------|
|**Cursor Mode**| Determine whether the virtual mouse should always use the cursor that it defines with the **Cursor Graphic** and **Cursor Transform** values, or whether it should use hardware cursors instead if it detects them. The options are: <br/>- **Software Cursor**: Use only the cursor that this Virtual Mouse component creates.  <br/>- **Hardware Cursor If Available**: Use a hardware cursor if the Input System detects a `Mouse` device. If it does not detect a `Mouse` device, the virtual mouse uses the software cursor as a fallback.  |
|**Cursor Graphic**| Select a UI graphic element for the software cursor's appearance. <br/><br/>The virtual mouse only uses this graphic for the software cursor, and not for hardware cursors. |
|**Cursor Transform**| Set the starting transform for the software cursor. Moving the cursor updates the anchored position of the transform. <br/><br/>The virtual mouse only uses this transform for the software cursor, and not for hardware cursors. |

## Motion

Use the **Motion** properties to configure the speed of movement and scrolling on the virtual mouse.

|**Property**|**Description**|
|--------|-----------|
|**Cursor Speed**| Set the speed at which the cursor should move, in pixels per second. This value multiplies the **Stick Action** value. |
|**Scroll Speed**| Set the speed at which the virtual mouse scroll should move. This value multiplies the **Scroll Wheel Action** value.|

## Action assignment

Use the action-related properties on the component to define which action triggers each virtual mouse input.

By default, the following inputs are available in the Virtual Mouse component:

|**Property**|**Description**|
|--------|-----------|
|**Stick Action** | Select the Vector2 action that moves the cursor left/right on the x-axis, and up/down on the y-axis. The input value from **Cursor Speed** multiplies this value. |
|**Left button action** | Select the button action that triggers a left-click on the virtual mouse.|
|**Middle button action** |Select the button action that triggers a middle-click on the virtual mouse. |
|**Right Button Action** |Select the button action that triggers a right-click on the virtual mouse. |
|**Forward Button Action** |Select the button action that triggers a forward-button (button 4) click on the virtual mouse. |
|**Back Button Action** |Select the button action that triggers a back-button (button 5) click on the virtual mouse. |
|**Scroll Wheel Action** | Select a Vector2 action that feeds into the virtual mouse's `scrollWheel` action. The input value from **Scroll Speed** multiplies this value. |

Each action-related property has identical settings to define actions and controls:

|**Setting**|**Description**|
|--------|-----------|
|Menu drop-down (**⋮**) | Options: <br/>- **Action**: Create a new action on the virtual mouse component. <br/>- **Reference**: Use a reference to an existing action. |
|Action Properties (**cog** icon) | Open an Action Properties window. Refer to [Action Properties panel reference](action-properties-panel-reference.md) for details on the properties available. <br/><br/>This setting only appears if you selected **Action** in the drop-down menu. |
|Add Binding (**+**)| Add a binding to the new action. <br/><br/>This setting only appears if you selected **Action** in the drop-down menu.|
|Delete Selection (**-**)|Delete the selected binding. <br/><br/>This setting only appears if you selected **Action** in the drop-down menu.|
