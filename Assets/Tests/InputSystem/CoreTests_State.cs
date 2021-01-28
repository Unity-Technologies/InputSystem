using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

partial class CoreTests
{
    [Test]
    [Category("State")]
    public void State_CanGetCurrentUpdateType()
    {
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;

        Assert.That(InputState.currentUpdateType, Is.EqualTo(default(InputUpdateType)));

        InputSystem.Update();
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Dynamic));

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Dynamic));

        InputSystem.Update();
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Fixed));

        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Fixed));

        InputSystem.Update();
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Manual));

        #if UNITY_EDITOR
        runtime.onShouldRunUpdate = _ => true;
        InputSystem.Update(InputUpdateType.Editor);
        Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Editor));
        #endif
    }

    [Test]
    [Category("State")]
    public void State_CanGetUpdateCount()
    {
        Assert.That(InputState.updateCount, Is.Zero);

        InputSystem.Update();
        Assert.That(InputState.updateCount, Is.EqualTo(1));

        InputSystem.Update();
        Assert.That(InputState.updateCount, Is.EqualTo(2));
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_CanGetUpdateCount_ForEditorUpdates()
    {
        InputSystem.Update(InputUpdateType.Editor);
        Assert.That(InputState.updateCount, Is.EqualTo(1));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(gamepad.stateBlock.sizeInBits, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>() * 8));
        Assert.That(gamepad.leftStick.stateBlock.byteOffset,
            Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "leftStick").ToInt32()));
        Assert.That(gamepad.dpad.stateBlock.byteOffset,
            Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "buttons").ToInt32()));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        InputSystem.RegisterLayout<CustomDevice>();

        var device = InputDevice.Build<CustomDevice>();
        var axis2 = device.GetChildControl("axis2");

        var nestedOffset = Marshal.OffsetOf(typeof(CustomDeviceState), "nested").ToInt32();
        var axis2Offset = nestedOffset + Marshal.OffsetOf(typeof(CustomNestedDeviceState), "axis2").ToInt32();

        Assert.That(axis2.stateBlock.byteOffset, Is.EqualTo(axis2Offset));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForMultiByteBitfieldWithFixedOffset()
    {
        var keyboard = InputDevice.Build<Keyboard>();
        var downArrow = keyboard.GetChildControl("DownArrow");

        Assert.That(downArrow.stateBlock.bitOffset, Is.EqualTo((int)Key.DownArrow));
        Assert.That(downArrow.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(keyboard.stateBlock.alignedSizeInBytes, Is.EqualTo(KeyboardState.kSizeInBytes));
    }

    [Test]
    [Category("State")]
    public void State_BeforeAddingDevice_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        var device = InputDevice.Build<Gamepad>();

        var leftStickOffset = Marshal.OffsetOf(typeof(GamepadState), "leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(leftStickXOffset));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_AfterAddingDevice_AllControlOffsetsAreRelativeToGlobalStateBuffer()
    {
        InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        var leftStickOffset = Marshal.OffsetOf(typeof(GamepadState), "leftStick").ToInt32();
        var leftStickXOffset = leftStickOffset;
        var leftStickYOffset = leftStickOffset + 4;

        var gamepad2StartOffset = gamepad2.stateBlock.byteOffset;

        Assert.That(gamepad2.leftStick.x.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickXOffset));
        Assert.That(gamepad2.leftStick.y.stateBlock.byteOffset, Is.EqualTo(gamepad2StartOffset + leftStickYOffset));
    }

    [Test]
    [Category("State")]
    public void State_StateOfMultipleDevicesIsLaidOutSequentially()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        var sizeofGamepadState = UnsafeUtility.SizeOf<GamepadState>();

        Assert.That(gamepad1.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(gamepad1.stateBlock.byteOffset + sizeofGamepadState));
    }

    [Test]
    [Category("State")]
    public void State_RunningUpdateSwapsCurrentAndPrevious()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var oldState = new GamepadState
        {
            leftTrigger = 0.25f
        };
        var newState = new GamepadState
        {
            leftTrigger = 0.75f
        };

        InputSystem.QueueStateEvent(gamepad, oldState);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25f).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75f).Within(0.00001));
        Assert.That(gamepad.leftTrigger.ReadValueFromPreviousFrame(), Is.EqualTo(0.25f).Within(0.00001));
    }

    // This test makes sure that a double-buffered state scheme does not lose state. In double buffering,
    // this only works if either the entire state is refreshed each step -- which for us is not guaranteed
    // as we don't know if a state event for a device will happen on a frame -- or if state is copied forward
    // between the buffers.
    [Test]
    [Category("State")]
    public void State_UpdateWithoutStateEventDoesNotAlterStateOfDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var state = new GamepadState
        {
            leftTrigger = 0.25f
        };

        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25f).Within(0.000001));
    }

    // The state layout for a given device is not fixed. Even though Gamepad, for example, specifies
    // GamepadState as its state struct, this does not necessarily mean that an actual Gamepad instance
    // will actually end up with that specific state layout. This is why Gamepad should not assume
    // that 'currentValuePtr' is a pointer to a GamepadState.
    //
    // Layouts can be used to re-arrange the state layout of their base layout. One case where
    // this is useful are HIDs. On OSX, for example, gamepad state data does not arrive in its own
    // distinct format but rather comes in as the same generic state data as any other HID device.
    // Yet we still want a gamepad to come out as a Gamepad and not as a generic InputDevice. If we
    // weren't able to customize the state layout of a gamepad, we'd have to have code somewhere
    // along the way that takes the incoming HID data, interprets it to determine that it is in
    // fact coming from a gamepad HID, and re-arranges it into a GamepadState-compatible format
    // (which requires knowledge of the specific layout used by the HID). By having flexible state
    // layouts we can do this entirely through data using just layouts.
    //
    // A layout that customizes state layout can also "park" unused controls outside the block of
    // data that will actually be sent in via state events. Space for the unused controls will still
    // be allocated in the state buffers (since InputControls still refer to it) but InputManager
    // is okay with sending StateEvents that are shorter than the full state block of a device.
    ////REVIEW: we might want to equip InputControls with the ability to be disabled (in which case they return default values)
    [Test]
    [Category("State")]
    public void State_CanCustomizeStateLayoutOfDevice()
    {
        // Create a custom layout that moves the offsets of some controls around.
        var jsonLayout = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""format"" : ""CUST"",
                ""controls"" : [
                    {
                        ""name"" : ""buttonSouth"",
                        ""offset"" : 800
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(jsonLayout);

        var device = InputDevice.Build<Gamepad>("CustomGamepad");

        Assert.That(device.GetChildControl("buttonSouth").stateBlock.byteOffset, Is.EqualTo(800));
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(801 * 8)); // Button bitfield adds one byte.
    }

    [Test]
    [Category("State")]
    public void State_DoesNotNeedToBe4ByteAligned()
    {
        var jsonLayout = @"
            {
                ""name"" : ""TestDevice"",
                ""format"" : ""CUST"",
                ""controls"" : [
                    {
                        ""name"" : ""button1"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(jsonLayout);

        var device1 = InputSystem.AddDevice("TestDevice");
        var device2 = InputSystem.AddDevice("TestDevice");

        // State block sizes should correspond exactly to what's on the device aligned
        // to next byte offset.
        Assert.That(device1.stateBlock.sizeInBits, Is.EqualTo(8));
        Assert.That(device2.stateBlock.sizeInBits, Is.EqualTo(8));

        // But offsets in the state buffers should be 4-byte aligned. This ensures that we
        // comply to alignment restrictions on ARMs.
        Assert.That(device1.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(device2.stateBlock.byteOffset, Is.EqualTo(4));
    }

    [Test]
    [Category("State")]
    public void State_CanStoreAxisAsShort()
    {
        // Make right trigger be represented as just a short and force it to different offset.
        var jsonLayout = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""format"" : ""SHRT"",
                        ""offset"" : 0
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(jsonLayout);

        var device = InputDevice.Build<Gamepad>("CustomGamepad");

        Assert.That(device.rightTrigger.stateBlock.format, Is.EqualTo(InputStateBlock.FormatShort));
    }

    [Test]
    [Category("State")]
    [TestCase("Axis")]
    [TestCase("Double")]
    public void State_CanStoreControlAsMultiBitfield(string controlType)
    {
        var json = @"
        {
            ""name"" : ""TestDevice"",
            ""controls"" : [
                {
                    ""name"" : ""max"",
                    ""layout"" : ""__CONTROLTYPE__"",
                    ""format"" : ""BIT"",
                    ""offset"" : 0,
                    ""sizeInBits"" : 7,
                    ""defaultState"" : 127
                },
                {
                    ""name"" : ""min"",
                    ""layout"" : ""__CONTROLTYPE__"",
                    ""format"" : ""BIT"",
                    ""offset"" : 1,
                    ""bit"" : 0,
                    ""sizeInBits"" : 7,
                    ""defaultState"" : 0
                },
                {
                    ""name"" : ""mid"",
                    ""layout"" : ""__CONTROLTYPE__"",
                    ""format"" : ""BIT"",
                    ""offset"" : 2,
                    ""sizeInBits"" : 7,
                    ""defaultState"" : 63
                }
            ]
        }".Replace("__CONTROLTYPE__", controlType);

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestDevice");

        var min = device["min"];
        var max = device["max"];
        var mid = device["mid"];

        var minValue = min.ReadValueAsObject();
        var maxValue = max.ReadValueAsObject();
        var midValue = mid.ReadValueAsObject();

        Assert.That(minValue, Is.EqualTo(0).Within(0.00001));
        Assert.That(maxValue, Is.EqualTo(1).Within(0.00001));
        Assert.That(midValue, Is.EqualTo(0.5).Within(1 / 128f)); // Precision dictated by number of bits we have available.
    }

    [Test]
    [Category("State")]
    public void State_AppendsControlsWithoutForcedOffsetToEndOfState()
    {
        var json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""controlWithFixedOffset"",
                        ""layout"" : ""Analog"",
                        ""offset"" : ""10"",
                        ""format"" : ""FLT""
                    },
                    {
                        ""name"" : ""controlWithAutomaticOffset"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputDevice.Build<InputDevice>("MyDevice");

        Assert.That(device.GetChildControl("controlWithAutomaticOffset").stateBlock.byteOffset, Is.EqualTo(14));
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(15 * 8));
    }

    [Test]
    [Category("State")]
    public void State_CanSpecifyBitOffsetsOnControlProperties()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.dpad.right.stateBlock.bitOffset, Is.EqualTo((int)DpadControl.ButtonBits.Right));
        Assert.That(gamepad.dpad.right.stateBlock.byteOffset, Is.EqualTo(gamepad.dpad.stateBlock.byteOffset));
    }

    // Using "offset = N" with an InputControlAttribute that doesn't specify a child path (or even then?)
    // should add the base offset of the field itself.
    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_SpecifyingOffsetOnControlAttribute_AddsBaseOffset()
    {
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void State_CanUpdateButtonStateUsingEvent()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonEast.isPressed, Is.False);

        var newState = new GamepadState {buttons = 1 << (int)GamepadButton.B};
        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.isPressed, Is.True);
    }

    [Test]
    [Category("State")]
    public void State_CanDetectWhetherButtonStateHasChangedThisFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonEast.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.buttonEast.wasReleasedThisFrame, Is.False);

        var firstState = new GamepadState {buttons = 1 << (int)GamepadButton.B};
        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasPressedThisFrame, Is.True);
        Assert.That(gamepad.buttonEast.wasReleasedThisFrame, Is.False);

        // Input update with no changes should make both properties go back to false.
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.buttonEast.wasReleasedThisFrame, Is.False);

        var secondState = new GamepadState {buttons = 0};
        InputSystem.QueueStateEvent(gamepad, secondState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.buttonEast.wasReleasedThisFrame, Is.True);
    }

    // The way we keep state does not allow observing the state change on the final
    // state of the button. However, actions will still see the change.
    [Test]
    [Category("State")]
    public void State_PressingAndReleasingButtonInSameFrame_DoesNotShowStateChange()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadButton.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(gamepad.buttonEast.isPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasPressedThisFrame, Is.False);
        Assert.That(gamepad.buttonEast.wasReleasedThisFrame, Is.False);
    }

    [Test]
    [Category("State")]
    public void State_CanStoreButtonAsFloat()
    {
        // Turn buttonSouth into float and move to left/x offset (so we can use
        // GamepadState to set it).
        const string json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""buttonSouth"",
                        ""format"" : ""FLT"",
                        ""offset"" : 4,
                        ""bit"" : 0
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var state = new GamepadState {leftStick = new Vector2(0.5f, 0.0f)};

        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(gamepad.buttonSouth.ReadValue(), Is.EqualTo(0.5f));
    }

    [Test]
    [Category("State")]
    public void State_CanListenForInputUpdates()
    {
        // Bypass InputManager.ShouldRunUpdate().
        runtime.onShouldRunUpdate = _ => true;

        var receivedUpdate = false;
        InputSystem.onBeforeUpdate +=
            () =>
        {
            Assert.That(receivedUpdate, Is.False);
            receivedUpdate = true;
        };

        InputSystem.Update();

        Assert.That(receivedUpdate, Is.True);

        receivedUpdate = false;

        // Before render. Disabled by default. Add a device that needs before-render updates
        // so that the update gets enabled.
        const string kBeforeRenderDevice = @"
            {
                ""name"" : ""BeforeRenderGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";
        InputSystem.RegisterLayout(kBeforeRenderDevice);
        InputSystem.AddDevice("BeforeRenderGamepad");
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(receivedUpdate, Is.True);

#if UNITY_EDITOR
        receivedUpdate = false;

        // Editor.
        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(receivedUpdate, Is.True);
#endif
    }

    // To build systems that can respond to inputs changing value, there's support for setting
    // up monitor on state (essentially locks around memory regions). This is used by the action
    // system to build its entire machinery but the core mechanism is available to anyone.
    [Test]
    [Category("State")]
    public void State_CanSetUpMonitorsForStateChanges()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var monitorFired = false;
        InputControl receivedControl = null;
        double? receivedTime = null;
        InputEventPtr? receivedEventPtr = null;

        var monitor = InputState.AddChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
                receivedControl = control;
                receivedTime = time;
                receivedEventPtr = eventPtr;
            });

        // Left stick only.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)}, 0.5);
        InputSystem.Update();

        Assert.That(monitorFired, Is.True);
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
        Assert.That(receivedTime.Value, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.deviceId));

        monitorFired = false;
        receivedControl = null;
        receivedTime = 0;
        receivedEventPtr = null;

        // Left stick again but with no value change.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)}, 0.6);
        InputSystem.Update();

        Assert.That(monitorFired, Is.False);

        // Left and right stick.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {rightStick = new Vector2(0.75f, 0.75f), leftStick = new Vector2(0.75f, 0.75f)}, 0.7);
        InputSystem.Update();

        Assert.That(monitorFired, Is.True);
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
        Assert.That(receivedTime.Value, Is.EqualTo(0.7).Within(0.000001));
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.deviceId));

        monitorFired = false;
        receivedControl = null;
        receivedTime = 0;
        receivedEventPtr = null;

        // Right stick only.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {rightStick = new Vector2(0.5f, 0.5f), leftStick = new Vector2(0.75f, 0.75f)}, 0.8);
        InputSystem.Update();

        Assert.That(monitorFired, Is.False);

        // Component control of left stick.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.75f, 0.5f)}, 0.9);
        InputSystem.Update();

        Assert.That(monitorFired, Is.True);
        ////REVIEW: do we want to be able to detect the child control that actually changed? could be multiple, though
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
        Assert.That(receivedTime.Value, Is.EqualTo(0.9).Within(0.000001));
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.deviceId));

        // Remove state monitor and change leftStick again.
        InputState.RemoveChangeMonitor(gamepad.leftStick, monitor);

        monitorFired = false;
        receivedControl = null;
        receivedTime = 0;
        receivedEventPtr = null;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.0f, 0.0f)}, 1.0);
        InputSystem.Update();

        Assert.That(monitorFired, Is.False);
    }

    #if UNITY_EDITOR
    [Test]
    [Category("State")]
    public void State_CanSetUpMonitorsForStateChanges_InEditor()
    {
        InputEditorUserSettings.lockInputToGameView = false;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        var monitorFired = false;
        InputState.AddChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) => monitorFired = true);

        runtime.PlayerFocusLost();
        Set(gamepad.leftStick, new Vector2(0.123f, 0.234f), queueEventOnly: true);
        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(monitorFired, Is.True);
    }

    #endif

    struct StateWithMultiBitControl : IInputStateTypeInfo
    {
        // Dpad layout that is identical to how the PS4 DualShock controller sets up its Dpad.
        // Offset the whole dpad by 3 bits to make sure we're not only supporting the case where
        // the multi-bit value is byte-aligned.
        [InputControl(name = "dpad", layout = "Dpad", sizeInBits = 4, bit = 3)]
        [InputControl(name = "dpad/up", layout = "DiscreteButton", parameters = "minValue=7,maxValue=1,nullValue=8,wrapAtValue=7", bit = 3, sizeInBits = 4)]
        [InputControl(name = "dpad/right", layout = "DiscreteButton", parameters = "minValue=1,maxValue=3", bit = 3, sizeInBits = 4)]
        [InputControl(name = "dpad/down", layout = "DiscreteButton", parameters = "minValue=3,maxValue=5", bit = 3, sizeInBits = 4)]
        [InputControl(name = "dpad/left", layout = "DiscreteButton", parameters = "minValue=5, maxValue=7", bit = 3, sizeInBits = 4)]
        public int buttons;

        // Add a whacky 23bit button that isn't byte aligned.
        [InputControl(name = "data", layout = "DiscreteButton", bit = 4, sizeInBits = 23)]
        public long data;

        public StateWithMultiBitControl WithDpad(int value)
        {
            buttons |= value << 3;
            return this;
        }

        public StateWithMultiBitControl WithData(int value)
        {
            data = value << 4 & (0x7fff << 4);
            return this;
        }

        public FourCC format => new FourCC('T', 'E', 'S', 'T');
    }

    [InputControlLayout(stateType = typeof(StateWithMultiBitControl))]
    [Preserve]
    private class TestDeviceWithMultiBitControl : InputDevice
    {
    }

    [Test]
    [Category("State")]
    public void State_CanSetUpMonitorsForStateChanges_OnMultiBitFields()
    {
        var device = InputSystem.AddDevice<TestDeviceWithMultiBitControl>();

        var monitorFired = false;
        InputControl receivedControl = null;

        void Callback(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
        {
            Assert.That(!monitorFired);
            monitorFired = true;
            receivedControl = control;
        }

        InputState.AddChangeMonitor(device["dpad"], Callback);
        InputState.AddChangeMonitor(device["data"], Callback);

        InputSystem.QueueStateEvent(device, new StateWithMultiBitControl().WithDpad(3));
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(receivedControl, Is.SameAs(device["dpad"]));

        monitorFired = false;
        receivedControl = null;

        InputSystem.QueueStateEvent(device, new StateWithMultiBitControl().WithDpad(3).WithData(1234));
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(receivedControl, Is.SameAs(device["data"]));
    }

    [Test]
    [Category("State")]
    public void State_CanRemoveStateChangeMonitorWithSpecificMonitorIndex()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        const int kLeftStick = 12345678;
        const int kRightStick = 87654321;

        var monitorFired = false;
        long? receivedMonitorIndex = null;
        var monitor = InputState.AddChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
                receivedMonitorIndex = monitorIndex;
            }, kLeftStick);
        InputState.AddChangeMonitor(gamepad.rightStick, monitor, kRightStick);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(receivedMonitorIndex.Value, Is.EqualTo(kLeftStick));

        monitorFired = false;
        receivedMonitorIndex = null;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightStick = Vector2.one, leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(receivedMonitorIndex.Value, Is.EqualTo(kRightStick));

        InputState.RemoveChangeMonitor(gamepad.leftStick, monitor, kLeftStick);

        monitorFired = false;
        receivedMonitorIndex = null;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightStick = Vector2.one});
        InputSystem.Update();

        Assert.That(!monitorFired);

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(receivedMonitorIndex.Value, Is.EqualTo(kRightStick));
    }

    [Test]
    [Category("State")]
    public void State_StateChangeMonitorsStayIntactWhenOtherDevicesAreRemoved()
    {
        InputSystem.AddDevice<Keyboard>(); // Noise.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();

        var positionMonitorFired = false;
        InputState.AddChangeMonitor(mouse.position, (control, d, arg3, arg4) => positionMonitorFired = true);

        InputSystem.RemoveDevice(gamepad);

        Set(mouse.position, new Vector2(123, 234));

        Assert.That(positionMonitorFired);
    }

    [Test]
    [Category("State")]
    public void State_CurrentTimeTakesOffsetToRealtimeSinceStartupIntoAccount()
    {
        runtime.currentTime += 2;
        runtime.currentTimeOffsetToRealtimeSinceStartup = 1;

        Assert.That(InputState.currentTime, Is.EqualTo(1));
        Assert.Greater(InputRuntime.s_Instance.currentTime, InputState.currentTime);
    }

    // For certain actions, we want to be able to tell whether a specific input arrives in time.
    // For example, we may want to only trigger an action if a specific button was released within
    // a certain amount of time. To support this, the system allows putting timeouts on individual
    // state monitors. If the state monitor fires before the timeout expires, nothing happens. If,
    // however, the timeout expires, NotifyTimerExpired() is called when the input system updates
    // and the IInputRuntime's currentTime has advanced to or past the given time.
    [Test]
    [Category("State")]
    public void State_CanWaitForStateChangeWithinGivenAmountOfTime()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var monitorFired = false;
        var timeoutFired = false;
        double? receivedTime = null;
        int? receivedTimerIndex = null;
        InputControl receivedControl = null;

        var monitor = InputState.AddChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
            }, timerExpiredCallback:
            (control, time, monitorIndex, timerIndex) =>
            {
                Assert.That(!timeoutFired);
                timeoutFired = true;
                receivedTime = time;
                receivedTimerIndex = timerIndex;
                receivedControl = control;
            });

        // Add and immediately expire timeout.
        InputState.AddChangeMonitorTimeout(gamepad.leftStick, monitor, runtime.currentTime + 1,
            timerIndex: 1234);
        runtime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutFired);
        Assert.That(!monitorFired);
        Assert.That(receivedTimerIndex.Value, Is.EqualTo(1234));
        Assert.That(receivedTime.Value, Is.EqualTo(runtime.currentTime).Within(0.00001));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        timeoutFired = false;
        receivedTimerIndex = null;
        receivedTime = null;

        // Add timeout and perform a state change. Then advance past timeout time
        // and make sure we *DO* get a notification.
        InputState.AddChangeMonitorTimeout(gamepad.leftStick, monitor, runtime.currentTime + 1,
            timerIndex: 4321);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(!timeoutFired);

        monitorFired = false;

        runtime.currentTime += 2;
        InputSystem.Update();

        Assert.That(!monitorFired);
        Assert.That(timeoutFired);

        timeoutFired = false;

        // Add and remove timeout. Then advance past timeout time and make sure we *don't*
        // get a notification.
        InputState.AddChangeMonitorTimeout(gamepad.leftStick, monitor, runtime.currentTime + 1,
            timerIndex: 1423);
        InputState.RemoveChangeMonitorTimeout(monitor, timerIndex: 1423);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(!timeoutFired);
    }

    // Actions will process interactions inside of monitor callbacks. Since interactions can add
    // timeouts, make sure that they make it through properly.
    [Test]
    [Category("State")]
    public void State_StateChangeMonitorTimeout_CanBeAddedFromMonitorCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var monitorFired = false;
        var timeoutFired = false;

        IInputStateChangeMonitor monitor = null;
        monitor = InputState.AddChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
                InputState.AddChangeMonitorTimeout(gamepad.leftStick, monitor,
                    runtime.currentTime + 1);
            }, timerExpiredCallback:
            (control, time, monitorIndex, timerIndex) =>
            {
                Assert.That(!timeoutFired);
                timeoutFired = true;
            });

        // Trigger monitor callback.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(monitorFired);

        // Expire timer.
        runtime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutFired);
    }

    [Test]
    [Category("State")]
    public void State_StateChangeMonitorTimeout_CanBeAddedFromTimeoutCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var timeoutCount = 0;

        IInputStateChangeMonitor monitor = null;
        monitor = InputState.AddChangeMonitor(gamepad.buttonSouth,
            (control, time, eventPtr, monitorIndex) => {},
            timerExpiredCallback: (control, time, monitorIndex, timerIndex) =>
            {
                ++timeoutCount;
                InputState.AddChangeMonitorTimeout(control, monitor, time + 1.5);
            });

        InputState.AddChangeMonitorTimeout(gamepad.buttonSouth, monitor, 1.5);

        // Trigger first timeout.
        runtime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutCount, Is.EqualTo(1));

        // Trigger second timeout.
        runtime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutCount, Is.EqualTo(2));
    }

    [Test]
    [Category("State")]
    public void State_StateChangeMonitor_CanBeAddedFromMonitorCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputState.AddChangeMonitor(gamepad.buttonWest,
            (c, d, e, m) => { Debug.Log("ButtonWest"); });
        InputState.AddChangeMonitor(gamepad.buttonEast,
            (control, time, eventPtr, monitorIndex) =>
            {
                Debug.Log("ButtonEast");
                InputState.AddChangeMonitor(gamepad.buttonSouth, (c, t, e, m) => { Debug.Log("ButtonSouth"); });
            });

        LogAssert.Expect(LogType.Log, "ButtonEast");
        Press(gamepad.buttonEast);
        LogAssert.NoUnexpectedReceived();

        LogAssert.Expect(LogType.Log, "ButtonSouth");
        Press(gamepad.buttonSouth);
        LogAssert.NoUnexpectedReceived();

        LogAssert.Expect(LogType.Log, "ButtonWest");
        Press(gamepad.buttonWest);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    [Category("State")]
    public void State_StateChangeMonitor_CanBeRemovedFromMonitorCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var buttonWestMonitor = InputState.AddChangeMonitor(gamepad.buttonWest,
            (c, d, e, m) => { Debug.Log("ButtonWest"); });
        InputState.AddChangeMonitor(gamepad.buttonEast,
            (control, time, eventPtr, monitorIndex) =>
            {
                Debug.Log("ButtonEast");
                InputState.RemoveChangeMonitor(gamepad.buttonWest, buttonWestMonitor);
            });

        LogAssert.Expect(LogType.Log, "ButtonEast");
        Press(gamepad.buttonEast);
        LogAssert.NoUnexpectedReceived();

        Press(gamepad.buttonWest);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    [Category("State")]
    public void State_RemovingMonitorRemovesTimeouts()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var monitor = InputState.AddChangeMonitor(gamepad.buttonWest,
            (c, d, e, m) => {},
            timerExpiredCallback: (control, time, monitorIndex, timerIndex) =>
            {
                Assert.Fail("Should not reach here");
            });
        InputState.AddChangeMonitorTimeout(gamepad.buttonWest, monitor, 2);
        InputState.RemoveChangeMonitor(gamepad.buttonWest, monitor);

        runtime.currentTime = 4;
        InputSystem.Update();
    }

    [Test]
    [Category("State")]
    public void State_CanThrowExceptionFromStateChangeMonitorCallback()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputState.AddChangeMonitor(gamepad.buttonWest,
            (c, d, e, m) => throw new InvalidOperationException("TESTEXCEPTION"));

        LogAssert.Expect(LogType.Error, new Regex("Exception.*thrown from state change monitor.*Gamepad.*buttonWest.*"));
        LogAssert.Expect(LogType.Exception, new Regex(".*InvalidOperationException.*TESTEXCEPTION"));

        Press(gamepad.buttonWest);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    [Category("State")]
    public void State_CanUpdateStateDirectly()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.onEvent += (e, d) => Assert.Fail("No event should be triggered");

        InputState.Change(gamepad, new GamepadState {leftTrigger = 0.123f});

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
    }

    [Test]
    [Category("State")]
    public void State_CanUpdatePartialStateDirectly()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputState.Change(gamepad.leftTrigger, 0.123f);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
    }

    [Test]
    [Category("State")]
    public void State_UpdatingStateDirectly_StillTriggersChangeMonitors()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        bool? wasTriggered = null;
        InputState.AddChangeMonitor(gamepad.leftTrigger,
            (c, d, e, m) =>
            {
                Assert.That(wasTriggered, Is.Null);
                wasTriggered = true;
            });

        InputState.Change(gamepad, new GamepadState {leftTrigger = 0.123f});

        Assert.That(wasTriggered, Is.True);
    }

    // If a device like Pointer uses InputState.Change() to reset deltas, we don't want that to affect timestamps on the
    // device as otherwise we may advance time beyond the events still waiting on the queue. However, if Touchscreen uses
    // IInputStateCallbackReceiver.OnStateEvent() and InputState.Change() to entirely handle its own state updates, we *do*
    // want to effect the timestamp and also make the device current.
    //
    // So, what we do is not timestamps and .current when using InputState.Change() and leave that event processing in
    // InputManager.OnUpdate() only.
    [Test]
    [Category("State")]
    public void State_UpdatingStateDirectly_DoesNotModifyTimestampOfDeviceAndDoesNotMakeItCurrent()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Gamepad.current, Is.SameAs(gamepad2));

        runtime.currentTime = 123;
        InputState.Change(gamepad1, new GamepadState {leftTrigger = 0.123f});

        Assert.That(gamepad1.lastUpdateTime, Is.Zero.Within(0.0001));
        Assert.That(Gamepad.current, Is.SameAs(gamepad2));
    }

    [Test]
    [Category("State")]
    public unsafe void State_CanGetMetrics()
    {
        // Make sure we start out with blank data.
        var metrics = InputSystem.metrics;

        Assert.That(metrics.totalEventCount, Is.Zero);
        Assert.That(metrics.totalEventBytes, Is.Zero);
        Assert.That(metrics.totalUpdateCount, Is.Zero);

        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Keyboard>();

        InputSystem.QueueStateEvent(device1, new GamepadState());
        InputSystem.QueueStateEvent(device1, new GamepadState());
        InputSystem.QueueStateEvent(device2, new KeyboardState());
        InputSystem.Update();

        var device3 = InputSystem.AddDevice<Mouse>();
        InputSystem.RemoveDevice(device3);

        metrics = InputSystem.metrics;

        // Manually compute the size of the combined state buffer so that we
        // have a check that catches if the size changes (for good or no good reason).
        var overheadPerBuffer = 3 * sizeof(void*) * 2; // Mapping table with front and back buffer pointers for three devices.
        var combinedDeviceStateSize =
            device1.stateBlock.alignedSizeInBytes.AlignToMultipleOf(4) +
            device2.stateBlock.alignedSizeInBytes.AlignToMultipleOf(4) +
            device3.stateBlock.alignedSizeInBytes.AlignToMultipleOf(4);
        var sizePerBuffer = overheadPerBuffer + combinedDeviceStateSize * 2; // Front+back
        var sizeOfSingleBuffer = combinedDeviceStateSize;

        const int kDoubleBufferCount =
            #if UNITY_EDITOR
            2     // Dynamic + editor
            #else
            1     // Dynamic
            #endif
        ;

        var eventByteCount =
            StateEvent.GetEventSizeWithPayload<GamepadState>() * 2 +
            StateEvent.GetEventSizeWithPayload<KeyboardState>();

        // QueueEvent aligns to 4-byte boundaries.
        eventByteCount = eventByteCount.AlignToMultipleOf(4);

        Assert.That(metrics.maxNumDevices, Is.EqualTo(3));
        Assert.That(metrics.maxStateSizeInBytes, Is.EqualTo(kDoubleBufferCount * sizePerBuffer + sizeOfSingleBuffer * 2));
        Assert.That(metrics.totalEventBytes, Is.EqualTo(eventByteCount));
        Assert.That(metrics.totalEventCount, Is.EqualTo(3));
        Assert.That(metrics.totalUpdateCount, Is.EqualTo(1));
        Assert.That(metrics.totalEventProcessingTime, Is.GreaterThan(0.000001));
        Assert.That(metrics.averageEventBytesPerFrame, Is.EqualTo(eventByteCount).Within(0.00001));
        Assert.That(metrics.averageProcessingTimePerEvent, Is.GreaterThan(0.0000001));
    }

    [Test]
    [Category("State")]
    public void State_FixedUpdatesAreDisabledByDefault()
    {
        Assert.That(InputSystem.settings.updateMode, Is.EqualTo(InputSettings.UpdateMode.ProcessEventsInDynamicUpdate));
        Assert.That(runtime.onShouldRunUpdate(InputUpdateType.Fixed), Is.False);
        Assert.That(InputSystem.s_Manager.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.None));
    }

    [Test]
    [Category("State")]
    public void State_CannotRunUpdatesThatAreNotEnabled()
    {
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;

        Assert.That(() => InputSystem.Update(InputUpdateType.Dynamic),
            Throws.InvalidOperationException.With.Message.Contains("not enabled").And.Message
                .Contains("Dynamic").And.Message.Contains("ProcessEventsInFixedUpdate"));
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_CanSetUpStateMonitorsUsingControlPath()
    {
        Assert.Fail();
    }

    // InputStateHistory helps creating traces of input over time. This is useful, for example, to track
    // the motion curve of a tracking device over time. The API allows to pretty flexibly capture state
    // and copy it around.
    [Test]
    [Category("State")]
    public void State_CanRecordHistory()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        using (var history = new InputStateHistory<float>("<Gamepad>/*trigger"))
        {
            Assert.That(history.controls,
                Is.EquivalentTo(
                    new[] {gamepad1.leftTrigger, gamepad1.rightTrigger, gamepad2.leftTrigger, gamepad2.rightTrigger}));

            history.StartRecording();

            InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftTrigger = 0.123f }, 0.111);
            InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftTrigger = 0.234f }, 0.222);
            InputSystem.QueueStateEvent(gamepad2, new GamepadState { rightTrigger = 0.345f }, 0.333);
            InputSystem.Update();
            InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftTrigger = 0.456f }, 0.444);
            InputSystem.Update();

            Set(gamepad1.leftStick, new Vector2(0.987f, 0.876f)); // Noise.

            Assert.That(history.Count, Is.EqualTo(4));
            Assert.That(history[0].ReadValue(), Is.EqualTo(0.123).Within(0.00001));
            Assert.That(history[1].ReadValue(), Is.EqualTo(0.234).Within(0.00001));
            Assert.That(history[2].ReadValue(), Is.EqualTo(0.345).Within(0.00001));
            Assert.That(history[3].ReadValue(), Is.EqualTo(0.456).Within(0.00001));
            Assert.That(history[0].time, Is.EqualTo(0.111));
            Assert.That(history[1].time, Is.EqualTo(0.222));
            Assert.That(history[2].time, Is.EqualTo(0.333));
            Assert.That(history[3].time, Is.EqualTo(0.444));
            Assert.That(history[0].control, Is.SameAs(gamepad1.leftTrigger));
            Assert.That(history[1].control, Is.SameAs(gamepad1.leftTrigger));
            Assert.That(history[2].control, Is.SameAs(gamepad2.rightTrigger));
            Assert.That(history[3].control, Is.SameAs(gamepad1.leftTrigger));
            Assert.That(history[0].valid, Is.True);
            Assert.That(history[1].valid, Is.True);
            Assert.That(history[2].valid, Is.True);
            Assert.That(history[3].valid, Is.True);
        }
    }

    [Test]
    [Category("State")]
    public void State_CanRecordHistory_AndGetCallbacksWhenNewStateIsRecorded()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.buttonSouth))
        {
            var receivedValues = new List<float>();
            history.onRecordAdded = v => receivedValues.Add(v.ReadValue<float>());
            history.StartRecording();

            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);

            Assert.That(receivedValues, Has.Count.EqualTo(2));
            Assert.That(receivedValues[0], Is.EqualTo(1));
            Assert.That(receivedValues[1], Is.Zero);
        }
    }

    [Test]
    [Category("State")]
    public unsafe void State_CanRecordHistory_AndAccessRawMemoryOfRecordedState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.buttonSouth))
        {
            history.StartRecording();

            Press(gamepad.buttonSouth);

            Assert.That(history, Has.Count.EqualTo(1));
            Assert.That(history[0].GetUnsafeMemoryPtr() != null, Is.True);

            var statePtr = (byte*)history[0].GetUnsafeMemoryPtr() - gamepad.buttonSouth.stateBlock.byteOffset;
            Assert.That(gamepad.buttonSouth.ReadValueFromState(statePtr), Is.EqualTo(1));
        }
    }

    // It can be very useful to be able to store custom data with each history record from the
    // onRecordAdded callback.
    [Test]
    [Category("State")]
    public unsafe void State_CanRecordHistory_AndStoreAdditionalCustomDataForEachStateChange()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.buttonSouth))
        {
            var i = 1;
            history.extraMemoryPerRecord = sizeof(int);
            history.onRecordAdded = v => *(int*)v.GetUnsafeExtraMemoryPtr() = i++;
            history.StartRecording();

            Press(gamepad.buttonSouth);
            Release(gamepad.buttonSouth);

            Assert.That(history, Has.Count.EqualTo(2));
            Assert.That(*(int*)history[0].GetUnsafeExtraMemoryPtr(), Is.EqualTo(1));
            Assert.That(*(int*)history[1].GetUnsafeExtraMemoryPtr(), Is.EqualTo(2));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanRecordHistory_AndDecideWhichStateChangesGetRecorded()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.onShouldRecordStateChange =
                (control, time, eventPtr) => ((InputControl<float>)control).ReadValue() > 0.5f;
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.567f);
            Set(gamepad.leftTrigger, 0.678f);
            Set(gamepad.leftTrigger, 0);

            Assert.That(history, Has.Count.EqualTo(2));
            Assert.That(history[0].ReadValue(), Is.EqualTo(0.567).Within(0.00001));
            Assert.That(history[1].ReadValue(), Is.EqualTo(0.678).Within(0.00001));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanRecordHistory_AndEnumerateRecords()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);
            Set(gamepad.leftTrigger, 0.456f);

            var result = history.ToArray<InputStateHistory<float>.Record>();
            Assert.That(result, Has.Length.EqualTo(4));
            Assert.That(result[0].ReadValue(), Is.EqualTo(0.123f).Within(0.00001));
            Assert.That(result[1].ReadValue(), Is.EqualTo(0.234f).Within(0.00001));
            Assert.That(result[2].ReadValue(), Is.EqualTo(0.345f).Within(0.00001));
            Assert.That(result[3].ReadValue(), Is.EqualTo(0.456f).Within(0.00001));
        }
    }

    [Test]
    [Category("State")]
    public void State_RecordingHistory_OverwritesOldStateWhenBufferIsFull()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.historyDepth = 2;
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);

            // Keep reference to first record and ensure it becomes invalid.
            var firstChange = history[0];
            var secondChange = history[1]; // But this one should stay valid.

            Set(gamepad.leftTrigger, 0.345f);

            var result = history.ToArray<InputStateHistory<float>.Record>();
            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0].ReadValue(), Is.EqualTo(0.234f).Within(0.00001));
            Assert.That(result[1].ReadValue(), Is.EqualTo(0.345f).Within(0.00001));

            Assert.That(firstChange.valid, Is.False);
            Assert.That(secondChange.valid, Is.True);
        }
    }

    [Test]
    [Category("State")]
    public void State_RecordingHistory_EnsuresControlsMatchedByPathHaveCompatibleValueType()
    {
        InputSystem.AddDevice<Gamepad>();

        Assert.That(() => new InputStateHistory<Vector2>("<Gamepad>/*Trigger"),
            Throws.ArgumentException.With.Message.Contains("Vector2")
                .And.With.Message.Contains("float")
                .And.With.Message.Contains("incompatible"));
    }

    [Test]
    [Category("State")]
    public void State_CanWriteStateChangesIntoHistoryManually()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            Set(gamepad.leftTrigger, 0.123f); // Should not get recorded.

            history.RecordStateChange(gamepad.leftTrigger, 0.234f);
            history.RecordStateChange(gamepad.leftTrigger, 0.345f);

            Assert.That(history, Has.Count.EqualTo(2));
            Assert.That(history[0].ReadValue(), Is.EqualTo(0.234).Within(0.00001));
            Assert.That(history[1].ReadValue(), Is.EqualTo(0.345).Within(0.00001));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanWriteStateChangesIntoHistoryManually_AndAddControlsOnTheFly()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var history = new InputStateHistory<float>())
        {
            Assert.That(history.controls, Is.Empty);

            history.RecordStateChange(gamepad.leftTrigger, 0.234f);
            history.RecordStateChange(gamepad.rightTrigger, 0.345f);

            Assert.That(history.controls, Is.EquivalentTo(new[] {gamepad.leftTrigger, gamepad.rightTrigger}));
            Assert.That(history[0].control, Is.SameAs(gamepad.leftTrigger));
            Assert.That(history[1].control, Is.SameAs(gamepad.rightTrigger));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanClearRecordedHistory()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);

            Assert.That(history, Has.Count.EqualTo(2));

            history.Clear();

            Assert.That(history, Has.Count.Zero);

            Set(gamepad.leftTrigger, 0.345f);
            Set(gamepad.leftTrigger, 0.456f);

            Assert.That(history, Has.Count.EqualTo(2));
            Assert.That(history[0].ReadValue(), Is.EqualTo(0.345).Within(0.00001));
            Assert.That(history[1].ReadValue(), Is.EqualTo(0.456).Within(0.00001));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanCopyRecordedHistory()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);

            history.AddRecord(history[0]);
            history[0] = history[1];
            history[1].CopyFrom(history[2]);

            Assert.That(history[0].ReadValue(), Is.EqualTo(0.234).Within(0.00001));
            Assert.That(history[1].ReadValue(), Is.EqualTo(0.345).Within(0.00001));
            Assert.That(history[2].ReadValue(), Is.EqualTo(0.345).Within(0.00001));
        }
    }

    [Test]
    [Category("State")]
    public unsafe void State_CanCopyRecordedHistory_FromOneHistoryToAnother()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history1 = new InputStateHistory<float>(gamepad.leftTrigger))
        using (var history2 = new InputStateHistory<float>())
        {
            history1.extraMemoryPerRecord = sizeof(int);
            history2.extraMemoryPerRecord = sizeof(int);
            history1.onRecordAdded = record => *(int*)record.GetUnsafeExtraMemoryPtr() = 123;
            history1.StartRecording();

            Set(gamepad.leftTrigger, 0.123f, 0.444f);

            history2.AddRecord(history1[0]);

            Assert.That(history2.Count, Is.EqualTo(1));
            Assert.That(history2[0].ReadValue(), Is.EqualTo(0.123).Within(0.00001));
            Assert.That(*(int*)history2[0].GetUnsafeExtraMemoryPtr(), Is.EqualTo(123));
            Assert.That(history2[0].time, Is.EqualTo(0.444).Within(0.00001));
            Assert.That(history2[0].control, Is.SameAs(gamepad.leftTrigger));
            Assert.That(history2.controls, Is.EquivalentTo(new[] { gamepad.leftTrigger }));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanTraverseRecordedHistoryStartingWithGivenRecord()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);
            Set(gamepad.leftTrigger, 0.456f);

            Assert.That(history[1].next.ReadValue(), Is.EqualTo(0.345).Within(0.000001));
            Assert.That(history[1].previous.ReadValue(), Is.EqualTo(0.123).Within(0.000001));

            Assert.That(history[0].previous.valid, Is.False);
            Assert.That(history[3].next.valid, Is.False);
        }
    }

    [Test]
    [Category("State")]
    public void State_CanTraverseRecordedHistoryStartingWithGivenRecord_WhenHistoryHasOverwrittenOlderRecords()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.historyDepth = 3;
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);
            Set(gamepad.leftTrigger, 0.456f);

            Assert.That(history[1].next.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
            Assert.That(history[1].previous.ReadValue(), Is.EqualTo(0.234).Within(0.000001));

            Assert.That(history[0].previous.valid, Is.False);
            Assert.That(history[2].next.valid, Is.False);
        }
    }

    [Test]
    [Category("State")]
    public void State_CanGetIndexFromHistoryRecord()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.historyDepth = 3;
            history.StartRecording();

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);
            Set(gamepad.leftTrigger, 0.456f);

            Assert.That(history[0].index, Is.EqualTo(0));
            Assert.That(history[1].index, Is.EqualTo(1));
            Assert.That(history[2].index, Is.EqualTo(2));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanGetHistoryVersion()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            Assert.That(history.version, Is.Zero);

            Set(gamepad.leftTrigger, 0.123f);
            Set(gamepad.leftTrigger, 0.234f);
            Set(gamepad.leftTrigger, 0.345f);

            Assert.That(history.version, Is.EqualTo(3));
        }
    }

    [Test]
    [Category("State")]
    public void State_CanGetHistoryFromRecord()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();
            Set(gamepad.leftTrigger, 0.123f);
            Assert.That(history[0].owner, Is.SameAs(history));
        }
    }

    #if UNITY_EDITOR
    [Test]
    [Category("State")]
    public void State_RecordingHistory_ExcludesEditorInputByDefault()
    {
        InputEditorUserSettings.lockInputToGameView = false;

        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.StartRecording();

            runtime.PlayerFocusLost();
            Set(gamepad.leftTrigger, 0.123f, queueEventOnly: true);
            InputSystem.Update(InputUpdateType.Editor);

            Assert.That(history, Is.Empty);
        }
    }

    [Test]
    [Category("State")]
    public void State_RecordingHistory_CanCaptureEditorInput()
    {
        InputEditorUserSettings.lockInputToGameView = false;

        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var history = new InputStateHistory<float>(gamepad.leftTrigger))
        {
            history.updateMask = InputUpdateType.Editor;
            history.StartRecording();

            runtime.PlayerFocusLost();
            Set(gamepad.leftTrigger, 0.123f, queueEventOnly: true);
            InputSystem.Update(InputUpdateType.Editor);

            Assert.That(history, Has.Count.EqualTo(1));
            Assert.That(history[0].ReadValue(), Is.EqualTo(0.123).Within(0.00001));
        }
    }

    #endif
    
    [Test]
    [Category("State")]
    // single bit
    [TestCase(InputStateBlock.kFormatBit, 5, 1, 0u, 0, 0.0f)]
    [TestCase(InputStateBlock.kFormatBit, 10, 1, 1u, 1, 1.0f)]
    [TestCase(InputStateBlock.kFormatSBit, 15, 1, 0u, -1, null)]
    [TestCase(InputStateBlock.kFormatSBit, 25, 1, 1u, 1, null)]
    // multiple bits
    [TestCase(InputStateBlock.kFormatBit, 5, 16, 0b1101010101010101u, 0b1101010101010101)]
    [TestCase(InputStateBlock.kFormatSBit, 15, 16, 0b1101010101010101u, 0b1101010101010101 + short.MinValue)]
    [TestCase(InputStateBlock.kFormatBit, 16, 31, 0x7fffffffu, int.MaxValue)]
    [TestCase(InputStateBlock.kFormatSBit, 24, 31, 0x7fffffffu, 1073741823)] // excess-K
    [TestCase(InputStateBlock.kFormatBit, 16, 32, uint.MaxValue, -1)]
    [TestCase(InputStateBlock.kFormatSBit, 24, 32, uint.MaxValue, int.MaxValue)] // excess-K
    // primitive types
    [TestCase(InputStateBlock.kFormatInt, 32, 32, 0u, 0, 0.0f)]
    [TestCase(InputStateBlock.kFormatInt, 64, 32, 1231231231u, 1231231231, 0.573336720466613769531f)]
    [TestCase(InputStateBlock.kFormatInt, 96, 32, 0x7fffffffu, int.MaxValue, 1.0f)]
    [TestCase(InputStateBlock.kFormatInt, 32, 32, 0x80000000u, int.MinValue, -1.0f)]
    [TestCase(InputStateBlock.kFormatUInt, 32, 32, 0u, 0, 0.0f)]
    [TestCase(InputStateBlock.kFormatUInt, 64, 32, 1231231231u, 1231231231, 0.286668360233306884766f)]
    [TestCase(InputStateBlock.kFormatUInt, 96, 32, 0x7fffffffu, int.MaxValue, 0.5f)] // no test for uint.MaxValue
    [TestCase(InputStateBlock.kFormatShort, 16, 16, 0u, 0, 0.0000152587890625f)]
    [TestCase(InputStateBlock.kFormatShort, 32, 16, 12312u, 12312, 0.3757534027099609375f)]
    [TestCase(InputStateBlock.kFormatShort, 48, 16, 0x7fffu, (int)short.MaxValue, 1.0f)]
    [TestCase(InputStateBlock.kFormatShort, 48, 16, 0x8000u, (int)short.MinValue, -1.0f)]
    [TestCase(InputStateBlock.kFormatUShort, 16, 16, 0u, 0, 0.0f)]
    [TestCase(InputStateBlock.kFormatUShort, 32, 16, 12312u, 12312, 0.18786907196044921875f)]
    [TestCase(InputStateBlock.kFormatUShort, 48, 16, 0xffffu, (int)ushort.MaxValue, 1.0f)]
    [TestCase(InputStateBlock.kFormatByte, 8, 8, 0u, 0, 0.0f)]
    [TestCase(InputStateBlock.kFormatByte, 16, 8, 123u, 123, 0.482352942228317260742f)]
    [TestCase(InputStateBlock.kFormatByte, 24, 8, 0xffu, 255, 1.0f)]
    [TestCase(InputStateBlock.kFormatSByte, 8, 8, 0u, 0, 0.00392156885936856269836f)]
    [TestCase(InputStateBlock.kFormatSByte, 16, 8, 123u, 123, 0.968627452850341796875f)]
    [TestCase(InputStateBlock.kFormatSByte, 24, 8, 0x7fu, (int)sbyte.MaxValue, 1.0f)]
    [TestCase(InputStateBlock.kFormatSByte, 24, 8, 0x80u, (int)sbyte.MinValue, -1.0f)]
    [TestCase(InputStateBlock.kFormatFloat, 64, 32, 0x0u, null, 0.0f)]
    [TestCase(InputStateBlock.kFormatFloat, 64, 32, 0x3f800000u, null, 1.0f)]
    [TestCase(InputStateBlock.kFormatFloat, 64, 32, 0xbf800000u, null, -1.0f)]
    [TestCase(InputStateBlock.kFormatDouble, 64, 64, 0x0u, null, 0.0f)]
    public unsafe void State_InputStateBlock_ReadWrite(int format, int bitOffset, int bitSize, uint bitValue, int? expectedIntValue = null, float? expectedFloatValue = null)
    {
        const int bufferSize = 16; // make buffer a bit larger to have guard bits
        if (bitOffset + bitSize > bufferSize * 8)
            throw new ArgumentException(
                $"bit offset and bit size are outside of data buffer range ({bitOffset}, {bitSize})");

        var dataRead = (byte*)UnsafeUtility.Malloc(bufferSize, 8, Allocator.Temp);
        var dataWrite = (byte*)UnsafeUtility.Malloc(bufferSize, 8, Allocator.Temp);

        for (var testOperation = 0; testOperation < 3; ++testOperation)
        {
            // write all 1's so we can track some false negatives
            UnsafeUtility.MemSet(dataRead, 0xff, bufferSize);

            // clear bits that are 0
            for (var i = 0; i < bitSize; ++i)
            {
                var value = (bitValue >> i) & 1;
                if (value != 0)
                    continue;
                var bytePosition = (i + bitOffset) / 8;
                var bitPosition = (i + bitOffset) % 8;
                dataRead[bytePosition] &= (byte) ~(1UL << bitPosition);
            }

            // prepare write array
            for (var i = 0; i < bufferSize; ++i)
                dataWrite[i] = (byte) ~dataRead[i];

            var block = new InputStateBlock
            {
                format = new FourCC(format),
                byteOffset = (uint) (bitOffset / 8),
                bitOffset = (uint) (bitOffset % 8),
                sizeInBits = (uint) bitSize
            };

            var testWrittenBinaryData = false;

            switch (testOperation)
            {
                case 0:
                    if (expectedIntValue.HasValue)
                    {
                        testWrittenBinaryData = true;
                        Assert.That(block.ReadInt(dataRead), Is.EqualTo(expectedIntValue.Value));
                        block.WriteInt(dataWrite, expectedIntValue.Value);
                        Assert.That(block.ReadInt(dataWrite), Is.EqualTo(expectedIntValue.Value));
                    }
                    break;
                case 1:
                    if (expectedFloatValue.HasValue)
                    {
                        // While this test should be able to test precise floats, we do some computations with hard to predict precision.
                        // Hence leaving some precision slack for now.
                        testWrittenBinaryData = expectedFloatValue == -1.0f || expectedFloatValue == 0.0f || expectedFloatValue == 1.0f;

                        var desiredPrecision = testWrittenBinaryData ? 0.0f : 0.00005f;
                        Assert.That(block.ReadFloat(dataRead), Is.EqualTo(expectedFloatValue.Value).Within(desiredPrecision));
                        block.WriteFloat(dataWrite, expectedFloatValue.Value);
                        Assert.That(block.ReadFloat(dataWrite), Is.EqualTo(expectedFloatValue.Value).Within(desiredPrecision));
                    }
                    break;
                case 2:
                    if (expectedFloatValue.HasValue)
                    {
                        var expectedDoubleValue = (double)expectedFloatValue;
                        
                        // While this test should be able to test precise floats, we do some computations with hard to predict precision.
                        // Hence leaving some precision slack for now.
                        testWrittenBinaryData = expectedDoubleValue == -1.0f || expectedDoubleValue == 0.0f || expectedDoubleValue == 1.0f;

                        var desiredPrecision = testWrittenBinaryData ? 0.0 : 0.00005;
                        Assert.That(block.ReadDouble(dataRead), Is.EqualTo(expectedDoubleValue).Within(desiredPrecision));
                        block.WriteDouble(dataWrite, expectedDoubleValue);
                        Assert.That(block.ReadDouble(dataWrite), Is.EqualTo(expectedDoubleValue).Within(desiredPrecision));
                    }
                    break;
            }

            if (!testWrittenBinaryData) continue;

            // now all bits except for expected bits should be different
            var validWrite = true;
            for (var i = 0; i < bufferSize * 8; ++i)
            {
                var bytePosition = i / 8;
                var bitPosition = i % 8;
                var readBit = (dataRead[bytePosition] & (byte) (1UL << bitPosition)) != 0 ? 1 : 0;
                var writeBit = (dataWrite[bytePosition] & (byte) (1UL << bitPosition)) != 0 ? 1 : 0;
                validWrite &= (i >= bitOffset && i < bitOffset + bitSize)
                    ? readBit == writeBit
                    : readBit != writeBit;
            }

            if (!validWrite)
            {
                var sb = new StringBuilder();
                sb.Append($"Offset {bitOffset} size {bitSize} in bits, read, write, xor:\n");
                for (var i = 0; i < bufferSize * 8; ++i)
                    sb.Append((dataRead[i / 8] & (byte) (1UL << (i % 8))) != 0 ? '1' : '0');
                sb.Append("\n");
                for (var i = 0; i < bufferSize * 8; ++i)
                    sb.Append((dataWrite[i / 8] & (byte) (1UL << (i % 8))) != 0 ? '1' : '0');
                sb.Append("\n");
                for (var i = 0; i < bufferSize * 8; ++i)
                    sb.Append(((dataRead[i / 8] ^ dataWrite[i / 8]) & (byte) (1UL << (i % 8))) != 0 ? '1' : '0');
                throw new AssertionException($"Written data is not matching expected data: {sb}");
            }
        }

        UnsafeUtility.Free(dataRead, Allocator.Temp);
        UnsafeUtility.Free(dataWrite, Allocator.Temp);
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
        ////TODO
        Assert.Fail();
    }
}
