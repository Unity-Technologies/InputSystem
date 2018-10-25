using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

#if UNITY_2018_3_OR_NEWER
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
#endif

partial class CoreTests
{
    #if UNITY_2018_3_OR_NEWER
    [Test]
    [Category("Controls")]
    [Ignore("TODO")]
    public void TODO_Controls_CanFindControls_WithoutAllocatingGCMemory()
    {
        InputSystem.AddDevice<Gamepad>();

        var list = new InputControlList<InputControl>();
        try
        {
            Assert.That(() =>
            {
                InputSystem.FindControls("<Gamepad>/*stick", ref list);
            }, Is.Not.AllocatingGCMemory());
        }
        finally
        {
            list.Dispose();
        }
    }

    #endif

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsInSetupByPath()
    {
        var setup = new InputDeviceBuilder("Gamepad");

        Assert.That(setup.TryGetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.TryGetControl("leftStick/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/y"), Is.TypeOf<AxisControl>());
        Assert.That(setup.TryGetControl("leftStick/up"), Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsByPath()
    {
        var gamepad = (Gamepad) new InputDeviceBuilder("Gamepad").Finish();
        Assert.That(gamepad["leftStick"], Is.SameAs(gamepad.leftStick));
        Assert.That(gamepad["leftStick/x"], Is.SameAs(gamepad.leftStick.x));
        Assert.That(gamepad.leftStick["x"], Is.SameAs(gamepad.leftStick.x));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DeviceAndControlsRememberTheirLayouts()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.layout, Is.EqualTo("Gamepad"));
        Assert.That(gamepad.leftStick.layout, Is.EqualTo("Stick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ReferToTheirParent()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ReferToTheirDevices()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(leftStick.device, Is.SameAs(device));
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

        var device = new InputDeviceBuilder("MyDevice").Finish();

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
        var setup = new InputDeviceBuilder("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.Throws<InvalidOperationException>(() => { device.leftStick.ReadValue(); });
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanProcessDeadzones()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        ////NOTE: Unfortunately, this relies on an internal method ATM.
        var processor = device.leftStick.TryGetProcessor<DeadzoneProcessor>();

        var firstState = new GamepadState {leftStick = new Vector2(0.05f, 0.05f)};
        var secondState = new GamepadState {leftStick = new Vector2(0.5f, 0.5f)};

        InputSystem.QueueStateEvent(device, firstState);
        InputSystem.Update();

        Assert.That(device.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));

        InputSystem.QueueStateEvent(device, secondState);
        InputSystem.Update();

        Assert.That(device.leftStick.ReadValue(),
            Is.EqualTo(processor.Process(new Vector2(0.5f, 0.5f), device.leftStick)));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanChangeDefaultDeadzoneValuesOnTheFly()
    {
        // Deadzone processor with no specified min/max should take default values
        // from InputConfiguration.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""processors"" : ""deadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        var processor = device.leftStick.TryGetProcessor<DeadzoneProcessor>();

        Assert.That(processor.minOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMin));
        Assert.That(processor.maxOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMax));

        InputConfiguration.DeadzoneMin = InputConfiguration.DeadzoneMin + 0.1f;
        InputConfiguration.DeadzoneMax = InputConfiguration.DeadzoneMin - 0.1f;

        Assert.That(processor.minOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMin));
        Assert.That(processor.maxOrDefault, Is.EqualTo(InputConfiguration.DeadzoneMax));
    }

    [Test]
    [Category("Controls")]
    public void Controls_SticksProvideAccessToHalfAxes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(-0.5f, -0.5f)});
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
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
    public unsafe void Controls_CanWriteValueAsObjectIntoMemoryBuffer()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var tempBufferSize = (int)gamepad.stateBlock.alignedSizeInBytes;
        using (var tempBuffer = new NativeArray<byte>(tempBufferSize, Allocator.Temp))
        {
            var tempBufferPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(tempBuffer);

            // The device is the first in the system so is guaranteed to start of offset 0 which
            // means we don't need to adjust the pointer here.
            Debug.Assert(gamepad.stateBlock.byteOffset == 0);

            gamepad.leftStick.WriteValueFromObjectInto(new IntPtr(tempBufferPtr), tempBufferSize, new Vector2(0.1234f, 0.5678f));

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
            eventPtr =>
        {
            ++receivedCalls;
            float value;
            Assert.IsTrue(gamepad.leftTrigger.ReadValueFrom(eventPtr, out value));
            Assert.That(value, Is.EqualTo(0.234f).Within(0.00001));
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
            eventPtr =>
        {
            Assert.That(value, Is.Null);
            float eventValue;
            ((AxisControl)device["extraControl"]).ReadValueFrom(eventPtr, out eventValue);
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
            eventPtr =>
        {
            ++receivedCalls;
            gamepad.leftTrigger.WriteValueInto(eventPtr, 0.1234f);
        };

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
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

        gamepad.leftStick.WriteValueInto(ref state, value);

        Assert.That(state.leftStick, Is.EqualTo(value));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DpadVectorsAreCircular()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Up.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.DpadUp});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.up));

        // Up left.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadLeft
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.DpadLeft});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.left));

        // Down left.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadLeft
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.down + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.down + Vector2.left).normalized.y).Within(0.00001));

        // Down.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.DpadDown});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.down));

        // Down right.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadRight
            });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x,
            Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y,
            Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.DpadRight});
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.right));

        // Up right.
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState
            {
                buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadRight
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

        public FourCC GetFormat()
        {
            return new FourCC('C', 'U', 'S', 'T');
        }
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

        InputSystem.QueueStateEvent(device, new DiscreteButtonDpadState(8));
        InputSystem.Update();

        Assert.That(dpad.left.isPressed, Is.True);
        Assert.That(dpad.down.isPressed, Is.True);
        Assert.That(dpad.up.isPressed, Is.False);
        Assert.That(dpad.right.isPressed, Is.False);
    }

    [Test]
    [Category("Controls")]
    public void Controls_AssignsFullPathToControls()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var leftStick = setup.GetControl("leftStick");

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));

        var device = setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(leftStick.path, Is.EqualTo("/Gamepad/leftStick"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_AfterAddingDeviceCanQueryValueOfControls()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var device = (Gamepad)setup.Finish();
        InputSystem.AddDevice(device);

        Assert.That(device.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));
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
    public void Controls_CanFindControlsUsingWildcardsInMiddleOfNames()
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
        var gamepad = (Gamepad) new InputDeviceBuilder("CustomGamepad").Finish();

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

        var setup = new InputDeviceBuilder("MyDevice");
        var control = setup.GetControl("control");

        Assert.That(control.displayName, Is.EqualTo("control"));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanTurnControlPathIntoHumanReadableText()
    {
        Assert.That(InputControlPath.ToHumanReadableString("*/{PrimaryAction}"), Is.EqualTo("PrimaryAction"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/leftStick"), Is.EqualTo("Gamepad leftStick"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/leftStick/x"), Is.EqualTo("Gamepad leftStick/x"));
        Assert.That(InputControlPath.ToHumanReadableString("<Gamepad>/leftStick/x"), Is.EqualTo("Gamepad leftStick/x"));
        Assert.That(InputControlPath.ToHumanReadableString("<XRController>{LeftHand}/position"), Is.EqualTo("LeftHand XRController position"));
        Assert.That(InputControlPath.ToHumanReadableString("*/leftStick"), Is.EqualTo("Any leftStick"));
        Assert.That(InputControlPath.ToHumanReadableString("*/{PrimaryMotion}/x"), Is.EqualTo("Any PrimaryMotion/x"));
    }

    ////TODO: doesnotallocate constraint
    [Test]
    [Category("Controls")]
    public void Controls_CanKeepListsOfControls_WithoutAllocatingGCMemory()
    {
        InputSystem.AddDevice<Mouse>(); // Noise.
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var list = new InputControlList<InputControl>();
        try
        {
            Assert.That(list.Count, Is.Zero);
            Assert.That(list.ToArray(), Is.Empty);
            Assert.That(() => list[0], Throws.TypeOf<ArgumentOutOfRangeException>());

            list.Capacity = 10;

            list.Add(gamepad.leftStick);
            list.Add(null); // Permissible to add null entry.
            list.Add(keyboard.spaceKey);
            list.Add(keyboard);

            Assert.That(list.Count, Is.EqualTo(4));
            Assert.That(list.Capacity, Is.EqualTo(6));
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

            list.RemoveAt(1);
            list.Remove(keyboard);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(8));
            Assert.That(list[0], Is.SameAs(gamepad.leftStick));
            Assert.That(list[1], Is.SameAs(keyboard.spaceKey));
            Assert.That(() => list[2], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(list.ToArray(), Is.EquivalentTo(new InputControl[] {gamepad.leftStick, keyboard.spaceKey}));
            Assert.That(list.Contains(gamepad.leftStick));
            Assert.That(!list.Contains(null));
            Assert.That(list.Contains(keyboard.spaceKey));
            Assert.That(!list.Contains(keyboard));

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
}
