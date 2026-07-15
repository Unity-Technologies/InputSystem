---
uid: input-system-action-properties-panel
---

# Action Properties panel reference

Use the Action Properties panel to configure actions, and their associated [interactions](Interactions.md) and [processors](processors.md).

The Action Properties panel changes depending on the **Action Type** of the selected Action.

|Property|Description|
|-|-|
|**Action Type**| Define whether the action is a **Value**, **Button**, or **Pass Through** action type. Refer to [Action and control types](action-and-control-types.md) for detailed information on action types. |
|**Control Type**| Define the control type for the selected action. Refer to [Action and control types](action-and-control-types.md) for detailed information on control types. <br/><br/>This property is only available when **Action Type** is set to **Value** or **Pass Through**.  |
|**Initial State Check**| Perform an initial state check when the Action is first enabled, to check the current state of any bound Control. <br/><br/>This setting is only available when Action Type is set to **Button** or **Pass Through**. It is always enabled for **Value**-type actions. Refer to [binding initial state checks|(binding-initial-state-checks.md) for detailed information.

[!include[Interactions reference](include-interactions-reference.md)]

[!include[Processors reference](include-processors-reference.md)]
