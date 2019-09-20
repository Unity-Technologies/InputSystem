# Changelog
All notable changes to the input system package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

Due to package verification, the latest version below is the unpublished version and the date is meaningless.
however, it has to be formatted properly to pass verification tests.

## [1.0.0-preview] - 2019-9-20

### Fixed

-Will now close Input Action Asset Editor windows from previous sessions when the corresponding action was deleted.
- Fixed an issue where Stick Controls could not be created in Players built with medium or high code stripping level enabled.
- Fixed incorrect default state for axes on some controllers.

#### Actions

- Fixed `CallbackContext.ReadValue` throwing when invoked during device removal

### Changed
### Added

## [0.9.6-preview] - 2019-9-6

### Fixed

- Exceptions in scenes of `Visualizers` sample if respective device was not present on system (e.g. in `PenVisualizer` if no pen was present in system).
- Fixed exception in Input Action Asset Editor window when typing whitespace into the search field.
- Fixed control scheme popup window in input action asset editor window showing in the correct screen position on windows.

#### Actions

- Setting timeouts from `IInputInteraction.Process` not working as expected when processing happened in response to previous timeout expiring (#714).
- Pending timeouts on a device not being removed when device was removed.

### Changed

- Replaced `HIDSupport.shouldCreateHID` event with a new `HIDSupport.supportedHIDUsages` property, which takes an array of supported usages.

### Added

#### Actions

- Added `PlayerInput.neverAutoSwitchControlSchemes` to disable logic that automatically enables control scheme switching when there is only a single `PlayerInput` in the game.
- Added `PlayerInput.SwitchControlScheme` to switch schemes manually.

## [0.9.5-preview] - 2019-8-29

### Fixed

- Don't pass events for null devices (for devices which have not been created) to `InputSystem.onEvent` callbacks.
- Will close debugger input state windows, when the state is no longer valid instead of throwing exceptions.
- Fixed pointer coordinates in editor windows for non-mouse pointing devices.
- Fixed using the input system in il2cpp when managed stripping level is set higher then "Low".
- Device debugger window will still show when reading from specific controls throws exceptions.
- Offsets and sizes for elements on Linux joysticks are now computed correctly.
- Joysticks now have a deadzone processor on the stick itself.
- Up/down/left/right on sticks are now deadzoned just like X and Y on sticks are.
- Removed toplevel `X` and `Y` controls on HIDs when there is a `Stick/X` and `Stick/Y` added for the device.
- HID fallback can now deal with sticks that have X and Y controls of different sizes and sitting in non-contiguous locations in the HID input report.
- Button 1 on HID joysticks will now correctly come out as the `trigger` control. Previously, the trigger control on the joystick was left pointing to random state.

#### Actions

- Binding paths now show the same way in the action editor UI as they do in the control picker.
  * For example, where before a binding to `<XInputController>/buttonSouth` was shown as `rightShoulder [XInputController]`, the same binding will now show as `A [Xbox Controller]`.
- When deleting a control scheme, bindings are now updated. A dialog is presented that allows choosing between deleting the bindings or just unassigning them from the control scheme.
- When renaming a control scheme, bindings are now updated. Previously the old name was in place on bindings.
- Control scheme names can no longer be set to empty strings.
- `PlayerInput.Instantiate` now correctly sets up a given control scheme, if specified.
  * When passing a `controlScheme:` argument, the result used to be a correctly assigned control scheme at the `InputUser` level but no restrictions being actually applied to the bindings, i.e. every single binding was active regardless of the specified control scheme.
- NullReferenceExceptions during event processing from `RebindingOperation`.

### Changed

- `InputUser.onUnpairedDeviceUsed` now receives a 2nd argument which is the event that triggered the callback.
  * Also, the callback is now triggered __BEFORE__ the given event is processed rather than after the event has already been written to the device. This allows updating the pairing state of the system before input is processed.
  * In practice, this means that, for example, if the user switches from keyboard&mouse to gamepad, the initial input that triggered the switch will get picked up right away.
- `InputControlPath.ToHumanReadableString` now takes display names from registered `InputControlLayout` instances into account.
  * This means that the method can now be used to generate strings to display in rebinding UIs.
- `AxisControl.clamp` is now an enum-valued property rather than a bool. Can now perform clamping *before* normalization.

#### Actions

- When switching devices/controls on actions, the system will no longer subsequently force an initial state check on __all__ actions. Instead, every time an action's bindings get re-resolved, the system will simply cancel all on-going actions and then re-enable them the same way it would happen by manually calling `InputAction.Enable`.
- Removed non-functional `InputControlScheme.baseScheme` API and `basedOn` serialized property. This was never fully implemented.

### Added

- Can right-click devices in Input Debugger (also those under "Unsupported") and select "Copy Device Description" to copy the internal `InputDeviceDescription` of the device in JSON format to the system clipboard.
  * This information is helpful for us to debug problems related to specific devices.
- If a device description has been copied to the clipboard, a new menu "Paste Device Description as Device" entry in the "Options" menu of the input debugger appears. This instantiates the device from the description as if it was reported locally by the Unity runtime.

## [0.9.3-preview] - 2019-8-15

### Fixed

- `XInputController` and `XboxOneGamepad` no longer have two extraneous, non-functional "menu" and "view" buttons.
- Fixed `InputUser.onUnpairedDeviceUser` ignoring input on controls that do not support `EvaluateMagnitude`.
  * This led to situations, for example, where `PlayerInput` would not initialize a control scheme switch from a `<Mouse>/delta` binding as the delta X and Y axes do not have min&max limits and thus return -1 from `EvaluateMagnitude`.
- Fixed available processor list not updated right away when changing the action type in the Input Action editor window.

#### Actions

- `NullReferenceException` when the input debugger is open with actions being enabled.
- When selecting a device to add to a control scheme, can now select devices with specific usages, too (e.g. "LeftHand" XRController).

### Changed

- Removed `timesliceEvents` setting - and made this tied to the update mode instead. We now always time slice when using fixed updates, and not when using dynamic updates.
- When adding a composite, only ones compatible with the value type of the current action are shown. This will, for example, no longer display a `2D Vector` composite as an option on a floating-point button action.
- The `InputState.onChange` callback now receives a second argument which is the event (if any) that triggered the state change on the device.

### Added

- `InputSystemUIInputModule` can now track multiple pointing devices separately, to allow multi-touch input - required to allow control of multiple On-Scree controls at the same time with different fingers.
- Two new composite bindings have been added.
  * `ButtonWithOneModifier` can be used to represent shortcut-like bindings such as "CTRL+1".
  * `ButtonWithTwoModifiers` can be used to represent shortcut-like bindings such as "CTRL+SHIFT+1".


## [0.9.2-preview] - 2019-8-9

### Fixed

- A `RebindingOperation` will now fall back to the default path generation behavior if the callback provided to `OnGeneratePath` returns null.
- Fixed the Input Action editor window throwing exceptions when trying to view action properties.

### Actions

- `PlayerInput` will now copy overrides when creating duplicate actions.
- It is now possible to use an empty binding path with a non empty override path.
- It is now possible to use set an empty override path to disable a binding.
- It is not possible to query the effectively used path of a binding using `effectivePath`.
- Actions embedded into MonoBehaviour components can now have their properties edited in the inspector. Previously there was no way to get to the properties in this workflow. There is a gear icon now on the action that will open the action properties.

### Changed

### Added

- Added a new sample to the package called `SimpleDemo`. You can install the sample from the package manager. See the [README.md](https://github.com/Unity-Technologies/InputSystem/Assets/Samples/SimpleDemo/README.md) file for details about the sample.

## [0.9.1-preview] - 2019-8-8

### Fixed

- Fixed GC heap garbage being caused by triggered by event processing.
  * This meant that every processing of input would trigger garbage being allocated on the managed heap. The culprit was a peculiarity in the C# compiler which caused a struct in `InputEventPtr.IsA` to be allocated on the heap.
- The bindings selection popup window will now show child controls matching the current action type even if the parent control does not match.
- Fixed `duration` values reported for Hold and Press interactions.
- DualShock 3 on macOS:
  * Fixed actions bound to the dpad control performing correctly.
  * Fixed non-present touchpad button control being triggered incorrectly.
- Fixed compile issues with switch classes on standalone Linux.
- Leak of unmanaged memory in `InputControlList`.

#### Actions

- Fixed actions not updating their set of controls when the usages of a device are changed.
- Composite bindings with the default interaction will now correctly cancel when the composite is released, even if there are multiple composite bindings on the action.

### Changed

- `MouseState`, `KeyboardState`, and `GamepadState` have been made public again.
- `PlayerInput` and `PlayerInputManager` have been moved from the `UnityEngine.InputSystem.PlayerInput` namespace to `UnityEngine.InputSystem`.
- The signature of `InputSystem.onEvent` has changed. The callback now takes a second argument which is the device the given event is sent to (null if there's no corresponding `InputDevice`).
  ```
  // Before:
  InputSystem.onEvent +=
      eventPtr =>
      {
          var device = InputSystem.GetDeviceById(eventPtr.deviceId);
          //...
      };

  // Now:
  InputSystem.onEvent +=
      (eventPtr, device) =>
      {
          //...
      };
  ```
- The signatures of `InputSystem.onBeforeUpdate` and `InputSystem.onAfterUpdate` have changed. The callbacks no longer receive an `InputUpdateType` argument.
  * Use `InputState.currentUpdateType` in case you need to know the type of update being run.
- `InputUpdateType` has been moved to the `UnityEngine.InputSystem.LowLevel` namespace.
- `InputSystem.Update(InputUpdateType)` has been removed from the public API.
- The way input devices are built internally has been streamlined.
  * `InputDeviceBuilder` is now internal. It is no longer necessary to access it to look up child controls. Simply use `InputControl.GetChildControl` instead.
  * To build a device without adding it to the system, call the newly added `InputDevice.Build` method.
    ```
    InputDevice.Build<Mouse>();
    ```
  * `InputSystem.SetLayoutVariant` has been removed. Layout variants can no longer be set retroactively but must be decided on as part of device creation.
- `InputSystem.RegisterControlProcessor` has been renamed to just `InputSystem.RegisterProcessor`.

#### Actions

* `InputAction.ReadValue<TValue>()` is longer correlated to `InputAction.triggered`. It simply returns the current value of a bound control or composite while the action is being interacted with.
* `InputInteractionContext.PerformedAndGoBackToWaiting` has been renamed to just `InputInteractionContext.Performed`.

#### Actions

- Individual composite part bindings can now no longer have interactions assigned to them as that never made any sense.

### Added

- Devices can now have more than one usage.
  * Call `InputSystem.AddDeviceUsage(device,usage)` to add additional usages to a device.
  * Call `InputSystem.RemoveDeviceUsage(device,usage)` to remove existing usages from a device.
  * `InputSystem.SetDeviceUsage(device,usage)` still exists. It will clear all existing usages from the given device.
- A new `VisualizerSamples` sample that can be installed through the package manager.
  * Contains two components `InputControlVisualizer` and `InputActionVisualizer` that help visualizing/debugging control/device and action activity through in-game overlays. A few sample scenes illustrate how to use them.

#### Actions

- Added `InputAction.ReadValueAsObject` API.
- Added `InputAction.activeControl` API.

## [0.9.0-preview] - 2019-7-18

### Fixed

- Validate all parameters on public APIs.
- Fixed an internal bug in `InlinedArray.RemoveAtByMovingTailWithCapacity`, which could cause data corruption.
- Fixed Xbox controller support on macOS il2cpp.
- Fixed issue of Xbox gamepads on Windows desktop not being able to navigate left and down in a UI.
- Allow using InputSystem package if the XR, VR or Physics modules are disabled for smaller builds.
- Fixed documentation landing page and table of contents.
- Fixed tracked devices assigning pointer ids for UI pointer events correctly.
- Adjusted some UI Elements to fit the Unity 19.3 font.
- Fixed NullReferenceException being thrown when project changes.
- Fixed duplicate devices showing in the "Supported Devices" popup when using a search filter.
- Fixed an error when adding new bindings in the Input Actions editor window when a filter was applied.
- Fixed scroll wheel handling in `InputSystemUIInputModule` not being smooth.
- Fixed compile errors from Switch Pro controller code on Linux.

#### Actions

- Fixed `CallbackContext.control` referencing the composite member control which was actually actuated for this trigger for composite bindings.
- Generated C# wrappers for .inputactions assets are no longer placed in Assets/Assets/ folder on Windows.

### Added

- Touch support has been reworked and extended.
  * `Touchscreen.touch[0..9]` are now bindable from the control picker.
  * `Touchscreen.primaryTouch` is now a separate control which tracks the primary touch on the screen.
  * The controls `Touchscreen` inherits from `Pointer` (such as `position`, `phase`, and `delta`) are now tied to `Touchscreen.primaryTouch` and allow for `Touchscreen` to function as a generic `Pointer` (like `Mouse` and `Pen`).
  * `Touchscreen.press` (renamed from `Touchscreen.button`) is now a working, synthetic button that is down whenever at least one finger is on the screen.
  * Recording of start time and start position has been added to touches.
    - `TouchControl.startPosition` gives the starting position of the touch.
    - `TouchControl.startTime` gives the starting time of the touch.
  * Tap detection has been added to `Touchscreen`.
    - Tap time (i.e. time within which a press-and-release must be completed for a tap to register) corresponds to `InputSettings.defaultTapTime`.
    - Tap release must happen within a certain radius of first contact. This is determined by a new setting `InputSettings.tapRadius`.
    - `TouchControl.tap` is a new button control that triggers then the touch is tapped. Note that this happens instantly when a touch ends. The button will go to 1 and __immediately__ go back to 0. This means that polling the button in `Update`, for example, will never trigger a tap. Either use actions to observe the button or use the `Touch` API from `EnhancedTouch` to poll taps.
  * `Touchscreen.activeTouches` has been removed. Use `Touch.activeTouches` from the new enhanced touch API instead for more reliable touch tracking.
  * `Touchscreen.allTouchControls` has been renamed to `Touchscreen.touches`.
  * A new `EnhancedTouch` plugin has been added which offers an enhanced `Touch` and `Finger` API to reliably track touches and fingers across updates. This obsoletes the need to manually track touch IDs and phases and gives access to individual touch history.
  * Touch can be simulated from mouse or pen input now. To enable simulation, call `TouchSimulation.Enable()` or put the `TouchSimulation` MonoBehaviour in your scene. Also, in the input debugger, you can now enable touch simulation from the "Options" dropdown.
- Changing state has been decoupled from events. While input events are the primary means by which to trigger state changes, anyone can perform state changes manually now from anywhere.
    ```
    InputState.Change(gamepad.leftStick, new Vector2(123, 234));
    ```
  * This change makes it possible to update state __from__ state and thus synthesize input data from other input coming in.
- A new API for recording state changes over time has been added.
    ```
    var history = new InputStateHistory("<Gamepad>/leftStick");
    history.StartRecording();

    //...

    foreach (var record in history)
        Debug.Log(record);
    ```
- Added support for generic joysticks on WebGL (which don't use the standard gamepad mapping).
- Added support for DualShock 3 gamepads on desktops.
- Added support for Nintendo Switch Pro Controllers on desktops.

#### Actions

- Actions now also have a __polling API__!
  * `InputAction.triggered` is true if the action was performed in the current frame.
  * `InputAction.ReadValue<TValue>()` yields the last value that `started`, `performed`, or `cancelled` (whichever came last) was called with. If the action is disabled, returns `default(TValue)`. For `InputActionType.Button` type actions, returns `1.0f` if `triggered==true` and `0.0f` otherwise.
- Generated C# wrappers for .inputactions can now placed relative to the .inputactions file by specifying a path starting with './' (e.g. `./foo/bar.cs`).

### Changed

- **The system no longer supports processing input in __BOTH__ fixed and dynamic updates**. Instead, a choice has to be made whether to process input before each `FixedUpdate()` or before each `Update()`.
  * Rationale: the existing code that supported having both updates receive input independently still had several holes and became increasingly complex and brittle. Our solution was based on not actually processing input twice but on channeling input concurrently into both the state of both updates. Together with the fact that specific inputs have to reset (and possibly accumulate) correctly with respect to their update time slices, this became increasingly hard to do right. This, together with the fact that we've come to increasingly question the value of this feature, led us to removing the capability while preserving the ability to determine where input is processed.
  * NOTE: Timeslicing is NOT affected by this. You can still switch to `ProcessEventInFixedUpdates` and get events timesliced to individual `FixedUpdate` periods according to their timestamps.
  * `InputSettings.UpdateMode.ProcessEventsInBothFixedAndDynamicUpdate` has been removed.
  * `InputSettings.UpdateMode.ProcessEventsInDynamicUpdateOnly` has been renamed to `InputSettings.UpdateMode.ProcessEventsInDynamicUpdate` and is now the default.
  * `InputSettings.UpdateMode.ProcessEventsInFixedUpdateOnly` has been renamed to `InputSettings.UpdateMode.ProcessEventsInFixedUpdate`.
- Added icons for PlayerInput, PlayerInputManager, InputSystemUIInputModule and MultiplayerEventSystem components.
- Changed `Keyboard` IME properties (`imeEnabled`, `imeCursorPosition`) to methods (`SetIMEEnabled`, `SetIMECursorPosition`).
- Added getters to all `IInputRuntime` properties.
- Replace some `GetXxx` methods in our API with `xxx`  properties.
- `Pointer.phase` has been removed and `PointerPhase` has been renamed to `TouchPhase`. Phases are now specific to touch. `PointerPhaseControl` has been renamed to `TouchPhaseControl`.
- `Pointer.button` has been renamed to `Pointer.press` and now is a control that indicates whether the pointer is in "press down" state.
  * For mouse, corresponds to left button press.
  * For pen, corresponds to tip contact.
  * For touch, corresponds to primary touch contact (i.e. whether __any__ finger is down).
- The state change monitor APIs (`IInputStateChangeMonitor` and friends) have been moved out of `InputSystem` into a new static class `InputState` in `UnityEngine.Experimental.Input.LowLevel`.
  * Rationale: These APIs are fairly low-level and not of general interest so having them out of `InputSystem` reduces the API surface visible to most users.
- `InputDeviceChange.StateChanged` has been removed and is now a separate callback `InputState.onChange`.
  * Rationale: The other `InputDeviceChange` notifications are low-frequency whereas `StateChanged` is high-frequency. Putting them all on the same callback made adding a callback to `InputSystem.onDeviceChange` unnecessarily expensive.
- `IInputStateCallbackReceiver` has been rewritten from scratch. Now has two simple methods `OnNextUpdate` and `OnEvent`. If implemented by a device, the device now has completely control over changing its own state. Use the `InputState.Change` methods to affect state changes while trigger state change monitors (e.g. for actions) correctly.
- Simplified handling of XR input in `InputSystemUIInputModule` by having only one set of actions for all XR devices.
- We now use the same hierarchical device picker in the "Add Control Scheme" popup, which is already used in the "Input Settings" window.
- Made all `IInputStateTypeInfo` implementations internal, as these did not offer value to the user.
- Made all `IInputDeviceCommandInfo` implementations internal, as these did not offer value to the user.
- Removed `ReadWriteArray`, which was only used for making `RebindingOperation.scores` editable, which did not add any value.
- Removed `PrimitiveValueOrArray`, as non of it's functionality over `PrimitiveValue` was implemented.
- Made all `InputProcessor` implementation internal, as access to these types is exposed only through text mode representations.
- Removed `CurveProcessor` as it was not implemented.
- Renamed XInputControllerOSX to a more descriptive XboxGamepadMacOS.

#### Actions

- `InputAction.continuous` has been removed. Running logic every frame regardless of input can easily be achieved in game code.
- The way action behavior is configured has been simplified.
  * The previous roster of toggles has been replaced with two settings:
    1. `Action Type`: Determines the behavior of the action. Choices are `Value`, `Button`, and `PassThrough`.
    2. `Control Type`: Determines the type of control (and implicitly the type of value) the action is looking for if the action is a `Value` or `PassThrough` action.
  * The previous `Initial State Check` toggle is now implicit in the action type now. `Value` actions perform an initial state check (i.e. trigger if their control is already actuated when the action is enabled). Other types of actions don't.
  * The previous `Pass Through` toggle is now rolled into the action type.

## [0.2.10-preview] - 2019-5-17

### Added

- Added a `MultiplayerEventSystem` class, which allows you use multiple UI event systems to control different parts of the UI by different players.
- `InputSystemUIInputModule` now lets you specify an `InputActionAsset` in the `actionsAsset` property. If this is set, the inspector will populate all actions from this asset. If you have a `PlayerInput` component on the same game object, referencing the same  `InputActionAsset`, the `PlayerInput` component will keep the actions on the `InputSystemUIInputModule` in synch, allowing easy setup of multiplayer UI systems.

### Changed

- `StickControl.x` and `StickControl.y` are now deadzoned, i.e. have `AxisDeadzone` processors on them. This affects all gamepads and joysticks.
  * __NOTE:__ The deadzoning is __independent__ of the stick. Whereas the stack has a radial deadzones, `x` and `y` have linear deadzones. This means that `leftStick.ReadValue().x` is __not__ necessary equal to `leftStick.x.ReadValue()`.
  * This change also fixes the problem of noise from sticks not getting filtered out and causing devices such as the PS4 controller to constantly make itself `Gamepad.current`.

- Redesigned `UIActionInputModule`
 * Added a button in the inspector to automatically assign actions from an input action asset based on commonly used action names.
 * Will now populate actions with useful defaults.
 * Removed `clickSpeed` property - will use native click counts from the OS where available instead.
 * Removed `sendEventsWhenInBackground` property.
 * Hiding `Touches` and `TrackedDevices` until we decide how to handle them.
 * Remove `moveDeadzone` property as it is made redundant by the action's dead zone.
 * Removed `UIActionInputModuleEnabler` component, `UIActionInputModule` will now enable itself.
- Changed default button press point to 0.5.
- Changed all constants in public API to match Unity naming conventions ("Constant" instead of "kConstant").
- Changed namespace from `UnityEngine.Experimental.Input` to `UnityEngine.InputSystem`.
- Generated wrapper code now has nicer formatting.
- Renamed `UIActionInputModule` to `InputSystemUIInputModule`.
- Nicer icons for `InputActionAssets` and `InputActions` and for `Button` and generic controls.
- Change all public API using `IntPtr` to use unsafe pointer types instead.
- `PlayerInput` will no longer disable any actions not in the currently active action map when disabling input or switching action maps.
- Change some public fields into properties.
- Input System project settings are now called "Input System Package" in the project window instead of "Input (NEW)".
- Removed `Plugins` from all namespaces.
- Rename "Cancelled" -> "Canceled" (US spelling) in all APIs.

### Fixed

- Adding devices to "Supported Devices" in input preferences not allowing to select certain device types (like "Gamepad").
- Fixed scrolling in `UIActionInputModule`.
- Fixed compiling the input system package in Unity 19.2 with ugui being moved to a package now.
- In the Input System project settings window, you can no longer add a supported device twice.

#### Actions

- Custom inspector for `PlayerInput` no longer adds duplicates of action events if `Invoke Unity Events` notification behavior is selected.
- Fixed `Hold` interactions firing immediately before the duration has passed.
- Fixed editing bindings or processors for `InputAction` fields in the inspector (Changes wouldn't persist before).
- Fixed exception message when calling `CallbackContext.ReadValue<TValue>()` for an action with a composite binding with `TValue` not matching the composite's value type.

### Added

#### Actions

- `PlayerInput` can now handle `.inputactions` assets that have no control schemes.
  * Will pair __all__ devices mentioned by any of the bindings except if already paired to another player.

## [0.2.8-preview] - 2019-4-23

### Added

- Added a `clickCount` control to the `Mouse` class, which specifies the click count for the last mouse click (to allow distinguishing between single-, double- and multi-clicks).
- Support for Bluetooth Xbox One controllers on macOS.

#### Actions

- New API for changing bindings on actions
```
    // Several variations exist that allow to look up bindings in various ways.
    myAction.ChangeBindingWithPath("<Gamepad>/buttonSouth")
        .WithPath("<Keyboard>/space");

    // Can also replace the binding wholesale.
    myAction.ChangeBindingWithPath("<Keyboard>/space")
        .To(new InputBinding { ... });

    // Can also remove bindings programmatically now.
    myAction.ChangeBindingWithPath("<Keyboard>/space").Erase();
```

### Changed

- `Joystick.axes` and `Joystick.buttons` have been removed.
- Generated wrapper code for Input Action Assets are now self-contained, generating all the data from code and not needing a reference to the asset; `InputActionAssetReference` has been removed.
- The option to generate interfaces on wrappers has been removed, instead we always do this now.
- The option to generate events on wrappers has been removed, we felt that this no longer made sense.
- Will now show default values in Input Action inspector if no custom values for file path, class name or namespace have been provided.
- `InputSettings.runInBackground` has been removed. This should now be supported or not on a per-device level. Most devices never supported it in the first place, so a global setting did not seem to be useful.
- Several new `Sensor`-based classes have been added. Various existing Android sensor implementations are now based on them.
- `InputControlLayoutAttribute` is no longer inherited.
  * Rationale: A class marked as a layout will usually be registered using `RegisterLayout`. A class derived from it will usually be registered the same way. Because of layout inheritance, properties applied to the base class through `InputControlLayoutAttribute` will affect the subclass as intended. Not inheriting the attribute itself, however, now allows having properties such as `isGenericTypeOfDevice` which should not be inherited.
- Removed `acceleration`, `orientation`, and `angularVelocity` controls from `DualShockGamepad` base class.
  * They are still on `DualShockGamepadPS4`.
  * The reason is that ATM we do not yet support these controls other than on the PS4. The previous setup pretended that these controls work when in fact they don't.
- Marking a control as noisy now also marks all child controls as noisy.
- The input system now defaults to ignoring any HID devices with usage types not known to map to game controllers. You can use `HIDSupport.supportedUsages` to enable specific usage types.
- In the Input Settings window, asset selection has now been moved to the "gear" popup menu. If no asset is created, we now automatically create one.
- In the inspector for Input Settings assets, we now show a button to go to the Input Settings window, and a button to make the asset active if it isn't.
- Tests are now no longer part of the com.unity.inputsystem package. The `InputTestFixture` class still is for when you want to write input-related tests for your project. You can reference the `Unity.InputSystem.TestFixture` assembly when you need to do that.
- Implemented adding usages to and removing them from devices.

#### Actions

- A number of changes have been made to the control picker UI in the editor. \
  ![Input Control Picker](Documentation~/Images/InputControlPicker.png)
  * The button to pick controls interactively (e.g. by pressing a button on a gamepad) has been moved inside the picker and renamed to "Listen". It now works as a toggle that puts the picker into a special kind of 'search' mode. While listening, suitable controls that are actuated will be listed in the picker and can then be picked from.
  * Controls are now displayed with their nice names (e.g. "Cross" instead of "buttonSouth" in the case of the PS4 controller).
  * Child controls are indented instead of listed in "parent/child" format.
  * The hierarchy of devices has been rearranged for clarity. The toplevel groups of "Specific Devices" and "Abstract Devices" are now merged into one hierarchy that progressively groups devices into more specific groups.
  * Controls now have icons displayed for them.
- There is new support for binding to keys on the keyboard by their generated character rather than by their location. \
  ![Keyboard Binding](Documentation~/Images/KeyboardBindByLocationVsCharacter.png)
  * At the toplevel of the the Keyboard device, you now have the choice of either binding by keyboard location or binding by generated/mapped character.
  * Binding by location shows differences between the local keyboard layout and the US reference layout.
  * The control path language has been extended to allow referencing controls by display name. `<Keyboard>/#(a)` binds to the control on a `Keyboard` with the display name `a`.
- `continuous` flag is now ignored for `Press and Release` interactions, as it did not  make sense.
- Reacting to controls that are already actuated when an action is enabled is now an __optional__ behavior rather than the default behavior. This is a __breaking__ change.
  * Essentially, this change reverts back to the behavior before 0.2-preview.
  * To reenable the behavior, toggle "Initial State Check" on in the UI or set the `initialStateCheck` property in code.
  ![Inital State Check](Documentation~/Images/InitialStateCheck.png)
  * The reason for the change is that having the behavior on by default made certain setups hard to achieve. For example, if `<Keyboard>/escape` is used in one action map to toggle *into* the main menu and in another action map to toggle *out* of it, then the previous behavior would immediately exit out of the menu if `escape` was still pressed from going into the menu. \
  We have come to believe that wanting to react to the current state of a control right away is the less often desirable behavior and so have made it optional with a separate toggle.
- Processors and Interactions are now shown in a component-inspector-like fashion in the Input Action editor window, allowing you to see the properties of all items at once.
- The various `InputAction.lastTriggerXXX` APIs have been removed.
  * Rationale: They have very limited usefulness and if you need the information, it's easy to set things up in order to keep track of it yourself. Also, we plan on having a polling API for actions in the future which is really what the `lastActionXXX` APIs were trying to (imperfectly) solve.
- `Tap`, `SlowTap`, and `MultiTap` interactions now respect button press points.
- `Tap`, `SlowTap`, and `MultiTap` interactions now have improved parameter editing UIs.

### Fixed

- Input Settings configured in the editor are now transferred to the built player correctly.
- Time slicing for fixed updates now works correctly, even when pausing or dropping frames.
- Make sure we Disable any InputActionAsset when it is being destroyed. Otherwise, callbacks which were not cleaned up would could cause exceptions.
- DualShock sensors on PS4 are now marked as noisy (#494).
- IL2CPP causing issues with XInput on windows and osx desktops.
- Devices not being available yet in `MonoBehavior.Awake`, `MonoBehaviour.Start`, and `MonoBehaviour.OnEnable` in player or when entering play mode in editor.
- Fixed a bug where the event buffer used by `InputEventTrace` could get corrupted.

#### Actions

- Actions and bindings disappearing when control schemes have spaces in their names.
- `InputActionRebindingExceptions.RebindOperation` can now be reused as intended; used to stop working properly the first time a rebind completed or was cancelled.
- Actions bound to multiple controls now trigger correctly when using `PressInteraction` set to `ReleaseOnly` (#492).
- `PlayerInput` no longer fails to find actions when using UnityEvents (#500).
- The `"{...}"` format for referencing action maps and actions using GUIDs as strings has been obsoleted. It will still work but adding the extra braces is no longer necessary.
- Drag&dropping bindings between other bindings that came before them in the list no longer drops the items at a location one higher up in the list than intended.
- Editing name of control scheme in editor not taking effect *except* if hitting enter key.
- Saving no longer causes the selection of the current processor or interaction to be lost.
  * This was especially annoying when having "Auto-Save" on as it made editing parameters on interactions and processors very tedious.
- In locales that use decimal separators other than '.', floating-point parameters on composites, interactions, and processors no longer lead to invalid serialized data being generated.
- Fix choosing "Add Action" in action map context menu throwing an exception.
- The input action asset editor window will no longer fail saving if the asset has been moved.
- The input action asset editor window will now show the name of the asset being edited when asking for saving changes.
- Clicking "Cancel" in the save changes dialog for the input action asset editor window will now cancel quitting the editor.
- Fixed pasting or dragging a composite binding from one action into another.
- In the action map editor window, switching from renaming an action to renaming an action map will no longer break the UI.
- Fixed calling Enable/Disable from within action callbacks sometimes leading to corruption of state which would then lead to actions not getting triggered (#472).
- Fixed setting of "Auto-Save" toggle in action editor getting lost on domain reload.
- Fixed blurry icons in editor for imported .inputactions assets and actions in them.
- `Press` and `Release` interactions will now work correctly if they have multiple bound controls.
- `Release` interactions will now invoke a `Started` callback when the control is pressed.
- Made Vector2 composite actions respect the press points of button controls used to compose the value.

## [0.2.6-preview] - 2019-03-20

>NOTE: The UI code for editing actions has largely been rewritten. There may be regressions.
>NOTE: The minimum version requirement for the new input system has been bumped
       to 2019.1

### Added

- Support gamepad vibration on Switch.
- Added support for Joysticks on Linux.

#### Actions

- Added ability to change which part of a composite a binding that is part of the composite is assigned to.
  * Part bindings can now be freely duplicated or copy-pasted. This allows having multiple bindings for "up", for example. Changing part assignments retroactively allows to freely edit the composite makeup.
- Can now drag&drop multiple items as well as drop items onto others (equivalent to cut&paste). Holding ALT copies data instead of moving it.
- Edits to control schemes are now undoable.
- Control schemes are now sorted alphabetically.
- Can now search by binding group (control scheme) or devices directly from search box.
  * `g:Gamepad` filters bindings to those in the "Gamepad" group.
  * `d:Gamepad` filters bindings to those from Gamepad-compatible devices.

### Changed

- The input debugger will no longer automatically show remote devices when the profiler is connected. Instead, use the new menu in debugger toolbar to connect to players or to enable/disable remote input debugging.
- "Press and Release" interactions will now invoke the `performed` callback on both press and release (instead of invoking `performed` and `cancel`, which was inconsistent with other behaviors).

#### Actions

- Bindings have GUIDs now like actions and maps already did. This allows to persistently and uniquely identify individual bindings.
- Replaced UI overlay while rebinding interactively with cancellable progress bar. Interactive rebinding now cancels automatically after 4 seconds without suitable input.
- Bindings that are not assigned to any control scheme are now visible when a particular control scheme is selected.
  * Bindings not assigned to any control scheme are active in *ALL* control schemes.
  * The change makes this visible in the UI now.
  * When a specific control scheme is selected, these bindings are affixed with `{GLOBAL}` for added visibility.
- When filtering by devices from a control scheme, the filtering now takes layout inheritance into account. So, a binding to a control on `Pointer` will now be shown when the filter is `Mouse`.
- The public control picker API has been revised.
  * The simplest way to add control picker UI to a control path is to add an `InputControlAttribute` to the field.
    ```
    // In the inspector, shows full UI to select a control interactively
    // (including interactive picking through device input).
    [InputControl(layout = "Button")]
    private string buttonControlPath;
    ```
- Processors of incompatible types will now be ignored instead of throwing an exception.

### Fixed

- Remote connections in input debugger now remain connected across domain reloads.
- Don't incorrectly create non-functioning devices if a physical device implements multiple incompatible logical HID devices (such as the MacBook keyboard/touch pad and touch bar).
- Removed non-functioning sort triangles in event list in Input Debugger device windows.
- Sort events in input debugger window by id rather then by timestamp.
- Make parsing of float parameters support floats represented in "e"-notation and "Infinity".
- Input device icons in input debugger window now render in appropriate resolution on retina displays.
- Fixed Xbox Controller on macOS reporting negative values for the sticks when represented as dpad buttons.
- `InputSettings.UpdateMode.ProcessEventsManually` now correctly triggers updates when calling `InputSystem.Update(InputUpdateType.Manual)`.

#### Actions

- Pasting or duplicating an action in an action map asset will now assign a new and unique ID to the action.
- "Add Action" button being active and triggering exceptions when no action map had been added yet.
- Fixed assert when generating C# class and make sure it gets imported correctly.
- Generate directories as needed when generating C# class, and allow path names without "Assets/" path prefix.
- Allow binding dpad controls to actions of type "Vector2".
- Fixed old name of action appearing underneath rename overlay.
- Fixed inspector UIs for on-screen controls throwing exceptions and being non-functional.
- Fixed deleting multiple items at same time in action editor leading to wrong items being deleted.
- Fixed copy-pasting actions not preserving action properties other than name.
- Fixed memory corruptions coming from binding resolution of actions.
- InputActionAssetReferences in ScriptableObjects will continue to work after domain reloads in the editor.
- Fixed `startTime` and `duration` properties of action callbacks.

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
- Removed TouchPositionTransformProcessor, was used only by Android, the position transformation will occur in native backend in 2019.x

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
