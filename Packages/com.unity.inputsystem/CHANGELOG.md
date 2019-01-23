# Changelog
All notable changes to the input system package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.3-preview] (Teuton) - ????

This release contains a number of fairly significant changes. The focus has been on further improving the action system to make it easier to use as well as to make it work more reliably and predictably.

>NOTE: There are some breaking changes. Please see the "Changed" section below.

### Changed

- Removed Unity 2018.2 support code.
- Removed .NET 3.5 support code.
- Started using C# 7.
- `IInputControlProcessor<TValue>` has been replaced with `InputProcessor` and `InputProcess<TValue>` base classes.
- `IInputBindingComposite` has been replaced with an `InputBindingComposite` base class and the `IInputBindingComposite<TValue>` interface has been merged with the `InputBindingComposite<TValue>` class which had already existed.
- `InputUser.onUnpairedDeviceUser` will now notify for each actuated control until the device is paired or there are no more actuated controls.
- `SensitivityProcessor` has been removed.
    * The approach needs rethinking. What `SensitivityProcessor` did caused more problems than it solved.
- State monitors no longer have their timeouts removed automatically when they fire. This makes it possible to have a timeout that is removed only in response to a specific state change.

#### Actions:
- Bindings that have no interactions on them will trigger differently now. __This is a breaking change__.
  * Previously, these bindings would trigger `performed` on every value change including when going back to their default value. This is why you would see two calls of `performed` with a button; one when the button was pressed, another when it was depressed.
  * Now, a binding without an interaction will trigger `started` and then `performed` when a bound control is actuated. Thereafter, the action will remain in `Started` phase. For as long as the control is actuated, every value change will trigger `performed` again. When the control stops being actuated, it will trigger `cancelled` and the action will remain in `Waiting` state.
  * Control actuation is defined as a control having a magnitude (see `InputControl.EvaluateMagnitude`) greater than zero. If a control does not support magnitudes (returns -1 from `EvaluateMagnitude`), then the control is considered actuated when it changes state away from its default state.
- As part of the aforementioned change, the following interactions have been removed as they are no longer relevant:
  - `StickInteraction`: Can simply be removed from bindings. The new default behavior obsoletes the need for what `StickInteraction` did. Use `started` to know then the stick starts being actuated, `performed` to be updated on movements, and `cancelled` to know when the stick goes back into rest position.
  - `PressInteraction`: Can simply be removed from bindings. The default behavior with no interaction encompasses press detection. Use either `started` or `performed` to know when a button is pressed. There will no longer be a `performed` call on button release. To set a custom button press point, simply put an `AxisDeadzoneProcessor` on the binding.
  - `PressAndReleaseInteraction`: Can simply be removed from bindings. The default behavior with no interaction encompasses press and release detection. Use `started` to know then a button is pressed and `cancelled` to know when it is released. To set a custom button press point, simply put an `AxisDeadzoneProcessor` on the binding.
- If bound controls are already actuated when an action is enabled, the action will now trigger in the next input update as if the control had just been moved from non-actuated to actuated state.
  - In other words, if e.g. you have a binding to the A button of the gamepad and the A button is already pressed when the action is first enabled, then the action associated with the A button will trigger as if the button had just been pressed. Previously, it required releasing and re-pressing the button first -- which, together with certain interactions, could lead to actions ending up in a confused state.
- When an action is disabled, it will now cancel all ongoing interactions, if any (i.e. you will see `InputAction.cancelled` being called).
  - Note that unlike the above-mentioned callbacks that happen when an action starts out with a control already actuated, the cancellation callbacks happen __immediately__ rather than in the next input update.
- Action editor now gets docked by default.
- Interactions and processors in the UI are now filtered based on the type of the action (if set) and sorted by name.
- Renamed "Axis" and "Dpad" composites to "1D Axis" and "2D Vector" composite.
    - The old names can still be used and existing data will load as expected.
    - `DpadComposite` got renamed to `Vector2Composite`; `AxisComposite` is unchanged.
- `InputInteractionContext.controlHasDefaultValue` has been replaced with `InputInteractionContext.ControlIsActuated()`.

### Added

- `PlayerInput` and `PlayerInputManager` MonoBehaviours that simplify setting up player device/action management, player joins, and split-screen setups.
- New processors:
    * `ScaleProcessor`
    * `ScaleVector2Processor`
    * `ScaleVector3Processor`

### Fixes

- `InputUser.UnpairDevicesAndRemoveUser()` corrupting devices pairings of other InputUsers
- Control picker in UI having no devices if list of supported devices is empty but not null

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
