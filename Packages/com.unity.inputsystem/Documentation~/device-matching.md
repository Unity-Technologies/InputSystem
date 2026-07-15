---
uid: input-system-device-matching
---

# Device matching

[`InputDeviceMatcher`](xref:UnityEngine.InputSystem.Layouts.InputDeviceMatcher) instances handle matching an [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription) to a registered layout. Each matcher loosely functions as a kind of regular expression. Each field in the description can be independently matched with either a plain string or regular expression. Matching is not case-sensitive. For a matcher to apply, all of its individual expressions have to match.

To matchers to any layout, call [`InputSystem.RegisterLayoutMatcher`](xref:UnityEngine.InputSystem.InputSystem). You can also supply them when you register a layout.

```CSharp
// Register a new layout and supply a matcher for it.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    matches: new InputDeviceMatcher()
        .WithInterface("HID")
        .WithProduct("MyDevice.*")
        .WithManufacturer("MyBrand");

// Register an alternate matcher for an already registered layout.
InputSystem.RegisterLayoutMatcher<MyDevice>(
    new InputDeviceMatcher()
        .WithInterface("HID")

```

If multiple matchers identify the same [`InputDeviceDescription`](xref:UnityEngine.InputSystem.Layouts.InputDeviceDescription), the Input System chooses the matcher that has the larger number of properties to match against.

## Hijacking the matching process

You can overrule the internal matching process from outside to select a different layout for a Device than the system would normally choose. This also makes it possible to quickly build new layouts. To do this, add a custom handler to the  [`InputSystem.onFindControlLayoutForDevice`](xref:UnityEngine.InputSystem.InputSystem) event. If your handler returns a non-null layout string, then the Input System uses this layout.
