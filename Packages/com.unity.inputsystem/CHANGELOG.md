# Changelog
All notable changes to the input system package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).
## [0.2.1-preview] - 2019-03-11
### Changed
 - NativeUpdateCallback API update to match Unity 2018.3.8f1

## [0.2.0-preview] - 2019-02-12

This release contains a number of fairly significant changes. The focus has been on further improving the action system to make it easier to use as well as to make it work more reliably and predictably.

>NOTE: There are some breaking changes. Please see the "Changed" section below.

### Changed

- Removed Unity 2018.2 support code.
- Removed .NET 3.5 support code.
- Started using C# 7.
- `IInputControlProcessor<TValue>` has been replaced with `InputProcessor` and `InputProcessor<TValue>` base classes.
- `IInputBindingComposite` has been replaced with an `InputBindingComposite` base class and the `IInputBindingComposite<TValue>` interface has been merged with the `InputBindingComposite<TValue>` class which had already existed.
- `InputUser.onUnpairedDeviceUser` will now notify for each actuated control until the device is paired or there are no more actuated controls.
- `SensitivityProcessor` has been removed.
    * The approach needs rethinking. What `SensitivityProcessor` did caused more problems than it solved.
- State monitors no longer have their timeouts removed automatically when they fire. This makes it possible to have a timeout that is removed only in response to a specific state change.
- Events for devices that implement `IInputStateCallbacks` (such as `Touchscreen`) are allowed to go back in time. Avoids the problem of having to order events between multiple fingers correctly or seeing events getting rejected.
- `PenState.Button` is now `PenButton`.

#### Actions:
- Bindings that have no interactions on them will trigger differently now. __This is a breaking change__.
  * Previously, these bindings would trigger `performed` on every value change including when going back to their default value. This is why you would see two calls of `performed` with a button; one when the button was pressed, another when it was depressed.
  * Now, a binding without an interaction will trigger `started` and then `performed` when a bound control is actuated. Thereafter, the action will remain in `Started` phase. For as long as the control is actuated, every value change will trigger `performed` again. When the control stops being actuated, it will trigger `cancelled` and the action will remain in `Waiting` state.
  * Control actuation is defined as a control having a magnitude (see `InputControl.EvaluateMagnitude`) greater than zero. If a control does not support magnitudes (returns -1 from `EvaluateMagnitude`), then the control is considered actuated when it changes state away from its default state.
  * To restore the previous behavior, simply change code like
      ```
        myAction.performed += MyCallback;
      ```
    to
      ```
        myAction.performed += MyCallback;
        myAction.cancelled += MyCallback;
      ```
  * Alternatively, enable `passThrough` mode on an action. This effectively restores the previous default behavior of actions.
    ```
        new InputAction(binding: "<Gamepad>/leftTrigger") { passThrough = true };
    ```
- As part of the aforementioned change, the following interactions have been removed as they are no longer relevant:
  - `StickInteraction`: Can simply be removed from bindings. The new default behavior obsoletes the need for what `StickInteraction` did. Use `started` to know then the stick starts being actuated, `performed` to be updated on movements, and `cancelled` to know when the stick goes back into rest position.
  - `PressAndReleaseInteraction`: Can simply be removed from bindings. The default behavior with no interaction encompasses press and release detection. Use `started` to know then a button is pressed and `cancelled` to know when it is released. To set a custom button press point, simply put an `AxisDeadzoneProcessor` on the binding.
- `PressInteraction` has been completely rewritten.
  - Trigger behavior can be set through `behavior` parameter and now provides options for observing just presses (`PressOnly`), just releases (`ReleaseOnly`), or both presses and releases (`PressAndRelease`).
  - Also, the interaction now operates on control actuation rather than reading out float values directly. This means that any control that supports magnitudes can be used.
  - Also supports continuous mode now.
- If bound controls are already actuated when an action is enabled, the action will now trigger in the next input update as if the control had just been moved from non-actuated to actuated state.
  - In other words, if e.g. you have a binding to the A button of the gamepad and the A button is already pressed when the action is first enabled, then the action associated with the A button will trigger as if the button had just been pressed. Previously, it required releasing and re-pressing the button first -- which, together with certain interactions, could lead to actions ending up in a confused state.
- When an action is disabled, it will now cancel all ongoing interactions, if any (i.e. you will see `InputAction.cancelled` being called).
  - Note that unlike the above-mentioned callbacks that happen when an action starts out with a control already actuated, the cancellation callbacks happen __immediately__ rather than in the next input update.
- Actions that at runtime are bound to multiple controls will now perform *conflict resolution*, if necessary.
  - This applies only if an action actually receives multiple concurrent actuations from controls.
  - When ambiguity is detected, the greatest amount of actuation on any of the controls gets to drive the action.
  - In practice, this means that as long as any of the controls bound to an action is actuated, the action will keep going. This resolves ambiguities when an action has primary and secondary bindings, for examples, or when an action is bound to multiple different devices at the same time.
  - Composite bindings count as single actuations regardless of how many controls participate in the composite.
  - This behavior __can be bypassed__ by setting the action to be pass-through.
- Action editor now closes when asset is deleted.
  - If there are unsaved changes, asks for confirmation first.
- Interactions and processors in the UI are now filtered based on the type of the action (if set) and sorted by name.
- Renamed "Axis" and "Dpad" composites to "1D Axis" and "2D Vector" composite.
  - The old names can still be used and existing data will load as expected.
  - `DpadComposite` got renamed to `Vector2Composite`; `AxisComposite` is unchanged.
- `InputInteractionContext.controlHasDefaultValue` has been replaced with `InputInteractionContext.ControlIsActuated()`.
- `InputActionChange.BindingsHaveChangedWhileEnabled` has been reworked and split in two:
    1. `InputActionChange.BoundControlsAboutToChange`: Bindings have been previously resolved but are about to be re-resolved.
    2. `InputActionChange.BoundControlsChanged`: Bindings have been resolved on one or more actions.
- Actions internally now allocate unmanaged memory.
  - Disposing should be taken care of automatically (though you can manually `Dispose` as well). If you see errors in the console log about unmanaged memory being leaked, please report the bug.
  - All execution state except for C# heap objects for processors, interactions, and composites has been collapsed into a single block of unmanaged memory. Actions should now be able to re-resolve efficiently without allocating additional GC memory.

### Added

- `PlayerInput` component which simplifies setting up individual player input actions and device pairings. \
  ![PlayerInput](Documentation~/Images/PlayerInput.png)
- `PlayerInputManager` component which simplifies player joining and split-screen setups. \
  ![PlayerInput](Documentation~/Images/PlayerInputManager.png)
- `InputDevice.all` (equivalent to `InputSystem.devices`)
- `InputControl.IsActuated()` can be used to determine whether control is currently actuated (defined as extension method in `InputControlExtensions`).
- Can now read control values from buffers as objects using `InputControl.ReadValueFromBufferAsObject`. This allows reading a value stored in memory without having to know the value type.
- New processors:
    * `ScaleProcessor`
    * `ScaleVector2Processor`
    * `ScaleVector3Processor`
    * `InvertVector2Processor`
    * `InvertVector3Processor`
    * `NormalizeVector2Processor`
    * `NormalizeVector3Processor`
- Added `MultiTapInteraction`. Can be used to listen for double-taps and the like.
- Can get total and average event lag times through `InputMetrics.totalEventLagTime` and `InputMetrics.averageEventLagTime`.
- `Mouse.forwardButton` and `Mouse.backButton`.
- The input debugger now shows users along with their paired devices and actions. See the [documentation](Documentation~/UserManagement.md#debugging)
- Added third and fourth barrel buttons on `Pen`.

#### Actions:
- Actions have a new continuous mode that will cause the action to trigger continuously even if there is no input. See the [documentation](Documentation~/Actions.md#continuous-actions) for details. \
  ![Continuous Action](Documentation~/Images/ContinuousAction.png)
- Actions have a new pass-through mode. In this mode an action will bypass any checks on control actuation and let any input activity on the action directly flow through. See the [documentation](Documentation~/Actions.md#pass-through-actions) for details. \
  ![Pass-Through Action](Documentation~/Images/PassThroughAction.png)
- Can now add interactions and processors directly to actions.
  ![Action Properties](Documentation~/Images/ActionProperties.png)
    * This is functionally equivalent to adding the respective processors and/or interactions to every binding on the action.
- Can now change the type of a composite retroactively.
  ![Composite Properties](Documentation~/Images/CompositeProperties.png)
- Values can now be read out as objects using `InputAction.CallbackContext.ReadValueAsObject()`.
    * Allocates GC memory. Should not be used during normal gameplay but is very useful for testing and debugging.
- Added auto-save mode for .inputactions editor.
  ![Auto Save](Documentation~/Images/AutoSave.png)
- Processors, interactions, and composites can now define their own parameter editor UIs by deriving from `InputParameterEditor`. This solves the problem of these elements not making it clear that the parameters usually have global defaults and do not need to be edited except if local overrides are necessary.
- Can now set custom min and max values for axis composites.
    ```
    var action = new InputAction();
    action.AddCompositeBinding("Axis(minValue=0,maxValue=2)")
        .With("Positive", "<Keyboard>/a")
        .With("Negative", "<Keyboard>/d");
    ```
- "C# Class File" property on .inputactions importer settings now has a file picker next to it.
- `InputActionTrace` has seen various improvements.
    * Recorded data will now stay valid even if actions are rebound to different controls.
    * Can listen to all actions using `InputActionTrace.SubscribeToAll`.
    * `InputActionTrace` now maintains a list of subscriptions. Add subscriptions with `SubscribeTo` and remove a subscription with `UnsubscribeFrom`. See the [documentation](Documentation~/Actions.md#tracing-actions) for details.

### Fixes

- Fixed support for Unity 2019.1 where we landed a native API change.
- `InputUser.UnpairDevicesAndRemoveUser()` corrupting device pairings of other InputUsers
- Control picker in UI having no devices if list of supported devices is empty but not null
- `IndexOutOfRangeException` when having multiple action maps in an asset (#359 and #358).
- Interactions timing out even if there was a pending event that would complete the interaction in time.
- Action editor updates when asset is renamed or moved.
- Exceptions when removing action in last position of action map.
- Devices marked as unsupported in input settings getting added back on domain reload.
- Fixed `Pen` causing exceptions and asserts.
- Composites that assign multiple bindings to parts failing to set up properly when parts are assigned out of order (#410).

### Known Issues

- Input processing in edit mode on 2019.1 is sporadic rather than happening on every editor update.

## [0.1.2-preview] - 2018-12-19

    NOTE: The minimum version requirement for the new input system has been bumped
          to 2018.3. The previous minum requirement of 2018.2 is no longer supported.
          Also, we have dropped support for the .NET 3.5 runtime. The new .NET 4
          runtime is now required to use the new input system.

We've started working on documentation. The current work-in-progress can be found on [GitHub](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/InputSystem.md).

### Changed

- `InputConfiguration` has been replaced with a new `InputSettings` class.
- `InputConfiguration.lockInputToGame` has been moved to `InputEditorUserSettings.lockInputToGameView`. This setting is now persisted as a local user setting.
- `InputSystem.updateMask` has been replaced with `InputSettings.updateMode`.
- `InputSystem.runInBackground` has been moved to `InputSettings.runInBackground`.
- Icons have been updated for improved styling and now have separate dark and light skin versions.
- `Lock Input To Game` and `Diagnostics Mode` are now persisted as user settings
- Brought back `.current` getters and added `InputSettings.filterNoiseOnCurrent` to control whether noise filtering on the getters is performed or not.
- Removed old and outdated Doxygen-generated API docs.

### Added

- `InputSystem.settings` contains the current input system settings.
- A new UI has been added to "Edit >> Project Settings..." to edit input system settings. Settings are stored in a user-controlled asset in any location inside `Assets/`. Multiple assets can be used and switched between.
- Joystick HIDs are now supported on Windows, Mac, and UWP.
- Can now put system into manual update mode (`InputSettings.updateMode`). In this mode, events will not get automatically processed. To process events, call `InputSystem.Update()`.
- Added shortcuts to action editor window (requires 2019.1).
- Added icons for .inputactions assets.

### Fixed

- `InputSystem.devices` not yet being initialized in `MonoBehaviour.Start` when in editor.

### Known Issues

- Input settings are not yet included in player builds. This means that at the moment, player builds will always start out with default input settings.
- There have been reports of some stickiness to buttons on 2019.1 alpha builds.  We are looking at this now.

## [0.0.14-preview] - 2018-12-11

### Changed

- `Pointer.delta` no longer has `SensitivityProcessor` on it. The processor was causing many issues with mouse deltas. It is still available for adding it manually to action bindings but the processor likely needs additional work.

### Fixed

Core:
- Invalid memory accesses when using .NET 4 runtime
- Mouse.button not being identical to Mouse.leftButton
- DualShock not being recognized when connected via Bluetooth

Actions:
- Parameters disappearing on processors and interactions in UI when edited
- Parameters on processors and interactions having wrong value type in UI (e.g. int instead of float)
- RebindingOperation calling OnComplete() after being cancelled

Misc:
- Documentation no longer picked up as assets in user project

## [0.0.13-preview] - 2018-12-5

First release from stable branch.
