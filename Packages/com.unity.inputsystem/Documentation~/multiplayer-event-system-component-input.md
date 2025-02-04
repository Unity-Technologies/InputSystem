## Multiplayer Event System component reference

Use the Multiplayer Event System component to configure input for a specific user in a multiplayer application.

<!--Note - First Selected, Send Navigation Events, and Drag Threshold are inherited from EventSystem. We should document these here, and I've re-used the EventSystem descriptions, but they don't make much sense, and I can't find further information to build them out!-->

|**Property**|**Description**|
|--------|-----------|
**First Selected**| The GameObject that was selected first. |
|**Send Navigation Events**| Should the EventSystem allow navigation events (move / submit / cancel). |
|**Drag Threshold**| The soft area for dragging in pixels. |
|**Player Root**|Specify which part of the hierarchy belongs to the current user. |
|**Add Default Input Modules**| Add the default Input System components to the same GameObject as this Multiplayer Event System component.|