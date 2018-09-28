using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.TestTools.Utils;

partial class CoreTests
{
    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

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
        var setup = new InputDeviceBuilder("CustomDevice");
        var axis2 = setup.GetControl("axis2");
        setup.Finish();

        var nestedOffset = Marshal.OffsetOf(typeof(CustomDeviceState), "nested").ToInt32();
        var axis2Offset = nestedOffset + Marshal.OffsetOf(typeof(CustomNestedDeviceState), "axis2").ToInt32();

        Assert.That(axis2.stateBlock.byteOffset, Is.EqualTo(axis2Offset));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForMultiByteBitfieldWithFixedOffset()
    {
        var setup = new InputDeviceBuilder("Keyboard");
        var downArrow = setup.GetControl("DownArrow");
        var keyboard = setup.Finish();

        Assert.That(downArrow.stateBlock.bitOffset, Is.EqualTo((int)Key.DownArrow));
        Assert.That(downArrow.stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(keyboard.stateBlock.alignedSizeInBytes, Is.EqualTo(KeyboardState.kSizeInBytes));
    }

    [Test]
    [Category("State")]
    public void State_BeforeAddingDevice_OffsetsInStateLayoutsAreRelativeToRoot()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var device = (Gamepad)setup.Finish();

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
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.EqualTo(0.25f).Within(0.00001));
    }

    [Test]
    [Category("State")]
    public void State_RunningMultipleFixedUpdates_FlipsDynamicUpdateBuffersOnlyOnFirstUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.25, previous=0.0
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.75, previous=0.0

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.Zero);
    }

    [Test]
    [Category("State")]
    public void State_RunningNoFixedUpdateInFrame_StillCapturesStateForNextFixedUpdate()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.75f});
        InputSystem.Update(InputUpdateType.Fixed); // Fixed: current=0.75, previous=0.0

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.25f});
        InputSystem.Update(InputUpdateType.Dynamic); // Fixed: current=0.25, previous=0.75
        InputSystem.Update(InputUpdateType.Fixed); // Unchanged.

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.25).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.EqualTo(0.75).Within(0.000001));
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

        var setup = new InputDeviceBuilder("CustomGamepad");
        Assert.That(setup.GetControl("buttonSouth").stateBlock.byteOffset, Is.EqualTo(800));

        var device = (Gamepad)setup.Finish();
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

        var setup = new InputDeviceBuilder("CustomGamepad");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.rightTrigger.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
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
        var setup = new InputDeviceBuilder("MyDevice");

        Assert.That(setup.GetControl("controlWithAutomaticOffset").stateBlock.byteOffset, Is.EqualTo(14));

        var device = setup.Finish();
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
    public void State_CanUpdateButtonState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonEast.isPressed, Is.False);

        var newState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.isPressed, Is.True);
    }

    [Test]
    [Category("State")]
    public void State_CanDetectWhetherButtonStateHasChangedThisFrame()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonEast.wasJustPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasJustReleased, Is.False);

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasJustPressed, Is.True);
        Assert.That(gamepad.buttonEast.wasJustReleased, Is.False);

        // Input update with no changes should make both properties go back to false.
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasJustPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasJustReleased, Is.False);

        var secondState = new GamepadState {buttons = 0};
        InputSystem.QueueStateEvent(gamepad, secondState);
        InputSystem.Update();

        Assert.That(gamepad.buttonEast.wasJustPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasJustReleased, Is.True);
    }

    // The way we keep state does not allow observing the state change on the final
    // state of the button. However, actions will still see the change.
    [Test]
    [Category("State")]
    public void State_PressingAndReleasingButtonInSameFrame_DoesNotShowStateChange()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(gamepad.buttonEast.isPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasJustPressed, Is.False);
        Assert.That(gamepad.buttonEast.wasJustReleased, Is.False);
    }

    [Test]
    [Category("State")]
    public void State_CanStoreButtonAsFloat()
    {
        // Turn buttonSouth into float and move to left/x offset (so we can use
        // GamepadState to set it).
        var json = @"
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
    public void State_CanDisableFixedUpdates()
    {
        // Add a device as otherwise we don't have any state.
        InputSystem.AddDevice<Gamepad>();

        // Disable fixed updates.
        InputSystem.updateMask &= ~InputUpdateType.Fixed;

        Assert.That(InputSystem.updateMask & InputUpdateType.Fixed, Is.EqualTo((InputUpdateType)0));
        Assert.That(InputSystem.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.Dynamic));
#if UNITY_EDITOR
        Assert.That(InputSystem.updateMask & InputUpdateType.Editor, Is.EqualTo(InputUpdateType.Editor));
#endif

        // Make sure we disabled the update in the runtime.
        Assert.That(InputSystem.updateMask, Is.EqualTo(InputSystem.updateMask));

        // Make sure we got rid of the memory for fixed update.
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.False);

        // Re-enable fixed updates.
        InputSystem.updateMask |= InputUpdateType.Fixed;

        Assert.That(InputSystem.updateMask & InputUpdateType.Fixed, Is.EqualTo(InputUpdateType.Fixed));
        Assert.That(InputSystem.updateMask & InputUpdateType.Dynamic, Is.EqualTo(InputUpdateType.Dynamic));
#if UNITY_EDITOR
        Assert.That(InputSystem.updateMask & InputUpdateType.Editor, Is.EqualTo(InputUpdateType.Editor));
#endif

        // Make sure we re-enabled the update in the runtime.
        Assert.That(InputSystem.updateMask, Is.EqualTo(InputSystem.updateMask));

        // Make sure we got re-instated the fixed update state buffers.
        Assert.That(InputSystem.s_Manager.m_StateBuffers.GetDoubleBuffersFor(InputUpdateType.Fixed).valid, Is.True);
    }

    ////REVIEW: if we do this, we have to have something like InputUpdateType.Manual that allows using the system
    ////        in a way where all updates are controlled manually by the user through InputSystem.Update
    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_DisablingAllUpdatesDisablesEventCollection()
    {
        InputSystem.updateMask = InputUpdateType.None;

        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(gamepad.leftTrigger, Is.Zero);
    }

    [Test]
    [Category("State")]
    public void State_CanListenForInputUpdates()
    {
        var receivedUpdate = false;
        InputUpdateType? receivedUpdateType = null;
        InputSystem.onBeforeUpdate +=
            type =>
        {
            Assert.That(receivedUpdate, Is.False);
            receivedUpdate = true;
            receivedUpdateType = type;
        };

        // Dynamic.
        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(receivedUpdate, Is.True);
        Assert.That(receivedUpdateType, Is.EqualTo(InputUpdateType.Dynamic));

        receivedUpdate = false;
        receivedUpdateType = null;

        // Fixed.
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(receivedUpdate, Is.True);
        Assert.That(receivedUpdateType, Is.EqualTo(InputUpdateType.Fixed));

        receivedUpdate = false;
        receivedUpdateType = null;

        // Before render.
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(receivedUpdate, Is.True);
        Assert.That(receivedUpdateType, Is.EqualTo(InputUpdateType.BeforeRender));

        receivedUpdate = false;
        receivedUpdateType = null;

        // Editor.
        InputSystem.Update(InputUpdateType.Editor);

        Assert.That(receivedUpdate, Is.True);
        Assert.That(receivedUpdateType, Is.EqualTo(InputUpdateType.Editor));
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

        var monitor = InputSystem.AddStateChangeMonitor(gamepad.leftStick,
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
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.id));

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
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.id));

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
        Assert.That(receivedEventPtr.Value.deviceId, Is.EqualTo(gamepad.id));

        // Remove state monitor and change leftStick again.
        InputSystem.RemoveStateChangeMonitor(gamepad.leftStick, monitor);

        monitorFired = false;
        receivedControl = null;
        receivedTime = 0;
        receivedEventPtr = null;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.0f, 0.0f)}, 1.0);
        InputSystem.Update();

        Assert.That(monitorFired, Is.False);
    }

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

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(StateWithMultiBitControl))]
    class TestDeviceWithMultiBitControl : InputDevice
    {
    }

    [Test]
    [Category("State")]
    public void State_CanSetUpMonitorsForStateChanges_OnMultiBitFields()
    {
        var device = InputSystem.AddDevice<TestDeviceWithMultiBitControl>();

        var monitorFired = false;
        InputControl receivedControl = null;
        Action<InputControl, double, InputEventPtr, long> action =
            (control, time, eventPtr, monitorIndex) =>
        {
            Assert.That(!monitorFired);
            monitorFired = true;
            receivedControl = control;
        };

        InputSystem.AddStateChangeMonitor(device["dpad"], action);
        InputSystem.AddStateChangeMonitor(device["data"], action);

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
        var monitor = InputSystem.AddStateChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
                receivedMonitorIndex = monitorIndex;
            }, kLeftStick);
        InputSystem.AddStateChangeMonitor(gamepad.rightStick, monitor, kRightStick);

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

        InputSystem.RemoveStateChangeMonitor(gamepad.leftStick, monitor, kLeftStick);

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

        var monitor = InputSystem.AddStateChangeMonitor(gamepad.leftStick,
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
        InputSystem.AddStateChangeMonitorTimeout(gamepad.leftStick, monitor, testRuntime.currentTime + 1,
            timerIndex: 1234);
        testRuntime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutFired);
        Assert.That(!monitorFired);
        Assert.That(receivedTimerIndex.Value, Is.EqualTo(1234));
        Assert.That(receivedTime.Value, Is.EqualTo(testRuntime.currentTime).Within(0.00001));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        timeoutFired = false;
        receivedTimerIndex = null;
        receivedTime = null;

        // Add timeout and obsolete it by state change. Then advance past timeout time
        // and make sure we *don't* get a notification.
        InputSystem.AddStateChangeMonitorTimeout(gamepad.leftStick, monitor, testRuntime.currentTime + 1,
            timerIndex: 4321);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one});
        InputSystem.Update();

        Assert.That(monitorFired);
        Assert.That(!timeoutFired);

        testRuntime.currentTime += 2;
        InputSystem.Update();

        Assert.That(!timeoutFired);

        // Add and remove timeout. Then advance past timeout time and make sure we *don't*
        // get a notification.
        InputSystem.AddStateChangeMonitorTimeout(gamepad.leftStick, monitor, testRuntime.currentTime + 1,
            timerIndex: 1423);
        InputSystem.RemoveStateChangeMonitorTimeout(monitor, timerIndex: 1423);
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
        monitor = InputSystem.AddStateChangeMonitor(gamepad.leftStick,
            (control, time, eventPtr, monitorIndex) =>
            {
                Assert.That(!monitorFired);
                monitorFired = true;
                InputSystem.AddStateChangeMonitorTimeout(gamepad.leftStick, monitor,
                    testRuntime.currentTime + 1);
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
        testRuntime.currentTime += 2;
        InputSystem.Update();

        Assert.That(timeoutFired);
    }

    [Test]
    [Category("State")]
    public unsafe void State_CanGetMetrics()
    {
        var device1 = InputSystem.AddDevice<Gamepad>();
        var device2 = InputSystem.AddDevice<Keyboard>();

        InputSystem.QueueStateEvent(device1, new GamepadState());
        InputSystem.QueueStateEvent(device1, new GamepadState());
        InputSystem.QueueStateEvent(device2, new KeyboardState());
        InputSystem.Update();

        var device3 = InputSystem.AddDevice<Mouse>();
        InputSystem.RemoveDevice(device3);

        var metrics = InputSystem.GetMetrics();

        // Manually compute the size of the combined state buffer so that we
        // have a check that catches if the size changes (for good or no good reason).
        var overheadPerBuffer = 3 * sizeof(void*) * 2; // Mapping table with front and back buffer pointers for three devices.
        var combinedDeviceStateSize = NumberHelpers.AlignToMultiple(
            device1.stateBlock.alignedSizeInBytes + device2.stateBlock.alignedSizeInBytes +
            device3.stateBlock.alignedSizeInBytes, 4);
        var sizePerBuffer = overheadPerBuffer + combinedDeviceStateSize * 2; // Front+back
        var sizeOfSingleBuffer = combinedDeviceStateSize;

        const int kDoubleBufferCount =
            #if UNITY_EDITOR
            3     // Dynamic + fixed + editor
            #else
            2     // Dynamic + fixed
            #endif
        ;

        var eventByteCount =
            StateEvent.GetEventSizeWithPayload<GamepadState>() * 2 +
            StateEvent.GetEventSizeWithPayload<KeyboardState>();

        Assert.That(metrics.maxNumDevices, Is.EqualTo(3));
        Assert.That(metrics.maxStateSizeInBytes, Is.EqualTo((kDoubleBufferCount * sizePerBuffer) + (sizeOfSingleBuffer * 2)));
        Assert.That(metrics.totalEventBytes, Is.EqualTo(eventByteCount));
        Assert.That(metrics.totalEventCount, Is.EqualTo(3));
        Assert.That(metrics.averageEventBytesPerFrame, Is.EqualTo(eventByteCount).Within(0.00001));
        Assert.That(metrics.averageProcessingTimePerEvent, Is.GreaterThan(0.000001));
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_FixedUpdatesAreDisabledByDefault()
    {
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_CannotRunUpdatesThatAreNotEnabled()
    {
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_CanSetUpStateMonitorsUsingControlPath()
    {
        Assert.Fail();
    }

    // InputStateHistory helps creating traces of input over time. This is useful, for example, to track
    // the motion curve of a tracking device over time.
    [Test]
    [Category("State")]
    [Ignore("TODO")]
    public void TODO_State_CanRecordHistoryOfState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        //have to record raw, unprocessed state; deadzone processor will break asserts below, though
        using (var history = new InputStateHistory<Vector2>(gamepad.leftStick))
        {
            history.Enable();

            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.123f, 0.234f)});
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.345f, 0.456f)});
            InputSystem.Update();
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.567f, 0.678f)});
            InputSystem.Update();

            Assert.That(history.Count, Is.EqualTo(3));
            Assert.That(history[0], Is.EqualTo(new Vector2(0.123f, 0.234f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(history[1], Is.EqualTo(new Vector2(0.345f, 0.456f)).Using(Vector2EqualityComparer.Instance));
            Assert.That(history[2], Is.EqualTo(new Vector2(0.567f, 0.678f)).Using(Vector2EqualityComparer.Instance));
        }
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
