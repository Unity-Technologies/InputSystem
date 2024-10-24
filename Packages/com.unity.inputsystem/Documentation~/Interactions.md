---
uid: input-system-interactions
---
# Interactions

- [Operation](#operation)
  - [Multiple Controls on an Action](#multiple-controls-on-an-action)
  - [Multiple Interactions on a Binding](#multiple-interactions-on-a-binding)
  - [Timeouts](#timeouts)
- [Using Interactions](#using-interactions)
  - [Interactions applied to Bindings](#interactions-applied-to-bindings)
  - [Interactions applied to Actions](#interactions-applied-to-actions)
- [Predefined Interactions](#predefined-interactions)
  - [Default Interaction](#default-interaction)
  - [Press](#press)
  - [Hold](#hold)
  - [Tap](#tap)
  - [SlowTap](#slowtap)
  - [MultiTap](#multitap)
- [Writing custom Interactions](#writing-custom-interactions)

An Interaction represents a specific input pattern. For example, a [hold](#hold) is an Interaction that requires a Control to be held for at least a minimum amount of time.

Interactions drive responses on Actions. You can place them on individual Bindings or an Action as a whole, in which case they apply to every Binding on the Action. At runtime, when a particular interaction completes, this triggers the Action.

![Interaction Properties](Images/InteractionProperties.png)


