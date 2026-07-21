---
uid: input-system-create-custom-device
---

# Create a custom device

> [!NOTE]
> This example deals only with Devices that have fixed layouts (that is, you know the specific model or models that you want to implement). This is different from an interface such as HID, where Devices can describe themselves through the interface and take on a wide variety of forms. A fixed Device layout can't cover self-describing Devices, so you need to use a [layout builder](add-layout-using-layout-builder.md) to build Device layouts from information you obtain at runtime.

There are two main situations in which you might need to create a custom Device:

1. You have an existing API that generates input, and which you want to reflect into the Input System.
2. You have an HID that the Input System ignores, or that the Input system auto-generates a layout for that doesn't work well enough for your needs.

For the second scenario, refer to [Overriding the HID Fallback](hid-create-custom-layout.md).

The steps deal with the first scenario, where you want to create a new Input Device entirely from scratch and provide input to it from a third-party API.
