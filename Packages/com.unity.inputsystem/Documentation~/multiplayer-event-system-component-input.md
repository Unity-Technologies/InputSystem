---
uid: input-system-multiplayer-event-system-component
---
## Multiplayer Event System component reference

Use the Multiplayer Event System component to configure input for a specific user in a multiplayer application.

<!--Note - First Selected, Send Navigation Events, and Drag Threshold are inherited from EventSystem. We should document these here, and I've re-used the EventSystem descriptions, but they don't make much sense, and I can't find further information to build them out!-->

|**Property**|**Description**|
|--------|-----------|
**First Selected**| Define which GameObject is selected first. |
|**Send Navigation Events**| Define whether the Event System should send navigation events such as move, submit, and cancel. |
|**Drag Threshold**| Define the soft area for dragging in pixels. |
|**Player Root**| Define which part of the hierarchy belongs to the current user. |
|**Add Default Input Modules**| Add the default Input System components to the same GameObject as this Multiplayer Event System component.|
