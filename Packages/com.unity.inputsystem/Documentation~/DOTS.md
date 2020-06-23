# DOTS

Support for the Unity [Entities](...) package and the Unity [Job System](...) uses a separate input runtime with its own API separate from [`UnityEngine.InputSystem`](../api/UnityEngine.InputSystem.InputSystem.html). However, both the [`.inputactions`](./Actions.md) format as well as the [control layout](./Layouts.md) system (and thus all input device knowledge) are shared between the two runtimes. This means that [input actions](./Actions.md) authored in the editor as well as support for devices authored for the input system generally transfers directly to the DOTS runtime without additional work.

## Input Devices in DOTS

## Input Actions in DOTS

## Internals

Input in the DOTS input runtime operates chiefly as a sequence of memory transformations. Each transformation consumes an input memory block in a specific format and writes the transformed output to another, output memory block (again in a specific format).

### Code Generation

### Memory Transformations

#### Converters

#### Processors

#### Combiners

Combiners are transformations that *merge* two or more sources of input into a single output. This allows, for example, to combine input both from the keyboard and the mouse into a single input. Or to take two keys (such as "SHIFT" and "B") and create a new "combined key" from them.

### Event Handling
