using System;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

partial class CoreTests
{
    [Test]
    [Category("Controls")]
    [Retry(2)] // Warm up JIT.
    public void Controls_CanFindControls_WithoutAllocatingGCMemory()
    {
        // In InputTestFixture, we enable stack traces on native leak detection. This will allocate memory for the
        // stack trace when NativeArray creates the DisposeSentinel. Disable leak detection entirely for this test.
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;

        InputSystem.AddDevice<Gamepad>();

        // Get rid of GC heap activity from first input update.
        InputSystem.Update();

        // Avoid GC activity from string literals.
        var kProfilerRegion = "Controls_CanFindControls_WithoutAllocatingGCMemory";
        var kPath = "<Gamepad>/*stick";

        Assert.That(() =>
        {
            Profiler.BeginSample(kProfilerRegion);
            var list = new InputControlList<InputControl>();
            InputSystem.FindControls(kPath, ref list);
            list.Dispose();
            Profiler.EndSample();
        }, Is.Not.AllocatingGCMemory());
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindFirstMatchingControlByPath()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputSystem.FindControl("*/leftStick/x"), Is.SameAs(gamepad.leftStick.x));
        Assert.That(InputSystem.FindControl("<Gamepad>"), Is.SameAs(gamepad));
        Assert.That(InputSystem.FindControl("<Mouse>/leftButton"), Is.Null);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOnDeviceByPath()
    {
        var gamepad = InputDevice.Build<Gamepad>();
        Assert.That(gamepad["leftStick"], Is.SameAs(gamepad.leftStick));
        Assert.That(gamepad["leftStick/x"], Is.SameAs(gamepad.leftStick.x));
        Assert.That(gamepad.leftStick["x"], Is.SameAs(gamepad.leftStick.x));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DeviceAndControlsRememberTheirLayouts()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(gamepad.layout, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.layout, Is.EqualTo("Stick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ReferToTheirParent()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ReferToTheirDevices()
    {
        var gamepad = InputDevice.Build<Gamepad>();
        Assert.That(gamepad.leftStick.device, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanGetValueType()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.valueType, Is.SameAs(typeof(Vector2)));
        Assert.That(gamepad.leftStick.x.valueType, Is.SameAs(typeof(float)));
        Assert.That(gamepad.buttonSouth.valueType, Is.SameAs(typeof(float)));
        Assert.That(gamepad.valueType, Is.SameAs(typeof(byte[])));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanGetValueSize()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.valueSizeInBytes, Is.EqualTo(sizeof(float) * 2));
        Assert.That(gamepad.leftStick.x.valueSizeInBytes, Is.EqualTo(sizeof(float)));
        Assert.That(gamepad.buttonSouth.valueSizeInBytes, Is.EqualTo(sizeof(float)));
        Assert.That(gamepad.valueSizeInBytes, Is.EqualTo(gamepad.stateBlock.alignedSizeInBytes));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanGetFlatListOfControlsFromDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""layout"" : ""Stick""
                    },
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputDevice.Build<InputDevice>("MyDevice");

        Assert.That(device.allControls.Count,
            Is.EqualTo(2 + 4 + 2)); // 2 toplevel controls, 4 added by Stick, 2 for X and Y
        Assert.That(device.allControls, Contains.Item(device["button"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["up"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["down"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["left"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["right"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["x"]));
        Assert.That(device.allControls, Contains.Item(device["stick"]["y"]));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AskingValueOfControlBeforeDeviceAddedToSystemIsInvalidOperation()
    {
        var device = InputDevice.Build<Gamepad>();

        Assert.Throws<InvalidOperationException>(() => { device.leftStick.ReadValue(); });
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanDetermineWhetherControlIsActuated()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                leftStick = new Vector2(0.01f, 0.02f),
                rightStick = new Vector2(0.4f, 0.5f),
                rightTrigger = 0.123f
            }.WithButton(
                GamepadButton.South));
        InputSystem.Update();

        Assert.That(gamepad.leftStick.IsActuated(), Is.False);
        Assert.That(gamepad.rightStick.IsActuated(), Is.True);
        Assert.That(gamepad.buttonSouth.IsActuated(), Is.True);
        Assert.That(gamepad.buttonNorth.IsActuated(), Is.False);
        Assert.That(gamepad.rightTrigger.IsActuated(), Is.True);
        Assert.That(gamepad.rightTrigger.IsActuated(0.2f), Is.False);
        Assert.That(gamepad.rightTrigger.IsActuated(0.123f), Is.True);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanHaveStickDeadzones()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""stickDeadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        var firstState = new GamepadState {leftStick = new Vector2(0.05f, 0.05f)};
        var secondState = new GamepadState {leftStick = new Vector2(0.5f, 0.5f)};

        InputSystem.QueueStateEvent(device, firstState);
        InputSystem.Update();

        Assert.That(device.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));

        InputSystem.QueueStateEvent(device, secondState);
        InputSystem.Update();

        var processedVector = new StickDeadzoneProcessor { min = 0.1f, max = 0.9f }.Process(new Vector2(0.5f, 0.5f));
        Assert.That(device.leftStick.ReadValue(), Is.EqualTo(processedVector));

        // Deadzoning on the stick axes is independent so we shouldn't see equivalent values on
        // the axes here.
        Assert.That(device.leftStick.x.ReadValue(), Is.Not.EqualTo(processedVector.x));
        Assert.That(device.leftStick.y.ReadValue(), Is.Not.EqualTo(processedVector.y));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanHaveAxisDeadzones()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftTrigger"",
                        ""processors"" : ""axisDeadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        ////NOTE: Unfortunately, this relies on an internal method ATM.
        var processor = device.leftTrigger.TryGetProcessor<AxisDeadzoneProcessor>();

        InputSystem.QueueStateEvent(device, new GamepadState {leftTrigger = 0.05f});
        InputSystem.Update();

        Assert.That(device.leftTrigger.ReadValue(), Is.Zero.Within(0.0001));

        InputSystem.QueueStateEvent(device, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(device.leftTrigger.ReadValue(),
            Is.EqualTo(processor.Process(0.5f)));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanChangeDefaultDeadzoneValuesOnTheFly()
    {
        // Deadzone processor with no specified min/max should take default values
        // from InputSettings.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.settings.defaultDeadzoneMin = 0.1f;
        InputSystem.settings.defaultDeadzoneMax = 0.9f;

        Set(gamepad.leftStick, new Vector2(0.5f, 0.5f));

        Assert.That(gamepad.leftStick.ReadValue(),
            Is.EqualTo(new StickDeadzoneProcessor {min = 0.1f, max = 0.9f}.Process(new Vector2(0.5f, 0.5f))));

        InputSystem.settings.defaultDeadzoneMin = 0.2f;
        InputSystem.settings.defaultDeadzoneMax = 0.8f;

        Assert.That(gamepad.leftStick.ReadValue(),
            Is.EqualTo(new StickDeadzoneProcessor {min = 0.2f, max = 0.8f}.Process(new Vector2(0.5f, 0.5f))));
    }

    [Test]
    [Category("Controls")]
    public void Controls_SticksProvideAccessToHalfAxes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.5f)));
        Assert.That(gamepad.leftStick.down.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.0f)));
        Assert.That(gamepad.leftStick.right.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.5f)));
        Assert.That(gamepad.leftStick.left.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.0f)));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(-0.5f, -0.5f)});
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.0f)));
        Assert.That(gamepad.leftStick.down.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.5f)));
        Assert.That(gamepad.leftStick.right.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.0f)));
        Assert.That(gamepad.leftStick.left.ReadValue(),
            Is.EqualTo(new AxisDeadzoneProcessor().Process(0.5f)));
    }

    // https://fogbugz.unity3d.com/f/cases/1336240/
    [Test]
    [Category("Controls")]
    public void Controls_CanWriteIntoHalfAxesOfSticks()
    {
        // Disable deadzoning.
        InputSystem.settings.defaultDeadzoneMax = 1;
        InputSystem.settings.defaultDeadzoneMin = 0;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick.left, 1f);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(Vector2.left));

        // Set "right" to 1 as well. This is a conflicting state. Result should
        // be that left is 0 and right is 1.
        Set(gamepad.leftStick.right, 1f);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(Vector2.right));

        Set(gamepad.leftStick.up, 1f);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo((Vector2.right + Vector2.up).normalized));

        Set(gamepad.leftStick.down, 1f);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo((Vector2.right + Vector2.down).normalized));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanEvaluateMagnitude()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                leftStick = new Vector2(0.5f, 0.5f),
                leftTrigger = 0.5f
            }.WithButton(GamepadButton.South));
        InputSystem.Update();

        Assert.That(gamepad.rightStick.EvaluateMagnitude(), Is.EqualTo(0).Within(0.00001));
        Assert.That(gamepad.leftStick.EvaluateMagnitude(),
            Is.EqualTo(new StickDeadzoneProcessor().Process(new Vector2(0.5f, 0.5f)).magnitude).Within(0.00001));
        Assert.That(gamepad.buttonNorth.EvaluateMagnitude(), Is.EqualTo(0).Within(0.00001));
        Assert.That(gamepad.buttonSouth.EvaluateMagnitude(), Is.EqualTo(1).Within(0.00001));
        Assert.That(gamepad.leftTrigger.EvaluateMagnitude(), Is.EqualTo(0.5).Within(0.00001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanReadDefaultValue()
    {
        InputSystem.RegisterLayout<TestDeviceWithDefaultState>();

        var device = InputSystem.AddDevice<TestDeviceWithDefaultState>();

        Assert.That(device["control"].ReadDefaultValueAsObject(), Is.EqualTo(0.1234).Within(0.00001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCheckWhetherControlIsAtDefaultValue()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.CheckStateIsAtDefault(), Is.True);
        Assert.That(gamepad.leftStick.CheckStateIsAtDefault(), Is.True);

        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.1234f, 0.2345f));
        InputSystem.Update();

        Assert.That(gamepad.CheckStateIsAtDefault(), Is.False);
        Assert.That(gamepad.leftStick.CheckStateIsAtDefault(), Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCheckWhetherControlIsAtDefaultValue_IgnoringNoise()
    {
        const string layout = @"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""notNoisy"", ""layout"" : ""Button"" },
                    { ""name"" : ""noisy"", ""layout"" : ""Button"", ""noisy"" : true }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestDevice");

        using (StateEvent.From(device, out var eventPtr))
        {
            Assert.That(device.CheckStateIsAtDefaultIgnoringNoise(), Is.True);

            var s1 = device["noisy"].stateBlock;
            var s2 = device["notNoisy"].stateBlock;

            // Actuate noisy control.
            device["noisy"].WriteValueIntoEvent(1f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(device.CheckStateIsAtDefaultIgnoringNoise(), Is.True);

            // Actuate non-noisy control, too.
            device["notNoisy"].WriteValueIntoEvent(1f, eventPtr);
            InputSystem.QueueEvent(eventPtr);
            InputSystem.Update();

            Assert.That(device.CheckStateIsAtDefaultIgnoringNoise(), Is.False);
        }
    }

    [Test]
    [Category("Controls")]
    public unsafe void Controls_CanWriteValueFromObjectIntoState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var tempBufferSize = (int)gamepad.stateBlock.alignedSizeInBytes;
        using (var tempBuffer = new NativeArray<byte>(tempBufferSize, Allocator.Temp))
        {
            var tempBufferPtr = (byte*)tempBuffer.GetUnsafeReadOnlyPtr();

            // The device is the first in the system so is guaranteed to start of offset 0 which
            // means we don't need to adjust the pointer here.
            Debug.Assert(gamepad.stateBlock.byteOffset == 0);

            gamepad.leftStick.WriteValueFromObjectIntoState(new Vector2(0.1234f, 0.5678f), tempBufferPtr);

            var leftStickXPtr = (float*)(tempBufferPtr + gamepad.leftStick.x.stateBlock.byteOffset);
            var leftStickYPtr = (float*)(tempBufferPtr + gamepad.leftStick.y.stateBlock.byteOffset);

            Assert.That(*leftStickXPtr, Is.EqualTo(0.1234).Within(0.00001));
            Assert.That(*leftStickYPtr, Is.EqualTo(0.5678).Within(0.00001));
        }
    }

    [Test]
    [Category("Controls")]
    public unsafe void Controls_CanWriteValueFromBufferIntoState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var tempBufferSize = (int)gamepad.stateBlock.alignedSizeInBytes;
        using (var tempBuffer = new NativeArray<byte>(tempBufferSize, Allocator.Temp))
        {
            var tempBufferPtr = (byte*)tempBuffer.GetUnsafeReadOnlyPtr();

            // The device is the first in the system so is guaranteed to start of offset 0 which
            // means we don't need to adjust the pointer here.
            Debug.Assert(gamepad.stateBlock.byteOffset == 0);

            var vector = new Vector2(0.1234f, 0.5678f);
            var vectorPtr = UnsafeUtility.AddressOf(ref vector);

            gamepad.leftStick.WriteValueFromBufferIntoState(vectorPtr, UnsafeUtility.SizeOf<Vector2>(), tempBufferPtr);

            var leftStickXPtr = (float*)(tempBufferPtr + gamepad.leftStick.x.stateBlock.byteOffset);
            var leftStickYPtr = (float*)(tempBufferPtr + gamepad.leftStick.y.stateBlock.byteOffset);

            Assert.That(*leftStickXPtr, Is.EqualTo(0.1234).Within(0.00001));
            Assert.That(*leftStickYPtr, Is.EqualTo(0.5678).Within(0.00001));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanReadValueFromStateEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputSystem.onEvent +=
            (eventPtr, device) =>
        {
            ++receivedCalls;
            float value;
            Assert.That(gamepad.leftTrigger.ReadValueFromEvent(eventPtr, out value), Is.True);
            Assert.That(value, Is.EqualTo(0.234f).Within(0.00001));
            Assert.That(gamepad.leftTrigger.ReadValueFromEventAsObject(eventPtr), Is.EqualTo(0.234f).Within(0.00001));
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.234f});
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ReadingValueFromStateEvent_ReturnsDefaultValueForControlsNotPartOfEvent()
    {
        // Add one extra control with default state to Gamepad but
        // don't change its state format (so we can send it GamepadState
        // events without the extra control).
        const string json = @"
            {
                ""name"" : ""TestDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""extraControl"",
                        ""layout"" : ""Axis"",
                        ""defaultState"" : ""0.1234""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestDevice");

        float? value = null;
        InputSystem.onEvent +=
            (eventPtr, _) =>
        {
            Assert.That(value, Is.Null);
            ((AxisControl)device["extraControl"]).ReadValueFromEvent(eventPtr, out var eventValue);
            value = eventValue;
        };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(0.1234).Within(0.00001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanWriteValueIntoStateEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputSystem.onEvent +=
            (eventPtr, device) =>
        {
            ++receivedCalls;
            gamepad.leftTrigger.WriteValueIntoEvent(0.1234f, eventPtr);
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.1234).Within(0.000001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanWriteValueIntoDeltaStateEvents()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputSystem.onEvent +=
            (eventPtr, device) =>
        {
            ++receivedCalls;
            gamepad.leftTrigger.WriteValueIntoEvent(0.1234f, eventPtr);
        };

        InputSystem.QueueDeltaStateEvent(gamepad.leftTrigger, 0.8765f);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.1234).Within(0.000001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanWriteValueIntoState()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var state = new GamepadState();
        var value = new Vector2(0.5f, 0.5f);

        gamepad.leftStick.WriteValueIntoState(value, ref state);

        Assert.That(state.leftStick, Is.EqualTo(value));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanQueueValueChange()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        gamepad.leftTrigger.QueueValueChange(0.123f);
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.Update();
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.00001));

        gamepad.leftTrigger.QueueValueChange(0.234f);
        gamepad.leftTrigger.QueueValueChange(0.345f);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.00001));

        InputSystem.Update();

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.345).Within(0.00001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanQueueValueChange_InFuture()
    {
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInFixedUpdate;
        var gamepad = InputSystem.AddDevice<Gamepad>();

        gamepad.leftTrigger.QueueValueChange(0.123f, 0.5);

        InputSystem.Update();
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0).Within(0.00001));

        runtime.currentTimeForFixedUpdate = 1;
        InputSystem.Update();
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.00001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DpadVectorsAreCircular()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Up.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadButton.DpadUp});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.up));

        // Up left.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadButton.DpadUp | 1 << (int)GamepadButton.DpadLeft
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadButton.DpadLeft});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.left));

        // Down left.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadButton.DpadDown | 1 << (int)GamepadButton.DpadLeft
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.down + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.down + Vector2.left).normalized.y).Within(0.00001));

        // Down.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadButton.DpadDown});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.down));

        // Down right.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadButton.DpadDown | 1 << (int)GamepadButton.DpadRight
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x,
            Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y,
            Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadButton.DpadRight});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.right));

        // Up right.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadButton.DpadUp | 1 << (int)GamepadButton.DpadRight
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.up + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.up + Vector2.right).normalized.y).Within(0.00001));
    }

    private struct DiscreteButtonDpadState : IInputStateTypeInfo
    {
        public int dpad;

        public DiscreteButtonDpadState(int dpad)
        {
            this.dpad = dpad;
        }

        public FourCC format => new FourCC('C', 'U', 'S', 'T');
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFormDpadOutOfDiscreteButtonStates()
    {
        // Create a custom device with just a Dpad and customize
        // the Dpad to use DiscreteButtonControls instead of ButtonControls.
        const string json = @"
        {
            ""name"" : ""MyDevice"",
            ""format"" : ""CUST"",
            ""controls"" : [
                { ""name"" : ""dpad"", ""layout"" : ""Dpad"" },
                { ""name"" : ""dpad/up"", ""layout"" : ""DiscreteButton"", ""parameters"" : ""minValue=2,maxValue=4"", ""bit"" : 0, ""sizeInBits"" : 4 },
                { ""name"" : ""dpad/down"", ""layout"" : ""DiscreteButton"", ""parameters"" : ""minValue=6,maxValue=8"", ""bit"" : 0, ""sizeInBits"" : 4 },
                { ""name"" : ""dpad/left"", ""layout"" : ""DiscreteButton"", ""parameters"" : ""minValue=8, maxValue=2"", ""bit"" : 0, ""sizeInBits"" : 4 },
                { ""name"" : ""dpad/right"", ""layout"" : ""DiscreteButton"", ""parameters"" : ""minValue=4,maxValue=6"", ""bit"" : 0, ""sizeInBits"" : 4 }
            ]
        }";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("MyDevice");
        var dpad = (DpadControl)device["dpad"];

        InputSystem.QueueStateEvent(device, new DiscreteButtonDpadState(1));
        InputSystem.Update();

        Assert.That(dpad.left.isPressed, Is.True);
        Assert.That(dpad.right.isPressed, Is.False);
        Assert.That(dpad.up.isPressed, Is.False);
        Assert.That(dpad.down.isPressed, Is.False);
        Assert.AreEqual(dpad.x.ReadValueAsObject(), -1.0f);
        Assert.AreEqual(dpad.y.ReadValueAsObject(), 0.0f);

        InputSystem.QueueStateEvent(device, new DiscreteButtonDpadState(8));
        InputSystem.Update();

        Assert.That(dpad.left.isPressed, Is.True);
        Assert.That(dpad.down.isPressed, Is.True);
        Assert.That(dpad.up.isPressed, Is.False);
        Assert.That(dpad.right.isPressed, Is.False);
        Assert.AreEqual(dpad.x.ReadValueAsObject(), -0.707107f);
        Assert.AreEqual(dpad.y.ReadValueAsObject(), -0.707107f);
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControls()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(gamepad.leftStick.path, Is.EqualTo("/Gamepad/leftStick"));

        InputSystem.AddDevice(gamepad);

        Assert.That(gamepad.leftStick.path, Is.EqualTo("/Gamepad/leftStick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanQueryValueOfControls_AfterAddingDevice()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(() => gamepad.leftStick.ReadValue(), Throws.InvalidOperationException);

        InputSystem.AddDevice(gamepad);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPath()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/Gamepad/leftStick"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPathCaseInsensitive()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/gamePAD/LeftSTICK"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    [TestCase("<Gamepad>{LeftHand}foo/*/left", "layout:Gamepad,usage:LeftHand,name:foo", "wildcard:", "name:left")]
    [TestCase("<Keyboard>/#(;)", "layout:Keyboard", "displayName:;")]
    public void Controls_CanParseControlPath(string path, params string[] parts)
    {
        var parsed = InputControlPath.Parse(path).ToArray();

        Assert.That(parsed, Has.Length.EqualTo(parts.Length));
        Assert.That(parsed.Zip(parts, (a, b) =>
        {
            var properties = b.Split(',');
            return properties.All(p =>
            {
                var nameAndValue = p.Split(':').ToArray();
                switch (nameAndValue[0])
                {
                    case "layout": return a.layout == nameAndValue[1];
                    case "usage": return a.usages.Count() == 1 && a.usages.First() == nameAndValue[1];
                    case "name": return a.name == nameAndValue[1];
                    case "displayName": return a.displayName == nameAndValue[1];
                    case "wildcard": return a.isWildcard;
                }
                return false;
            });
        }), Has.All.True);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByType()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls<StickControl>("/<Gamepad>/*"))
        {
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByDisplayName()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("<Gamepad>/#(right shoulder)"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightShoulder));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByUsage()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/gamepad/{Primary2DMotion}"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_FindingControlsByUsage_IgnoresUsagesOnDevice()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetDeviceUsage(gamepad, "Primary2DMotion");

        using (var matches = InputSystem.FindControls("<Gamepad>/{Primary2DMotion}"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOfControlsFoundByUsage()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/gamepad/{Primary2DMotion}/x"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick.x));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByLayout()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/gamepad/<stick>"))
        {
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByBaseLayout()
    {
        const string json = @"
            {
                ""name"" : ""MyGamepad"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("MyGamepad");

        using (var matches = InputSystem.FindControls("/<gamepad>"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(device));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsFromMultipleDevices()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();

        using (var matches = InputSystem.FindControls("/*/*Stick"))
        {
            Assert.That(matches, Has.Count.EqualTo(4));

            Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.leftStick));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.rightStick));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.leftStick));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.rightStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanOmitLeadingSlashWhenFindingControls()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("gamepad/leftStick"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTheirAliases()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matchByName = InputSystem.FindControls("/gamepad/buttonSouth"))
        using (var matchByAlias1 = InputSystem.FindControls("/gamepad/a"))
        using (var matchByAlias2 = InputSystem.FindControls("/gamepad/cross"))
        {
            Assert.That(matchByName, Has.Count.EqualTo(1));
            Assert.That(matchByName, Has.Exactly(1).SameAs(gamepad.buttonSouth));
            Assert.That(matchByAlias1, Is.EqualTo(matchByName));
            Assert.That(matchByAlias2, Is.EqualTo(matchByName));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcards()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches1 = InputSystem.FindControls("<Gamepad>/left*"))
        using (var matches2 = InputSystem.FindControls("<Gamepad>/*Trigger"))
        {
            Assert.That(matches1, Has.Count.EqualTo(4));
            Assert.That(matches1, Has.Exactly(1).SameAs(gamepad.leftStick));
            Assert.That(matches1, Has.Exactly(1).SameAs(gamepad.leftTrigger));
            Assert.That(matches1, Has.Exactly(1).SameAs(gamepad.leftStickButton));
            Assert.That(matches1, Has.Exactly(1).SameAs(gamepad.leftShoulder));

            Assert.That(matches2, Has.Count.EqualTo(2));
            Assert.That(matches2, Has.Exactly(1).SameAs(gamepad.leftTrigger));
            Assert.That(matches2, Has.Exactly(1).SameAs(gamepad.rightTrigger));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcards_InMiddleOfNames()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var matches = InputSystem.FindControls("/g*pad/leftStick"))
        {
            Assert.That(matches, Has.Count.EqualTo(1));
            Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanDetermineIfControlIsPressed()
    {
        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Set(gamepad.leftStick, Vector2.one);
        Set(gamepad.leftTrigger, 0.6f);
        Press(gamepad.buttonSouth);

        //// https://jira.unity3d.com/browse/ISX-926
        ////REVIEW: IsPressed() should probably be renamed. As is apparent from the calls here, it's not always
        ////        readily apparent that the way it is defined ("actuation level at least at button press threshold")
        ////        does not always connect to what it intuitively means for the specific control.

        Assert.That(gamepad.leftTrigger.IsPressed(), Is.True);
        Assert.That(gamepad.rightTrigger.IsPressed(), Is.False);
        Assert.That(gamepad.buttonSouth.IsPressed(), Is.True);
        Assert.That(gamepad.buttonNorth.IsPressed(), Is.False);
        Assert.That(gamepad.leftStick.IsPressed(), Is.True); // Note how this diverges from the actual meaning of "is the left stick pressed?"
        Assert.That(gamepad.rightStick.IsPressed(), Is.False);

        // https://fogbugz.unity3d.com/f/cases/1374024/
        // Calling it on the entire device should be false.
        Assert.That(gamepad.IsPressed(), Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCustomizeDefaultButtonPressPoint()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.settings.defaultButtonPressPoint = 0.4f;

        Set(gamepad.leftTrigger, 0.39f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        Set(gamepad.leftTrigger, 0.4f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        InputSystem.settings.defaultButtonPressPoint = 0.5f;

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        InputSystem.settings.defaultButtonPressPoint = 0;

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        // Setting the trigger to 0 requires the system to be "smart" enough to
        // figure out that 0 as a default button press point doesn't make sense
        // and that instead the press point should clamp off at some low, non-zero value.
        // https://fogbugz.unity3d.com/f/cases/1349002/
        Set(gamepad.leftTrigger, 0f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);

        Set(gamepad.leftTrigger, 0.001f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.True);

        InputSystem.settings.defaultButtonPressPoint = -1;
        Set(gamepad.leftTrigger, 0f);

        Assert.That(gamepad.leftTrigger.isPressed, Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCustomizePressPointOfGamepadTriggers()
    {
        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""pressPoint=0.2""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var gamepad = InputDevice.Build<Gamepad>("CustomGamepad");

        Assert.That(gamepad.rightTrigger.pressPoint, Is.EqualTo(0.2f).Within(0.0001f));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DisplayNameDefaultsToControlName()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""control"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var control = InputDevice.Build<InputDevice>("MyDevice")["control"];

        Assert.That(control.displayName, Is.EqualTo("control"));
        Assert.That(control.shortDisplayName, Is.Null);
    }

    [Test]
    [Category("Controls")]
    public void Controls_DisplayNameForNestedControls_IncludesNameOfParentControl()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.up.displayName, Is.EqualTo("Left Stick Up"));
        Assert.That(gamepad.leftStick.up.shortDisplayName, Is.EqualTo("LS Up"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanTurnControlPathIntoHumanReadableText()
    {
        Assert.That(InputControlPath.ToHumanReadableString("*/{PrimaryAction}"), Is.EqualTo("PrimaryAction [Any]"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/leftStick"), Is.EqualTo("Left Stick [Gamepad]"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/leftStick/x"), Is.EqualTo("Left Stick/X [Gamepad]"));
        Assert.That(InputControlPath.ToHumanReadableString("<XRController>{LeftHand}/position"), Is.EqualTo("position [LeftHand XR Controller]"));
        Assert.That(InputControlPath.ToHumanReadableString("*/leftStick"), Is.EqualTo("leftStick [Any]"));
        Assert.That(InputControlPath.ToHumanReadableString("*/{PrimaryMotion}/x"), Is.EqualTo("PrimaryMotion/x [Any]"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/buttonSouth"), Is.EqualTo("Button South [Gamepad]"));
        Assert.That(InputControlPath.ToHumanReadableString("<XInputController>/buttonSouth"), Is.EqualTo("A [Xbox Controller]"));
        Assert.That(InputControlPath.ToHumanReadableString("<Touchscreen>/touch4/tap"), Is.EqualTo("Touch #4/Tap [Touchscreen]"));

        // OmitDevice.
        Assert.That(
            InputControlPath.ToHumanReadableString("<Gamepad>/buttonSouth",
                InputControlPath.HumanReadableStringOptions.OmitDevice), Is.EqualTo("Button South"));
        Assert.That(
            InputControlPath.ToHumanReadableString("*/{PrimaryAction}",
                InputControlPath.HumanReadableStringOptions.OmitDevice), Is.EqualTo("PrimaryAction"));

        // UseShortName.
        Assert.That(
            InputControlPath.ToHumanReadableString("<Gamepad>/buttonSouth", InputControlPath.HumanReadableStringOptions.UseShortNames),
            Is.EqualTo(GamepadState.ButtonSouthShortDisplayName + " [Gamepad]"));
        Assert.That(
            InputControlPath.ToHumanReadableString("<Mouse>/leftButton", InputControlPath.HumanReadableStringOptions.UseShortNames),
            Is.EqualTo("LMB [Mouse]"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanTurnControlPathIntoHumanReadableText_UsingDisplayNamesFromActualDevice()
    {
        InputSystem.AddDevice<Keyboard>();

        // Pretend 'a' key is mapped to 'q' in current keyboard layout.
        SetKeyInfo(Key.A, "q");

        Assert.That(InputControlPath.ToHumanReadableString("<Keyboard>/a", control: Keyboard.current), Is.EqualTo("Q [Keyboard]"));
    }

    private class DeviceWithoutAnyControls : InputDevice
    {
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanTurnControlPathIntoHumanReadableText_EvenIfLayoutCannotBeFoundOrHasErrors()
    {
        // This one will throw as the layout will result in a zero-size memory block.
        InputSystem.RegisterLayout<DeviceWithoutAnyControls>();

        Assert.That(InputControlPath.ToHumanReadableString("<UnknownGamepad>/leftStick"), Is.EqualTo("leftStick [UnknownGamepad]"));
        Assert.That(InputControlPath.ToHumanReadableString("<DeviceWithoutAnyControls>/control"), Is.EqualTo("control [DeviceWithoutAnyControls]"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCheckIfControlMatchesGivenPath()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputControlPath.Matches("<Gamepad>/leftStick", gamepad.leftStick), Is.True);
        Assert.That(InputControlPath.Matches("<Gamepad>/rightStick", gamepad.leftStick), Is.False);
        Assert.That(InputControlPath.Matches("<Gamepad>", gamepad.leftStick), Is.False);
        Assert.That(InputControlPath.Matches("<Gamepad>/*", gamepad.leftStick), Is.True);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanCheckIfControlMatchesGivenPathPrefix()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(InputControlPath.MatchesPrefix("<Gamepad>", gamepad.leftStick), Is.True);
        Assert.That(InputControlPath.MatchesPrefix("<Gamepad>/leftStick", gamepad.leftStick), Is.True);
        Assert.That(InputControlPath.MatchesPrefix("<Gamepad>/rightStick", gamepad.rightStick.x), Is.True);
        Assert.That(InputControlPath.MatchesPrefix("<Gamepad>/*", gamepad.leftStick), Is.True);
        Assert.That(InputControlPath.MatchesPrefix("<Keyboard>", gamepad.leftStick), Is.False);
        Assert.That(InputControlPath.MatchesPrefix("<Gamepad>/rightStick", gamepad.leftStick), Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanKeepListsOfControls_WithoutAllocatingGCMemory()
    {
        InputSystem.AddDevice<Mouse>(); // Noise.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var list = default(InputControlList<InputControl>);
        Assert.That(() => { list = new InputControlList<InputControl>(); }, Is.Not.AllocatingGCMemory());

        try
        {
            Assert.That(list.Count, Is.Zero);
            Assert.That(list.ToArray(), Is.Empty);
            Assert.That(() => list[0], Throws.TypeOf<ArgumentOutOfRangeException>());

            list.Capacity = 4;

            Assert.That(() =>
            {
                list.Add(gamepad.leftStick);
                list.Add(null); // Permissible to add null entry.
                list.Add(keyboard.spaceKey);
                list.Add(keyboard);
            }, Is.Not.AllocatingGCMemory());

            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Capacity, Is.EqualTo(4));
            Assert.That(list[0], Is.SameAs(gamepad.leftStick));
            Assert.That(list[1], Is.Null);
            Assert.That(list[2], Is.SameAs(keyboard.spaceKey));
            Assert.That(list[3], Is.SameAs(keyboard));
            Assert.That(() => list[4], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(list.ToArray(),
                Is.EquivalentTo(new InputControl[] {gamepad.leftStick, null, keyboard.spaceKey, keyboard}));
            Assert.That(list.Contains(gamepad.leftStick));
            Assert.That(list.Contains(null));
            Assert.That(list.Contains(keyboard.spaceKey));
            Assert.That(list.Contains(keyboard));

            Assert.That(() =>
            {
                list.RemoveAt(1);
                list.Remove(keyboard);
            }, Is.Not.AllocatingGCMemory());

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(4));
            Assert.That(list[0], Is.SameAs(gamepad.leftStick));
            Assert.That(list[1], Is.SameAs(keyboard.spaceKey));
            Assert.That(() => list[2], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(list.ToArray(), Is.EquivalentTo(new InputControl[] {gamepad.leftStick, keyboard.spaceKey}));
            Assert.That(list.Contains(gamepad.leftStick));
            Assert.That(!list.Contains(null));
            Assert.That(list.Contains(keyboard.spaceKey));
            Assert.That(!list.Contains(keyboard));

            list.AddRange(new InputControl[] {keyboard.aKey, keyboard.bKey}, count: 1, destinationIndex: 0);

            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(4));
            Assert.That(list,
                Is.EquivalentTo(new InputControl[]
                    {keyboard.aKey, gamepad.leftStick, keyboard.spaceKey}));

            list.AddRange(new InputControl[] {keyboard.bKey, keyboard.cKey});

            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list,
                Is.EquivalentTo(new InputControl[]
                    {keyboard.aKey, gamepad.leftStick, keyboard.spaceKey, keyboard.bKey, keyboard.cKey}));

            using (var toAdd = new InputControlList<InputControl>(gamepad.buttonNorth, gamepad.buttonEast, gamepad.buttonWest))
                list.AddSlice(toAdd, count: 1, destinationIndex: 1, sourceIndex: 2);

            Assert.That(list.Count, Is.EqualTo(6));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list,
                Is.EquivalentTo(new InputControl[]
                    {keyboard.aKey, gamepad.buttonWest, gamepad.leftStick, keyboard.spaceKey, keyboard.bKey, keyboard.cKey}));

            list[0] = keyboard.zKey;

            Assert.That(list,
                Is.EquivalentTo(new InputControl[]
                    {keyboard.zKey, gamepad.buttonWest, gamepad.leftStick, keyboard.spaceKey, keyboard.bKey, keyboard.cKey}));

            list.Clear();

            Assert.That(list.Count, Is.Zero);
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.ToArray(), Is.Empty);
            Assert.That(() => list[0], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(!list.Contains(gamepad.leftStick));
            Assert.That(!list.Contains(null));
            Assert.That(!list.Contains(keyboard.spaceKey));
            Assert.That(!list.Contains(keyboard));
        }
        finally
        {
            list.Dispose();
        }
    }

    [Test]
    [Category("Controls")]
    public void Controls_TouchControlStateCorrespondsToTouchState()
    {
        var touchscreen = InputSystem.AddDevice<Touchscreen>();

        Assert.That(UnsafeUtility.SizeOf<TouchState>(), Is.EqualTo(TouchState.kSizeInBytes));
        Assert.That(touchscreen.touches[0].stateBlock.alignedSizeInBytes, Is.EqualTo(TouchState.kSizeInBytes));
    }
}
