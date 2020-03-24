using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;

#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

#pragma warning disable CS0649
partial class CoreTests
{
    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreatePrimitiveControlsFromLayout()
    {
        var gamepad = InputDevice.Build<Gamepad>();

        // The default ButtonControl layout has no controls inside of it.
        Assert.That(gamepad["start"], Is.TypeOf<ButtonControl>());
        Assert.That(gamepad["start"].children, Is.Empty);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreateCompoundControlsFromLayout()
    {
        const int kNumControlsInAStick = 6;

        var gamepad = InputDevice.Build<Gamepad>();

        Assert.That(gamepad["leftStick"], Is.TypeOf<StickControl>());
        Assert.That(gamepad["leftStick"].children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(gamepad["leftStick"].children, Has.Exactly(1).With.Property("name").EqualTo("x"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUpDeviceFromJsonLayout()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2""
            }
        ";
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""myThing"",
                        ""layout"" : ""MyControl"",
                        ""usage"" : ""LeftStick""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(deviceJson);
        InputSystem.RegisterLayout(controlJson);

        var device = InputDevice.Build<InputDevice>("MyDevice");

        Assert.That(device, Is.TypeOf<InputDevice>());
        Assert.That(device["myThing/x"], Is.TypeOf<AxisControl>());
        Assert.That(device["myThing"], Has.Property("layout").EqualTo("MyControl"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CannotUseLayoutForControlToBuildDevice()
    {
        Assert.That(() => InputDevice.Build<InputDevice>("Button"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_UsagesOverrideThoseFromBaseLayout()
    {
        const string baseLayout = @"
            {
                ""name"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""layout"" : ""Stick"",
                        ""usage"" : ""BaseUsage""
                    },
                    {
                        ""name"" : ""axis"",
                        ""layout"" : ""Axis"",
                        ""usage"" : ""BaseUsage""
                    },
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";
        const string derivedLayout = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""usage"" : ""DerivedUsage""
                    },
                    {
                        ""name"" : ""axis""
                    },
                    {
                        ""name"" : ""button"",
                        ""usage"" : ""DerivedUsage""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(baseLayout);
        InputSystem.RegisterLayout(derivedLayout);

        var layout = InputSystem.LoadLayout("DerivedLayout");

        Assert.That(layout["stick"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["stick"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
        Assert.That(layout["axis"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["axis"].usages, Has.Exactly(1).EqualTo(new InternedString("BaseUsage")));
        Assert.That(layout["button"].usages.Count, Is.EqualTo(1));
        Assert.That(layout["button"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyControlInBaseLayoutUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""format"" : ""BYTE""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputDevice.Build<Gamepad>("MyDevice");

        Assert.That(device.leftStick.x.stateBlock.format, Is.EqualTo(InputStateBlock.FormatByte));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetControlParametersThroughControlAttribute()
    {
        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.up.clamp, Is.EqualTo(AxisControl.Clamp.AfterNormalize));
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUsagesThroughControlAttribute()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.Primary2DMotion));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetAliasesThroughControlAttribute()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("square")));
        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("x")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetDefaultStateOfControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""analog"",
                        ""layout"" : ""Axis"",
                        ""defaultState"" : ""0.5""
                    },
                    {
                        ""name"" : ""digital"",
                        ""layout"" : ""Digital"",
                        ""defaultState"" : ""1234""
                    },
                    {
                        ""name"" : ""hexDigital"",
                        ""layout"" : ""Digital"",
                        ""defaultState"" : ""0x1234""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var layout = InputSystem.LoadLayout("MyDevice");

        Assert.That(layout["analog"].defaultState.type, Is.EqualTo(TypeCode.Double));
        Assert.That(layout["analog"].defaultState.ToDouble(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(layout["digital"].defaultState.type, Is.EqualTo(TypeCode.Int64));
        Assert.That(layout["digital"].defaultState.ToInt64(), Is.EqualTo(1234));
        Assert.That(layout["hexDigital"].defaultState.type, Is.EqualTo(TypeCode.Int64));
        Assert.That(layout["hexDigital"].defaultState.ToInt64(), Is.EqualTo(0x1234));
    }

    [Preserve]
    class TestDeviceWithDefaultState : InputDevice
    {
        [Preserve]
        [InputControl(defaultState = 0.1234)]
        public AxisControl control { get; set; }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetDefaultStateOfControlOnAttribute()
    {
        InputSystem.RegisterLayout<TestDeviceWithDefaultState>();

        var layout = InputSystem.LoadLayout("TestDeviceWithDefaultState");

        Assert.That(layout["control"].defaultState.type, Is.EqualTo(TypeCode.Double));
        Assert.That(layout["control"].defaultState.ToDouble(), Is.EqualTo(0.1234).Within(0.00001));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanOverrideDefaultStateValuesFromBaseLayout()
    {
        const string baseLayout = @"
            {
                ""name"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""control1"",
                        ""layout"" : ""Axis"",
                        ""defaultState"" : ""0.2345""
                    },
                    {
                        ""name"" : ""control2"",
                        ""layout"" : ""Axis"",
                        ""defaultState"" : ""0.3456""
                    },
                    {
                        ""name"" : ""control3"",
                        ""layout"" : ""Axis""
                    }
                ]
            }
        ";
        const string derivedLayout = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""control1"",
                        ""defaultState"" : ""0.9876""
                    },
                    {
                        ""name"" : ""control3"",
                        ""defaultState"" : ""0.1234""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(baseLayout);
        InputSystem.RegisterLayout(derivedLayout);

        var layout = InputSystem.LoadLayout("DerivedLayout");

        Assert.That(layout["control1"].defaultState.type, Is.EqualTo(TypeCode.Double));
        Assert.That(layout["control1"].defaultState.ToDouble(), Is.EqualTo(0.9876).Within(0.00001));
        Assert.That(layout["control2"].defaultState.type, Is.EqualTo(TypeCode.Double));
        Assert.That(layout["control2"].defaultState.ToDouble(), Is.EqualTo(0.3456).Within(0.00001));
        Assert.That(layout["control3"].defaultState.type, Is.EqualTo(TypeCode.Double));
        Assert.That(layout["control3"].defaultState.ToDouble(), Is.EqualTo(0.1234).Within(0.00001));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetParametersOnControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""rightTrigger"",
                        ""parameters"" : ""clamp=1,clampMin=0.123,clampMax=0.456""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        Assert.That(device.rightTrigger.clamp, Is.EqualTo(AxisControl.Clamp.BeforeNormalize));
        Assert.That(device.rightTrigger.clampMin, Is.EqualTo(0.123).Within(0.00001f));
        Assert.That(device.rightTrigger.clampMax, Is.EqualTo(0.456).Within(0.00001f));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddProcessorsToControlInJson()
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

        // NOTE: Unfortunately, this currently relies on an internal method (TryGetProcessor).

        Assert.That(device.leftStick.TryGetProcessor<StickDeadzoneProcessor>(), Is.Not.Null);
        Assert.That(device.leftStick.TryGetProcessor<StickDeadzoneProcessor>().min, Is.EqualTo(0.1).Within(0.00001f));
        Assert.That(device.leftStick.TryGetProcessor<StickDeadzoneProcessor>().max, Is.EqualTo(0.9).Within(0.00001f));
    }

    private unsafe struct StateStructWithArrayOfControls
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float value[5];
    }

    [InputControlLayout(stateType = typeof(StateStructWithArrayOfControls))]
    [Preserve]
    private class TestDeviceWithArrayOfControls : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddArrayOfControls_InStateStruct()
    {
        InputSystem.RegisterLayout<TestDeviceWithArrayOfControls>();

        var device = InputDevice.Build<InputDevice>("TestDeviceWithArrayOfControls");

        Assert.That(device.allControls, Has.Count.EqualTo(5));
        Assert.That(device["value0"], Is.TypeOf<AxisControl>());
        Assert.That(device["value1"], Is.TypeOf<AxisControl>());
        Assert.That(device["value2"], Is.TypeOf<AxisControl>());
        Assert.That(device["value3"], Is.TypeOf<AxisControl>());
        Assert.That(device["value4"], Is.TypeOf<AxisControl>());
        Assert.That(device["value0"].stateBlock.byteOffset, Is.EqualTo(0 * sizeof(float)));
        Assert.That(device["value1"].stateBlock.byteOffset, Is.EqualTo(1 * sizeof(float)));
        Assert.That(device["value2"].stateBlock.byteOffset, Is.EqualTo(2 * sizeof(float)));
        Assert.That(device["value3"].stateBlock.byteOffset, Is.EqualTo(3 * sizeof(float)));
        Assert.That(device["value4"].stateBlock.byteOffset, Is.EqualTo(4 * sizeof(float)));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddArrayOfControls_InJSON()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""value"",
                        ""layout"" : ""Axis"",
                        ""arraySize"" : 5
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputDevice.Build<InputDevice>("MyDevice");

        Assert.That(device.allControls, Has.Count.EqualTo(5));
        Assert.That(device["value0"], Is.TypeOf<AxisControl>());
        Assert.That(device["value1"], Is.TypeOf<AxisControl>());
        Assert.That(device["value2"], Is.TypeOf<AxisControl>());
        Assert.That(device["value3"], Is.TypeOf<AxisControl>());
        Assert.That(device["value4"], Is.TypeOf<AxisControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BooleanParameterDefaultsToTrueIfValueOmitted()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""parameters"" : ""normalize""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputDevice.Build<Gamepad>("MyDevice");

        Assert.That(device.leftStick.x.normalize, Is.True);
    }

    [InputControlLayout(commonUsages = new[] {"LeftHand", "RightHand"})]
    [Preserve]
    private class DeviceWithCommonUsages : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyCommonUsagesForDevices()
    {
        const string derivedJson = @"
            {
                ""name"" : ""DerivedDevice"",
                ""extend"" : ""BaseDevice"",
                ""commonUsages"" : [ ""LeftToe"" ]
            }
        ";

        InputSystem.RegisterLayout(typeof(DeviceWithCommonUsages), "BaseDevice");
        InputSystem.RegisterLayout(derivedJson);

        var layout = InputSystem.LoadLayout("DerivedDevice");

        Assert.That(layout.commonUsages, Has.Count.EqualTo(3));
        Assert.That(layout.commonUsages[0], Is.EqualTo(CommonUsages.LeftHand));
        Assert.That(layout.commonUsages[1], Is.EqualTo(CommonUsages.RightHand));
        Assert.That(layout.commonUsages[2], Is.EqualTo(new InternedString("LeftToe")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanFindLayoutFromDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""mything.*""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        var layout = InputSystem.TryFindMatchingLayout(new InputDeviceDescription
        {
            product = "MyThingy"
        });

        Assert.That(layout, Is.EqualTo("MyDevice"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanFindAllLayoutsBasedOnGivenLayout()
    {
        const string rootLayout = @"
            {
                ""name"" : ""RootDevice""
            }
        ";
        const string baseLayout = @"
            {
                ""name"" : ""BaseDevice"",
                ""extend"" : ""RootDevice""
            }
        ";
        const string derivedLayout = @"
            {
                ""name"" : ""DerivedDevice"",
                ""extend"" : ""BaseDevice""
            }
        ";

        InputSystem.RegisterLayout(rootLayout);
        InputSystem.RegisterLayout(baseLayout);
        InputSystem.RegisterLayout(derivedLayout);

        var layouts = InputSystem.ListLayoutsBasedOn("RootDevice");

        Assert.That(layouts.Count, Is.EqualTo(2));
        Assert.That(layouts, Has.Exactly(1).EqualTo("BaseDevice"));
        Assert.That(layouts, Has.Exactly(1).EqualTo("DerivedDevice"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanOverrideLayoutMatchesForDiscoveredDevices()
    {
        InputSystem.onFindLayoutForDevice +=
            (ref InputDeviceDescription description, string layoutMatch, InputDeviceExecuteCommandDelegate runtime) =>
                "Keyboard";

        var device = InputSystem.AddDevice(new InputDeviceDescription {deviceClass = "Gamepad"});

        Assert.That(device, Is.TypeOf<Keyboard>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAlterDeviceDescriptionsForDiscoveredDevices()
    {
        // Add a callback returning a layout name both before and after the callback that
        // alters the device description. This way we can make sure that no matter which order
        // the callbacks are processed in, the system should call our callback in the middle
        // and not stop at one of the callbacks returning a layout name.
        InputSystem.onFindLayoutForDevice +=
            (ref InputDeviceDescription description, string layoutMatch, InputDeviceExecuteCommandDelegate executeCommandDelegate) =>
                "Keyboard";

        InputSystem.onFindLayoutForDevice +=
            (ref InputDeviceDescription description, string layoutMatch, InputDeviceExecuteCommandDelegate executeCommandDelegate) =>
        {
            description.product = "Test";
            return null;
        };

        InputSystem.onFindLayoutForDevice +=
            (ref InputDeviceDescription description, string layoutMatch, InputDeviceExecuteCommandDelegate executeCommandDelegate) =>
                "Keyboard";

        var device = InputSystem.AddDevice(new InputDeviceDescription {deviceClass = "Gamepad", product = "Original"});

        Assert.That(device.description.product, Is.EqualTo("Test"));
    }

    // By the time someone adds a callback to onFindLayoutForDevice, we may already
    // have devices that have been reported that we didn't recognize but which would
    // now be recognizable. The system should use any new hook added to the callback
    // to find out if that is the case.
    [Test]
    [Category("Layouts")]
    public void Layouts_CanRetroactivelyFindLayoutForAlreadyReportedDevice()
    {
        var deviceDescription = new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            product = "TestProduct"
        };

        var deviceId = runtime.ReportNewInputDevice(deviceDescription);

        InputSystem.Update();

        Assert.That(InputSystem.GetUnsupportedDevices(), Is.EquivalentTo(new[] {deviceDescription}));
        Assert.That(InputSystem.devices, Is.Empty);

        InputSystem.onFindLayoutForDevice +=
            (ref InputDeviceDescription description, string layoutMatch, InputDeviceExecuteCommandDelegate executeCommandDelegate) =>
                "Keyboard";

        Assert.That(InputSystem.GetUnsupportedDevices(), Is.Empty);
        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Keyboard>().With.Property("deviceId").EqualTo(deviceId));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanRegisterMultipleMatchersForSingleLayout()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterLayout(json);

        InputSystem.RegisterLayoutMatcher("TestLayout",
            new InputDeviceMatcher()
                .WithManufacturer("Manufacturer")
                .WithProduct("ProductA"));
        InputSystem.RegisterLayoutMatcher("TestLayout",
            new InputDeviceMatcher()
                .WithManufacturer("Manufacturer")
                .WithProduct("ProductB"));

        var device1 = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                manufacturer = "Manufacturer",
                product = "ProductA"
            });
        var device2 = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                manufacturer = "Manufacturer",
                product = "ProductB"
            });

        Assert.That(device1, Is.TypeOf<Gamepad>());
        Assert.That(device2, Is.TypeOf<Gamepad>());
        Assert.That(device1.layout, Is.EqualTo("TestLayout"));
        Assert.That(device2.layout, Is.EqualTo("TestLayout"));
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_RegisteringMatcherForLayout_OverridesExistingMatchers()
    {
        const string jsonA = @"
            {
                ""name"" : ""LayoutA"",
                ""extend"" : ""Gamepad""
            }
        ";
        const string jsonB = @"
            {
                ""name"" : ""LayoutB"",
                ""extend"" : ""Mouse""
            }
        ";

        InputSystem.RegisterLayout(jsonA);
        InputSystem.RegisterLayout(jsonB);

        InputSystem.RegisterLayoutMatcher("LayoutA",
            new InputDeviceMatcher()
                .WithProduct("ProductA"));
        InputSystem.RegisterLayoutMatcher("LayoutB",
            new InputDeviceMatcher()
                .WithProduct("ProductA"));

        var device = InputSystem.AddDevice(
            new InputDeviceDescription
            {
                product = "ProductA"
            });

        Assert.That(device.layout, Is.EqualTo("LayoutB"));
        Assert.That(device, Is.TypeOf<Mouse>());

        // Make sure it's gone from the layout cache.
        #if UNITY_EDITOR
        var matchers = EditorInputControlLayoutCache.GetDeviceMatchers("LayoutA");
        Assert.That(matchers, Is.Empty);
        #endif
    }

    // At some point we may actually want to allow this. Could lead to some interesting capabilities.
    [Test]
    [Category("Layouts")]
    public void Layouts_CannotBeBasedOnMultipleLayouts()
    {
        const string json = @"
            {
                ""name"" : ""Test"",
                ""extendMultiple"" : [ ""Mouse"", ""Keyboard"" ],
                ""controls"" : [
                    { ""name"" : ""button"", ""layout"" : ""Button"" }
                ]
            }
        ";

        Assert.That(() => InputSystem.RegisterLayout(json),
            Throws.Exception.TypeOf<NotSupportedException>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanLoadLayoutOverride()
    {
        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayoutOverride(json);

        var layout = InputSystem.LoadLayout("Overrides");

        Assert.That(layout, Is.Not.Null);
        Assert.That(layout.isOverride, Is.True);
        Assert.That(layout.controls, Has.Count.EqualTo(1)); // Should not have merged base layout.
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanApplyOverridesToExistingLayouts()
    {
        // Add a control to mice.
        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extend"" : ""Mouse"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayoutOverride(json);

        var device = InputSystem.AddDevice<Mouse>();

        Assert.That(device["extraControl"], Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanApplyOverridesToMultipleLayouts()
    {
        // Add a control to mice.
        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extendMultiple"" : [ ""Mouse"", ""Keyboard"" ],
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayoutOverride(json);

        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();

        Assert.That(mouse["extraControl"], Is.TypeOf<ButtonControl>());
        Assert.That(keyboard["extraControl"], Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanApplyOverridesToControlLayouts()
    {
        // Add a button to the Stick layout.
        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extend"" : ""Stick"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.AddDevice<Gamepad>();
        InputSystem.RegisterLayoutOverride(json);

        Assert.That(Gamepad.current.leftStick["extraControl"], Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanApplyOverridesToOverrides()
    {
        const string json1 = @"
            {
                ""name"" : ""Overrides1"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";
        const string json2 = @"
            {
                ""name"" : ""Overrides2"",
                ""extend"" : ""Overrides1"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Axis"" }
                ]
            }
        ";

        InputSystem.RegisterLayoutOverride(json1);
        InputSystem.RegisterLayoutOverride(json2);

        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(device["extraControl"].layout, Is.EqualTo("Axis"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanOverrideCommonUsagesOnExistingLayout()
    {
        // Change all Gamepads to have the common usages "A", "B", and "C".
        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extend"" : ""Gamepad"",
                ""commonUsages"" : [ ""A"", ""B"", ""C"" ]
            }
        ";

        InputSystem.RegisterLayoutOverride(json);

        var layout = InputSystem.LoadLayout("Gamepad");

        Assert.That(layout.appliedOverrides, Is.EquivalentTo(new[] {new InternedString("Overrides")}));
        Assert.That(layout.commonUsages.Count, Is.EqualTo(3));
        Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("A")));
        Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("B")));
        Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("C")));

        // Applying the override should not have created a cycle.
        Assert.That(layout.baseLayouts, Does.Not.Contains(new InternedString("Gamepad")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ApplyingOverrideToExistingLayout_UpdatesAllDevicesUsingTheLayout()
    {
        InputSystem.AddDevice<Mouse>();

        const string json = @"
            {
                ""name"" : ""Overrides"",
                ""extend"" : ""Mouse"",
                ""controls"" : [
                    { ""name"" : ""extraControl"", ""layout"" : ""Button"" }
                ]
            }
        ";
        InputSystem.RegisterLayoutOverride(json);

        Assert.That(Mouse.current["extraControl"], Is.TypeOf<ButtonControl>());
    }

    ////REVIEW: should this just be an open-ended tagging ability?
    // We want to have the ability to filter layouts based on platform so that the user
    // can narrow focus on just what's interesting to the current project.
    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_CanSpecifyPlatformsThatLayoutAppliesTo()
    {
        Assert.Fail();
    }

    // If a layout only specifies an interface in its descriptor, it is considered
    // a fallback for when there is no more specific layout that is able to match
    // by product.
    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_CanHaveLayoutFallbackForInterface()
    {
        const string fallbackJson = @"
            {
                ""name"" : ""FallbackLayout"",
                ""device"" : {
                    ""interface"" : ""MyInterface""
                }
            }
        ";
        const string productJson = @"
            {
                ""name"" : ""ProductLayout"",
                ""device"" : {
                    ""interface"" : ""MyInterface"",
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterLayout(fallbackJson);
        InputSystem.RegisterLayout(productJson);

        Assert.Fail();
    }

    ////REVIEW: if this behavior is guaranteed, we also have to make sure we preserve it across domain reloads
    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_WhenTwoLayoutsConflict_LastOneRegisteredWins()
    {
        const string firstLayout = @"
            {
                ""name"" : ""FirstLayout"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";
        const string secondLayout = @"
            {
                ""name"" : ""SecondLayout"",
                ""device"" : {
                    ""product"" : ""MyProduct""
                }
            }
        ";

        InputSystem.RegisterLayout(firstLayout);
        InputSystem.RegisterLayout(secondLayout);

        var layout = InputSystem.TryFindMatchingLayout(new InputDeviceDescription {product = "MyProduct"});

        Assert.That(layout, Is.EqualTo("SecondLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_AddingTwoControlsWithSameName_WillCauseException()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""MyControl"",
                        ""layout"" : ""Button""
                    },
                    {
                        ""name"" : ""MyControl"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        // We do minimal processing when adding a layout so verification
        // only happens when we actually try to instantiate the layout.
        InputSystem.RegisterLayout(json);

        Assert.That(() => InputSystem.AddDevice("MyDevice"),
            Throws.TypeOf<InvalidOperationException>().With.Property("Message").Contain("Duplicate control"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingDeviceLayoutAffectsAllDevicesUsingLayout()
    {
        // Create a device hiearchy and then replace the base layout. We can't easily use
        // the gamepad (or something similar) as a base layout as it will use the Gamepad
        // class which will expect a number of controls to be present on the device.
        const string baseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"" }
                ]
            }
        ";
        const string derivedDeviceJson = @"
            {
                ""name"" : ""MyDerived"",
                ""extend"" : ""MyBase""
            }
        ";
        const string newBaseDeviceJson = @"
            {
                ""name"" : ""MyBase"",
                ""controls"" : [
                    { ""name"" : ""yeah"", ""layout"" : ""Stick"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(derivedDeviceJson);
        InputSystem.RegisterLayout(baseDeviceJson);

        InputSystem.AddDevice("MyDerived");

        InputSystem.RegisterLayout(newBaseDeviceJson);

        var device = InputSystem.devices[0];
        Assert.That(device.children, Has.Count.EqualTo(1));
        Assert.That(device.children,
            Has.Exactly(1).With.Property("name").EqualTo("yeah").And.Property("layout").EqualTo("Stick"));
    }

    ////REVIEW: reusing the same device ID has implications; in some situations it is desirable but in others
    ////        (e.g. when the layout change actually alters the format), it is dangerous and can lead to
    ////        memory corruption
    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingDeviceLayoutWithLayoutUsingDifferentType_PreservesDeviceIdAndDescription()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : { ""product"" : ""Test"" }
            }
        ";

        InputSystem.RegisterLayout(initialJson);

        runtime.ReportNewInputDevice(new InputDeviceDescription {product = "Test"}.ToJson());
        InputSystem.Update();

        var oldDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        var oldDeviceId = oldDevice.deviceId;
        var oldDeviceDescription = oldDevice.description;

        const string newJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Keyboard""
            }
        ";

        InputSystem.RegisterLayout(newJson);
        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("layout").EqualTo("MyDevice"));

        var newDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        Assert.That(newDevice.deviceId, Is.EqualTo(oldDeviceId));
        Assert.That(newDevice.description, Is.EqualTo(oldDeviceDescription));
    }

    [Preserve]
    private class MyButtonControl : ButtonControl
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingControlLayoutAffectsAllDevicesUsingLayout()
    {
        InputSystem.AddDevice<Gamepad>();

        // Replace "Button" layout.
        InputSystem.RegisterLayout<MyButtonControl>("Button");

        Assert.That(Gamepad.current.leftTrigger, Is.TypeOf<MyButtonControl>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayout_WithMatcher_RecreatesDevicesForWhichItIsABetterMatch()
    {
        const string oldLayout = @"
            {
                ""name"" : ""OldLayout"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""manufacturer"" : ""TestManufacturer""
                }
            }
        ";

        const string newLayout = @"
            {
                ""name"" : ""NewLayout"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""TestProduct"",
                    ""manufacturer"" : ""TestManufacturer""
                }
            }
        ";

        InputSystem.RegisterLayout(oldLayout);

        InputSystem.AddDevice<Mouse>(); // Noise.

        InputSystem.AddDevice(new InputDeviceDescription
        {
            product = "TestProduct",
            manufacturer = "TestManufacturer",
        });

        InputSystem.AddDevice<Mouse>(); // Noise.

        var device = InputSystem.devices.First(x => x.description.product == "TestProduct");
        Assert.That(device.layout, Is.EqualTo("OldLayout"));

        InputSystem.RegisterLayout(newLayout);

        device = InputSystem.devices.First(x => x.description.product == "TestProduct");
        Assert.That(device.layout, Is.EqualTo("NewLayout"));
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_RegisteringLayoutBuilder_MarksResultingLayoutAsGenerated()
    {
        Assert.Fail();
    }

    [Preserve]
    private class TestLayoutType : Pointer
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_UsesBaseTypeAsBaseLayout()
    {
        InputSystem.RegisterLayout<TestLayoutType>();

        var layout = InputSystem.LoadLayout("TestLayoutType");

        Assert.That(layout.baseLayouts, Is.EquivalentTo(new[] {new InternedString("Pointer")}));
    }

    [Preserve]
    class DeviceWithControlProperties : InputDevice
    {
        public ButtonControl propertyWithoutAttribute { get; set; }
        [Preserve]
        [InputControl]
        public ButtonControl propertyWithAttribute { get; set; }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_OnlyAddsPropertiesWithExplicitAttribute()
    {
        var device = InputSystem.AddDevice<DeviceWithControlProperties>();

        Assert.That(device.TryGetChildControl("propertyWithoutAttribute"), Is.Null);
        Assert.That(device.TryGetChildControl("propertyWithAttribute"), Is.Not.Null);
        Assert.That(device.TryGetChildControl("propertyWithAttribute"), Is.TypeOf<ButtonControl>());
    }

    // We consider layouts built by layout builders as being auto-generated. We want them to
    // be overridable by layouts built specifically for a device so we boost the score of
    // of type and JSON layouts such that they will override auto-generated layouts even if
    // they match less perfectly according to their InputDeviceMatcher.
    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutBuilder_WithMatcher_StillGivesPrecedenceToTypeAndJSONLayouts()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Mouse"};

        InputSystem.RegisterLayoutBuilder(() => builder.DoIt(), name: "GeneratedLayout",
            matches: new InputDeviceMatcher()
                .WithInterface("TestInterface")
                .WithProduct("TestProduct")
                .WithManufacturer("TestManufacturer"));

        const string json = @"
            {
                ""name"" : ""ManualLayout"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""TestProduct"",
                    ""manufacturer"" : ""TestManufacturer""
                }
            }
        ";

        InputSystem.RegisterLayout(json);

        // This should pick ManualLayout and not GeneratedLayout.
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            product = "TestProduct",
            manufacturer = "TestManufacturer",
        });

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.layout, Is.EqualTo("ManualLayout"));
    }

    // Want to ensure that if a state struct declares an "int" field, for example, and then
    // assigns it then Axis layout (which has a default format of float), the AxisControl
    // comes out with an "INT" format and not a "FLT" format.
    private struct StateStructWithPrimitiveFields : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public byte byteAxis;
        [InputControl(layout = "Axis")] public short shortAxis;
        [InputControl(layout = "Axis")] public int intAxis;
        // No float as that is the default format for Axis anyway.
        [InputControl(layout = "Axis")] public double doubleAxis;

        public FourCC format
        {
            get { return new FourCC('T', 'E', 'S', 'T'); }
        }
    }

    [InputControlLayout(stateType = typeof(StateStructWithPrimitiveFields))]
    [Preserve]
    private class DeviceWithStateStructWithPrimitiveFields : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithPrimitiveTypeInStateStructInferredFromType()
    {
        InputSystem.RegisterLayout<DeviceWithStateStructWithPrimitiveFields>("Test");
        var device = InputDevice.Build<InputDevice>("Test");

        Assert.That(device["byteAxis"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatByte));
        Assert.That(device["shortAxis"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatShort));
        Assert.That(device["intAxis"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(device["doubleAxis"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatDouble));
    }

    private unsafe struct StateWithFixedArray : IInputStateTypeInfo
    {
        [InputControl] public fixed float buffer[2];

        public FourCC format => new FourCC('T', 'E', 'S', 'T');
    }

    [InputControlLayout(stateType = typeof(StateWithFixedArray))]
    [Preserve]
    private class DeviceWithStateStructWithFixedArray : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithFixedArrayType_IsNotInferredFromType()
    {
        InputSystem.RegisterLayout<DeviceWithStateStructWithFixedArray>();

        Assert.That(() => InputDevice.Build<InputDevice>("DeviceWithStateStructWithFixedArray"),
            Throws.Exception.With.Message.Contain("Layout has not been set"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanHaveOneControlUseStateOfAnotherControl()
    {
        // It's useful to be able to say that control X should simply use the same state as control
        // Y. An example of this is the up/down/left/right controls of sticks that simply want to reuse
        // state from the x and y controls already on the stick. "useStateFrom" not only ensures that
        // if the state is moved around we move with it, it allows to redirect entire controls from
        // one part of the hierarchy to another part of the hierarchy (Touchscreen does that to point
        // the controls expected by the base Pointer class to the controls inside of "touch0").
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""test"", ""layout"" : ""Axis"", ""useStateFrom"" : ""leftStick/x"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputDevice.Build<Gamepad>("MyDevice");
        var testControl = device.GetChildControl<AxisControl>("test");

        Assert.That(device.stateBlock.alignedSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>()));
        Assert.That(testControl.stateBlock.byteOffset, Is.EqualTo(device.leftStick.x.stateBlock.byteOffset));
        Assert.That(testControl.stateBlock.sizeInBits, Is.EqualTo(device.leftStick.x.stateBlock.sizeInBits));
        Assert.That(testControl.stateBlock.format, Is.EqualTo(device.leftStick.x.stateBlock.format));
        Assert.That(testControl.stateBlock.bitOffset, Is.EqualTo(device.leftStick.x.stateBlock.bitOffset));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanHaveOneControlUseStateOfAnotherControl_EvenWhenStateSetupIsChangedInDerivedLayout()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2"",
                ""controls"" : [
                    { ""name"" : ""x"", ""layout"" : ""Axis"" },
                    { ""name"" : ""y"", ""layout"" : ""Axis"", ""useStateFrom"" : ""x"" }
                ]
            }
        ";

        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Sensor"",
                ""controls"" : [
                    { ""name"" : ""ctrl"", ""layout"" : ""MyControl"" },
                    { ""name"" : ""ctrl/x"", ""format"" : ""BIT"", ""bit"" : 12, ""offset"" : 21, ""sizeInBits"" : 7 }
                ]
            }
        ";

        InputSystem.RegisterLayout(controlJson);
        InputSystem.RegisterLayout(deviceJson);

        var device = InputDevice.Build<InputDevice>("MyDevice");

        Assert.That(device["ctrl/y"].stateBlock.format, Is.EqualTo(new FourCC("BIT")));
        Assert.That(device["ctrl/y"].stateBlock.sizeInBits, Is.EqualTo(7));
        Assert.That(device["ctrl/y"].stateBlock.byteOffset, Is.EqualTo(21));
        Assert.That(device["ctrl/y"].stateBlock.bitOffset, Is.EqualTo(12));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddChildControlToExistingControl()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick/enabled"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputDevice.Build<Gamepad>("TestLayout");

        ////TODO: this ignores layouting; ATM there's a conflict between the automatic layout used by the added button
        ////      and the manual layouting employed by Gamepad; we don't detect conflicts between manual and automatic
        ////      layouts yet so this goes undiagnosed

        Assert.That(device.leftStick.children, Has.Exactly(1).With.Property("name").EqualTo("enabled"));
        Assert.That(device.leftStick.children.Count, Is.EqualTo(device.rightStick.children.Count + 1));
        Assert.That(device.leftStick["enabled"].layout, Is.EqualTo("Button"));
        Assert.That(device.leftStick["enabled"].parent, Is.SameAs(device.leftStick));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddChildControlToExistingControl_UsingStateFromOtherControl()
    {
        InputSystem.RegisterLayout(@"
            {
                ""name"" : ""TestDevice"",
                ""controls"" : [
                    { ""name"" : ""noise"", ""layout"" : ""Button"" },
                    { ""name"" : ""scroll"", ""layout"" : ""Vector2"" },
                    { ""name"" : ""scroll/up"", ""layout"" : ""Button"", ""useStateFrom"" : ""scroll/y"", ""parameters"" : ""clamp=1,clampMin=0,clampMax=99999"" },
                    { ""name"" : ""scroll/down"", ""layout"" : ""Button"", ""useStateFrom"" : ""scroll/y"", ""parameters"" : ""clamp=1,clampMin=-99999,clampMax=0,invert"" },
                    { ""name"" : ""scroll/left"", ""layout"" : ""Button"", ""useStateFrom"" : ""scroll/x"", ""parameters"" : ""clamp=1,clampMin=-99999,clampMax=0,invert"" },
                    { ""name"" : ""scroll/right"", ""layout"" : ""Button"", ""useStateFrom"" : ""scroll/x"", ""parameters"" : ""clamp=1,clampMin=0,clampMax=99999"" }
                ]
            }
        ");

        var device = InputSystem.AddDevice("TestDevice");
        var scroll = (Vector2Control)device["scroll"];

        // Left.
        Set(scroll, new Vector2(-123, 0));

        Assert.That(device["scroll/up"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/down"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/left"].ReadValueAsObject(), Is.EqualTo(123).Within(0.00001));
        Assert.That(device["scroll/right"].ReadValueAsObject(), Is.Zero);

        // Right.
        Set(scroll, new Vector2(234, 0));

        Assert.That(device["scroll/up"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/down"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/left"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/right"].ReadValueAsObject(), Is.EqualTo(234).Within(0.00001));

        // Up.
        Set(scroll, new Vector2(0, 123));

        Assert.That(device["scroll/up"].ReadValueAsObject(), Is.EqualTo(123).Within(0.00001));
        Assert.That(device["scroll/down"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/left"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/right"].ReadValueAsObject(), Is.Zero);

        // Down.
        Set(scroll, new Vector2(0, -432));

        Assert.That(device["scroll/up"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/down"].ReadValueAsObject(), Is.EqualTo(432).Within(0.00001));
        Assert.That(device["scroll/left"].ReadValueAsObject(), Is.Zero);
        Assert.That(device["scroll/right"].ReadValueAsObject(), Is.Zero);
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_WhenModifyingChildControlsByPath_DependentControlsUsingStateFromAreUpdatedAsWell()
    {
        const string baseJson = @"
            {
                ""name"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick"", ""layout"" : ""Stick"" }
                ]
            }
        ";
        // When modifying the state layout of the X and Y controls of a stick from outside,
        // we also expect the up/down/left/right controls to get updated with the new state
        // layout.
        const string derivedJson = @"
            {
                ""name"" : ""Derived"",
                ""extend"" : ""Base"",
                ""controls"" : [
                    { ""name"" : ""stick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""stick/y"", ""format"" : ""SHRT"", ""offset"" : 2 }
                ]
            }
        ";

        InputSystem.RegisterLayout(baseJson);
        InputSystem.RegisterLayout(derivedJson);

        var device = InputDevice.Build<InputDevice>("Derived");

        Assert.That(device["stick"].stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
    }

    ////TODO: write combined test that applies all possible modifications to a child control
    ////      (also add - and + capability)

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyLayoutOfChildControlUsingPath()
    {
        const string json = @"
        {
            ""name"" : ""MyGamepad"",
            ""extend"" : ""Gamepad"",
            ""controls"" : [
                { ""name"" : ""dpad/up"", ""layout"" : ""DiscreteButton"" }
            ]
        }";

        InputSystem.RegisterLayout(json);

        var gamepad = InputDevice.Build<Gamepad>("MyGamepad");

        Assert.That(gamepad.dpad.up.layout, Is.EqualTo("DiscreteButton"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyUsagesOfChildControlUsingPath()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""stick"", ""layout"" : ""Stick"" },
                    { ""name"" : ""stick/x"", ""usage"" : ""TestUsage"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("MyDevice");

        Assert.That(device["stick/x"].usages, Has.Exactly(1).EqualTo(new InternedString("TestUsage")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyLongAndShortDisplayNameForControl()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad"",
                ""displayName"" : ""Test Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""displayName"" : ""Primary Stick"",
                        ""shortDisplayName"" : ""PS""
                    },
                    {
                        ""name"" : ""leftStick/x"",
                        ""displayName"" : ""Horizontal""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputDevice.Build<Gamepad>("MyLayout");

        Assert.That(device.displayName, Is.EqualTo("Test Gamepad"));
        Assert.That(device.leftStick.displayName, Is.EqualTo("Primary Stick"));
        Assert.That(device.leftStick.x.displayName, Is.EqualTo("Primary Stick Horizontal"));
        Assert.That(device.leftStick.shortDisplayName, Is.EqualTo("PS"));
        Assert.That(device.leftStick.x.shortDisplayName, Is.EqualTo("PS Horizontal"));
    }

    [Preserve]
    class TestDeviceWithMinMaxValue : InputDevice
    {
        [Preserve]
        [InputControl(minValue = 0.1234f, maxValue = 0.5432f)]
        public AxisControl control { get; set; }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyMinAndMaxValuesForControlInOnAttribute()
    {
        InputSystem.RegisterLayout<TestDeviceWithMinMaxValue>();

        var layout = InputSystem.LoadLayout("TestDeviceWithMinMaxValue");

        Assert.That(layout["control"].minValue.isEmpty, Is.False);
        Assert.That(layout["control"].maxValue.isEmpty, Is.False);
        Assert.That(layout["control"].minValue.ToSingle(), Is.EqualTo(0.1234f));
        Assert.That(layout["control"].maxValue.ToSingle(), Is.EqualTo(0.5432f));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyMinAndMaxValuesForControlInJson()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""control"",
                        ""layout"" : ""Axis"",
                        ""minValue"" : ""-123"",
                        ""maxValue"" : ""123""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var layout = InputSystem.LoadLayout("TestLayout");

        Assert.That(layout["control"].minValue.isEmpty, Is.False);
        Assert.That(layout["control"].maxValue.isEmpty, Is.False);
        Assert.That(layout["control"].minValue.ToInt32(), Is.EqualTo(-123));
        Assert.That(layout["control"].maxValue.ToInt32(), Is.EqualTo(123));
    }

    [Preserve]
    class BaseClassWithControl : InputDevice
    {
        [Preserve]
        [InputControl]
        public AxisControl controlFromBase { get; set; }
    }

    [Preserve]
    class DerivedClassModifyingControlFromBaseClass : BaseClassWithControl
    {
        // One kink is that InputControlAttribute can only go on fields and properties
        // so we have to put it on some unrelated control.
        [Preserve]
        [InputControl(name = "controlFromBase", format = "SHRT")]
        public ButtonControl controlFromDerived { get; set; }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanModifyControlDefinedInBaseClass()
    {
        InputSystem.RegisterLayout<BaseClassWithControl>();
        InputSystem.RegisterLayout<DerivedClassModifyingControlFromBaseClass>();

        var baseLayout = InputSystem.LoadLayout<BaseClassWithControl>();
        var derivedLayout = InputSystem.LoadLayout<DerivedClassModifyingControlFromBaseClass>();

        Assert.That(baseLayout["controlFromBase"].format, Is.EqualTo(new FourCC())); // Unset in base.
        Assert.That(derivedLayout["controlFromBase"].format, Is.EqualTo(InputStateBlock.FormatShort));

        // This is probably somewhat counterintuitive but if there's InputControlAttributes on a property or field,
        // there won't be a control generated automatically from the field or property.
        Assert.That(() => derivedLayout["controlFromDerived"], Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanMarkControlAsNoisy()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button"",
                        ""noisy"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(device["button"].noisy, Is.True);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_NoisyControls_AutomaticallyMakeAllTheirChildrenNoisy()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""layout"" : ""Stick"",
                        ""noisy"" : true
                    },
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device["stick"].As<StickControl>().x.noisy, Is.True);
        Assert.That(device["stick"].As<StickControl>().y.noisy, Is.True);
        Assert.That(device["stick"].As<StickControl>().left.noisy, Is.True);
        Assert.That(device["stick"].As<StickControl>().right.noisy, Is.True);
        Assert.That(device["stick"].As<StickControl>().up.noisy, Is.True);
        Assert.That(device["stick"].As<StickControl>().down.noisy, Is.True);

        // Make sure it didn't spill over.
        Assert.That(device["button"].noisy, Is.False);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanMarkControlAsSynthetic()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button"",
                        ""synthetic"" : true
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(device["button"].synthetic, Is.True);
    }

    [Preserve]
    class DeviceWithAutoOffsetControl : InputDevice
    {
        [Preserve]
        [InputControl(offset = 4, sizeInBits = 32)]
        public ButtonControl button1;

        [Preserve]
        [InputControl(offset = InputStateBlock.AutomaticOffset)]
        public ButtonControl button2 { get; set; }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanPlaceControlsAutomatically()
    {
        var device = InputSystem.AddDevice<DeviceWithAutoOffsetControl>();

        Assert.That(device["button2"].stateBlock.byteOffset, Is.EqualTo(8));
    }

    [Preserve]
    private class BaseDeviceFixedFixedOffsetControl : InputDevice
    {
        [Preserve]
        [InputControl(offset = 4, format = "FLT")]
        public ButtonControl control;

        [Preserve]
        [InputControl(offset = 8)]
        public AxisControl otherControl;
    }

    [Preserve]
    private class DerivedDeviceWithAutomaticOffsetControl : BaseDeviceFixedFixedOffsetControl
    {
        [Preserve]
        [InputControl(offset = InputStateBlock.AutomaticOffset)]
        public new ButtonControl control;
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanPlaceControlsAutomatically_EvenIfControlIsInheritedWithFixedOffset()
    {
        InputSystem.RegisterLayout<BaseDeviceFixedFixedOffsetControl>();
        InputSystem.RegisterLayout<DerivedDeviceWithAutomaticOffsetControl>();

        var device = InputSystem.AddDevice<DerivedDeviceWithAutomaticOffsetControl>();

        // Should have gotten placed *after* `otherControl`.
        Assert.That(device.stateBlock.alignedSizeInBytes, Is.EqualTo(16));
        Assert.That(device["control"].stateBlock.format, Is.EqualTo(new FourCC("FLT")));
        Assert.That(device["control"].stateBlock.byteOffset, Is.EqualTo(12));
        Assert.That(device["control"].stateBlock.sizeInBits, Is.EqualTo(32));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanPlaceBitfieldControlsAutomatically()
    {
        const string layout = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"" },
                    { ""name"" : ""fixed"", ""layout"" : ""Axis"", ""offset"" : 4 },
                    { ""name"" : ""third"", ""layout"" : ""Button"" },
                    { ""name"" : ""fourth"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(layout);
        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device["fixed"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(device["fixed"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(device["fixed"].stateBlock.sizeInBits, Is.EqualTo(32));

        // Automatically placed controls are slotted in after the last fixed offset control.

        Assert.That(device["first"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["first"].stateBlock.bitOffset, Is.Zero);
        Assert.That(device["first"].stateBlock.sizeInBits, Is.EqualTo(1));

        Assert.That(device["second"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["second"].stateBlock.bitOffset, Is.EqualTo(1));
        Assert.That(device["second"].stateBlock.sizeInBits, Is.EqualTo(1));

        Assert.That(device["third"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["third"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(device["third"].stateBlock.sizeInBits, Is.EqualTo(1));

        Assert.That(device["fourth"].stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device["fourth"].stateBlock.bitOffset, Is.EqualTo(3));
        Assert.That(device["fourth"].stateBlock.sizeInBits, Is.EqualTo(1));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanBuildLayoutsInCode()
    {
        var builder = new InputControlLayout.Builder()
            .WithName("MyLayout")
            .WithType<Gamepad>()
            .Extend("Pointer")
            .WithFormat("CUST");

        builder.AddControl("button")
            .WithLayout("Button")
            .WithUsages("Foo", "Bar");

        var layout = builder.Build();

        Assert.That(layout.name.ToString(), Is.EqualTo("MyLayout"));
        Assert.That(layout.type, Is.SameAs(typeof(Gamepad)));
        Assert.That(layout.stateFormat, Is.EqualTo(new FourCC("CUST")));
        Assert.That(layout.baseLayouts, Is.EquivalentTo(new[] {new InternedString("Pointer")}));
        Assert.That(layout.controls, Has.Count.EqualTo(1));
        Assert.That(layout.controls[0].name.ToString(), Is.EqualTo("button"));
        Assert.That(layout.controls[0].layout.ToString(), Is.EqualTo("Button"));
        Assert.That(layout.controls[0].usages.Count, Is.EqualTo(2));
        Assert.That(layout.controls[0].usages[0].ToString(), Is.EqualTo("Foo"));
        Assert.That(layout.controls[0].usages[1].ToString(), Is.EqualTo("Bar"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BuildingLayoutInCode_WithEmptyUsageString_Throws()
    {
        var builder = new InputControlLayout.Builder().WithName("TestLayout");

        Assert.That(() => builder.AddControl("TestControl").WithUsages(""),
            Throws.ArgumentException.With.Message.Contains("TestControl")
                .And.With.Message.Contains("TestLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_BuildingLayoutInCode_WithoutType_OnlySetsTypeIfNotExtendingLayout()
    {
        var builderExtendingLayout = new InputControlLayout.Builder()
            .WithName("TestLayout")
            .Extend("Pointer");
        var builderNotExtendingLayout = new InputControlLayout.Builder()
            .WithName("TestLayout");

        builderExtendingLayout.AddControl("button").WithLayout("Button");
        builderNotExtendingLayout.AddControl("button").WithLayout("Button");

        var layout1 = builderExtendingLayout.Build();
        var layout2 = builderNotExtendingLayout.Build();

        Assert.That(layout1.type, Is.Null);
        Assert.That(layout2.type, Is.SameAs(typeof(InputDevice)));
    }

    [Serializable]
    private class TestLayoutBuilder
    {
        [SerializeField] public string layoutToLoad;
        [NonSerialized] public InputControlLayout layout;

        public InputControlLayout DoIt()
        {
            // To make this as simple as possible, just load another layout.
            layout = InputSystem.LoadLayout(layoutToLoad);
            return layout;
        }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddCustomLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};

        InputSystem.RegisterLayoutBuilder(() => builder.DoIt(), "MyLayout");

        var result = InputSystem.LoadLayout("MyLayout");

        Assert.That(result.name.ToString(), Is.EqualTo("MyLayout"));
        Assert.That(result, Is.SameAs(builder.layout));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanTurnLayoutIntoJson()
    {
        var layout = InputSystem.LoadLayout("Gamepad");
        var json = layout.ToJson();
        var deserializedLayout = InputControlLayout.FromJson(json);

        Assert.That(deserializedLayout.name, Is.EqualTo(layout.name));
        Assert.That(deserializedLayout.controls, Has.Count.EqualTo(layout.controls.Count));
        Assert.That(deserializedLayout.stateFormat, Is.EqualTo(layout.stateFormat));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanGetControlLayoutFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        // Control layout mentioned explicitly.
        Assert.That(InputControlPath.TryGetControlLayout("*/<button>"),
            Is.EqualTo("button")); // Does not "correct" casing.
        // Control layout can be looked up from device layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/leftStick"), Is.EqualTo("Stick"));
        // With multiple controls, only returns result if all controls use the same layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/*Stick"), Is.EqualTo("Stick"));
        // Except if we match all controls on the device in which case it's taken to mean "any layout goes".
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>/*"), Is.EqualTo("*"));
        ////TODO
        // However, having a wildcard on the device path is taken to mean "all device layouts" in this case.
        //Assert.That(InputControlPath.TryGetControlLayout("/*/*Stick"), Is.EqualTo("Stick"));
        // Can determine layout used by child control.
        Assert.That(InputControlPath.TryGetControlLayout("<gamepad>/leftStick/x"), Is.EqualTo("Axis"));
        // Can determine layout from control with usage.
        Assert.That(InputControlPath.TryGetControlLayout("<gamepad>/{PrimaryAction}"), Is.EqualTo("Button"));
        // Will not look up from instanced devices at runtime so can't know device layout from this path.
        Assert.That(InputControlPath.TryGetControlLayout("/gamepad/leftStick"), Is.Null);
        // If only a device layout is given, can't know control layout.
        Assert.That(InputControlPath.TryGetControlLayout("/<gamepad>"), Is.Null);

        // Try it with controls that are modifying children by path.
        Assert.That(InputControlPath.TryGetControlLayout("<Mouse>/scroll/x"), Is.EqualTo("Axis"));
        Assert.That(InputControlPath.TryGetControlLayout("<Touchscreen>/primaryTouch/tap"), Is.EqualTo("Button"));

        ////TODO: make sure that finding by usage can look arbitrarily deep into the hierarchy
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanGetDeviceLayoutFromControlPath()
    {
        InputSystem.AddDevice("gamepad"); // Just to make sure we don't use this.

        Assert.That(InputControlPath.TryGetDeviceLayout("<gamepad>/leftStick"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/<gamepad>"), Is.EqualTo("gamepad"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/*/*Stick"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/*"), Is.EqualTo("*"));
        Assert.That(InputControlPath.TryGetDeviceLayout("/gamepad/leftStick"), Is.Null);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanGetNameOfBaseLayout()
    {
        Assert.That(InputSystem.GetNameOfBaseLayout("DualShockGamepad"), Is.EqualTo("Gamepad"));
        Assert.That(InputSystem.GetNameOfBaseLayout("Mouse"), Is.EqualTo("Pointer"));
        Assert.That(InputSystem.GetNameOfBaseLayout("Pointer"), Is.EqualTo(null));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanDetermineIfLayoutIsBasedOnGivenLayout()
    {
        Assert.That(InputSystem.IsFirstLayoutBasedOnSecond("DualShockGamepad", "Gamepad"), Is.True);
        Assert.That(InputSystem.IsFirstLayoutBasedOnSecond("DualShock4GamepadHID", "Gamepad"), Is.True);
        Assert.That(InputSystem.IsFirstLayoutBasedOnSecond("Gamepad", "Gamepad"), Is.True);
        Assert.That(InputSystem.IsFirstLayoutBasedOnSecond("Gamepad", "Pointer"), Is.False);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanLoadLayout()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [ { ""name"" : ""MyControl"" } ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var jsonLayout = InputSystem.LoadLayout("MyLayout");

        Assert.That(jsonLayout, Is.Not.Null);
        Assert.That(jsonLayout.name, Is.EqualTo(new InternedString("MyLayout")));
        Assert.That(jsonLayout.controls, Has.Count.EqualTo(1));
        Assert.That(jsonLayout.controls[0].name, Is.EqualTo(new InternedString("MyControl")));

        var gamepadLayout = InputSystem.LoadLayout("Gamepad");

        Assert.That(gamepadLayout, Is.Not.Null);
        Assert.That(gamepadLayout.name, Is.EqualTo(new InternedString("Gamepad")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanRemoveLayout()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(InputSystem.ListLayouts(), Has.Exactly(1).EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(device));

        InputSystem.RemoveLayout("MyLayout");

        Assert.That(InputSystem.ListLayouts(), Has.None.EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.None.SameAs(device));
        Assert.That(InputSystem.devices, Has.None.With.Property("layout").EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ChangingLayouts_SendsNotifications()
    {
        InputControlLayoutChange? receivedChange = null;
        string receivedLayout = null;

        InputSystem.onLayoutChange +=
            (layout, change) =>
        {
            receivedChange = change;
            receivedLayout = layout;
        };

        const string jsonV1 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add layout.
        InputSystem.RegisterLayout(jsonV1);

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Added));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Keyboard""
            }
        ";

        receivedChange = null;
        receivedLayout = null;

        // Change layout.
        InputSystem.RegisterLayout(jsonV2);

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Replaced));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));

        receivedChange = null;
        receivedLayout = null;

        // RemoveLayout.
        InputSystem.RemoveLayout("MyLayout");

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Removed));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_RemovingLayouts_RemovesAllLayoutsBasedOnIt()
    {
        Assert.Fail();
    }

    private struct StateWithTwoLayoutVariants : IInputStateTypeInfo
    {
        [InputControl(name = "button", layout = "Button", variants = "A")]
        public int buttons;

        [InputControl(name = "axis", layout = "Axis", variants = "B")]
        public float axis;

        public FourCC format
        {
            get { return new FourCC('T', 'E', 'S', 'T'); }
        }
    }

    [InputControlLayout(variants = "A", stateType = typeof(StateWithTwoLayoutVariants))]
    [Preserve]
    private class DeviceWithLayoutVariantA : InputDevice
    {
    }

    [InputControlLayout(variants = "B", stateType = typeof(StateWithTwoLayoutVariants))]
    [Preserve]
    private class DeviceWithLayoutVariantB : InputDevice
    {
    }

    // Sometimes you have a single state format that you want to use with multiple
    // different types of devices that each have a different control setup. For example,
    // a given state may just be a generic set of axis and button values with the
    // assignment of axis and button controls depending on which type of device the
    // state is used with.
    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUpMultipleLayoutsFromSingleState_UsingVariants()
    {
        InputSystem.RegisterLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterLayout<DeviceWithLayoutVariantB>();

        var deviceA = InputSystem.AddDevice<DeviceWithLayoutVariantA>();
        var deviceB = InputSystem.AddDevice<DeviceWithLayoutVariantB>();

        Assert.That(deviceA.allControls.Count, Is.EqualTo(1));
        Assert.That(deviceB.allControls.Count, Is.EqualTo(1));

        Assert.That(deviceA["button"], Is.TypeOf<ButtonControl>());
        Assert.That(deviceB["axis"], Is.TypeOf<AxisControl>());

        Assert.That(deviceA["button"].variants, Is.EqualTo("A"));
        Assert.That(deviceB["axis"].variants, Is.EqualTo("B"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_SettingVariantOnLayout_MergesAwayNonMatchingControlInformationFromBaseLayouts()
    {
        const string jsonBase = @"
            {
                ""name"" : ""BaseLayout"",
                ""extend"" : ""DeviceWithLayoutVariantA"",
                ""controls"" : [
                    { ""name"" : ""ControlFromBase"", ""layout"" : ""Button"" },
                    { ""name"" : ""OtherControlFromBase"", ""layout"" : ""Axis"" },
                    { ""name"" : ""ControlWithExplicitDefaultVariant"", ""layout"" : ""Axis"", ""variants"" : ""default"" },
                    { ""name"" : ""StickControl"", ""layout"" : ""Stick"" },
                    { ""name"" : ""StickControl/x"", ""offset"" : 14, ""variants"" : ""A"" }
                ]
            }
        ";
        const string jsonDerived = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    { ""name"" : ""ControlFromBase"", ""variants"" : ""A"", ""offset"" : 20 }
                ]
            }
        ";

        InputSystem.RegisterLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterLayout(jsonBase);
        InputSystem.RegisterLayout(jsonDerived);

        var layout = InputSystem.LoadLayout("DerivedLayout");

        // The variants setting here is coming all the way from the base layout so itself already has
        // to come through properly in the merge.
        Assert.That(layout.variants, Is.EqualTo(new InternedString("A")));

        // Not just the variants setting itself should come through but it also should affect the
        // merging of control items. `ControlFromBase` has a layout set on it which should get picked
        // up by the variants defined for it in `DerivedLayout`. Also, controls that don't have the right
        // variants should have been removed.
        Assert.That(layout.controls.Count, Is.EqualTo(6));
        Assert.That(layout.controls, Has.None.Matches<InputControlLayout.ControlItem>(
            x => x.name == new InternedString("axis")));     // Axis control should have disappeared.
        Assert.That(layout.controls, Has.Exactly(1).Matches<InputControlLayout.ControlItem>(
            x => x.name == new InternedString(
                "OtherControlFromBase")));      // But this one targeting no specific variants should be included.
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("ControlFromBase") && x.layout == new InternedString("Button") &&
                x.offset == 20 && x.variants == new InternedString("A")));
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("ControlWithExplicitDefaultVariant")));
        // Make sure that the "StickControl/x" item came through along with the stick itself.
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("StickControl")));
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("StickControl/x")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanMixMultipleLayoutVariants()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    { ""name"" : ""ButtonA"", ""layout"" : ""Button"", ""variants"" : ""A"" },
                    { ""name"" : ""ButtonB"", ""layout"" : ""Button"", ""variants"" : ""B"" },
                    { ""name"" : ""ButtonC"", ""layout"" : ""Button"", ""variants"" : ""C"" },
                    { ""name"" : ""ButtonAB"", ""layout"" : ""Button"", ""variants"" : ""A,B"" },
                    { ""name"" : ""ButtonNoVariant"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("TestLayout", variants: "A,B");

        Assert.That(device.variants, Is.EqualTo("A,B"));
        Assert.That(device.allControls, Has.Count.EqualTo(4));
        Assert.That(device.allControls, Has.Exactly(1).With.Property("name").EqualTo("ButtonA"));
        Assert.That(device.allControls, Has.Exactly(1).With.Property("name").EqualTo("ButtonB"));
        Assert.That(device.allControls, Has.Exactly(1).With.Property("name").EqualTo("ButtonAB"));
        Assert.That(device.allControls, Has.Exactly(1).With.Property("name").EqualTo("ButtonNoVariant"));
        Assert.That(device.allControls, Has.None.With.Property("name").EqualTo("ButtonC"));
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_CurrentPlatformIsImplicitLayoutVariant()
    {
        var json = @"
            {
                ""name"" : ""TestLayout"",
                ""controls"" : [
                    { ""name"" : ""Button"", ""layout"" : ""Button"", ""variants"" : ""__PLATFORM__"" }
                ]
            }
        ".Replace("__PLATFORM__", Application.platform.ToString());

        InputSystem.RegisterLayout(json);

        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device.allControls, Has.Count.EqualTo(1));
        Assert.That(device.allControls[0].name, Is.EqualTo("Button"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanReformatAndResizeControlHierarchy()
    {
        // Turn left stick into a 2D vector of shorts.
        // NOTE: Child offsets are not absolute! They are relative to their parent.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""leftStick"", ""format"" : ""VC2S"", ""offset"" : 6 },
                    { ""name"" : ""leftStick/x"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/y"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/up"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/down"", ""format"" : ""SHRT"", ""offset"" : 2 },
                    { ""name"" : ""leftStick/left"", ""format"" : ""SHRT"", ""offset"" : 0 },
                    { ""name"" : ""leftStick/right"", ""format"" : ""SHRT"", ""offset"" : 0 }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);
        var device = InputDevice.Build<Gamepad>("MyDevice");

        Assert.That(device.leftStick.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
        Assert.That(device.leftStick.x.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.y.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.x.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.up.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.up.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.down.stateBlock.byteOffset, Is.EqualTo(8));
        Assert.That(device.leftStick.down.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.left.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.left.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
        Assert.That(device.leftStick.right.stateBlock.byteOffset, Is.EqualTo(6));
        Assert.That(device.leftStick.right.stateBlock.sizeInBits, Is.EqualTo(2 * 8));
    }

    ////TODO: This test doesn't yet make sense. The thought of how the feature should work is
    ////      correct, but the setup makes no sense and doesn't work. Gamepad adds deadzones
    ////      on the *sticks* so modifying that requires a Vector2 type processor which invert
    ////      isn't.
    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_CanMoveProcessorFromBaseLayoutInProcessorStack()
    {
        // The base gamepad layout is adding deadzone processors to sticks. However, a
        // layout based on that one may want to add processors *before* deadzoning is
        // applied. It can do so very simply by listing where the deadzone processor should
        // occur.
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick/x"",
                        ""processors"" : ""invert,stickDeadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterLayout(json);

        var device = InputDevice.Build<InputDevice>("MyDevice");
        var leftStickX = device.GetChildControl<AxisControl>("leftStick/x");

        Assert.That(leftStickX.processors, Has.Length.EqualTo(2));
        Assert.That(leftStickX.processors[0], Is.TypeOf<InvertProcessor>());
        Assert.That(leftStickX.processors[1], Is.TypeOf<StickDeadzoneProcessor>());
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layout_CustomizedStateLayoutWillNotUseFormatCodeFromBaseLayout()
    {
        //make sure that if you customize a gamepad layout, you don't end up with the "GPAD" format on the device
        //in fact, the system should require a format code to be specified in that case
        Assert.Fail();
    }

    ////REVIEW: This one seems like it adds quite a bit of complexity for somewhat minor gain.
    ////        May even be safer to *not* support this as it may inject controls at offsets where you don't expect them.
    //[InputControl(name = "axis", offset = InputStateBlock.InvalidOffset)]
    private struct BaseInputState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public float axis;
        public int padding;

        public FourCC format => new FourCC("BASE");
    }

    [InputControlLayout(stateType = typeof(BaseInputState))]
    [Preserve]
    private class BaseInputDevice : InputDevice
    {
    }

    private struct DerivedInputState : IInputStateTypeInfo
    {
        public FourCC format => new FourCC("DERI");
    }

    [InputControlLayout(stateType = typeof(DerivedInputState))]
    [Preserve]
    private class DerivedInputDevice : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    [Ignore("TODO")]
    public void TODO_Layouts_InputStateInDerivedClassMergesWithControlsOfInputStateFromBaseClass()
    {
        //axis should appear in DerivedInputDevice and should have been moved to offset 8 (from automatic assignment)
        Assert.Fail();
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanConfigureDeviceUsages()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        Assert.That(gamepad.usages, Is.Empty);

        // Set "Vertical" as usage
        InputSystem.SetDeviceUsage(gamepad, CommonUsages.Vertical);
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Vertical));

        // Change usage with "Horizontal"
        InputSystem.SetDeviceUsage(gamepad, CommonUsages.Horizontal);
        Assert.That(gamepad.usages, Has.Exactly(0).EqualTo(CommonUsages.Vertical));
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Horizontal));


        // Add "Vertical" to usages
        InputSystem.AddDeviceUsage(gamepad, CommonUsages.Vertical);
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Vertical));
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Horizontal));

        // Set "Horizontal" as the only one usage
        InputSystem.SetDeviceUsage(gamepad, CommonUsages.Horizontal);
        Assert.That(gamepad.usages, Has.Exactly(0).EqualTo(CommonUsages.Vertical));
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Horizontal));

        // Try to add "Horizontal" again
        InputSystem.AddDeviceUsage(gamepad, CommonUsages.Horizontal);
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Horizontal));


        // Remove the existed "Horizontal" usage from usages
        InputSystem.AddDeviceUsage(gamepad, CommonUsages.Vertical);
        InputSystem.RemoveDeviceUsage(gamepad, CommonUsages.Horizontal);
        Assert.That(gamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.Vertical));
        Assert.That(gamepad.usages, Has.Exactly(0).EqualTo(CommonUsages.Horizontal));

        InputSystem.RemoveDeviceUsage(gamepad, CommonUsages.Vertical);
        Assert.That(gamepad.usages, Is.Empty);
    }
}
