---
uid: input-system-enable-correct-input-system
---

# Enable the correct input system

When installing the new Input System, Unity prompts you to enable the new input system and disable the old one. You can change this setting at any time later, by going to **Edit > Project Settings > Player > Other Settings > Active Input Handling**, [as described here](./Installation.md#enable-the-new-input-backends).

There are scripting symbols defined which allow you to use conditional compilation based on which system is enabled, as shown in the example below.

```CSharp
#if ENABLE_INPUT_SYSTEM
    // New input system backends are enabled.
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    // Old input backends are enabled.
#endif
```

> [!NOTE]
> It is possible to have both systems enabled at the same time, in which case both sets of code in the example above above will be active.
