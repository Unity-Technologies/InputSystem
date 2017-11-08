namespace ISX
{
    public enum InputDeviceChange
    {
        // New device was added to system.
        Added,

        // Existing device was removed from system.
        Removed,

        // Previously added device was re-connected after having been
        // disconnected before.
        Connected,

        // Previously added device was disconnected but remains added
        // to the system.
        Disconnected,

        // Usages on device have changed. See InputSystem.SetUsage()
        // and InputControl.usages. This may signal, for example, that
        // what was the right hand XR controller before is now the left
        // hand controller.
        UsageChanged,

        VariantChanged
    }
}
