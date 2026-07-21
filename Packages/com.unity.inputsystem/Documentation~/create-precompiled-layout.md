---
uid: input-system-create-precompiled-layout
---

# Create a precompiled layout

The first step in setting up a precompiled layout is to generate it.

To generate a layout:

1. Open the [Input Debugger](xref:input-debugging).
2. Navigate to the layout you want to precompile within the **Layouts** branch and right-click it.
3. Select **Generate Precompiled Layout**.

    Unity will ask you where to store the generated code. Pick a directory in your project, enter a file name, and click **Save**.

Once generated, you can register the precompiled layout with the Input System using [`InputSystem.RegisterPrecompiledLayout`](xref:UnityEngine.InputSystem.InputSystem.RegisterPrecompiledLayout``1(System.String)). The method expects a string argument containing metadata for the precompiled layout. This string is automatically emitted as a `const` inside the generated class.

```CSharp
InputSystem.RegisterPrecompiledLayout<MyPrecompiledDevice>(MyPrecompiledDevice.metadata);
```

> [!IMPORTANT]
> It is very important that this method is called with all relevant layout registrations being in the same state as at the time the layout was precompiled. There is no internal check whether the precompiled layout will still generate an identical result to the non-precompiled version.

Once registered, a precompiled layout is automatically used whenever the layout that the precompiled layout is based on is instantiated.

```CSharp
// Let's assume you have a custom device class.
public class MyDevice : InputDevice
{
    // Setters for your control getters need to have at least `protected`
    // or `internal` access so the precompiled version can use them.
    [InputControl]
    public ButtonControl button { get; protected set; }

    // This method will *NOT* be invoked by the precompiled version. Instead, all the lookups
    // performed here will get hardcoded into the generated C# code.
    protected override void FinishSetup()
    {
        base.FinishSetup();

        button = GetChildControl<ButtonControl>("button1");
    }
}

// You register the device as a layout somewhere during startup.
InputSystem.RegisterLayout<MyDevice>();

// And you register a precompiled version of it then as well.
InputSystem.RegisterPrecompiledLayout<PrecompiledMyDevice>(PrecompiledMyDevice.metadata);

// Then the following will implicitly use the precompiled version.
InputSystem.AddDevice<MyDevice>();
```

A precompiled layout will automatically be unregistered in the following cases:

* A [layout override](override-layout-definitions.md) is applied to one of the layouts used by the precompiled Device. This also extends to [controls](controls.md) used by the Device.
* A layout with the same name as one of the layouts used by the precompiled Device is registered (which replaces the layout already registered under the name).
* A [processor](processors.md) is registered that replaces a processor used by the precompiled Device.

This causes the Input System to fall back to the non-precompiled version of the layout. Note also that a precompiled layout will not be used for layouts [derived](layout-inheritance.md) from the layout the precompiled version is based on. In the example above, if someone derives a new layout from `MyDevice`, the precompiled version is unaffected (it will not be unregistered) but is also not used for the newly created type of device.

```CSharp
// Let's constinue from the example above and assume that sometime
// later, someone replaces the built-in button with an extended version.
InputSystem.RegisterLayout<ExtendedButtonControl>("Button");

// PrecompiledMyDevice has implicitly been removed now, because the ButtonControl it uses
// has now been replaced with ExtendedButtonControl.
```

If needed, you can add `#if` checks to the generated code, if needed. The code generator will scan the start of an existing file for a line starting with `#if` and, if found, preserve it in newly generated code and generate a corresponding `#endif` at the end of the file. Similarly, you can change the generated class from `public` to `internal` and the modifier will be preserved when regenerating the class. Finally, you can also modify the namespace in the generated file with the change being preserved.

The generated class is marked as `partial`, which means you can add additional overloads and other code by  having a parallel, `partial` class definition.

```CSharp
// The next line will be preserved when regenerating the precompiled layout. A
// corresponding #endif will be emitted at the end of the file.
#if UNITY_EDITOR || UNITY_STANDALONE

// If you change the namespace to a different one, the name of the namespace will be
// preserved when you regenerate the precompiled layout.
namepace MyNamespace
{
    // If you change `public` to `internal`, the change will be preserved
    // when regenerating the precompiled layout.
    public partial class PrecompiledMyDevice : MyDevice
    {
        //...
```
