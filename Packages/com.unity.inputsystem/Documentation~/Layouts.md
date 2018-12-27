    ////WIP

>NOTE: Layouts are an advanced, mostly internal feature of the input system. It is not necessary to understand this feature to use the input system. Knowledge of the layout system is mostly useful when wanting to support custom devices or when wanting to modify the behavior of existing devices.

# Layouts

"Layouts" are the central mechanism by which the input system learns about types of input devices and input controls. Each layout describes the composition of a specific control or device. By matching the description of a device to a layout, the input system is able to create the correct type of device and interpret the incoming input data correctly.

The set of currently understood layouts can be browsed from the input debugger.

![Layouts in Debugger](Images/LayoutsInDebugger.png)

A layout has two primary functions:

* Describe a certain memory layout containing input data.
* Assign names, structure, and meaning to the controls operating on the data.

## Layout Formats

New layouts can be added in one of three ways.

1. Represented by C# structs and classes.
2. In JSON format.
3. Built-on the fly at runtime using what's called "layout builders".

### Layout from Type

In its most basic form, a layout can simply be a C# class.

```
// A control layout.
public class My
```

### Layout from JSON

### Layout Builders

## Layout Inheritance

## Layout Overrides

## Built-In Layouts

### Controls

|Layout|Description|
|------|-----------|
|`Stick`|A thumbstick-like controls. Based on `Vector2`. Has an `X` and a `Y` axis as well as `up`, `down`, `left`, and `right` buttons corresponding to the cardinal directions.|

### Devices

|Layout|Description|
|------|-----------|
