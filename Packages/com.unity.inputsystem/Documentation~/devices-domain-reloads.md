---
uid: input-system-devices-domain-reloads
---

# Devices and domain reloads

The Editor reloads the C# application domain whenever it reloads and recompiles scripts, or when the Editor goes into Play mode. This requires the Input System to reinitialize itself after each domain reload. During this process, the Input System attempts to recreate devices that were instantiated before the domain reload. However, the state of each Device doesn't carry across, which means that Devices reset to their default state on domain reloads.

Note that layout registrations do not persist across domain reloads. Instead, the Input System relies on all registrations to become available as part of the initialization process (for example, by using `[InitializeOnLoad]` to run registration as part of the domain startup code in the Editor). This allows you to change registrations and layouts in script, and the change to immediately take effect after a domain reload.

## Support for Configurable Enter Play Mode

Input System 1.20.0 and later supports Configurable Enter Play Mode, which allows entering Play mode with domain reload disabled. For more information, refer to [Configuring how Unity enters Play mode](https://docs.unity3d.com/Manual/configurable-enter-play-mode.html) and [Enter Play mode with domain reload disabled](https://docs.unity3d.com/Manual/domain-reloading.html).
