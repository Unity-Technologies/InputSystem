using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestTools;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Modifiers;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;
using Gyroscope = UnityEngine.Experimental.Input.Gyroscope;
#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Editor;
using UnityEditor;
#endif

#if !(NET_4_0 || NET_4_6)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

// These tests rely on the default layout setup present in the code
// of the system (e.g. they make assumptions about how Gamepad is set up).
class CoreTests : InputTestFixture
{
    // The test categories give the feature area associated with the test:
    //
    //     a) Controls
    //     b) Layouts
    //     c) Devices
    //     d) State
    //     e) Events
    //     f) Actions
    //     g) Editor
    //     h) Remote

    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreatePrimitiveControlsFromLayout()
    {
        var setup = new InputDeviceBuilder("Gamepad");

        // The default ButtonControl layout has no constrols inside of it.
        Assert.That(setup.GetControl("start"), Is.TypeOf<ButtonControl>());
        Assert.That(setup.GetControl("start").children, Is.Empty);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanCreateCompoundControlsFromLayout()
    {
        const int kNumControlsInAStick = 6;

        var setup = new InputDeviceBuilder("Gamepad");

        Assert.That(setup.GetControl("leftStick"), Is.TypeOf<StickControl>());
        Assert.That(setup.GetControl("leftStick").children, Has.Count.EqualTo(kNumControlsInAStick));
        Assert.That(setup.GetControl("leftStick").children, Has.Exactly(1).With.Property("name").EqualTo("x"));
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

        InputSystem.RegisterControlLayout(deviceJson);
        InputSystem.RegisterControlLayout(controlJson);

        var setup = new InputDeviceBuilder("MyDevice");

        Assert.That(setup.GetControl("myThing/x"), Is.TypeOf<AxisControl>());
        Assert.That(setup.GetControl("myThing"), Has.Property("layout").EqualTo("MyControl"));

        var device = setup.Finish();
        Assert.That(device, Is.TypeOf<InputDevice>());
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CannotUseControlLayoutAsToplevelLayout()
    {
        Assert.That(() => new InputDeviceBuilder("Button"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanExtendControlInBaseLayoutUsingPath()
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

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.leftStick.x.stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetControlParametersThroughControlAttribute()
    {
        // StickControl sets parameters on its axis controls. Check that they are
        // there.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.up.clamp, Is.True);
        Assert.That(gamepad.leftStick.up.clampMin, Is.EqualTo(0));
        Assert.That(gamepad.leftStick.up.clampMax, Is.EqualTo(1));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetUsagesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.leftStick.usages, Has.Exactly(1).EqualTo(CommonUsages.Primary2DMotion));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSetAliasesThroughControlAttribute()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("square")));
        Assert.That(gamepad.buttonWest.aliases, Has.Exactly(1).EqualTo(new InternedString("x")));
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
                        ""parameters"" : ""clamp=true,clampMin=0.123,clampMax=0.456""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        Assert.That(device.rightTrigger.clamp, Is.True);
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
                        ""processors"" : ""deadzone(min=0.1,max=0.9)""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad)InputSystem.AddDevice("MyDevice");

        // NOTE: Unfortunately, this currently relies on an internal method (TryGetProcessor).

        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>(), Is.Not.Null);
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().min, Is.EqualTo(0.1).Within(0.00001f));
        Assert.That(device.leftStick.TryGetProcessor<DeadzoneProcessor>().max, Is.EqualTo(0.9).Within(0.00001f));
    }

    unsafe struct StateStructWithArrayOfControls
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float value[5];
    }
    [InputControlLayout(stateType = typeof(StateStructWithArrayOfControls))]
    class TestDeviceWithArrayOfControls : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddArrayOfControls_InStateStruct()
    {
        InputSystem.RegisterControlLayout<TestDeviceWithArrayOfControls>();

        var device = new InputDeviceBuilder("TestDeviceWithArrayOfControls").Finish();

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

        InputSystem.RegisterControlLayout(json);
        var device = new InputDeviceBuilder("MyDevice").Finish();

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
                        ""parameters"" : ""clamp""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("MyDevice").Finish();

        Assert.That(device.leftStick.x.clamp, Is.True);
    }

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    class DeviceWithCommonUsages : InputDevice
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

        InputSystem.RegisterControlLayout(typeof(DeviceWithCommonUsages), "BaseDevice");
        InputSystem.RegisterControlLayout(derivedJson);

        var layout = InputSystem.TryLoadLayout("DerivedDevice");

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

        InputSystem.RegisterControlLayout(json);

        var layout = InputSystem.TryFindMatchingControlLayout(new InputDeviceDescription
        {
            product = "MyThingy"
        });

        Assert.That(layout, Is.EqualTo("MyDevice"));
    }

    [Test]
    [Category("Layout")]
    public void Layouts_CanOverrideLayoutMatchesForDiscoveredDevices()
    {
        InputSystem.onFindControlLayoutForDevice +=
            (int deviceId, ref InputDeviceDescription description, string layoutMatch, IInputRuntime runtime) => "Keyboard";

        var device = InputSystem.AddDevice(new InputDeviceDescription {deviceClass = "Gamepad"});

        Assert.That(device, Is.TypeOf<Keyboard>());
    }

    // If a layout only specifies an interface in its descriptor, it is considered
    // a fallback for when there is no more specific layout that is able to match
    // by product.
    [Test]
    [Category("Layouts")]
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

        InputSystem.RegisterControlLayout(fallbackJson);
        InputSystem.RegisterControlLayout(productJson);

        Assert.Fail();
    }

    ////REVIEW: if this behavior is guaranteed, we also have to make sure we preserve it across domain reloads
    [Test]
    [Category("Layouts")]
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

        InputSystem.RegisterControlLayout(firstLayout);
        InputSystem.RegisterControlLayout(secondLayout);

        var layout = InputSystem.TryFindMatchingControlLayout(new InputDeviceDescription {product = "MyProduct"});

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
        InputSystem.RegisterControlLayout(json);

        Assert.That(() => InputSystem.AddDevice("MyDevice"),
            Throws.TypeOf<Exception>().With.Property("Message").Contain("Duplicate control"));
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

        InputSystem.RegisterControlLayout(derivedDeviceJson);
        InputSystem.RegisterControlLayout(baseDeviceJson);

        var device = InputSystem.AddDevice("MyDerived");

        InputSystem.RegisterControlLayout(newBaseDeviceJson);

        Assert.That(device.children, Has.Count.EqualTo(1));
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("yeah").And.Property("layout").EqualTo("Stick"));
    }

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

        InputSystem.RegisterControlLayout(initialJson);

        testRuntime.ReportNewInputDevice(new InputDeviceDescription {product = "Test"}.ToJson());
        InputSystem.Update();

        var oldDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        var oldDeviceId = oldDevice.id;
        var oldDeviceDescription = oldDevice.description;

        const string newJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Keyboard""
            }
        ";

        InputSystem.RegisterControlLayout(newJson);
        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("layout").EqualTo("MyDevice"));

        var newDevice = InputSystem.devices.First(x => x.layout == "MyDevice");

        Assert.That(newDevice.id, Is.EqualTo(oldDeviceId));
        Assert.That(newDevice.description, Is.EqualTo(oldDeviceDescription));
    }

    private class MyButtonControl : ButtonControl
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ReplacingControlLayoutAffectsAllDevicesUsingLayout()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Replace "Button" layout.
        InputSystem.RegisterControlLayout<MyButtonControl>("Button");

        Assert.That(gamepad.leftTrigger, Is.TypeOf<MyButtonControl>());
    }

    class TestLayoutType : Pointer
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_UsesBaseTypeAsBaseLayout()
    {
        InputSystem.RegisterControlLayout<TestLayoutType>();

        var layout = InputSystem.TryLoadLayout("TestLayoutType");

        Assert.That(layout.extendsLayout, Is.EqualTo("Pointer"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutType_WithMatcher_PutsMatcherInLayoutWhenLoaded()
    {
        InputSystem.RegisterControlLayout<TestLayoutType>(
            matches: new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithManufacturer("TestManufacturer")
            .WithProduct("TestProduct"));

        var layout = InputSystem.TryLoadLayout("TestLayoutType");

        Assert.That(layout.deviceMatcher.empty, Is.False);
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1).Matches<KeyValuePair<string, object>>(x => x.Key == "product" && x.Value.Equals("TestProduct")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_RegisteringLayoutBuilder_WithMatcher_PutsMatcherInLayoutWhenLoaded()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Mouse"};

        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), name: "TestLayout",
            matches: new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithProduct("TestProduct")
            .WithManufacturer("TestManufacturer"));

        var layout = InputSystem.TryLoadLayout("TestLayout");

        Assert.That(layout.deviceMatcher.empty, Is.False);
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1).Matches<KeyValuePair<string, object>>(x => x.Key == "product" && x.Value.Equals("TestProduct")));
        Assert.That(layout.deviceMatcher.patterns,
            Has.Exactly(1)
            .Matches<KeyValuePair<string, object>>(x => x.Key == "interface" && x.Value.Equals("TestInterface")));
    }

    // Want to ensure that if a state struct declares an "int" field, for example, and then
    // assigns it then Axis layout (which has a default format of float), the AxisControl
    // comes out with an "INT" format and not a "FLT" format.
    struct StateStructWithPrimitiveFields : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public byte byteAxis;
        [InputControl(layout = "Axis")] public short shortAxis;
        [InputControl(layout = "Axis")] public int intAxis;
        // No float as that is the default format for Axis anyway.
        [InputControl(layout = "Axis")] public double doubleAxis;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }
    [InputControlLayout(stateType = typeof(StateStructWithPrimitiveFields))]
    class DeviceWithStateStructWithPrimitiveFields : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithPrimitiveTypeInStateStructInferredFromType()
    {
        InputSystem.RegisterControlLayout<DeviceWithStateStructWithPrimitiveFields>("Test");
        var setup = new InputDeviceBuilder("Test");

        Assert.That(setup.GetControl("byteAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeByte));
        Assert.That(setup.GetControl("shortAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeShort));
        Assert.That(setup.GetControl("intAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeInt));
        Assert.That(setup.GetControl("doubleAxis").stateBlock.format, Is.EqualTo(InputStateBlock.kTypeDouble));
    }

    unsafe struct StateWithFixedArray : IInputStateTypeInfo
    {
        [InputControl] public fixed float buffer[2];

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }
    [InputControlLayout(stateType = typeof(StateWithFixedArray))]
    class DeviceWithStateStructWithFixedArray : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_FormatOfControlWithFixedArrayType_IsNotInferredFromType()
    {
        InputSystem.RegisterControlLayout<DeviceWithStateStructWithFixedArray>();

        Assert.That(() => new InputDeviceBuilder("DeviceWithStateStructWithFixedArray"),
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

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var testControl = setup.GetControl<AxisControl>("test");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.stateBlock.alignedSizeInBytes, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>()));
        Assert.That(testControl.stateBlock.byteOffset, Is.EqualTo(device.leftStick.x.stateBlock.byteOffset));
        Assert.That(testControl.stateBlock.sizeInBits, Is.EqualTo(device.leftStick.x.stateBlock.sizeInBits));
        Assert.That(testControl.stateBlock.format, Is.EqualTo(device.leftStick.x.stateBlock.format));
        Assert.That(testControl.stateBlock.bitOffset, Is.EqualTo(device.leftStick.x.stateBlock.bitOffset));
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

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("TestLayout").Finish();

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

        InputSystem.RegisterControlLayout(baseJson);
        InputSystem.RegisterControlLayout(derivedJson);

        var setup = new InputDeviceBuilder("Derived");
        var stick = setup.GetControl<StickControl>("stick");

        Assert.That(stick.stateBlock.sizeInBits, Is.EqualTo(2 * 2 * 8));
    }

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

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyGamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.dpad.up.layout, Is.EqualTo("DiscreteButton"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanSpecifyDisplayNameForControl()
    {
        const string json = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad"",
                ""displayName"" : ""Test Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""leftStick"",
                        ""displayName"" : ""Primary Stick""
                    },
                    {
                        ""name"" : ""leftStick/x"",
                        ""displayName"" : ""Horizontal""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = (Gamepad) new InputDeviceBuilder("MyLayout").Finish();

        Assert.That(device.displayName, Is.EqualTo("Test Gamepad"));
        Assert.That(device.leftStick.displayName, Is.EqualTo("Primary Stick"));
        Assert.That(device.leftStick.x.displayName, Is.EqualTo("Horizontal"));
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

        InputSystem.RegisterControlLayout(json);

        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(device["button"].noisy, Is.True);
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
        Assert.That(layout.extendsLayout, Is.EqualTo("Pointer"));
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
    class TestLayoutBuilder
    {
        [SerializeField] public string layoutToLoad;
        [NonSerialized] public InputControlLayout layout;

        public InputControlLayout DoIt()
        {
            // To make this as simple as possible, just load another layout.
            layout = InputSystem.TryLoadLayout(layoutToLoad);
            return layout;
        }
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanAddCustomLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};

        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), "MyLayout");

        var result = InputSystem.TryLoadLayout("MyLayout");

        Assert.That(result.name.ToString(), Is.EqualTo("MyLayout"));
        Assert.That(result, Is.SameAs(builder.layout));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_CanTurnLayoutIntoJson()
    {
        var layout = InputSystem.TryLoadLayout("Gamepad");
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
        Assert.That(InputControlPath.TryGetControlLayout("*/<button>"), Is.EqualTo("button")); // Does not "correct" casing.
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

        ////TODO: make sure we can find layouts from control layout modifying child paths
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
    public void Layouts_CanLoadLayout()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [ { ""name"" : ""MyControl"" } ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var jsonLayout = InputSystem.TryLoadLayout("MyLayout");

        Assert.That(jsonLayout, Is.Not.Null);
        Assert.That(jsonLayout.name, Is.EqualTo(new InternedString("MyLayout")));
        Assert.That(jsonLayout.controls, Has.Count.EqualTo(1));
        Assert.That(jsonLayout.controls[0].name, Is.EqualTo(new InternedString("MyControl")));

        var gamepadLayout = InputSystem.TryLoadLayout("Gamepad");

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

        InputSystem.RegisterControlLayout(json);
        var device = InputSystem.AddDevice("MyLayout");

        Assert.That(InputSystem.ListControlLayouts(), Has.Exactly(1).EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(device));

        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(InputSystem.ListControlLayouts(), Has.None.EqualTo("MyLayout"));
        Assert.That(InputSystem.devices, Has.None.SameAs(device));
        Assert.That(InputSystem.devices, Has.None.With.Property("layout").EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void Layouts_ChangingLayouts_SendsNotifications()
    {
        InputControlLayoutChange? receivedChange = null;
        string receivedLayout = null;

        InputSystem.onControlLayoutChange +=
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
        InputSystem.RegisterControlLayout(jsonV1);

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
        InputSystem.RegisterControlLayout(jsonV2);

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Replaced));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));

        receivedChange = null;
        receivedLayout = null;

        // RemoveControlLayout.
        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(receivedChange, Is.EqualTo(InputControlLayoutChange.Removed));
        Assert.That(receivedLayout, Is.EqualTo("MyLayout"));
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_RemovingLayouts_RemovesAllLayoutsBasedOnIt()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_CanQueryResourceNameFromControl()
    {
        var json = @"
            {
                ""name"" : ""MyLayout"",
                ""controls"" : [ { ""name"" : ""MyControl"",  } ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        Assert.Fail();
    }

    struct StateWithTwoLayoutVariants : IInputStateTypeInfo
    {
        [InputControl(name = "button", layout = "Button", variant = "A")]
        public int buttons;
        [InputControl(name = "axis", layout = "Axis", variant = "B")]
        public float axis;

        public FourCC GetFormat()
        {
            return new FourCC('T', 'E', 'S', 'T');
        }
    }

    [InputControlLayout(variant = "A", stateType = typeof(StateWithTwoLayoutVariants))]
    class DeviceWithLayoutVariantA : InputDevice
    {
    }
    [InputControlLayout(variant = "B", stateType = typeof(StateWithTwoLayoutVariants))]
    class DeviceWithLayoutVariantB : InputDevice
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
        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantB>();

        var deviceA = InputSystem.AddDevice<DeviceWithLayoutVariantA>();
        var deviceB = InputSystem.AddDevice<DeviceWithLayoutVariantB>();

        Assert.That(deviceA.allControls.Count, Is.EqualTo(1));
        Assert.That(deviceB.allControls.Count, Is.EqualTo(1));

        Assert.That(deviceA["button"], Is.TypeOf<ButtonControl>());
        Assert.That(deviceB["axis"], Is.TypeOf<AxisControl>());

        Assert.That(deviceA["button"].variant, Is.EqualTo("A"));
        Assert.That(deviceB["axis"].variant, Is.EqualTo("B"));
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
                    { ""name"" : ""ControlWithExplicitDefaultVariant"", ""layout"" : ""Axis"", ""variant"" : ""default"" },
                    { ""name"" : ""StickControl"", ""layout"" : ""Stick"" },
                    { ""name"" : ""StickControl/x"", ""offset"" : 14, ""variant"" : ""A"" }
                ]
            }
        ";
        const string jsonDerived = @"
            {
                ""name"" : ""DerivedLayout"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    { ""name"" : ""ControlFromBase"", ""variant"" : ""A"", ""offset"" : 20 }
                ]
            }
        ";

        InputSystem.RegisterControlLayout<DeviceWithLayoutVariantA>();
        InputSystem.RegisterControlLayout(jsonBase);
        InputSystem.RegisterControlLayout(jsonDerived);

        var layout = InputSystem.TryLoadLayout("DerivedLayout");

        // The variant setting here is coming all the way from the base layout so itself already has
        // to come through properly in the merge.
        Assert.That(layout.variant, Is.EqualTo(new InternedString("A")));

        // Not just the variant setting itself should come through but it also should affect the
        // merging of control items. `ControlFromBase` has a layout set on it which should get picked
        // up by the variant defined for it in `DerivedLayout`. Also, controls that don't have the right
        // variant should have been removed.
        Assert.That(layout.controls.Count, Is.EqualTo(6));
        Assert.That(layout.controls, Has.None.Matches<InputControlLayout.ControlItem>(
                x => x.name == new InternedString("axis"))); // Axis control should have disappeared.
        Assert.That(layout.controls, Has.Exactly(1).Matches<InputControlLayout.ControlItem>(
                x => x.name == new InternedString("OtherControlFromBase"))); // But this one targeting no specific variant should be included.
        Assert.That(layout.controls, Has.Exactly(1)
            .Matches<InputControlLayout.ControlItem>(x =>
                x.name == new InternedString("ControlFromBase") && x.layout == new InternedString("Button") && x.offset == 20 && x.variant == new InternedString("A")));
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
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayout()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var device = setup.Finish();

        Assert.That(device, Is.TypeOf<Gamepad>());
        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("leftStick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_WithNestedState()
    {
        InputSystem.RegisterControlLayout<CustomDevice>();
        var setup = new InputDeviceBuilder("CustomDevice");
        var device = setup.Finish();

        Assert.That(device.children, Has.Exactly(1).With.Property("name").EqualTo("button1"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayoutMatchedByDeviceDescription()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""interface"" : ""AA|BB"",
                    ""product"" : ""Shtabble""
                }
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "BB",
            product = "Shtabble"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.layout, Is.EqualTo("MyDevice"));
        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceCreatedFromDeviceDescriptionStoresDescriptionOnDevice()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""Product""
                }
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var description = new InputDeviceDescription
        {
            interfaceName = "Interface",
            product = "Product",
            manufacturer = "Manufacturer",
            deviceClass = "DeviceClass",
            version = "Version",
            serial = "Serial"
        };

        var device = InputSystem.AddDevice(description);

        Assert.That(device.description, Is.EqualTo(description));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateDevice_FromLayoutVariant()
    {
        var leftyGamepadSetup = new InputDeviceBuilder("Gamepad", variant: "Lefty");
        var leftyGamepadPrimary2DMotion = leftyGamepadSetup.GetControl("{Primary2DMotion}");
        var leftyGamepadSecondary2DMotion = leftyGamepadSetup.GetControl("{Secondary2DMotion}");
        //var leftyGamepadPrimaryTrigger = leftyGamepadSetup.GetControl("{PrimaryTrigger}");
        //var leftyGamepadSecondaryTrigger = leftyGamepadSetup.GetControl("{SecondaryTrigger}");
        //shoulder?

        var defaultGamepadSetup = new InputDeviceBuilder("Gamepad");
        var defaultGamepadPrimary2DMotion = defaultGamepadSetup.GetControl("{Primary2DMotion}");
        var defaultGamepadSecondary2DMotion = defaultGamepadSetup.GetControl("{Secondary2DMotion}");
        //var defaultGamepadPrimaryTrigger = defaultGamepadSetup.GetControl("{PrimaryTrigger}");
        //var defaultGamepadSecondaryTrigger = defaultGamepadSetup.GetControl("{SecondaryTrigger}");

        var leftyGamepad = (Gamepad)leftyGamepadSetup.Finish();
        var defaultGamepad = (Gamepad)defaultGamepadSetup.Finish();

        Assert.That(leftyGamepad.variant, Is.EqualTo("Lefty"));
        Assert.That(leftyGamepadPrimary2DMotion, Is.SameAs(leftyGamepad.rightStick));
        Assert.That(leftyGamepadSecondary2DMotion, Is.SameAs(leftyGamepad.leftStick));

        Assert.That(defaultGamepadPrimary2DMotion, Is.SameAs(defaultGamepad.leftStick));
        Assert.That(defaultGamepadSecondary2DMotion, Is.SameAs(defaultGamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CannotChangeSetupOfDeviceWhileAddedToSystem()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(() => new InputDeviceBuilder("Keyboard", device), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeControlSetupAfterCreation()
    {
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(initialJson);

        // Create initial version of device.
        var initialSetup = new InputDeviceBuilder("MyDevice");
        var initialFirstControl = initialSetup.GetControl("first");
        var initialSecondControl = initialSetup.GetControl("second");
        var initialDevice = initialSetup.Finish();

        // Change layout.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""first"", ""layout"" : ""Button"" },
                    { ""name"" : ""second"", ""layout"" : ""Axis"" },
                    { ""name"" : ""third"", ""layout"" : ""Button"" }
                ]
            }
        ";
        InputSystem.RegisterControlLayout(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputDeviceBuilder("MyDevice", existingDevice: initialDevice);
        var modifiedFirstControl = modifiedSetup.GetControl("first");
        var modifiedSecondControl = modifiedSetup.GetControl("second");
        var modifiedThirdControl = modifiedSetup.GetControl("third");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.SameAs(initialDevice));
        Assert.That(modifiedFirstControl, Is.SameAs(initialFirstControl));
        Assert.That(initialFirstControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.Not.SameAs(initialSecondControl));
        Assert.That(initialSecondControl, Is.TypeOf<ButtonControl>());
        Assert.That(modifiedSecondControl, Is.TypeOf<AxisControl>());
        Assert.That(modifiedThirdControl, Is.TypeOf<ButtonControl>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanChangeDeviceTypeAfterCreation()
    {
        // Device layout for a generic InputDevice.
        const string initialJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    { ""name"" : ""buttonSouth"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(initialJson);

        // Create initial version of device.
        var initialSetup = new InputDeviceBuilder("MyDevice");
        var initialButton = initialSetup.GetControl<ButtonControl>("buttonSouth");
        var initialDevice = initialSetup.Finish();

        // Change layout to now be a gamepad.
        const string modifiedJson = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";
        InputSystem.RegisterControlLayout(modifiedJson);

        // Modify device.
        var modifiedSetup = new InputDeviceBuilder("MyDevice", existingDevice: initialDevice);
        var modifiedButton = modifiedSetup.GetControl<ButtonControl>("buttonSouth");
        var modifiedDevice = modifiedSetup.Finish();

        Assert.That(modifiedDevice, Is.Not.SameAs(initialDevice));
        Assert.That(modifiedDevice, Is.TypeOf<Gamepad>());
        Assert.That(initialDevice, Is.TypeOf<InputDevice>());
        Assert.That(modifiedButton, Is.SameAs(initialButton)); // Button survives.
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingUsageOfDevice_SendsDeviceChangeNotification()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                receivedDevice = d;
                receivedDeviceChange = c;
            };

        InputSystem.SetUsage(device, CommonUsages.LeftHand);

        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.UsageChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsage()
    {
        InputSystem.AddDevice<Gamepad>();
        var device = InputSystem.AddDevice<Keyboard>();

        InputSystem.SetUsage(device, CommonUsages.LeftHand);

        var controls = InputSystem.GetControls("/{LeftHand}");

        Assert.That(controls, Has.Count.EqualTo(1));
        Assert.That(controls, Has.Exactly(1).SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDeviceByUsageAndLayout()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetUsage(gamepad, CommonUsages.LeftHand);

        var keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.SetUsage(keyboard, CommonUsages.LeftHand);

        var controls = InputSystem.GetControls("/<Keyboard>{LeftHand}");

        Assert.That(controls, Has.Count.EqualTo(1));
        Assert.That(controls, Has.Exactly(1).SameAs(keyboard));
    }

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
    public void Controls_ControlsReferToTheirParent()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.leftStick.parent, Is.SameAs(gamepad));
        Assert.That(gamepad.leftStick.x.parent, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_ControlsReferToTheirDevices()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var leftStick = setup.GetControl("leftStick");
        var device = setup.Finish();

        Assert.That(leftStick.device, Is.SameAs(device));
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

        InputSystem.RegisterControlLayout(json);

        var device = new InputDeviceBuilder("MyDevice").Finish();

        Assert.That(device.allControls.Count, Is.EqualTo(2 + 4 + 2)); // 2 toplevel controls, 4 added by Stick, 2 for X and Y
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

        InputSystem.RegisterControlLayout(json);
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

        Assert.That(device.leftStick.ReadValue(), Is.EqualTo(processor.Process(new Vector2(0.5f, 0.5f), device.leftStick)));
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

        InputSystem.RegisterControlLayout(json);
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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.0).Within(0.000001));

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(-0.5f, -0.5f) });
        InputSystem.Update();

        Assert.That(gamepad.leftStick.up.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.down.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.right.ReadValue(), Is.EqualTo(0.0).Within(0.000001));
        Assert.That(gamepad.leftStick.left.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanQueryValueFromStateEvents()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                Assert.That(gamepad.leftTrigger.ReadValueFrom(eventPtr), Is.EqualTo(0.234f).Within(0.00001));
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.234f });
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanWriteValueIntoState()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var state = new GamepadState();
        var value = new Vector2(0.5f, 0.5f);

        gamepad.leftStick.WriteValueInto(ref state, value);

        Assert.That(state.leftStick, Is.EqualTo(value));
    }

    [Test]
    [Category("Controls")]
    public void Controls_DpadVectorsAreCircular()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Up.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.up));

        // Up left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.left));

        // Down left.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadLeft });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.down + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.down + Vector2.left).normalized.y).Within(0.00001));

        // Down.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.down));

        // Down right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadDown | 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue(), Is.EqualTo(Vector2.right));

        // Up right.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadState.Button.DpadUp | 1 << (int)GamepadState.Button.DpadRight });
        InputSystem.Update();

        Assert.That(gamepad.dpad.ReadValue().x, Is.EqualTo((Vector2.up + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(gamepad.dpad.ReadValue().y, Is.EqualTo((Vector2.up + Vector2.right).normalized.y).Within(0.00001));
    }

    struct DiscreteButtonDpadState : IInputStateTypeInfo
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

        InputSystem.RegisterControlLayout(json);
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
    [Category("State")]
    public void State_CanComputeStateLayoutFromStateStructure()
    {
        var setup = new InputDeviceBuilder("Gamepad");
        var gamepad = (Gamepad)setup.Finish();

        Assert.That(gamepad.stateBlock.sizeInBits, Is.EqualTo(UnsafeUtility.SizeOf<GamepadState>() * 8));
        Assert.That(gamepad.leftStick.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "leftStick").ToInt32()));
        Assert.That(gamepad.dpad.stateBlock.byteOffset, Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "buttons").ToInt32()));
    }

    [Test]
    [Category("State")]
    public void State_CanComputeStateLayoutForNestedStateStructures()
    {
        InputSystem.RegisterControlLayout<CustomDevice>();
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
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.25, previous=0.0
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Fixed); // Dynamic: current=0.75, previous=0.0

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.Zero);
    }

    [Test]
    [Category("State")]
    public void State_RunningNoFixedUpdateInFrame_StillCapturesStateForNextFixedUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Fixed); // Fixed: current=0.75, previous=0.0

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
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

        InputSystem.RegisterControlLayout(jsonLayout);

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

        InputSystem.RegisterControlLayout(jsonLayout);

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

        InputSystem.RegisterControlLayout(json);
        var device = (Gamepad) new InputDeviceBuilder("MyDevice").Finish();

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

        InputSystem.RegisterControlLayout(jsonLayout);

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

        InputSystem.RegisterControlLayout(json);
        var setup = new InputDeviceBuilder("MyDevice");

        Assert.That(setup.GetControl("controlWithAutomaticOffset").stateBlock.byteOffset, Is.EqualTo(14));

        var device = setup.Finish();
        Assert.That(device.stateBlock.sizeInBits, Is.EqualTo(15 * 8));
    }

    [Test]
    [Category("State")]
    public void State_CanSpecifyBitOffsetsOnControlProperties()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad.dpad.right.stateBlock.bitOffset, Is.EqualTo((int)DpadControl.ButtonBits.Right));
        Assert.That(gamepad.dpad.right.stateBlock.byteOffset, Is.EqualTo(gamepad.dpad.stateBlock.byteOffset));
    }

    // Using "offset = N" with an InputControlAttribute that doesn't specify a child path (or even then?)
    // should add the base offset of the field itself.
    [Test]
    [Category("State")]
    public void TODO_State_SpecifyingOffsetOnControlAttribute_AddsBaseOffset()
    {
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void State_CanUpdateButtonState()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

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
                        ""offset"" : 4
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

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

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromLayout()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddDeviceFromLayout_LookedUpFromType()
    {
        // Register layout with name different from name of type
        // so that trying to find the layout using the type name
        // would fail.
        InputSystem.RegisterControlLayout<CustomDevice>("MyDevice");

        var device = InputSystem.AddDevice<CustomDevice>();

        Assert.That(device, Is.TypeOf<CustomDevice>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTwiceIsIgnored()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.onDeviceChange +=
            (d, c) => Assert.Fail("Shouldn't send notification for duplicate adding of device.");

        InputSystem.AddDevice(device);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Contains.Item(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceTriggersNotification()
    {
        var receivedCallCount = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receiveDeviceChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                ++receivedCallCount;
                receivedDevice = device;
                receiveDeviceChange = change;
            };

        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(receivedCallCount, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receiveDeviceChange, Is.EqualTo(InputDeviceChange.Added));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceMakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceDoesNotCauseExistingDevicesToForgetTheirState()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        InputSystem.AddDevice("Keyboard");

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceAffectsControlPaths()
    {
        InputSystem.AddDevice("Gamepad");   // Add a gamepad so that when we add another, its name will have to get adjusted.

        var setup = new InputDeviceBuilder("Gamepad");
        var device = (Gamepad)setup.Finish();

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad/dpad/up"));

        InputSystem.AddDevice(device);

        Assert.That(device.dpad.up.path, Is.EqualTo("/Gamepad1/dpad/up"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingDeviceThatUsesBeforeRenderUpdates_CausesBeforeRenderUpdatesToBeEnabled()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterControlLayout(deviceJson);

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));

        InputSystem.AddDevice("CustomGamepad");

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingLastDeviceThatUsesBeforeRenderUpdates_CausesBeforeRenderUpdatesToBeDisabled()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterControlLayout(deviceJson);

        var device1 = InputSystem.AddDevice("CustomGamepad");
        var device2 = InputSystem.AddDevice("CustomGamepad");

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device1);

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo(InputUpdateType.BeforeRender));

        InputSystem.RemoveDevice(device2);

        Assert.That(InputSystem.updateMask & InputUpdateType.BeforeRender, Is.EqualTo((InputUpdateType)0));
    }

    class TestDeviceReceivingAddAndRemoveNotification : Mouse
    {
        public int addedCount;
        public int removedCount;

        protected override void OnAdded()
        {
            ++addedCount;
        }

        protected override void OnRemoved()
        {
            ++removedCount;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_AddingAndRemovingDevice_InvokesNotificationOnDeviceItself()
    {
        InputSystem.RegisterControlLayout<TestDeviceReceivingAddAndRemoveNotification>();

        var device = InputSystem.AddDevice<TestDeviceReceivingAddAndRemoveNotification>();

        Assert.That(device.addedCount, Is.EqualTo(1));
        Assert.That(device.removedCount, Is.Zero);

        InputSystem.RemoveDevice(device);

        Assert.That(device.addedCount, Is.EqualTo(1));
        Assert.That(device.removedCount, Is.EqualTo(1));
    }

    [Test]
    [Category("Devices")]
    public void Devices_UnsupportedDevices_AreAddedToList()
    {
        const string json = @"
            {
                ""interface"" : ""TestInterface"",
                ""product"" : ""TestProduct"",
                ""manufacturer"" : ""TestManufacturer""
            }
        ";

        testRuntime.ReportNewInputDevice(json);
        InputSystem.Update();

        var unsupportedDevices = new List<InputDeviceDescription>();
        var count = InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(count, Is.EqualTo(1));
        Assert.That(unsupportedDevices.Count, Is.EqualTo(1));
        Assert.That(unsupportedDevices[0].interfaceName, Is.EqualTo("TestInterface"));
        Assert.That(unsupportedDevices[0].product, Is.EqualTo("TestProduct"));
        Assert.That(unsupportedDevices[0].manufacturer, Is.EqualTo("TestManufacturer"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_UnsupportedDevices_AreRemovedFromList_WhenMatchingLayoutIsAdded()
    {
        const string json = @"
            {
                ""interface"" : ""TestInterface"",
                ""product"" : ""TestProduct"",
                ""manufacturer"" : ""TestManufacturer""
            }
        ";

        testRuntime.ReportNewInputDevice(json);
        InputSystem.Update();

        InputSystem.RegisterControlLayout<TestLayoutType>(
            matches: new InputDeviceMatcher()
            .WithInterface("TestInterface"));

        var unsupportedDevices = new List<InputDeviceDescription>();
        var count = InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(count, Is.Zero);
        Assert.That(unsupportedDevices.Count, Is.Zero);
        Assert.That(InputSystem.devices.Count, Is.EqualTo(1));
        Assert.That(InputSystem.devices[0].description.interfaceName, Is.EqualTo("TestInterface"));
        Assert.That(InputSystem.devices[0].description.product, Is.EqualTo("TestProduct"));
        Assert.That(InputSystem.devices[0].description.manufacturer, Is.EqualTo("TestManufacturer"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByItsIdAfterItHasBeenAdded()
    {
        var device = InputSystem.AddDevice("Gamepad");

        Assert.That(InputSystem.TryGetDeviceById(device.id), Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpDeviceByLayout()
    {
        var device = InputSystem.AddDevice("Gamepad", "test"); // Give name to make sure we're not matching by name.
        var result = InputSystem.GetDevice("Gamepad");

        Assert.That(result, Is.SameAs(device));
    }

    [Test]
    [Category("Devices")]
    public void Devices_EnsuresDeviceNamesAreUnique()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.name, Is.Not.EqualTo(gamepad2.name));
    }

    [Test]
    [Category("Devices")]
    public void Devices_AssignsUniqueNumericIdToDevices()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        Assert.That(gamepad1.id, Is.Not.EqualTo(gamepad2.id));
    }

    [Test]
    [Category("Devices")]
    public void Devices_NameDefaultsToNameOfLayout()
    {
        var device = InputSystem.AddDevice<Mouse>();

        Assert.That(device.name, Is.EqualTo("Mouse"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_NameDefaultsToNameOfTemplate_AlsoWhenProductNameIsNotSupplied()
    {
        InputSystem.RegisterControlLayout(@"
            {
                ""name"" : ""TestTemplate"",
                ""device"" : { ""interface"" : ""TEST"" },
                ""controls"" : [
                    { ""name"" : ""button"", ""layout"" : ""Button"" }
                ]
            }
        ");

        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            interfaceName = "TEST",
        }.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).With.Property("name").EqualTo("TestTemplate"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingConfigurationOfDevice_TriggersNotification()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                ++receivedCalls;
                receivedDevice = d;
                receivedDeviceChange = c;
            };

        InputSystem.QueueConfigChangeEvent(gamepad);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.ConfigurationChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_TriggersNotification()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                ++receivedCalls;
                receivedDevice = d;
                receivedDeviceChange = c;
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.StateChanged));
    }

    class TestDeviceThatResetsStateInCallback : InputDevice, IInputStateCallbackReceiver
    {
        public ButtonControl button { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            button = builder.GetControl<ButtonControl>(this, "button");
            base.FinishSetup(builder);
        }

        public bool OnCarryStateForward(IntPtr statePtr)
        {
            button.WriteValueInto(statePtr, 1);
            return true;
        }

        public void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
        }

        public bool OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize, ref uint offsetToStoreAt)
        {
            return false;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_InStateCallback_TriggersNotification()
    {
        InputSystem.RegisterControlLayout<TestDeviceThatResetsStateInCallback>();
        var device = InputSystem.AddDevice<TestDeviceThatResetsStateInCallback>();

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedDeviceChange = null;

        InputSystem.onDeviceChange +=
            (d, c) =>
            {
                ++receivedCalls;
                receivedDevice = d;
                receivedDeviceChange = c;
            };

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(device));
        Assert.That(receivedDeviceChange, Is.EqualTo(InputDeviceChange.StateChanged));
    }

    [Test]
    [Category("Devices")]
    public void Devices_ChangingStateOfDevice_MarksDeviceAsUpdatedThisFrame()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        Assert.That(device.wasUpdatedThisFrame, Is.False);

        InputSystem.QueueStateEvent(device, new GamepadState {rightTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(device.wasUpdatedThisFrame, Is.True);

        InputSystem.Update();

        Assert.That(device.wasUpdatedThisFrame, Is.False);
    }

    struct TestDevicePartialState : IInputStateTypeInfo
    {
        public float axis;

        public FourCC GetFormat()
        {
            return new FourCC("PART");
        }
    }
    unsafe struct TestDeviceFullState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis", arraySize = 5)]
        public fixed float axis[5];

        public FourCC GetFormat()
        {
            return new FourCC("FULL");
        }
    }
    [InputControlLayout(stateType = typeof(TestDeviceFullState))]
    class TestDeviceDecidingWhereToIntegrateState : InputDevice, IInputStateCallbackReceiver
    {
        public bool OnCarryStateForward(IntPtr statePtr)
        {
            return false;
        }

        public void OnBeforeWriteNewState(IntPtr oldStatePtr, IntPtr newStatePtr)
        {
        }

        public unsafe bool OnReceiveStateWithDifferentFormat(IntPtr statePtr, FourCC stateFormat, uint stateSize, ref uint offsetToStoreAt)
        {
            Assert.That(stateFormat, Is.EqualTo(new FourCC("PART")));
            Assert.That(stateSize, Is.EqualTo(UnsafeUtility.SizeOf<TestDevicePartialState>()));

            var values = (float*)currentStatePtr.ToPointer();
            for (var i = 0; i < 5; ++i)
                if (Mathf.Approximately(values[i], 0))
                {
                    offsetToStoreAt = (uint)i * sizeof(float);
                    return true;
                }
            Assert.Fail();
            return false;
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_DeviceWithStateCallback_CanDecideHowToIntegrateState()
    {
        InputSystem.RegisterControlLayout<TestDeviceDecidingWhereToIntegrateState>();
        var device = InputSystem.AddDevice<TestDeviceDecidingWhereToIntegrateState>();

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.123f });
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));

        InputSystem.QueueStateEvent(device, new TestDevicePartialState { axis = 0.234f });
        InputSystem.Update();

        Assert.That(device["axis0"].ReadValueAsObject(), Is.EqualTo(0.123).Within(0.00001));
        Assert.That(device["axis1"].ReadValueAsObject(), Is.EqualTo(0.234).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanReadStateOfDeviceAsByteArray()
    {
        var device = InputSystem.AddDevice<Gamepad>();

        InputSystem.QueueStateEvent(device, new GamepadState { leftStick = new Vector2(0.123f, 0.456f) });
        InputSystem.Update();

        var state = device.ReadValueAsObject();

        Assert.That(state, Is.TypeOf<byte[]>());
        var buffer = (byte[])state;

        Assert.That(buffer.Length, Is.EqualTo(Marshal.SizeOf(typeof(GamepadState))));

        unsafe
        {
            fixed(byte* bufferPtr = buffer)
            {
                var statePtr = (GamepadState*)bufferPtr;
                Assert.That(statePtr->leftStick.x, Is.EqualTo(0.123).Within(0.00001));
                Assert.That(statePtr->leftStick.y, Is.EqualTo(0.456).Within(0.00001));
            }
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAddLayoutForDeviceThatsAlreadyBeenReported()
    {
        testRuntime.ReportNewInputDevice(new InputDeviceDescription {product = "MyController"}.ToJson());
        InputSystem.Update();

        var json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""device"" : {
                    ""product"" : ""MyController""
                }
            }
        ";

        InputSystem.RegisterControlLayout(json);

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("layout").EqualTo("CustomGamepad").And.TypeOf<Gamepad>());
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchLayoutByDeviceClass()
    {
        testRuntime.ReportNewInputDevice(new InputDeviceDescription {deviceClass = "Touchscreen"}.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).TypeOf<Touchscreen>());

        // Should not try to use a control layout.
        testRuntime.ReportNewInputDevice(new InputDeviceDescription {deviceClass = "Touch"}.ToJson());
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
    }

    // For some devices, we need to discover their setup at runtime and cannot create layouts
    // in advance. HID joysticks are one such case. We want to be able to turn any HID joystick
    // into a Joystick device and accurately represent all the axes and buttons the device
    // actually has. If we couldn't make up layouts on the fly, we would have to have a fallback
    // joystick layout that simply has N buttons and M axes.
    //
    // So, instead we have a callback that tells us when a device has been discovered. We can use
    // this callback to generate a layout on the fly.
    [Test]
    [Category("Devices")]
    public void TODO_Devices_CanDetermineWhichLayoutIsChosenOnDeviceDiscovery()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeRemoved()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad3 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var gamepad2Offset = gamepad2.stateBlock.byteOffset;

        var receivedCalls = 0;
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                ++receivedCalls;
                receivedDevice = device;
                receivedChange = change;
            };

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(InputSystem.devices, Has.Count.EqualTo(2));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad3));
        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDevice, Is.SameAs(gamepad2));
        Assert.That(receivedChange, Is.EqualTo(InputDeviceChange.Removed));
        Assert.That(gamepad2.stateBlock.byteOffset, Is.EqualTo(0)); // Should have lost its offset into state buffers.
        Assert.That(gamepad3.stateBlock.byteOffset, Is.EqualTo(gamepad2Offset)); // 3 should have moved into 2's position.
        Assert.That(gamepad2.leftStick.stateBlock.byteOffset,
            Is.EqualTo(Marshal.OffsetOf(typeof(GamepadState), "leftStick").ToInt32())); // Should have unbaked offsets in control hierarchy.
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeRemoved_ThroughEvents()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");

        var gamepad1WasRemoved = false;
        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                if (device == gamepad1)
                    gamepad1WasRemoved = true;
            };

        var inputEvent = DeviceRemoveEvent.Create(gamepad1.id, Time.time);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad2));
        Assert.That(Gamepad.current, Is.Not.SameAs(gamepad1));
        Assert.That(gamepad1WasRemoved, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenRemoved_DoNotEmergeOnUnsupportedList()
    {
        // Devices added directly via AddDevice() don't end up on the list of
        // available devices. Devices reported by the runtime do.
        testRuntime.ReportNewInputDevice(@"
            {
                ""type"" : ""Gamepad""
            }
        ");

        InputSystem.Update();
        var device = InputSystem.devices[0];

        var inputEvent = DeviceRemoveEvent.Create(device.id, Time.time);
        InputSystem.QueueEvent(ref inputEvent);
        InputSystem.Update();

        var unsupportedDevices = new List<InputDeviceDescription>();
        InputSystem.GetUnsupportedDevices(unsupportedDevices);

        ////TODO: also make sure that when the layout support it is removed, the device goes back on the unsupported list

        Assert.That(unsupportedDevices.Count, Is.Zero);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeReadded()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice("Keyboard");

        InputSystem.RemoveDevice(gamepad);
        InputSystem.AddDevice(gamepad);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Exactly(1).SameAs(gamepad));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5f).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanBeDisabledAndReEnabled()
    {
        var device = InputSystem.AddDevice<Mouse>();

        bool? disabled = null;
        testRuntime.SetDeviceCommandCallback(device.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == DisableDeviceCommand.Type)
                    {
                        Assert.That(disabled, Is.Null);
                        disabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                    if (commandPtr->type == EnableDeviceCommand.Type)
                    {
                        Assert.That(disabled, Is.Null);
                        disabled = false;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }

                return InputDeviceCommand.kGenericFailure;
            });


        Assert.That(device.enabled, Is.True);
        Assert.That(disabled, Is.Null);

        InputSystem.DisableDevice(device);

        Assert.That(device.enabled, Is.False);
        Assert.That(disabled.HasValue, Is.True);
        Assert.That(disabled.Value, Is.True);

        // Make sure that state sent against the device is ignored.
        InputSystem.QueueStateEvent(device, new MouseState { buttons = 0xffff });
        InputSystem.Update();

        Assert.That(device.CheckStateIsAllZeros(), Is.True);

        // Re-enable device.

        disabled = null;
        InputSystem.EnableDevice(device);

        Assert.That(device.enabled, Is.True);
        Assert.That(disabled.HasValue, Is.True);
        Assert.That(disabled.Value, Is.False);
    }

    [Test]
    [Category("Devices")]
    public void Devices_WhenDisabledOrReEnabled_TriggersNotification()
    {
        InputDevice receivedDevice = null;
        InputDeviceChange? receivedChange = null;

        InputSystem.onDeviceChange +=
            (device, change) =>
            {
                receivedDevice = device;
                receivedChange = change;
            };

        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.DisableDevice(mouse);

        Assert.That(receivedDevice, Is.SameAs(mouse));
        Assert.That(receivedChange.Value, Is.EqualTo(InputDeviceChange.Disabled));

        receivedDevice = null;
        receivedChange = null;

        InputSystem.EnableDevice(mouse);

        Assert.That(receivedDevice, Is.SameAs(mouse));
        Assert.That(receivedChange.Value, Is.EqualTo(InputDeviceChange.Enabled));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_WhenDisabled_StateIsReset()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_WhenDisabled_RefreshActions()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_ThatHaveNoKnownLayout_AreDisabled()
    {
        var deviceId = testRuntime.AllocateDeviceId();
        testRuntime.ReportNewInputDevice(new InputDeviceDescription {deviceClass = "TestThing"}.ToJson(), deviceId);

        bool? wasDisabled = null;
        testRuntime.SetDeviceCommandCallback(deviceId,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == DisableDeviceCommand.Type)
                    {
                        Assert.That(wasDisabled, Is.Null);
                        wasDisabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }

                Assert.Fail("Should not get other IOCTLs");
                return InputDeviceCommand.kGenericFailure;
            });

        InputSystem.Update();

        Assert.That(wasDisabled.HasValue);
        Assert.That(wasDisabled.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_ThatHadNoKnownLayout_AreReEnabled_WhenLayoutBecomesKnown()
    {
        var deviceId = testRuntime.AllocateDeviceId();
        testRuntime.ReportNewInputDevice(new InputDeviceDescription {deviceClass = "TestThing"}.ToJson(), deviceId);
        InputSystem.Update();

        bool? wasEnabled = null;
        testRuntime.SetDeviceCommandCallback(deviceId,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == EnableDeviceCommand.Type)
                    {
                        Assert.That(wasEnabled, Is.Null);
                        wasEnabled = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }

                Assert.Fail("Should not get other IOCTLs");
                return InputDeviceCommand.kGenericFailure;
            });

        InputSystem.RegisterControlLayout<Mouse>(matches: new InputDeviceMatcher().WithDeviceClass("TestThing"));

        Assert.That(wasEnabled.HasValue);
        Assert.That(wasEnabled.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_QueryTheirEnabledStateFromRuntime()
    {
        var deviceId = testRuntime.AllocateDeviceId();

        var queryEnabledStateResult = false;
        bool? receivedQueryEnabledStateCommand = null;
        testRuntime.SetDeviceCommandCallback(deviceId,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QueryEnabledStateCommand.Type)
                    {
                        Assert.That(receivedQueryEnabledStateCommand, Is.Null);
                        receivedQueryEnabledStateCommand = true;
                        ((QueryEnabledStateCommand*)commandPtr)->isEnabled = queryEnabledStateResult;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }

                Assert.Fail("Should not get other IOCTLs");
                return InputDeviceCommand.kGenericFailure;
            });

        testRuntime.ReportNewInputDevice(new InputDeviceDescription {deviceClass = "Mouse"}.ToJson(), deviceId);
        InputSystem.Update();
        var device = InputSystem.devices.First(x => x.id == deviceId);

        var isEnabled = device.enabled;

        Assert.That(isEnabled, Is.False);
        Assert.That(receivedQueryEnabledStateCommand, Is.Not.Null);
        Assert.That(receivedQueryEnabledStateCommand.Value, Is.True);

        receivedQueryEnabledStateCommand = null;
        queryEnabledStateResult = true;

        // A configuration change event should cause the cached state to become invalid
        // and thus cause InputDevice.enabled to issue another IOCTL.
        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();

        isEnabled = device.enabled;

        Assert.That(isEnabled, Is.True);
        Assert.That(receivedQueryEnabledStateCommand, Is.Not.Null);
        Assert.That(receivedQueryEnabledStateCommand.Value, Is.True);

        // Make sure that querying the state *again* does not lead to another IOCTL.

        receivedQueryEnabledStateCommand = null;

        isEnabled = device.enabled;

        Assert.That(isEnabled, Is.True);
        Assert.That(receivedQueryEnabledStateCommand, Is.Null);
    }

    [Test]
    [Category("Devices")]
    public void Devices_NativeDevicesAreFlaggedAsSuch()
    {
        var description = new InputDeviceDescription {deviceClass = "Gamepad"};
        var deviceId = testRuntime.ReportNewInputDevice(description.ToJson());

        InputSystem.Update();

        var device = InputSystem.TryGetDeviceById(deviceId);

        Assert.That(device, Is.Not.Null);
        Assert.That(device.native, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_DisplayNameDefaultsToProductName()
    {
        var device = InputSystem.AddDevice(new InputDeviceDescription
        {
            deviceClass = "Gamepad",
            product = "Product Name"
        });

        Assert.That(device.displayName, Is.EqualTo("Product Name"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAssociateUserIdWithDevice()
    {
        var device = InputSystem.AddDevice<Gamepad>();
        string userId = null;

        testRuntime.SetDeviceCommandCallback(device.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QueryUserIdCommand.Type)
                    {
                        var queryUserIdPtr = (QueryUserIdCommand*)commandPtr;
                        StringHelpers.WriteStringToBuffer(userId, new IntPtr(queryUserIdPtr->idBuffer),
                            QueryUserIdCommand.kMaxIdLength);
                        return 1;
                    }
                }

                return InputDeviceCommand.kGenericFailure;
            });

        Assert.That(device.userId, Is.Null);

        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();
        Assert.That(device.userId, Is.Null);

        userId = "testId";
        InputSystem.QueueConfigChangeEvent(device);
        InputSystem.Update();
        Assert.That(device.userId, Is.EqualTo(userId));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanPauseResumeAndResetHapticsOnAllDevices()
    {
        InputSystem.AddDevice<Gamepad>();
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

        DualMotorRumbleCommand? receivedCommand = null;
        testRuntime.SetDeviceCommandCallback(gamepad.id,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }
                Assert.Fail();
                return InputDeviceCommand.kGenericFailure;
            });

        InputSystem.PauseHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));

        receivedCommand = null;
        InputSystem.ResumeHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        InputSystem.ResetHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public unsafe void Devices_CanRumbleGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        DualMotorRumbleCommand? receivedCommand = null;
        testRuntime.SetDeviceCommandCallback(gamepad.id,
            (deviceId, command) =>
            {
                if (command->type == DualMotorRumbleCommand.Type)
                {
                    Assert.That(receivedCommand.HasValue, Is.False);
                    receivedCommand = *((DualMotorRumbleCommand*)command);
                    return 1;
                }
                Assert.Fail();
                return InputDeviceCommand.kGenericFailure;
            });

        gamepad.SetMotorSpeeds(0.1234f, 0.5678f);

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        gamepad.PauseHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));

        receivedCommand = null;
        gamepad.ResumeHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.EqualTo(0.5678).Within(0.000001));

        receivedCommand = null;
        gamepad.ResetHaptics();

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.lowFrequencyMotorSpeed, Is.Zero.Within(0.000001));
        Assert.That(receivedCommand.Value.highFrequencyMotorSpeed, Is.Zero.Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanQueryAllGamepadsWithSimpleGetter()
    {
        var gamepad1 = InputSystem.AddDevice<Gamepad>();
        var gamepad2 = InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        Assert.That(Gamepad.all, Has.Count.EqualTo(2));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad2));

        var gamepad3 = InputSystem.AddDevice<Gamepad>();

        Assert.That(Gamepad.all, Has.Count.EqualTo(3));
        Assert.That(Gamepad.all, Has.Exactly(1).SameAs(gamepad3));

        InputSystem.RemoveDevice(gamepad2);

        Assert.That(Gamepad.all, Has.Count.EqualTo(2));
        Assert.That(Gamepad.all, Has.None.SameAs(gamepad2));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCreateGenericJoystick()
    {
        var json = @"
            {
                ""name"" : ""MyJoystick"",
                ""extend"" : ""Joystick"",
                ""controls"" : [
                    { ""name"" : ""button1"", ""layout"" : ""Button"" },
                    { ""name"" : ""button2"", ""layout"" : ""Button"" },
                    { ""name"" : ""axis1"", ""layout"" : ""Axis"" },
                    { ""name"" : ""axis2"", ""layout"" : ""Axis"" },
                    { ""name"" : ""discrete"", ""layout"" : ""Digital"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var device = InputSystem.AddDevice("MyJoystick");

        Assert.That(device, Is.TypeOf<Joystick>());
        Assert.That(Joystick.current, Is.SameAs(device));

        var joystick = (Joystick)device;

        Assert.That(joystick.axes, Has.Count.EqualTo(4)); // Includes stick.
        Assert.That(joystick.buttons, Has.Count.EqualTo(3)); // Includes trigger.
        Assert.That(joystick.trigger.name, Is.EqualTo("trigger"));
        Assert.That(joystick.stick.name, Is.EqualTo("stick"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltasResetBetweenUpdates()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        InputSystem.Update();

        Assert.That(pointer.delta.x.ReadValue(), Is.Zero);
        Assert.That(pointer.delta.y.ReadValue(), Is.Zero);
    }

    // The whole dynamic vs fixed vs before-render vs editor update mechanic is a can of worms. In the
    // ECS version, all this should be thrown out entirely.
    //
    // This test here is another peculiarity we need to watch out for. Events received by the system will
    // get written into either player or editor buffers. If written into player buffers, they get written
    // into *both* dynamic and fixed update buffers. However, depending on what the *current* update is
    // (fixed or dynamic) this means we are writing state into the *next* update of the other type. E.g.
    // when receiving state during fixed update, we write it both into the current fixed update and into
    // the *next* dynamic update.
    //
    // For the reset logic, this means we have to be extra careful to not overwrite that state we've written
    // "for the future".
    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltaUpdatedInFixedUpdate_DoesNotGetResetInDynamicUpdate()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update(InputUpdateType.Fixed);

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_PointerDeltasAccumulateBetweenUpdates()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(pointer.delta.x.ReadValue(), Is.EqualTo(1).Within(0.0000001));
        Assert.That(pointer.delta.y.ReadValue(), Is.EqualTo(1).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanAdjustSensitivityOnPointerDeltas()
    {
        var pointer = InputSystem.AddDevice<Pointer>();

        const float kWindowWidth = 640f;
        const float kWindowHeight = 480f;
        const float kSensitivity = 6f;

        InputConfiguration.PointerDeltaSensitivity = kSensitivity;

        testRuntime.SetDeviceCommandCallback(pointer.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QueryDimensionsCommand.Type)
                    {
                        var windowDimensionsCommand = (QueryDimensionsCommand*)commandPtr;
                        windowDimensionsCommand->outDimensions = new Vector2(kWindowWidth, kWindowHeight);
                        return InputDeviceCommand.kGenericSuccess;
                    }

                    return InputDeviceCommand.kGenericFailure;
                }
            });

        InputSystem.QueueStateEvent(pointer, new PointerState { delta = new Vector2(32f, 64f) });
        InputSystem.Update();

        // NOTE: Whereas the tests above access .delta.x.value and .delta.y.value, here we access
        //       delta.value.x and delta.value.y. This is because the sensitivity processor sits
        //       on the vector control and not on the individual component axes.

        Assert.That(pointer.delta.ReadValue().x, Is.EqualTo(32 / kWindowWidth * kSensitivity).Within(0.00001));
        Assert.That(pointer.delta.ReadValue().y, Is.EqualTo(64 / kWindowHeight * kSensitivity).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    [TestCase("Gamepad", typeof(Gamepad))]
    [TestCase("Keyboard", typeof(Keyboard))]
    [TestCase("Pointer", typeof(Pointer))]
    [TestCase("Mouse", typeof(Mouse))]
    [TestCase("Pen", typeof(Pen))]
    [TestCase("Touchscreen", typeof(Touchscreen))]
    [TestCase("Joystick", typeof(Joystick))]
    [TestCase("Accelerometer", typeof(Accelerometer))]
    [TestCase("Gyroscope", typeof(Gyroscope))]
    public void Devices_CanCreateDevice(string layout, Type type)
    {
        var device = InputSystem.AddDevice(layout);

        Assert.That(device, Is.InstanceOf<InputDevice>());
        Assert.That(device.layout, Is.EqualTo(layout));
        Assert.That(device, Is.TypeOf(type));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanCheckAnyKeyOnKeyboard()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();

        Assert.That(keyboard.anyKey.isPressed, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetTextInputFromKeyboard()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        var textReceived = "";
        keyboard.onTextInput += ch => textReceived += ch;

        InputSystem.QueueTextEvent(keyboard, 'a');
        InputSystem.QueueTextEvent(keyboard, 'b');
        InputSystem.QueueTextEvent(keyboard, 'c');

        InputSystem.Update();

        Assert.That(textReceived, Is.EqualTo("abc"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetDisplayNameFromKeyboardKey()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var currentLayoutName = "default";
        testRuntime.SetDeviceCommandCallback(keyboard.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QueryKeyNameCommand.Type)
                    {
                        var keyNameCommand = (QueryKeyNameCommand*)commandPtr;

                        var scanCode = 0x02;
                        var name = "other";

                        if (keyNameCommand->scanOrKeyCode == (int)Key.A)
                        {
                            scanCode = 0x01;
                            name = currentLayoutName == "default" ? "m" : "q";
                        }

                        keyNameCommand->scanOrKeyCode = scanCode;
                        StringHelpers.WriteStringToBuffer(name, (IntPtr)keyNameCommand->nameBuffer,
                            QueryKeyNameCommand.kMaxNameLength);

                        return QueryKeyNameCommand.kSize;
                    }

                    return InputDeviceCommand.kGenericFailure;
                }
            });

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("m"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("other"));

        // Change layout.
        currentLayoutName = "other";
        InputSystem.QueueConfigChangeEvent(keyboard);
        InputSystem.Update();

        Assert.That(keyboard.aKey.displayName, Is.EqualTo("q"));
        Assert.That(keyboard.bKey.displayName, Is.EqualTo("other"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetNameOfCurrentKeyboardLayout()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var currentLayoutName = "default";
        testRuntime.SetDeviceCommandCallback(keyboard.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QueryKeyboardLayoutCommand.Type)
                    {
                        var layoutCommand = (QueryKeyboardLayoutCommand*)commandPtr;
                        if (StringHelpers.WriteStringToBuffer(currentLayoutName, (IntPtr)layoutCommand->nameBuffer,
                                QueryKeyboardLayoutCommand.kMaxNameLength))
                            return QueryKeyboardLayoutCommand.kMaxNameLength;
                    }

                    return InputDeviceCommand.kGenericFailure;
                }
            });

        Assert.That(keyboard.keyboardLayout, Is.EqualTo("default"));

        currentLayoutName = "new";
        InputSystem.QueueConfigChangeEvent(keyboard);
        InputSystem.Update();

        Assert.That(keyboard.keyboardLayout, Is.EqualTo("new"));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetKeyCodeFromKeyboardKey()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        Assert.That(keyboard.aKey.keyCode, Is.EqualTo(Key.A));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanLookUpKeyFromKeyboardUsingKeyCode()
    {
        var keyboard = (Keyboard)InputSystem.AddDevice("Keyboard");

        Assert.That(keyboard[Key.A], Is.SameAs(keyboard.aKey));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanPerformHorizontalAndVerticalScrollWithMouse()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        InputSystem.QueueDeltaStateEvent(mouse.scroll, new Vector2(10, 12));
        InputSystem.Update();

        Assert.That(mouse.scroll.x.ReadValue(), Is.EqualTo(10).Within(0.0000001));
        Assert.That(mouse.scroll.y.ReadValue(), Is.EqualTo(12).Within(0.0000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanWarpMousePosition()
    {
        var mouse = InputSystem.AddDevice<Mouse>();

        WarpMousePositionCommand? receivedCommand = null;
        testRuntime.SetDeviceCommandCallback(mouse.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == WarpMousePositionCommand.Type)
                    {
                        Assert.That(receivedCommand.HasValue, Is.False);
                        receivedCommand = *((WarpMousePositionCommand*)commandPtr);
                        return 1;
                    }

                    Assert.Fail();
                    return InputDeviceCommand.kGenericFailure;
                }
            });

        mouse.WarpCursorPosition(new Vector2(0.1234f, 0.5678f));

        Assert.That(receivedCommand.HasValue, Is.True);
        Assert.That(receivedCommand.Value.warpPositionInPlayerDisplaySpace.x, Is.EqualTo(0.1234).Within(0.000001));
        Assert.That(receivedCommand.Value.warpPositionInPlayerDisplaySpace.y, Is.EqualTo(0.5678).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenCanFunctionAsPointer()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Primary touch functions as pointing element of touchscreen.

        InputSystem.QueueDeltaStateEvent(device.primaryTouch,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(0.123f, 0.456f)
        });
        InputSystem.Update();

        Assert.That(device.pointerId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));
        Assert.That(device.phase.ReadValue(), Is.EqualTo(PointerPhase.Began));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenReturnsActiveAndJustEndedTouches()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        Assert.That(device.activeTouches.Count, Is.Zero);
        Assert.That(device.allTouchControls.Count, Is.EqualTo(TouchscreenState.kMaxTouches));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(0.123f, 0.456f)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(1));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));
        Assert.That(device.activeTouches[0].position.x.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(device.activeTouches[0].position.y.ReadValue(), Is.EqualTo(0.456).Within(0.000001));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(0.123f, 0.456f)
        });
        InputSystem.QueueDeltaStateEvent(device.allTouchControls[1],
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 5,
            position = new Vector2(0.789f, 0.123f)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[1].touchId.ReadValue(), Is.EqualTo(5));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));
        Assert.That(device.activeTouches[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        // No change. Touches should become stationary and stay in list.
        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.activeTouches[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.activeTouches[1].touchId.ReadValue(), Is.EqualTo(5));
        Assert.That(device.activeTouches[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));
        Assert.That(device.activeTouches[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        InputSystem.QueueDeltaStateEvent(device.allTouchControls[0],
            new TouchState
        {
            phase = PointerPhase.Ended,
            touchId = 4,
        });
        InputSystem.QueueDeltaStateEvent(device.allTouchControls[1],
            new TouchState
        {
            phase = PointerPhase.Cancelled,
            touchId = 5,
        });
        InputSystem.Update();

        // For one frame, the ended and cancelled touches should stick around on the active touches list

        Assert.That(device.activeTouches.Count, Is.EqualTo(2));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.Cancelled));

        // But then they should disappear from the list.

        InputSystem.Update();

        Assert.That(device.activeTouches.Count, Is.Zero);
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
        Assert.That(device.allTouchControls[1].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    ////REVIEW: if we allow this, InputControl.ReadValueFrom() is in trouble
    ////        (actually, is this true? TouchControl should be able to read a state event like here just fine)
    // Touchscreen is somewhat special in that treats its available TouchState slots like a pool
    // from which it dynamically assigns entries to track individual touches.
    [Test]
    [Category("Devices")]
    public void Devices_TouchscreenDynamicallyAllocatesTouchStates()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 5,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[1].touchId.ReadValue(), Is.EqualTo(5));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchStaysOnSameControlForDurationOfTouch()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        // Begin touch.
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // Move.
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Moved));

        // Don't move.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // Random unrelated touch.
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 5,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));

        // End.
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Ended,
            touchId = 4,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].touchId.ReadValue(), Is.EqualTo(4));
        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Ended));

        // Release.
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.None));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchesBecomeStationaryWhenNotMovedInFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
        });
        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Began));

        InputSystem.Update();

        Assert.That(device.allTouchControls[0].phase.ReadValue(), Is.EqualTo(PointerPhase.Stationary));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasAreComputedAutomatically()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(10, 20)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Ended,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsStationary()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(10, 20)
        });
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(10).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(20).Within(0.00001));

        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchDeltasResetWhenTouchIsMovingInPlace()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(10, 20)
        });
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.Update();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(0).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_TouchesAccumulateDeltasWithinFrame()
    {
        var device = InputSystem.AddDevice<Touchscreen>();

        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Began,
            touchId = 4,
            position = new Vector2(10, 20)
        });
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(20, 40)
        });
        InputSystem.QueueStateEvent(device,
            new TouchState
        {
            phase = PointerPhase.Moved,
            touchId = 4,
            position = new Vector2(30, 50)
        });
        InputSystem.Update();

        Assert.That(device.activeTouches[0].delta.x.ReadValue(), Is.EqualTo(20).Within(0.00001));
        Assert.That(device.activeTouches[0].delta.y.ReadValue(), Is.EqualTo(30).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_TouchControlCanReadTouchStateEventForTouchscreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CannotChangeStateLayoutOfTouchControls()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void TODO_Devices_CannotChangeStateLayoutOfTouchscreen()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetSensorSamplingFrequency()
    {
        var sensor = InputSystem.AddDevice<Accelerometer>();

        bool? receivedQueryFrequencyCommand = null;
        testRuntime.SetDeviceCommandCallback(sensor.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == QuerySamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedQueryFrequencyCommand, Is.Null);
                        receivedQueryFrequencyCommand = true;
                        ((QuerySamplingFrequencyCommand*)commandPtr)->frequency = 120.0f;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }
                return InputDeviceCommand.kGenericFailure;
            });

        Assert.That(sensor.samplingFrequency, Is.EqualTo(120.0).Within(0.000001));
        Assert.That(receivedQueryFrequencyCommand, Is.Not.Null);
        Assert.That(receivedQueryFrequencyCommand.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanSetSensorSamplingFrequency()
    {
        var sensor = InputSystem.AddDevice<Accelerometer>();

        bool? receivedSetFrequencyCommand = null;
        testRuntime.SetDeviceCommandCallback(sensor.id,
            (id, commandPtr) =>
            {
                unsafe
                {
                    if (commandPtr->type == SetSamplingFrequencyCommand.Type)
                    {
                        Assert.That(receivedSetFrequencyCommand, Is.Null);
                        receivedSetFrequencyCommand = true;
                        return InputDeviceCommand.kGenericSuccess;
                    }
                }
                return InputDeviceCommand.kGenericFailure;
            });

        sensor.samplingFrequency = 30.0f;

        Assert.That(receivedSetFrequencyCommand, Is.Not.Null);
        Assert.That(receivedSetFrequencyCommand.Value, Is.True);
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetAccelerometerReading()
    {
        var accelerometer = InputSystem.AddDevice<Accelerometer>();

        InputSystem.QueueStateEvent(accelerometer, new AccelerometerState { acceleration = new Vector3(0.123f, 0.456f, 0.789f) });
        InputSystem.Update();

        Assert.That(Accelerometer.current, Is.SameAs(accelerometer));

        Assert.That(accelerometer.acceleration.ReadValue().x, Is.EqualTo(0.123).Within(0.00001));
        Assert.That(accelerometer.acceleration.ReadValue().y, Is.EqualTo(0.456).Within(0.00001));
        Assert.That(accelerometer.acceleration.ReadValue().z, Is.EqualTo(0.789).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGyroReading()
    {
        var gyro = InputSystem.AddDevice<Gyroscope>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(gyro, new GyroscopeState {angularVelocity = value});
        InputSystem.Update();

        Assert.That(Gyroscope.current, Is.SameAs(gyro));
        Assert.That(gyro.angularVelocity.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetGravityReading()
    {
        var sensor = InputSystem.AddDevice<Gravity>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new GravityState { gravity = value });
        InputSystem.Update();

        Assert.That(Gravity.current, Is.SameAs(sensor));
        Assert.That(sensor.gravity.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetAttitudeReading()
    {
        var sensor = InputSystem.AddDevice<Attitude>();
        var value = new Quaternion(0.987f, 0.654f, 0.321f, 0.5f);
        InputSystem.QueueStateEvent(sensor, new AttitudeState { attitude = value });
        InputSystem.Update();

        Assert.That(Attitude.current, Is.SameAs(sensor));
        Assert.That(sensor.attitude.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanGetLinearAccelerationReading()
    {
        var sensor = InputSystem.AddDevice<LinearAcceleration>();
        var value = new Vector3(0.987f, 0.654f, 0.321f);
        InputSystem.QueueStateEvent(sensor, new LinearAccelerationState { acceleration = value });
        InputSystem.Update();

        Assert.That(LinearAcceleration.current, Is.SameAs(sensor));
        Assert.That(sensor.acceleration.ReadValue(), Is.EqualTo(value).Within(0.00001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchDeviceDescriptions()
    {
        var matchOne = new InputDeviceMatcher()
            .WithInterface("TestInterface");
        var matchTwo = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithDeviceClass("TestDeviceClass");
        var matchOneWithRegex = new InputDeviceMatcher()
            .WithInterface(".*Interface");
        var matchAll = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithDeviceClass("TestDeviceClass")
            .WithManufacturer("TestManufacturer")
            .WithProduct("TestProduct")
            .WithVersion(@"1\.0");
        var matchNone = new InputDeviceMatcher()
            .WithInterface("Test")
            .WithDeviceClass("Test")
            .WithManufacturer("Test");
        var matchMoreThanItHas = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithCapability("canDo", true);

        var description = new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            deviceClass = "TestDeviceClass",
            product = "TestProduct",
            manufacturer = "TestManufacturer",
            version = "1.0",
        };

        Assert.That(matchOne.MatchPercentage(description), Is.EqualTo(1.0f / 5).Within(0.0001));
        Assert.That(matchTwo.MatchPercentage(description), Is.EqualTo(1.0f / 5 * 2).Within(0.0001));
        Assert.That(matchOneWithRegex.MatchPercentage(description), Is.EqualTo(1.0f / 5).Within(0.0001));
        Assert.That(matchAll.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchNone.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
        Assert.That(matchMoreThanItHas.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
    }

    [Serializable]
    struct TestDeviceCapabilities
    {
        public string stringCap;
        public int intCap;
        public float floatCap;
        public bool boolCap;
        public NestedCaps nestedCap;
        public string[] arrayCap;
        public EnumCaps enumCap;

        [Serializable]
        public struct NestedCaps
        {
            public string value;
        }

        [Serializable]
        public enum EnumCaps
        {
            First,
            Second
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanMatchDeviceDescriptions_WithCapabilities()
    {
        var matchOneAndOneCap = new InputDeviceMatcher()
            .WithInterface("TestInterface")
            .WithCapability("stringCap", "string");
        var matchOneCapInArray = new InputDeviceMatcher()
            .WithCapability("arrayCap[]", "second");
        var matchOneIntAndOneFloatCap = new InputDeviceMatcher()
            .WithCapability("floatCap", 1.234f)
            .WithCapability("intCap", 1234);
        var matchBoolCap = new InputDeviceMatcher()
            .WithCapability("boolCap", true);
        var matchEnumCap = new InputDeviceMatcher()
            .WithCapability("enumCap", TestDeviceCapabilities.EnumCaps.Second);
        var matchNestedCap = new InputDeviceMatcher()
            .WithCapability("nestedCap/value", "value");
        var matchStringCapWithRegex = new InputDeviceMatcher()
            .WithCapability("stringCap", ".*ng$");
        var matchIntCapWithRegex = new InputDeviceMatcher()
            .WithCapability("intCap", "1.*4");
        var matchIntCapWithString = new InputDeviceMatcher()
            .WithCapability("intCap", "1234");
        var matchFloatCapWithString = new InputDeviceMatcher()
            .WithCapability("floatCap", "1.234");
        var matchBoolCapWithString = new InputDeviceMatcher()
            .WithCapability("boolCap", "true");
        var matchNone = new InputDeviceMatcher()
            .WithCapability("intCap", 4567);

        var description = new InputDeviceDescription
        {
            interfaceName = "TestInterface",
            capabilities = new TestDeviceCapabilities
            {
                stringCap = "string",
                intCap = 1234,
                floatCap = 1.234f,
                boolCap = true,
                nestedCap = new TestDeviceCapabilities.NestedCaps
                {
                    value = "value"
                },
                arrayCap = new[] { "first", "second" },
                enumCap = TestDeviceCapabilities.EnumCaps.Second,
            }.ToJson()
        };

        Assert.That(matchOneAndOneCap.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchOneCapInArray.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchOneIntAndOneFloatCap.MatchPercentage(description), Is.EqualTo(1).Within(0.0001));
        Assert.That(matchBoolCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchEnumCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchNestedCap.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchStringCapWithRegex.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchIntCapWithRegex.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchIntCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchFloatCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchBoolCapWithString.MatchPercentage(description), Is.EqualTo(1 / 2.0).Within(0.0001));
        Assert.That(matchNone.MatchPercentage(description), Is.EqualTo(0).Within(0.0001));
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
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/Gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByExactPathCaseInsensitive()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamePAD/LeftSTICK");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{Primary2DMotion}");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindChildControlsOfControlsFoundByUsage()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/{Primary2DMotion}/x");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick.x));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByLayout()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/gamepad/<stick>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Devices")]
    public void Devices_CanFindDevicesByLayouts()
    {
        var gamepad1 = InputSystem.AddDevice("Gamepad");
        var gamepad2 = InputSystem.AddDevice("Gamepad");
        InputSystem.AddDevice("Keyboard");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(2));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2));
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

        InputSystem.RegisterControlLayout(json);
        var device = InputSystem.AddDevice("MyGamepad");

        var matches = InputSystem.GetControls("/<gamepad>");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(device));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsFromMultipleDevices()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var matches = InputSystem.GetControls("/*/*Stick");

        Assert.That(matches, Has.Count.EqualTo(4));

        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad1.rightStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.leftStick));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad2.rightStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanOmitLeadingSlashWhenFindingControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("gamepad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsByTheirAliases()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matchByName = InputSystem.GetControls("/gamepad/buttonSouth");
        var matchByAlias1 = InputSystem.GetControls("/gamepad/a");
        var matchByAlias2 = InputSystem.GetControls("/gamepad/cross");

        Assert.That(matchByName, Has.Count.EqualTo(1));
        Assert.That(matchByName, Has.Exactly(1).SameAs(gamepad.buttonSouth));
        Assert.That(matchByAlias1, Is.EqualTo(matchByName));
        Assert.That(matchByAlias2, Is.EqualTo(matchByName));
    }

    [Test]
    [Category("Controls")]
    public void Controls_CanFindControlsUsingWildcardsInMiddleOfNames()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var matches = InputSystem.GetControls("/g*pad/leftStick");

        Assert.That(matches, Has.Count.EqualTo(1));
        Assert.That(matches, Has.Exactly(1).SameAs(gamepad.leftStick));
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

        InputSystem.RegisterControlLayout(json);
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

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var control = setup.GetControl("control");

        Assert.That(control.displayName, Is.EqualTo("control"));
    }

    // This is one of the most central tests. If this one breaks, it most often
    // hints at the state layouting or state updating machinery being borked.
    [Test]
    [Category("Events")]
    public void Events_CanUpdateStateOfDeviceWithEvent()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.123f));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.456f));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdatePartialStateOfDeviceWithEvent()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Full state update to make sure we won't be overwriting other
        // controls with state. Also, make sure we actually carry over
        // those values on buffer flips.
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 0xffffffff, rightStick = Vector2.one, leftTrigger = 0.123f, rightTrigger = 0.456f });
        InputSystem.Update();

        // Update just left stick.
        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, new Vector2(0.5f, 0.5f));
        InputSystem.Update();

        Assert.That(gamepad.leftStick.x.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftStick.y.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123).Within(0.000001));
        Assert.That(gamepad.rightStick.x.ReadValue(), Is.EqualTo(1).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateEventToDevice_MakesItCurrent()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        // Adding a device makes it current so add another one so that .current
        // is not already set to the gamepad we just created.
        InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(Gamepad.current, Is.SameAs(gamepad));
    }

    [Test]
    [Category("Events")]
    public void TODO_Events_SendingStateEvent_WithOnlyNoise_DoesNotMakeDeviceCurrent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithoutBeforeRenderEnabled_DoesNothingInBeforeRenderUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var newState = new GamepadState { leftStick = new Vector2(0.123f, 0.456f) };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftStick.ReadValue(), Is.EqualTo(default(Vector2)));
    }

    [Test]
    [Category("Events")]
    public void Events_SendingStateToDeviceWithBeforeRenderEnabled_UpdatesDeviceInBeforeRender()
    {
        const string deviceJson = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterControlLayout(deviceJson);

        var gamepad = (Gamepad)InputSystem.AddDevice("CustomGamepad");
        var newState = new GamepadState { leftTrigger = 0.123f };

        InputSystem.QueueStateEvent(gamepad, newState);
        InputSystem.Update(InputUpdateType.BeforeRender);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.123f).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanListenToEventStream()
    {
        var device = InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputSystem.onEvent += inputEvent =>
            {
                ++receivedCalls;
                Assert.That(inputEvent.IsA<StateEvent>(), Is.True);
                Assert.That(inputEvent.deviceId, Is.EqualTo(device.id));
            };

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    // Should be possible to have a pointer to a state event and from it, return
    // the list of controls that have non-default values.
    // Probably makes sense to also be able to return from it a list of changed
    // controls by comparing it to a device's current state.
    [Test]
    [Category("Events")]
    public void TODO_Events_CanFindActiveControlsFromStateEvent()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Events")]
    public void Events_AreProcessedInOrderTheyAreQueuedIn()
    {
        const double kFirstTime = 0.5;
        const double kSecondTime = 1.5;
        const double kThirdTime = 2.5;

        var receivedCalls = 0;
        var receivedFirstTime = 0.0;
        var receivedSecondTime = 0.0;
        var receivedThirdTime = 0.0;

        InputSystem.onEvent +=
            inputEvent =>
            {
                ++receivedCalls;
                if (receivedCalls == 1)
                    receivedFirstTime = inputEvent.time;
                else if (receivedCalls == 2)
                    receivedSecondTime = inputEvent.time;
                else
                    receivedThirdTime = inputEvent.time;
            };

        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState(), kSecondTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kFirstTime);
        InputSystem.QueueStateEvent(device, new GamepadState(), kThirdTime);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(3));
        Assert.That(receivedFirstTime, Is.EqualTo(kSecondTime).Within(0.00001));
        Assert.That(receivedSecondTime, Is.EqualTo(kFirstTime).Within(0.00001));
        Assert.That(receivedThirdTime, Is.EqualTo(kThirdTime).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanQueueAndReceiveEventsAgainstNonExistingDevices()
    {
        // Device IDs are looked up only *after* the system shows the event to us.

        var receivedCalls = 0;
        var receivedDeviceId = InputDevice.kInvalidDeviceId;
        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                receivedDeviceId = eventPtr.deviceId;
            };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);
        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedDeviceId, Is.EqualTo(4));
    }

    [Test]
    [Category("Events")]
    public void Events_HandledFlagIsResetWhenEventIsQueued()
    {
        var receivedCalls = 0;
        var wasHandled = true;

        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                wasHandled = eventPtr.handled;
            };

        var inputEvent = DeviceConfigurationEvent.Create(4, 1.0);

        // This should go back to false when we inputEvent goes on the queue.
        // The way the behavior is implemented is a side-effect of how we store
        // the handled flag as a bit on the event ID -- which will get set by
        // native on an event when it is queued.
        inputEvent.baseEvent.handled = true;

        InputSystem.QueueEvent(ref inputEvent);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(wasHandled, Is.False);
    }

    [Test]
    [Category("Events")]
    public void Events_AlreadyHandledEventsAreIgnoredWhenProcessingEvents()
    {
        // Need a device with before render enabled so we can produce
        // the effect of having already handled events in the event queue.
        // If we use an invalid device, before render updates will simply
        // ignore the event.
        const string json = @"
            {
                ""name"" : ""CustomGamepad"",
                ""extend"" : ""Gamepad"",
                ""beforeRender"" : ""Update""
            }
        ";

        InputSystem.RegisterControlLayout(json);
        var device = InputSystem.AddDevice("CustomGamepad");

        InputSystem.onEvent +=
            inputEvent =>
            {
                inputEvent.handled = true;
            };

        var event1 = DeviceConfigurationEvent.Create(device.id, 1.0);
        var event2 = DeviceConfigurationEvent.Create(device.id, 2.0);

        InputSystem.QueueEvent(ref event1);

        // Before render update won't clear queue so after the update
        // event1 is still in there.
        InputSystem.Update(InputUpdateType.BeforeRender);

        // Add new unhandled event.
        InputSystem.QueueEvent(ref event2);

        var receivedCalls = 0;
        var receivedTime = 0.0;

        InputSystem.onEvent +=
            inputEvent =>
            {
                ++receivedCalls;
                receivedTime = inputEvent.time;
            };
        InputSystem.Update();

        // On the second update, we should have seen only event2.
        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedTime, Is.EqualTo(2.0).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanPreventEventsFromBeingProcessed()
    {
        InputSystem.onEvent +=
            inputEvent =>
            {
                // If we mark the event handled, the system should skip it and not
                // let it go to the device.
                inputEvent.handled = true;
            };

        var device = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState { rightTrigger = 0.45f });
        InputSystem.Update();

        Assert.That(device.rightTrigger.ReadValue(), Is.EqualTo(0.0).Within(0.00001));
    }

    [Test]
    [Category("Events")]
    public unsafe void Events_CanTraceEventsOfDevice()
    {
        var device = InputSystem.AddDevice("Gamepad");
        var noise = InputSystem.AddDevice("Gamepad");

        using (var trace = new InputEventTrace {deviceId = device.id})
        {
            trace.Enable();
            Assert.That(trace.enabled, Is.True);

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(noise, new GamepadState()); // This one just to make sure we don't get it.

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));

            Assert.That(events[0].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[0].deviceId, Is.EqualTo(device.id));
            Assert.That(events[0].time, Is.EqualTo(0.5).Within(0.000001));
            Assert.That(events[0].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref firstState),
                    StateEvent.From(events[0])->state.ToPointer(), UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);

            Assert.That(events[1].type, Is.EqualTo((FourCC)StateEvent.Type));
            Assert.That(events[1].deviceId, Is.EqualTo(device.id));
            Assert.That(events[1].time, Is.EqualTo(1.5).Within(0.000001));
            Assert.That(events[1].sizeInBytes, Is.EqualTo(StateEvent.GetEventSizeWithPayload<GamepadState>()));
            Assert.That(UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref secondState),
                    StateEvent.From(events[1])->state.ToPointer(), UnsafeUtility.SizeOf<GamepadState>()), Is.Zero);
        }
    }

    [Test]
    [Category("Events")]
    public void Events_WhenTraceIsFull_WillStartOverwritingOldEvents()
    {
        var device = InputSystem.AddDevice("Gamepad");
        using (var trace =
                   new InputEventTrace(StateEvent.GetEventSizeWithPayload<GamepadState>() * 2) {deviceId = device.id})
        {
            trace.Enable();

            var firstState = new GamepadState {rightTrigger = 0.35f};
            var secondState = new GamepadState {leftTrigger = 0.75f};
            var thirdState = new GamepadState {leftTrigger = 0.95f};

            InputSystem.QueueStateEvent(device, firstState, 0.5);
            InputSystem.QueueStateEvent(device, secondState, 1.5);
            InputSystem.QueueStateEvent(device, thirdState, 2.5);

            InputSystem.Update();

            trace.Disable();

            var events = trace.ToList();

            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(1.5).Within(0.000001));
            Assert.That(events, Has.Exactly(1).With.Property("time").EqualTo(2.5).Within(0.000001));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_CanClearEventTrace()
    {
        using (var trace = new InputEventTrace())
        {
            trace.Enable();

            var device = InputSystem.AddDevice("Gamepad");
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.QueueStateEvent(device, new GamepadState());
            InputSystem.Update();

            Assert.That(trace.ToList(), Has.Count.EqualTo(2));

            trace.Clear();

            Assert.That(trace.ToList(), Has.Count.EqualTo(0));
        }
    }

    [Test]
    [Category("Events")]
    public void Events_GetUniqueIds()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.QueueStateEvent(device, new GamepadState());

        var receivedCalls = 0;
        var firstId = InputEvent.kInvalidId;
        var secondId = InputEvent.kInvalidId;

        InputSystem.onEvent +=
            eventPtr =>
            {
                ++receivedCalls;
                if (receivedCalls == 1)
                    firstId = eventPtr.id;
                else if (receivedCalls == 2)
                    secondId = eventPtr.id;
            };

        InputSystem.Update();

        Assert.That(firstId, Is.Not.EqualTo(secondId));
    }

    [Test]
    [Category("Events")]
    public void Events_DoNotLeakIntoNextUpdate()
    {
        var device = InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(device, new GamepadState(), 1.0);
        InputSystem.QueueStateEvent(device, new GamepadState(), 2.0);

        var receivedUpdateCalls = 0;
        var receivedEventCount = 0;

        Action<InputUpdateType, int, IntPtr> onUpdate =
            (updateType, eventCount, eventData) =>
            {
                ++receivedUpdateCalls;
                receivedEventCount += eventCount;
            };
        testRuntime.onUpdate += onUpdate;

        InputSystem.Update();

        Assert.That(receivedUpdateCalls, Is.EqualTo(1));
        Assert.That(receivedEventCount, Is.EqualTo(2));

        receivedEventCount = 0;
        receivedUpdateCalls = 0;

        InputSystem.Update();

        Assert.That(receivedEventCount, Is.Zero);
        Assert.That(receivedUpdateCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Events")]
    public void Events_IfOldStateEventIsSentToDevice_IsIgnored()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightTrigger = 0.5f }, 2.0);
        InputSystem.Update();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightTrigger = 0.75f }, 1.0);
        InputSystem.Update();

        Assert.That(gamepad.rightTrigger.ReadValue(), Is.EqualTo(0.5f).Within(0.000001));
    }

    struct CustomNestedDeviceState : IInputStateTypeInfo
    {
        [InputControl(name = "button1", layout = "Button")]
        public int buttons;

        [InputControl(layout = "Axis")]
        public float axis2;

        public FourCC GetFormat()
        {
            return new FourCC('N', 'S', 'T', 'D');
        }
    }

    struct CustomDeviceState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")]
        public float axis;

        public CustomNestedDeviceState nested;

        public FourCC GetFormat()
        {
            return new FourCC('C', 'U', 'S', 'T');
        }
    }

    [InputControlLayout(stateType = typeof(CustomDeviceState))]
    class CustomDevice : InputDevice
    {
        public AxisControl axis { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            axis = builder.GetControl<AxisControl>(this, "axis");
            base.FinishSetup(builder);
        }
    }

    class CustomDeviceWithUpdate : CustomDevice, IInputUpdateCallbackReceiver
    {
        public int onUpdateCallCount;
        public InputUpdateType onUpdateType;

        public void OnUpdate(InputUpdateType updateType)
        {
            ++onUpdateCallCount;
            onUpdateType = updateType;
            InputSystem.QueueStateEvent(this, new CustomDeviceState {axis = 0.234f});
        }
    }

    // We want devices to be able to "park" unused controls outside of the state
    // memory region that is being sent to the device in events.
    [Test]
    [Category("Events")]
    public void Events_CanSendSmallerStateToDeviceWithLargerState()
    {
        const string json = @"
            {
                ""name"" : ""TestLayout"",
                ""extend"" : ""CustomDevice"",
                ""controls"" : [
                    { ""name"" : ""extra"", ""layout"" : ""Button"" }
                ]
            }
        ";

        InputSystem.RegisterControlLayout<CustomDevice>();
        InputSystem.RegisterControlLayout(json);
        var device = (CustomDevice)InputSystem.AddDevice("TestLayout");

        InputSystem.QueueStateEvent(device, new CustomDeviceState {axis = 0.5f});
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    struct ExtendedCustomDeviceState : IInputStateTypeInfo
    {
        public CustomDeviceState baseState;
        public int extra;

        public FourCC GetFormat()
        {
            return baseState.GetFormat();
        }
    }

    // HIDs rely on this behavior as we may only use a subset of a HID's set of
    // controls and thus get state events that are larger than the device state
    // that we store for the HID.
    [Test]
    [Category("Events")]
    public void Events_CandSendLargerStateToDeviceWithSmallerState()
    {
        InputSystem.RegisterControlLayout<CustomDevice>();
        var device = (CustomDevice)InputSystem.AddDevice("CustomDevice");

        var state = new ExtendedCustomDeviceState();
        state.baseState.axis = 0.5f;
        InputSystem.QueueStateEvent(device, state);
        InputSystem.Update();

        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.5).Within(0.000001));
    }

    [Test]
    [Category("Events")]
    public void Events_CanUpdateDeviceWithEventsFromUpdateCallback()
    {
        InputSystem.RegisterControlLayout<CustomDeviceWithUpdate>();
        var device = (CustomDeviceWithUpdate)InputSystem.AddDevice("CustomDeviceWithUpdate");

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.EqualTo(1));
        Assert.That(device.onUpdateType, Is.EqualTo(InputUpdateType.Dynamic));
        Assert.That(device.axis.ReadValue(), Is.EqualTo(0.234).Within(0.000001));
    }

    [Test]
    [Category("Devices")]
    public void Devices_RemovingDeviceCleansUpUpdateCallback()
    {
        InputSystem.RegisterControlLayout<CustomDeviceWithUpdate>();
        var device = (CustomDeviceWithUpdate)InputSystem.AddDevice("CustomDeviceWithUpdate");
        InputSystem.RemoveDevice(device);

        InputSystem.Update();

        Assert.That(device.onUpdateCallCount, Is.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotQueryControlsOnActionThatIsNotEnabled()
    {
        var action = new InputAction();

        Assert.That(() => action.controls, Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSingleControl()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetMultipleControls()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/*stick");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftStick));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenEnabled_GoesIntoWaitingPhase()
    {
        InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateActionsWithoutAnActionSet()
    {
        var action = new InputAction();

        Assert.That(action.set, Is.Null);
    }

    ////REVIEW: not sure whether this is the best behavior
    [Test]
    [Category("Actions")]
    public void Actions_PathLeadingNowhereIsIgnored()
    {
        var action = new InputAction(binding: "nothing");

        Assert.DoesNotThrow(() => action.Enable());
    }

    [Test]
    [Category("Actions")]
    public void Actions_StartOutInDisabledPhase()
    {
        var action = new InputAction();

        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));
    }

    [Test]
    [Category("Actions")]
    public void Actions_LoseActionHasNoSet()
    {
        var action = new InputAction();
        action.Enable(); // Force to create private action set.

        Assert.That(action.set, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionIsPerformedWhenSourceControlChangesValue()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputAction receivedAction = null;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad/leftStick");
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
                receivedAction = ctx.action;
                receivedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.Enable();

        var state = new GamepadState
        {
            leftStick = new Vector2(0.5f, 0.5f)
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedAction, Is.SameAs(action));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanListenForStateChangeOnEntireDevice()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var receivedCalls = 0;
        InputControl receivedControl = null;

        var action = new InputAction(binding: "/gamepad");
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
                receivedControl = ctx.control;
            };
        action.Enable();

        var state = new GamepadState
        {
            rightTrigger = 0.5f
        };
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad)); // We do not drill down to find the actual control that changed.
    }

    // Actions are able to observe every state change, even if the changes occur within
    // the same frame.
    [Test]
    [Category("Actions")]
    public void Actions_PressingAndReleasingButtonInSameFrame_StillTriggersAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/<gamepad>/<button>", modifiers: "press");

        var receivedCalls = 0;
        action.performed +=
            ctx =>
            {
                ++receivedCalls;
            };
        action.Enable();

        var firstState = new GamepadState {buttons = 1 << (int)GamepadState.Button.B};
        var secondState = new GamepadState {buttons = 0};

        InputSystem.QueueStateEvent(gamepad, firstState);
        InputSystem.QueueStateEvent(gamepad, secondState);

        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformHoldAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", modifiers: "hold(duration=0.4)");
        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedAction = ctx.action;
                performedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.started +=
            ctx =>
            {
                ++startedReceivedCalls;
                startedAction = ctx.action;
                startedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Started));
            };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), 0.5);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.buttonSouth));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanPerformTapAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var performedReceivedCalls = 0;
        InputAction performedAction = null;
        InputControl performedControl = null;

        var startedReceivedCalls = 0;
        InputAction startedAction = null;
        InputControl startedControl = null;

        var action = new InputAction(binding: "/gamepad/{primaryAction}", modifiers: "tap");
        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedAction = ctx.action;
                performedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Performed));
            };
        action.started +=
            ctx =>
            {
                ++startedReceivedCalls;
                startedAction = ctx.action;
                startedControl = ctx.control;

                Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Started));
            };
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.South}, 0.0);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedReceivedCalls, Is.Zero);
        Assert.That(startedAction, Is.SameAs(action));
        Assert.That(startedControl, Is.SameAs(gamepad.buttonSouth));

        startedReceivedCalls = 0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState(), InputConfiguration.TapTime);
        InputSystem.Update();

        Assert.That(startedReceivedCalls, Is.EqualTo(0));
        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedAction, Is.SameAs(action));
        Assert.That(performedControl, Is.SameAs(gamepad.buttonSouth));

        // Action should be waiting again.
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Waiting));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddActionsToSet()
    {
        var set = new InputActionSet();

        set.AddAction("action1");
        set.AddAction("action2");

        Assert.That(set.actions, Has.Count.EqualTo(2));
        Assert.That(set.actions[0], Has.Property("name").EqualTo("action1"));
        Assert.That(set.actions[1], Has.Property("name").EqualTo("action2"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddBindingsToActionsInSet()
    {
        var set = new InputActionSet();

        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        action1.AddBinding("/gamepad/leftStick");
        action2.AddBinding("/gamepad/rightStick");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action2.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddUnnamedActionToSet()
    {
        var set = new InputActionSet();
        Assert.That(() => set.AddAction(""), Throws.ArgumentException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CannotAddTwoActionsWithTheSameNameToSet()
    {
        var set = new InputActionSet();
        set.AddAction("action");

        Assert.That(() => set.AddAction("action"), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpActionInSet()
    {
        var set = new InputActionSet();

        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        Assert.That(set.TryGetAction("action1"), Is.SameAs(action1));
        Assert.That(set.TryGetAction("action2"), Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertActionSetToAndFromJson()
    {
        var set = new InputActionSet("test");

        set.AddAction(name: "action1", binding: "/gamepad/leftStick").AddBinding("/gamepad/rightStick", groups: "group");
        set.AddAction(name: "action2", binding: "/gamepad/buttonSouth", modifiers: "tap,slowTap(duration=0.1)");

        var json = set.ToJson();
        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(1));
        Assert.That(sets[0], Has.Property("name").EqualTo("test"));
        Assert.That(sets[0].actions, Has.Count.EqualTo(2));
        Assert.That(sets[0].actions[0].name, Is.EqualTo("action1"));
        Assert.That(sets[0].actions[1].name, Is.EqualTo("action2"));
        Assert.That(sets[0].actions[0].bindings, Has.Count.EqualTo(2));
        Assert.That(sets[0].actions[1].bindings, Has.Count.EqualTo(1));
        Assert.That(sets[0].actions[0].bindings[0].group, Is.Null);
        Assert.That(sets[0].actions[0].bindings[1].group, Is.EqualTo("group"));
        Assert.That(sets[0].actions[0].bindings[0].modifiers, Is.Null);
        Assert.That(sets[0].actions[0].bindings[1].modifiers, Is.Null);
        Assert.That(sets[0].actions[1].bindings[0].group, Is.Null);
        Assert.That(sets[0].actions[1].bindings[0].modifiers, Is.EqualTo("tap,slowTap(duration=0.1)"));
        Assert.That(sets[0].actions[0].set, Is.SameAs(sets[0]));
        Assert.That(sets[0].actions[1].set, Is.SameAs(sets[0]));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ActionSetJsonCanBeEmpty()
    {
        var sets = InputActionSet.FromJson("{}");
        Assert.That(sets, Is.Not.Null);
        Assert.That(sets, Has.Length.EqualTo(0));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanConvertMultipleActionSetsToAndFromJson()
    {
        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");

        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set2.AddAction(name: "action2", binding: "/gamepad/rightStick");

        var json = InputActionSet.ToJson(new[] {set1, set2});
        var sets = InputActionSet.FromJson(json);

        Assert.That(sets, Has.Length.EqualTo(2));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("set1"));
        Assert.That(sets, Has.Exactly(1).With.Property("name").EqualTo("set2"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryAllEnabledActions()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var enabledActions = InputSystem.ListEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAction()
    {
        var action = new InputAction(name: "MyAction", binding: "/gamepad/leftStick");

        // Unity's JSON serializer goes through Unity's normal serialization machinery so if
        // this works, we should have a pretty good shot that binary and YAML serialization
        // are also working.
        var json = JsonUtility.ToJson(action);
        var deserializedAction = JsonUtility.FromJson<InputAction>(json);

        Assert.That(deserializedAction.name, Is.EqualTo(action.name));
        Assert.That(deserializedAction.bindings, Has.Count.EqualTo(1));
        Assert.That(deserializedAction.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeActionSet()
    {
        var set = new InputActionSet("set");

        set.AddAction("action1", binding: "/gamepad/leftStick");
        set.AddAction("action2", binding: "/gamepad/rightStick");

        var json = JsonUtility.ToJson(set);
        var deserializedSet = JsonUtility.FromJson<InputActionSet>(json);

        Assert.That(deserializedSet.name, Is.EqualTo("set"));
        Assert.That(deserializedSet.actions, Has.Count.EqualTo(2));
        Assert.That(deserializedSet.actions[0].name, Is.EqualTo("action1"));
        Assert.That(deserializedSet.actions[1].name, Is.EqualTo("action2"));
        Assert.That(deserializedSet.actions[0].bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(deserializedSet.actions[1].bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
        Assert.That(deserializedSet.actions[0].set, Is.SameAs(deserializedSet));
        Assert.That(deserializedSet.actions[1].set, Is.SameAs(deserializedSet));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddMultipleBindings()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftStick");
        action.AddBinding("/gamepad/rightStick");

        action.Enable();

        Assert.That(action.bindings, Has.Count.EqualTo(2));
        Assert.That(action.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action.bindings[1].path, Is.EqualTo("/gamepad/rightStick"));

        var performedReceivedCalls = 0;
        InputControl performedControl = null;

        action.performed +=
            ctx =>
            {
                ++performedReceivedCalls;
                performedControl = ctx.control;
            };

        var state = new GamepadState { leftStick = new Vector2(0.5f, 0.5f)};
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.leftStick));

        performedReceivedCalls = 0;

        state.rightStick = new Vector2(0.5f, 0.5f);
        InputSystem.QueueStateEvent(gamepad, state);
        InputSystem.Update();

        Assert.That(performedReceivedCalls, Is.EqualTo(1));
        Assert.That(performedControl, Is.SameAs(gamepad.rightStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenNewDeviceIsAdded()
    {
        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/<gamepad>/buttonSouth");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad1.buttonSouth));

        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.buttonSouth));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.buttonSouth));
    }

    [Test]
    [Category("Actions")]
    public void Actions_ControlsUpdateWhenDeviceIsRemoved()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction(binding: "/<Gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad.leftTrigger));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Has.Count.Zero);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanFindEnabledActions()
    {
        var action1 = new InputAction(name: "a");
        var action2 = new InputAction(name: "b");

        action1.Enable();
        action2.Enable();

        var enabledActions = InputSystem.ListEnabledActions();

        Assert.That(enabledActions, Has.Count.EqualTo(2));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action1));
        Assert.That(enabledActions, Has.Exactly(1).SameAs(action2));
    }

    private class TestModifier : IInputBindingModifier
    {
        #pragma warning disable CS0649
        public float parm1; // Assigned through reflection
        #pragma warning restore CS0649

        public static bool s_GotInvoked;

        public void Process(ref InputAction.ModifierContext context)
        {
            Assert.That(parm1, Is.EqualTo(5.0).Within(0.000001));
            s_GotInvoked = true;
        }

        public void Reset()
        {
        }
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRegisterNewModifier()
    {
        InputSystem.RegisterBindingModifier<TestModifier>();
        TestModifier.s_GotInvoked = false;

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick/x", modifiers: "test(parm1=5.0)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0.5f) });
        InputSystem.Update();

        Assert.That(TestModifier.s_GotInvoked, Is.True);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTriggerActionFromPartialStateUpdate()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var receivedCalls = 0;
        InputControl receivedControl = null;
        action.performed += ctx =>
            {
                ++receivedCalls;
                receivedControl = ctx.control;
            };

        InputSystem.QueueDeltaStateEvent(gamepad.leftStick, Vector2.one);
        InputSystem.Update();

        Assert.That(receivedCalls, Is.EqualTo(1));
        Assert.That(receivedControl, Is.SameAs(gamepad.leftStick));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDistinguishTapAndSlowTapOnSameAction()
    {
        // Bindings can have more than one modifier. Depending on the interaction happening on the bound
        // controls one of the modifiers may initiate a phase shift and which modifier initiated the
        // shift is visible on the callback.
        //
        // This is most useful for allowing variations of the same action. For example, you can have a
        // "Fire" action, bind it to the "PrimaryAction" button, and then put both a TapModifier and a
        // SlowTapModifier on the same binding. In the 'performed' callback you can then detect whether
        // the button was slow-pressed or fast-pressed. Depending on that, you can perform a normal
        // fire action or a charged fire action.

        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/buttonSouth",
                modifiers: "tap(duration=0.1),slowTap(duration=0.5)");
        action.Enable();

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

        action.started += ctx => started.Add(ctx);
        action.performed += ctx => performed.Add(ctx);
        action.cancelled += ctx => cancelled.Add(ctx);

        // Perform tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0}, 0.05);
        InputSystem.Update();

        // Only tap was started.
        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(started[0].modifier, Is.TypeOf<TapModifier>());

        // Only tap was performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<TapModifier>());

        // Nothing was cancelled.
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();
        cancelled.Clear();

        // Perform slow tap.
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 1 << (int)GamepadState.Button.A}, 2.0);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {buttons = 0}, 2.0 + InputConfiguration.SlowTapTime + 0.0001);
        InputSystem.Update();

        // First tap was started, then slow tap was started.
        Assert.That(started, Has.Count.EqualTo(2));
        Assert.That(started[0].modifier, Is.TypeOf<TapModifier>());
        Assert.That(started[1].modifier, Is.TypeOf<SlowTapModifier>());

        // Tap got cancelled.
        Assert.That(cancelled, Has.Count.EqualTo(1));
        Assert.That(cancelled[0].modifier, Is.TypeOf<TapModifier>());

        // Slow tap got performed.
        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<SlowTapModifier>());
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanChainBindings()
    {
        // Set up an action that requires the left trigger to be held when pressing the A button.

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        // Last control in combination is considered the trigger control.
        Assert.That(performed[0].control, Is.SameAs(gamepad.buttonSouth));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_ChainedBindingsTriggerIfControlsActivateAtSameTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_ChainedBindingsDoNotTriggerIfControlsActivateInWrongOrder()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A});
        InputSystem.Update();

        Assert.That(performed, Is.Empty);
    }

    // The ability to combine bindings and have modifiers on them is crucial to be able to perform
    // most gestures as they usually require a button-like control that indicates whether a possible
    // gesture has started and then a positional control of some kind that gives the motion data for
    // the gesture.
    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanChainBindingsWithModifiers()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        // Tap or slow tap on A button when left trigger is held.
        var action = new InputAction(name: "Test");
        action.AddBinding("/gamepad/leftTrigger").CombinedWith("/gamepad/buttonSouth", modifiers: "tap,slowTap");
        action.Enable();

        var performed = new List<InputAction.CallbackContext>();
        action.performed += ctx => performed.Add(ctx);

        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 1 << (int)GamepadState.Button.A}, 0.0);
        InputSystem.QueueStateEvent(gamepad,
            new GamepadState {leftTrigger = 1.0f, buttons = 0}, InputConfiguration.SlowTapTime + 0.1);
        InputSystem.Update();

        Assert.That(performed, Has.Count.EqualTo(1));
        Assert.That(performed[0].modifier, Is.TypeOf<SlowTapModifier>());
    }

    ////REVIEW: don't think this one makes sense to have
    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanPerformContinuousAction()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick", modifiers: "continuous");
        action.Enable();

        var started = new List<InputAction.CallbackContext>();
        var performed = new List<InputAction.CallbackContext>();
        var cancelled = new List<InputAction.CallbackContext>();

        action.started += ctx => performed.Add(ctx);
        action.cancelled += ctx => performed.Add(ctx);
        action.performed +=
            ctx =>
            {
                performed.Add(ctx);
                Assert.That(ctx.GetValue<Vector2>(), Is.EqualTo(new Vector2(0.123f, 0.456f)));
            };

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.123f, 0.456f)});
        InputSystem.Update();
        InputSystem.Update();

        Assert.That(started, Has.Count.EqualTo(1));
        Assert.That(performed, Has.Count.EqualTo(2));
        Assert.That(cancelled, Has.Count.Zero);

        started.Clear();
        performed.Clear();

        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        InputSystem.Update();

        Assert.That(started, Has.Count.Zero);
        Assert.That(performed, Has.Count.Zero);
        Assert.That(cancelled, Has.Count.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void Actions_AddingDeviceWillUpdateControlsOnAction()
    {
        var action = new InputAction(binding: "/<gamepad>/leftTrigger");
        action.Enable();

        Assert.That(action.controls, Has.Count.Zero);

        var gamepad1 = (Gamepad)InputSystem.AddDevice("Gamepad");

        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad1.leftTrigger));

        // Make sure it actually triggers correctly.
        InputSystem.QueueStateEvent(gamepad1, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad1.leftTrigger));

        // Also make sure that this device creation path gets it right.
        testRuntime.ReportNewInputDevice(
            new InputDeviceDescription {product = "Test", deviceClass = "Gamepad"}.ToJson());
        InputSystem.Update();
        var gamepad2 = (Gamepad)InputSystem.devices.First(x => x.description.product == "Test");

        Assert.That(action.controls, Has.Count.EqualTo(2));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad1.leftTrigger));
        Assert.That(action.controls, Has.Exactly(1).SameAs(gamepad2.leftTrigger));
    }

    [Test]
    [Category("Actions")]
    public void Actions_RemovingDeviceWillUpdateControlsOnAction()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        Assert.That(action.controls, Contains.Item(gamepad.leftStick));

        InputSystem.RemoveDevice(gamepad);

        Assert.That(action.controls, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAction()
    {
        InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftStick");

        action.Enable();
        action.Disable();

        Assert.That(InputSystem.ListEnabledActions(), Has.Exactly(0).SameAs(action));
        Assert.That(() => action.controls, Throws.InvalidOperationException);
        Assert.That(action.phase, Is.EqualTo(InputAction.Phase.Disabled));
        Assert.That(action.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanTargetSingleDeviceWithMultipleActions()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/leftStick");
        var action3 = new InputAction(binding: "/gamepad/rightStick");

        var action1Performed = 0;
        var action2Performed = 0;
        var action3Performed = 0;

        action1.performed += _ => ++ action1Performed;
        action2.performed += _ => ++ action2Performed;
        action3.performed += _ => ++ action3Performed;

        action1.Enable();
        action2.Enable();
        action3.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = Vector2.one, rightStick = Vector2.one});
        InputSystem.Update();

        Assert.That(action1Performed, Is.EqualTo(1));
        Assert.That(action2Performed, Is.EqualTo(1));
        Assert.That(action3Performed, Is.EqualTo(1));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_ButtonTriggersActionOnlyAfterCrossingPressThreshold()
    {
        // Axis controls trigger for every value change whereas buttons only trigger
        // when crossing the press threshold.

        //should this depend on the modifiers being used?
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryStartAndPerformTime()
    {
        var gamepad = InputSystem.AddDevice("Gamepad");

        var action = new InputAction(binding: "/gamepad/leftTrigger", modifiers: "slowTap");
        action.Enable();

        var receivedStartTime = 0.0;
        var receivedTime = 0.0;

        action.performed +=
            ctx =>
            {
                receivedStartTime = ctx.startTime;
                receivedTime = ctx.time;
            };

        var startTime = 0.123;
        var endTime = 0.123 + InputConfiguration.SlowTapTime + 1.0;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 1.0f}, startTime);
        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.0f}, endTime);
        InputSystem.Update();

        Assert.That(receivedStartTime, Is.EqualTo(startTime).Within(0.000001));
        Assert.That(receivedTime, Is.EqualTo(endTime).Within(0.000001));
    }

    // Make sure that if we target "*/{ActionAction}", for example, and the gamepad's A button
    // goes down and starts the action, then whatever happens with the mouse's left button
    // shouldn't matter until the gamepad's A button comes back up.
    [Test]
    [Category("Actions")]
    public void TODO_Actions_StartingOfActionCapturesControl()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanAddSetsToAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");

        asset.AddActionSet(set1);
        asset.AddActionSet(set2);

        Assert.That(asset.actionSets, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets, Has.Exactly(1).SameAs(set1));
        Assert.That(asset.actionSets, Has.Exactly(1).SameAs(set2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_SetsInAssetMustHaveName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var set = new InputActionSet();

        Assert.That(() => asset.AddActionSet(set), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_SetsInAssetsMustHaveUniqueNames()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();

        var set1 = new InputActionSet("same");
        var set2 = new InputActionSet("same");

        asset.AddActionSet(set1);
        Assert.That(() => asset.AddActionSet(set2), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanLookUpSetInAssetByName()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var set = new InputActionSet("test");
        asset.AddActionSet(set);

        Assert.That(asset.TryGetActionSet("test"), Is.SameAs(set));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRemoveActionSetFromAsset()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(new InputActionSet("test"));
        asset.RemoveActionSet("test");

        Assert.That(asset.actionSets, Is.Empty);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanQueryLastTrigger()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/rightTrigger", modifiers: "slowTap(duration=1)");
        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1}, 2);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerModifier, Is.TypeOf<SlowTapModifier>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 0}, 4);
        InputSystem.Update();

        Assert.That(action.lastTriggerControl, Is.SameAs(gamepad.rightTrigger));
        Assert.That(action.lastTriggerTime, Is.EqualTo(4).Within(0.0000001));
        Assert.That(action.lastTriggerStartTime, Is.EqualTo(2).Within(0.0000001));
        Assert.That(action.lastTriggerModifier, Is.TypeOf<SlowTapModifier>());
        Assert.That(action.lastTriggerBinding.path, Is.EqualTo("/gamepad/rightTrigger"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindings()
    {
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.Enable();

        var wasPerformed = false;
        action.performed += ctx => wasPerformed = true;

        InputSystem.QueueStateEvent(gamepad, new GamepadState {rightTrigger = 1});
        InputSystem.Update();

        Assert.That(wasPerformed);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateButtonAxisComposite()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var action = new InputAction();
        action.AddCompositeBinding("ButtonAxis")
        .With("Negative", "/<Gamepad>/leftShoulder")
        .With("Positive", "/<Gamepad>/rightShoulder");
        action.Enable();

        float? value = null;
        action.performed += ctx => { value = ctx.GetValue<float>(); };

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.LeftShoulder));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(-1).Within(0.00001));

        value = null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadState.Button.RightShoulder));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(1).Within(0.00001));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCreateButtonVectorComposite()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        // Set up classic WASD control.
        var action = new InputAction();
        action.AddCompositeBinding("ButtonVector")
        .With("Up", "/<Keyboard>/w")
        .With("Down", "/<Keyboard>/s")
        .With("Left", "/<Keyboard>/a")
        .With("Right", "/<Keyboard>/d");
        action.Enable();

        Vector2? value = null;
        action.performed += ctx => { value = ctx.GetValue<Vector2>(); };

        // Up.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.up));

        // Up left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.A));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.up + Vector2.left).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.up + Vector2.left).normalized.y).Within(0.00001));

        // Left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.left));

        // Down left.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.left + Vector2.down).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.left + Vector2.down).normalized.y).Within(0.00001));

        // Down.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.down));

        // Down right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.D));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.down + Vector2.right).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.down + Vector2.right).normalized.y).Within(0.00001));

        // Right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value, Is.EqualTo(Vector2.right));

        // Up right.
        value = null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
        InputSystem.Update();

        Assert.That(value, Is.Not.Null);
        Assert.That(value.Value.x, Is.EqualTo((Vector2.right + Vector2.up).normalized.x).Within(0.00001));
        Assert.That(value.Value.y, Is.EqualTo((Vector2.right + Vector2.up).normalized.y).Within(0.00001));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_WhenPartOfCompositeResolvesToMultipleControls_WhatHappensXXX()
    {
        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanSerializeAndDeserializeActionsWithCompositeBindings()
    {
        var set = new InputActionSet(name: "test");
        set.AddAction("test")
        .AddCompositeBinding("ButtonVector")
        .With("Up", "/<Keyboard>/w")
        .With("Down", "/<Keyboard>/s")
        .With("Left", "/<Keyboard>/a")
        .With("Right", "/<Keyboard>/d");

        var json = set.ToJson();
        var deserialized = InputActionSet.FromJson(json);

        Assert.That(deserialized.Length, Is.EqualTo(1));
        Assert.That(deserialized[0].actions.Count, Is.EqualTo(1));
        Assert.That(deserialized[0].actions[0].bindings.Count, Is.EqualTo(5));
        Assert.That(deserialized[0].actions[0].bindings[0].path, Is.EqualTo("ButtonVector"));
        Assert.That(deserialized[0].actions[0].bindings[0].isComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[0].isPartOfComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[1].name, Is.EqualTo("Up"));
        Assert.That(deserialized[0].actions[0].bindings[1].path, Is.EqualTo("/<Keyboard>/w"));
        Assert.That(deserialized[0].actions[0].bindings[1].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[1].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[2].name, Is.EqualTo("Down"));
        Assert.That(deserialized[0].actions[0].bindings[2].path, Is.EqualTo("/<Keyboard>/s"));
        Assert.That(deserialized[0].actions[0].bindings[2].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[2].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[3].name, Is.EqualTo("Left"));
        Assert.That(deserialized[0].actions[0].bindings[3].path, Is.EqualTo("/<Keyboard>/a"));
        Assert.That(deserialized[0].actions[0].bindings[3].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[3].isPartOfComposite, Is.True);
        Assert.That(deserialized[0].actions[0].bindings[4].name, Is.EqualTo("Right"));
        Assert.That(deserialized[0].actions[0].bindings[4].path, Is.EqualTo("/<Keyboard>/d"));
        Assert.That(deserialized[0].actions[0].bindings[4].isComposite, Is.False);
        Assert.That(deserialized[0].actions[0].bindings[4].isPartOfComposite, Is.True);
    }

	[Test]
    [Category("Actions")]
    public void Actions_WhileActionIsEnabled_CannotApplyOverrides()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.Enable();

        Assert.That(() => action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"}),
            Throws.InvalidOperationException);
    }

    // If there's multiple bindings on an action, we don't readily know which binding to apply
    // an override to. We use groups to disambiguate the case.
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_OverridingRequiresGroups()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "b");

        Assert.That(() => action.ApplyBindingOverride("/gamepad/buttonSouth"), Throws.InvalidOperationException);

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "b");

        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    // We don't do anything smart when groups are ambiguous. It's a perfectly valid case to have
    // multiple bindings in the same group but when you try to override and only give a group,
    // only the first binding that uses the group is affected.
    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindings_IfGroupIsAmbiguous_OverridesOnlyFirst()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "a");

        Assert.That(action.bindings[0].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
        Assert.That(action.bindings[1].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_OnActionWithMultipleBindingsWithSameGroup_CanTargetIndividualBindingsByIndex()
    {
        var action = new InputAction(name: "test");

        action.AddBinding("/gamepad/leftTrigger", groups: "a");
        action.AddBinding("/gamepad/rightTrigger", groups: "a");

        action.ApplyBindingOverride("/gamepad/buttonSouth", group: "a[1]");

        Assert.That(action.bindings[0].overridePath, Is.Null);
        Assert.That(action.bindings[1].overridePath, Is.EqualTo("/gamepad/buttonSouth"));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultsAfterOverridingBinding()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.RemoveAllBindingOverrides();

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_ApplyingNullOrEmptyOverride_IsSameAsRemovingOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");

        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.ApplyBindingOverride(new InputBindingOverride());
        Assert.That(action.bindings[0].overridePath, Is.Null);

        action.ApplyBindingOverride(new InputBindingOverride {binding = "/gamepad/rightTrigger"});
        action.ApplyBindingOverride(new InputBindingOverride { binding = "" });
        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveOverrides()
    {
        var action = new InputAction(name: "foo");
        action.Enable();
        Assert.That(() => action.RemoveAllBindingOverrides(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanRestoreDefaultForSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBindingOverride {binding = "/gamepad/rightTrigger"};

        action.ApplyBindingOverride(bindingOverride);
        action.RemoveBindingOverride(bindingOverride);

        Assert.That(action.bindings[0].overridePath, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_WhenActionIsEnabled_CannotRemoveSpecificOverride()
    {
        var action = new InputAction(binding: "/gamepad/leftTrigger");
        var bindingOverride = new InputBindingOverride {binding = "/gamepad/rightTrigger"};
        action.ApplyBindingOverride(bindingOverride);
        action.Enable();
        Assert.That(() => action.RemoveBindingOverride(bindingOverride), Throws.InvalidOperationException);
    }

    // The following functionality is meant in a way where you have a base action set that
    // you then clone multiple times and put overrides on each of the clones to associate them
    // with specific devices.
    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices()
    {
        // Action that matches leftStick on *any* gamepad in the system.
        var action = new InputAction(binding: "/<gamepad>/leftStick");
        action.AddBinding("/keyboard/enter"); // Add unrelated binding which should not be touched.

        InputSystem.AddDevice("Gamepad");
        var gamepad2 = (Gamepad)InputSystem.AddDevice("Gamepad");

        // Add overrides to make bindings specific to #2 gamepad.
        var numOverrides = action.ApplyOverridesUsingMatchingControls(gamepad2);
        action.Enable();

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action.controls, Has.Count.EqualTo(1));
        Assert.That(action.controls[0], Is.SameAs(gamepad2.leftStick));
        Assert.That(action.bindings[0].overridePath, Is.EqualTo(gamepad2.leftStick.path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanOverrideBindingsWithControlsFromSpecificDevices_OnActionsInSet()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        var action2 = set.AddAction("action2", "/<gamepad>/buttonSouth");
        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        var numOverrides = set.ApplyOverridesUsingMatchingControls(gamepad);

        Assert.That(numOverrides, Is.EqualTo(1));
        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo(gamepad.buttonSouth.path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanEnableAndDisableEntireSet()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1");
        var action2 = set.AddAction("action2");

        set.Enable();

        Assert.That(set.enabled);
        Assert.That(action1.enabled);
        Assert.That(action2.enabled);

        set.Disable();

        Assert.That(set.enabled, Is.False);
        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneAction()
    {
        var action = new InputAction(name: "action");
        action.AddBinding("/gamepad/leftStick", modifiers: "tap", groups: "group");
        action.AddBinding("/gamepad/rightStick");

        var clone = action.Clone();

        Assert.That(clone, Is.Not.SameAs(action));
        Assert.That(clone.name, Is.EqualTo(action.name));
        Assert.That(clone.bindings, Has.Count.EqualTo(action.bindings.Count));
        Assert.That(clone.bindings[0].path, Is.EqualTo(action.bindings[0].path));
        Assert.That(clone.bindings[0].modifiers, Is.EqualTo(action.bindings[0].modifiers));
        Assert.That(clone.bindings[0].group, Is.EqualTo(action.bindings[0].group));
        Assert.That(clone.bindings[1].path, Is.EqualTo(action.bindings[1].path));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningActionFromSet_ProducesSingletonAction()
    {
        var set = new InputActionSet("set");
        var action = set.AddAction("action1");

        var clone = action.Clone();

        Assert.That(clone.set, Is.Null);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CloningEnabledAction_ProducesDisabledAction()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        action.Enable();

        var clone = action.Clone();

        Assert.That(clone.enabled, Is.False);
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionSets()
    {
        var set = new InputActionSet("set");
        var action1 = set.AddAction("action1", binding: "/gamepad/leftStick", modifiers: "tap");
        var action2 = set.AddAction("action2", binding: "/gamepad/rightStick", modifiers: "tap");

        var clone = set.Clone();

        Assert.That(clone, Is.Not.SameAs(set));
        Assert.That(clone.name, Is.EqualTo(set.name));
        Assert.That(clone.actions, Has.Count.EqualTo(set.actions.Count));
        Assert.That(clone.actions, Has.None.SameAs(action1));
        Assert.That(clone.actions, Has.None.SameAs(action2));
        Assert.That(clone.actions[0].name, Is.EqualTo(set.actions[0].name));
        Assert.That(clone.actions[1].name, Is.EqualTo(set.actions[1].name));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanCloneActionAssets()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.name = "Asset";
        var set1 = new InputActionSet("set1");
        var set2 = new InputActionSet("set2");
        asset.AddActionSet(set1);
        asset.AddActionSet(set2);

        var clone = asset.Clone();

        Assert.That(clone, Is.Not.SameAs(asset));
        Assert.That(clone.GetInstanceID(), Is.Not.EqualTo(asset.GetInstanceID()));
        Assert.That(clone.actionSets, Has.Count.EqualTo(2));
        Assert.That(clone.actionSets, Has.None.SameAs(set1));
        Assert.That(clone.actionSets, Has.None.SameAs(set2));
        Assert.That(clone.actionSets[0].name, Is.EqualTo("set1"));
        Assert.That(clone.actionSets[1].name, Is.EqualTo("set2"));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanRebindFromUserInput()
    {
        var action = new InputAction(binding: "/gamepad/leftStick");
        //var gamepad = InputSystem.AddDevice("Gamepad");

        using (var rebind = InputActionRebinding.PerformUserRebind(action))
        {
        }

        Assert.Fail();
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanResolveActionReference()
    {
        var set = new InputActionSet("set");
        set.AddAction("action1");
        var action2 = set.AddAction("action2");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        reference.Set(asset, "set", "action2");

        var referencedAction = reference.action;

        Assert.That(referencedAction, Is.SameAs(action2));
    }

    [Test]
    [Category("Actions")]
    public void Actions_CanDisableAllEnabledActionsInOneGo()
    {
        var action1 = new InputAction(binding: "/gamepad/leftStick");
        var action2 = new InputAction(binding: "/gamepad/rightStick");
        var set = new InputActionSet();
        var action3 = set.AddAction("action", "/gamepad/buttonSouth");

        action1.Enable();
        action2.Enable();
        set.Enable();

        InputSystem.DisableAllEnabledActions();

        Assert.That(action1.enabled, Is.False);
        Assert.That(action2.enabled, Is.False);
        Assert.That(action3.enabled, Is.False);
        Assert.That(set.enabled, Is.False);
    }

    // This test requires that pointer deltas correctly snap back to 0 when the pointer isn't moved.
    [Test]
    [Category("Actions")]
    public void Actions_CanDriveFreeLookFromGamepadStickAndPointerDelta()
    {
        var mouse = InputSystem.AddDevice<Mouse>();
        var gamepad = InputSystem.AddDevice<Gamepad>();

        // Deadzoning alters values on the stick. For this test, get rid of it.
        InputConfiguration.DeadzoneMin = 0f;
        InputConfiguration.DeadzoneMax = 1f;

        // Same for pointer sensitivity.
        InputConfiguration.PointerDeltaSensitivity = 1f;

        var action = new InputAction();

        action.AddBinding("/<Gamepad>/leftStick");
        action.AddBinding("/<Pointer>/delta");

        Vector2? movement = null;
        action.performed +=
            ctx => { movement = ctx.GetValue<Vector2>(); };

        action.Enable();

        InputSystem.QueueStateEvent(gamepad, new GamepadState {leftStick = new Vector2(0.5f, 0.5f)});
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0.5).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0.5).Within(0.000001));

        movement = null;
        InputSystem.QueueStateEvent(mouse, new MouseState {delta = new Vector2(0.25f, 0.25f)});
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0.25).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0.25).Within(0.000001));

        movement = null;
        InputSystem.Update();

        Assert.That(movement.HasValue, Is.True);
        Assert.That(movement.Value.x, Is.EqualTo(0).Within(0.000001));
        Assert.That(movement.Value.y, Is.EqualTo(0).Within(0.000001));
    }

    [Test]
    [Category("Actions")]
    public void TODO_Actions_CanDriveMoveActionFromWASDKeys()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var action = new InputAction();

        action.AddBinding("/<Keyboard>/a").WithModifiers("axisvector(x=-1,y=0)");
        action.AddBinding("/<Keyboard>/d").WithModifiers("axisvector(x=1,y=0)");
        action.AddBinding("/<Keyboard>/w").WithModifiers("axisvector(x=0,y=1)");
        action.AddBinding("/<Keyboard>/s").WithModifiers("axisvector(x=0,y=-1)");

        Vector2? vector = null;
        action.performed +=
            ctx => { vector = ctx.GetValue<Vector2>(); };

        action.Enable();

        //Have a concept of "composite bindings"?

        //This leads to the bigger question of how the system handles an action
        //that has multiple bindings where each may independently go through a
        //full phase cycle.

        ////TODO: need to have names on the bindings ("up", "down", "left", right")
        ////      (so it becomes "Move Up" etc in a binding UI)

        ////REVIEW: how should we handle mixed-device bindings? say there's an additional
        ////        gamepad binding on the action above. what if both the gamepad and
        ////        the keyboard trigger?

        // A pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(-1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(0).Within(0.000001));
        vector = null;

        // D pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(0).Within(0.000001));
        vector = null;

        // W pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(0).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(1).Within(0.000001));
        vector = null;

        // S pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(0).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(-1).Within(0.000001));
        vector = null;

        ////FIXME: these need to behave like Dpad vectors and be normalized

        // A+W pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.W));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(-1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(1).Within(0.000001));
        vector = null;

        // D+W pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(1).Within(0.000001));
        vector = null;

        // A+S pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(-1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(-1).Within(0.000001));
        vector = null;

        // D+S pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.S));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(1).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(-1).Within(0.000001));
        vector = null;

        // A+D+W+S pressed.
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.S, Key.W, Key.A));
        InputSystem.Update();

        Assert.That(vector, Is.Not.Null);
        Assert.That(vector.Value.x, Is.EqualTo(0).Within(0.000001));
        Assert.That(vector.Value.y, Is.EqualTo(0).Within(0.000001));
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanConnectTwoInputSystemsOverNetwork()
    {
        // Add some data to the local input system.
        InputSystem.AddDevice("Gamepad");
        InputSystem.RegisterControlLayout(@"{ ""name"" : ""MyGamepad"", ""extend"" : ""Gamepad"" }");
        var localGamepad = (Gamepad)InputSystem.AddDevice("MyGamepad");

        // Now create another input system instance and connect it
        // to our "local" instance.
        // NOTE: This relies on internal APIs. We want remoting as such to be available
        //       entirely from user land but having multiple input systems in the same
        //       application isn't something that we necessarily want to expose (we do
        //       have global state so it can easily lead to surprising results).
        var secondInputRuntime = new InputTestRuntime();
        var secondInputManager = new InputManager();
        secondInputManager.InstallRuntime(secondInputRuntime);
        secondInputManager.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputManager);

        // We wire the two directly into each other effectively making function calls
        // our "network transport layer". In a real networking situation, we'd effectively
        // have an RPC-like mechanism sitting in-between.
        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        var remoteGamepadLayout =
            string.Format("{0}0::{1}", InputRemoting.kRemoteLayoutNamespacePrefix, localGamepad.layout);

        // Make sure that our "remote" system now has the data we initially
        // set up on the local system.
        Assert.That(secondInputManager.devices,
            Has.Exactly(1).With.Property("layout").EqualTo(remoteGamepadLayout));
        Assert.That(secondInputManager.devices, Has.Exactly(2).TypeOf<Gamepad>());
        Assert.That(secondInputManager.devices, Has.All.With.Property("remote").True);

        // Send state event to local gamepad.
        InputSystem.QueueStateEvent(localGamepad, new GamepadState { leftTrigger = 0.5f });
        InputSystem.Update();

        // Make second input manager process the events it got.
        // NOTE: This will also switch the system to the state buffers from the second input manager.
        secondInputManager.Update();

        var remoteGamepad = (Gamepad)secondInputManager.devices.First(x => x.layout == remoteGamepadLayout);

        Assert.That(remoteGamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        secondInputRuntime.Dispose();
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingDevicesWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputRuntime = new InputTestRuntime();
        var secondInputManager = new InputManager();
        secondInputManager.InstallRuntime(secondInputRuntime);
        secondInputManager.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputManager);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        // Add device.
        var localGamepad = InputSystem.AddDevice("Gamepad");
        secondInputManager.Update();

        Assert.That(secondInputManager.devices, Has.Count.EqualTo(1));
        var remoteGamepad = secondInputManager.devices[0];
        Assert.That(remoteGamepad, Is.TypeOf<Gamepad>());
        Assert.That(remoteGamepad.remote, Is.True);
        Assert.That(remoteGamepad.layout, Contains.Substring("Gamepad"));

        // Change usage.
        InputSystem.SetUsage(localGamepad, CommonUsages.LeftHand);
        secondInputManager.Update();
        Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));

        // Bind and disconnect are events so no need to test those.

        // Remove device.
        InputSystem.RemoveDevice(localGamepad);
        secondInputManager.Update();
        Assert.That(secondInputManager.devices, Has.Count.Zero);

        secondInputRuntime.Dispose();
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingLayoutsWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputSystem = new InputManager();
        secondInputSystem.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputSystem);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        const string jsonV1 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add layout.
        InputSystem.RegisterControlLayout(jsonV1);

        var layout = secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout"));
        Assert.That(layout, Is.Not.Null);
        Assert.That(layout.extendsLayout, Is.EqualTo("remote0::Gamepad"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Keyboard""
            }
        ";

        // Change layout.
        InputSystem.RegisterControlLayout(jsonV2);

        layout = secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout"));
        Assert.That(layout.extendsLayout, Is.EqualTo("remote0::Keyboard"));

        // Remove layout.
        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout")), Is.Null);
    }

    // If we have more than two players connected, for example, and we add a layout from player A
    // to the system, we don't want to send the layout to player B in turn. I.e. all data mirrored
    // from remotes should stay local.
    [Test]
    [Category("Remote")]
    public void TODO_Remote_WithMultipleRemotesConnected_DoesNotDuplicateDataFromOneRemoteToOtherRemotes()
    {
        Assert.Fail();
    }

    // PlayerConnection isn't connected in the editor and EditorConnection isn't connected
    // in players so we can't really test actual transport in just the application itself.
    // This will act as an IEditorPlayerConnection that immediately makes the FakePlayerConnection
    // on the other end receive messages.
    class FakePlayerConnection : IEditorPlayerConnection
    {
        public int playerId;
        // The fake connection acting as the socket on the opposite end of us.
        public FakePlayerConnection otherEnd;

        public void Register(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            MessageEvent msgEvent;
            if (!m_MessageListeners.TryGetValue(messageId, out msgEvent))
            {
                msgEvent = new MessageEvent();
                m_MessageListeners[messageId] = msgEvent;
            }

            msgEvent.AddListener(callback);
        }

        public void Unregister(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            m_MessageListeners[messageId].RemoveListener(callback);
        }

        public void DisconnectAll()
        {
            m_MessageListeners.Clear();
            m_ConnectionListeners.RemoveAllListeners();
            m_DisconnectionListeners.RemoveAllListeners();
        }

        public void RegisterConnection(UnityAction<int> callback)
        {
            m_ConnectionListeners.AddListener(callback);
        }

        public void RegisterDisconnection(UnityAction<int> callback)
        {
            m_DisconnectionListeners.AddListener(callback);
        }

        public void Receive(Guid messageId, byte[] data)
        {
            MessageEvent msgEvent;
            if (m_MessageListeners.TryGetValue(messageId, out msgEvent))
                msgEvent.Invoke(new MessageEventArgs {playerId = playerId, data = data});
        }

        public void Send(Guid messageId, byte[] data)
        {
            otherEnd.Receive(messageId, data);
        }

        private Dictionary<Guid, MessageEvent> m_MessageListeners = new Dictionary<Guid, MessageEvent>();
        private ConnectEvent m_ConnectionListeners = new ConnectEvent();
        private ConnectEvent m_DisconnectionListeners = new ConnectEvent();
        private class MessageEvent : UnityEvent<MessageEventArgs>
        {
        }
        private class ConnectEvent : UnityEvent<int>
        {
        }
    }

    public class RemoteTestObserver : IObserver<InputRemoting.Message>
    {
        public List<InputRemoting.Message> messages = new List<InputRemoting.Message>();

        public void OnNext(InputRemoting.Message msg)
        {
            messages.Add(msg);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanConnectInputSystemsOverEditorPlayerConnection()
    {
        var connectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
        var connectionToPlayer = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();

        connectionToEditor.name = "ConnectionToEditor";
        connectionToPlayer.name = "ConnectionToPlayer";

        var fakeEditorConnection = new FakePlayerConnection {playerId = 0};
        var fakePlayerConnection = new FakePlayerConnection {playerId = 1};

        fakeEditorConnection.otherEnd = fakePlayerConnection;
        fakePlayerConnection.otherEnd = fakeEditorConnection;

        var observer = new RemoteTestObserver();

        // In the Unity API, "PlayerConnection" is the connection to the editor
        // and "EditorConnection" is the connection to the player. Seems counter-intuitive.
        connectionToEditor.Bind(fakePlayerConnection, true);
        connectionToPlayer.Bind(fakeEditorConnection, true);

        // Bind a local remote on the player side.
        var local = new InputRemoting(InputSystem.s_Manager);
        local.Subscribe(connectionToEditor);
        local.StartSending();

        connectionToPlayer.Subscribe(observer);

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();
        InputSystem.RemoveDevice(device);

        ////TODO: make sure that we also get the connection sequence right and send our initial layouts and devices
        Assert.That(observer.messages, Has.Count.EqualTo(4));
        Assert.That(observer.messages[0].type, Is.EqualTo(InputRemoting.MessageType.Connect));
        Assert.That(observer.messages[1].type, Is.EqualTo(InputRemoting.MessageType.NewDevice));
        Assert.That(observer.messages[2].type, Is.EqualTo(InputRemoting.MessageType.NewEvents));
        Assert.That(observer.messages[3].type, Is.EqualTo(InputRemoting.MessageType.RemoveDevice));

        ////TODO: test disconnection

        ScriptableObject.Destroy(connectionToEditor);
        ScriptableObject.Destroy(connectionToPlayer);
    }

#if UNITY_EDITOR
    [Test]
    [Category("Editor")]
    public void Editor_CanSaveAndRestoreState()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad""
            }
        ";

        InputSystem.RegisterControlLayout(json);
        InputSystem.AddDevice("MyDevice");
        testRuntime.ReportNewInputDevice(new InputDeviceDescription
        {
            product = "Product",
            manufacturer = "Manufacturer",
            interfaceName = "Test"
        }.ToJson());
        InputSystem.Update();

        InputSystem.Save();
        InputSystem.Reset();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(0));

        InputSystem.Restore();

        Assert.That(InputSystem.devices,
            Has.Exactly(1).With.Property("layout").EqualTo("MyDevice").And.TypeOf<Gamepad>());

        var unsupportedDevices = new List<InputDeviceDescription>();
        InputSystem.GetUnsupportedDevices(unsupportedDevices);

        Assert.That(unsupportedDevices.Count, Is.EqualTo(1));
        Assert.That(unsupportedDevices[0].product, Is.EqualTo("Product"));
        Assert.That(unsupportedDevices[0].manufacturer, Is.EqualTo("Manufacturer"));
        Assert.That(unsupportedDevices[0].interfaceName, Is.EqualTo("Test"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringDeviceFromSave_RestoresRelevantDynamicConfiguration()
    {
        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.SetUsage(device, CommonUsages.LeftHand);
        ////TODO: set variant

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        var newDevice = InputSystem.devices.First(x => x is Gamepad);

        Assert.That(newDevice.layout, Is.EqualTo("Gamepad"));
        Assert.That(newDevice.usages, Has.Count.EqualTo(1));
        Assert.That(newDevice.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        Assert.That(Gamepad.current, Is.SameAs(newDevice));
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillCleanUpEventHooks()
    {
        InputSystem.Save();

        var receivedOnEvent = 0;
        var receivedOnDeviceChange = 0;

        InputSystem.onEvent += _ => ++ receivedOnEvent;
        InputSystem.onDeviceChange += (c, d) => ++ receivedOnDeviceChange;

        InputSystem.Restore();

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();

        Assert.That(receivedOnEvent, Is.Zero);
        Assert.That(receivedOnDeviceChange, Is.Zero);
    }

    [Test]
    [Category("Editor")]
    public void Editor_RestoringStateWillRestoreObjectsOfLayoutBuilder()
    {
        var builder = new TestLayoutBuilder {layoutToLoad = "Gamepad"};
        InputSystem.RegisterControlLayoutBuilder(() => builder.DoIt(), "TestLayout");

        InputSystem.Save();
        InputSystem.Reset();
        InputSystem.Restore();

        var device = InputSystem.AddDevice("TestLayout");

        Assert.That(device, Is.TypeOf<Gamepad>());
    }

    // Editor updates are confusing in that they denote just another point in the
    // application loop where we push out events. They do not mean that the events
    // we send necessarily go to the editor state buffers.
    [Test]
    [Category("Editor")]
    public void Editor_WhenPlaying_EditorUpdatesWriteEventIntoPlayerState()
    {
        InputConfiguration.LockInputToGame = true;

        var gamepad = (Gamepad)InputSystem.AddDevice("Gamepad");

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.25f });
        InputSystem.Update(InputUpdateType.Dynamic);

        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.75f });
        InputSystem.Update(InputUpdateType.Editor);

        InputSystem.Update(InputUpdateType.Dynamic);

        Assert.That(gamepad.leftTrigger.ReadValue(), Is.EqualTo(0.75).Within(0.000001));
        Assert.That(gamepad.leftTrigger.ReadPreviousValue(), Is.EqualTo(0.25).Within(0.000001));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionSetThroughSerialization()
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var obj = new SerializedObject(asset);

        InputActionSerializationHelpers.AddActionSet(obj);
        InputActionSerializationHelpers.AddActionSet(obj);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets[0].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionSets[1].name, Is.Not.Null.Or.Empty);
        Assert.That(asset.actionSets[0].name, Is.Not.EqualTo(asset.actionSets[1].name));

        var actionSet2Name = asset.actionSets[1].name;

        InputActionSerializationHelpers.DeleteActionSet(obj, 0);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets, Has.Count.EqualTo(1));
        Assert.That(asset.actionSets[0].name, Is.EqualTo(actionSet2Name));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveActionThroughSerialization()
    {
        var set = new InputActionSet("set");
        set.AddAction(name: "action", binding: "/gamepad/leftStick");
        set.AddAction(name: "action1", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var obj = new SerializedObject(asset);
        var setProperty = obj.FindProperty("m_ActionSets").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AddAction(setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets[0].actions, Has.Count.EqualTo(3));
        Assert.That(asset.actionSets[0].actions[2].name, Is.EqualTo("action2"));
        Assert.That(asset.actionSets[0].actions[2].bindings, Has.Count.Zero);

        InputActionSerializationHelpers.DeleteAction(setProperty, 2);
        obj.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(asset.actionSets[0].actions, Has.Count.EqualTo(2));
        Assert.That(asset.actionSets[0].actions[0].name, Is.EqualTo("action"));
        Assert.That(asset.actionSets[0].actions[1].name, Is.EqualTo("action1"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_InputAsset_CanAddAndRemoveBindingThroughSerialization()
    {
        var set = new InputActionSet("set");
        set.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set);

        var obj = new SerializedObject(asset);
        var setProperty = obj.FindProperty("m_ActionSets").GetArrayElementAtIndex(0);
        var action1Property = setProperty.FindPropertyRelative("m_Actions").GetArrayElementAtIndex(0);

        InputActionSerializationHelpers.AppendBinding(action1Property, setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        // Sets and actions aren't UnityEngine.Objects so the modifications will not
        // be in-place. Look up the actions after each apply.
        var action1 = asset.actionSets[0].TryGetAction("action1");
        var action2 = asset.actionSets[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(2));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action1.bindings[1].path, Is.EqualTo(""));
        Assert.That(action1.bindings[1].modifiers, Is.EqualTo(""));
        Assert.That(action1.bindings[1].group, Is.EqualTo(""));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));

        InputActionSerializationHelpers.RemoveBinding(action1Property, 1, setProperty);
        obj.ApplyModifiedPropertiesWithoutUndo();

        action1 = asset.actionSets[0].TryGetAction("action1");
        action2 = asset.actionSets[0].TryGetAction("action2");

        Assert.That(action1.bindings, Has.Count.EqualTo(1));
        Assert.That(action1.bindings[0].path, Is.EqualTo("/gamepad/leftStick"));
        Assert.That(action2.bindings[0].path, Is.EqualTo("/gamepad/rightStick"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset()
    {
        var set1 = new InputActionSet("set1");
        set1.AddAction(name: "action1", binding: "/gamepad/leftStick");
        set1.AddAction(name: "action2", binding: "/gamepad/rightStick");
        var set2 = new InputActionSet("set2");
        set2.AddAction(name: "action1", binding: "/gamepad/buttonSouth");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set1);
        asset.AddActionSet(set2);
        asset.name = "MyControls";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
                new InputActionCodeGenerator.Options {namespaceName = "MyNamespace", sourceAssetPath = "test"});

        // Our version of Mono doesn't implement the CodeDom stuff so all we can do here
        // is just perform some textual verification. Once we have the newest Mono, this should
        // use CSharpCodeProvider and at least parse if not compile and run the generated wrapper.

        Assert.That(code, Contains.Substring("namespace MyNamespace"));
        Assert.That(code, Contains.Substring("public class MyControls"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputActionSet Clone()"));
    }

    [Test]
    [Category("Editor")]
    public void Editor_CanGenerateCodeWrapperForInputAsset_WhenAssetNameContainsSpacesAndSymbols()
    {
        var set1 = new InputActionSet("set1");
        set1.AddAction(name: "action ^&", binding: "/gamepad/leftStick");
        set1.AddAction(name: "1thing", binding: "/gamepad/leftStick");
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        asset.AddActionSet(set1);
        asset.name = "New Controls (4)";

        var code = InputActionCodeGenerator.GenerateWrapperCode(asset,
                new InputActionCodeGenerator.Options {sourceAssetPath = "test"});

        Assert.That(code, Contains.Substring("class NewControls_4_"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputAction @action__"));
        Assert.That(code, Contains.Substring("public UnityEngine.Experimental.Input.InputAction @_1thing"));
    }

    class TestEditorWindow : EditorWindow
    {
        public Vector2 mousePosition;
        public void OnGUI()
        {
            mousePosition = Mouse.current.position.ReadValue();
        }
    }

    [Test]
    [Category("Editor")]
    public void TODO_Editor_PointerCoordinatesInEditorWindowOnGUI_AreInEditorWindowSpace()
    {
        Assert.Fail();
    }

    ////TODO: the following tests have to be edit mode tests but it looks like putting them into
    ////      Assembly-CSharp-Editor is the only way to mark them as such

    ////REVIEW: support actions in the editor at all?
    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_ActionSetUpInEditor_DoesNotTriggerInPlayMode()
    {
        throw new NotImplementedException();
    }

    [UnityTest]
    [Category("Editor")]
    public IEnumerator TODO_Editor_PlayerActionDoesNotTriggerWhenGameViewIsNotFocused()
    {
        throw new NotImplementedException();
    }

    ////TODO: tests for InputAssetImporter; for this we need C# mocks to be able to cut us off from the actual asset DB

#endif

    ////TODO:-----------------------------------------------------------------

    [Test]
    [Category("State")]
    public void TODO_State_SupportsBitAddressingControlsWithAutomaticOffsets()
    {
        ////TODO
        Assert.Fail();
    }

    [Test]
    [Category("State")]
    public void TODO_State_WithSingleStateAndSingleUpdate_XXXXX()
    {
        //test memory consumption
        ////TODO
        Assert.Fail();
    }

    ////TODO: This test doesn't yet make sense. The thought of how the feature should work is
    ////      correct, but the setup makes no sense and doesn't work. Gamepad adds deadzones
    ////      on the *sticks* so modifying that requires a Vector2 type processor which invert
    ////      isn't.
    [Test]
    [Category("Layouts")]
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
                        ""processors"" : ""invert,deadzone""
                    }
                ]
            }
        ";

        InputSystem.RegisterControlLayout(json);

        var setup = new InputDeviceBuilder("MyDevice");
        var leftStickX = setup.GetControl<AxisControl>("leftStick/x");

        Assert.That(leftStickX.processors, Has.Length.EqualTo(2));
        Assert.That(leftStickX.processors[0], Is.TypeOf<InvertProcessor>());
        Assert.That(leftStickX.processors[1], Is.TypeOf<DeadzoneProcessor>());
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layout_CustomizedStateLayoutWillNotUseFormatCodeFromBaseLayout()
    {
        //make sure that if you customize a gamepad layout, you don't end up with the "GPAD" format on the device
        //in fact, the system should require a format code to be specified in that case
        Assert.Fail();
    }

    ////REVIEW: This one seems like it adds quite a bit of complexity for somewhat minor gain.
    ////        May even be safer to *not* support this as it may inject controls at offsets where you don't expect them.
    struct BaseInputState : IInputStateTypeInfo
    {
        [InputControl(layout = "Axis")] public float axis;
        public int padding;
        public FourCC GetFormat()
        {
            return new FourCC("BASE");
        }
    }
    [InputControlLayout(stateType = typeof(BaseInputState))]
    class BaseInputDevice : InputDevice
    {
    }
    //[InputControl(name = "axis", offset = InputStateBlock.kInvalidOffset)]
    struct DerivedInputState : IInputStateTypeInfo
    {
        public FourCC GetFormat()
        {
            return new FourCC("DERI");
        }
    }
    [InputControlLayout(stateType = typeof(DerivedInputState))]
    class DerivedInputDevice : InputDevice
    {
    }

    [Test]
    [Category("Layouts")]
    public void TODO_Layouts_InputStateInDerivedClassMergesWithControlsOfInputStateFromBaseClass()
    {
        //axis should appear in DerivedInputDevice and should have been moved to offset 8 (from automatic assignment)
        Assert.Fail();
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_ApplyOverrides()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        var action2 = set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        Assert.DoesNotThrow(() => set.ApplyOverrides(listOverrides));

        action1.Enable();
        action2.Enable();

        Assert.That(action1.bindings[0].overridePath, Is.Not.Null);
        Assert.That(action2.bindings[0].overridePath, Is.Not.Null);
        Assert.That(action1.bindings[0].overridePath, Is.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo("/gamepad/rightTrigger"));

        var action = new InputAction(binding: "/gamepad/leftTrigger");
        action.Enable();
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_CannotChangeBindindsThatIsNotEnabled()
    {
        var set = new InputActionSet();
        set.AddAction("action1", "/<keyboard>/enter").Enable();
        set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        Assert.That(() => set.ApplyOverrides(listOverrides), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_CannotRemoveBindindsThatIsNotEnabled()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        set.ApplyOverrides(listOverrides);

        action1.Enable();

        Assert.That(() => set.RemoveOverrides(listOverrides), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_CannotRemoveAllBindindsThatIsNotEnabled()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        set.ApplyOverrides(listOverrides);

        action1.Enable();

        Assert.That(() => set.RemoveAllOverrides(), Throws.InvalidOperationException);
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_RemoveAllBindindsThatIsNotEnabled()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        var action2 = set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        set.ApplyOverrides(listOverrides);
        set.RemoveAllOverrides();

        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.Null);
        Assert.That(action1.bindings[0].path, Is.Not.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].path, Is.Not.EqualTo("/gamepad/rightTrigger"));
    }

    [Test]
    [Category("Sets")]
    public void Sets_OnSetWithMultipleOverrideBindings_RemoveBindindsThatIsNotEnabled()
    {
        var set = new InputActionSet();
        var action1 = set.AddAction("action1", "/<keyboard>/enter");
        var action2 = set.AddAction("action2", "/<gamepad>/buttonSouth");

        var listOverrides = new List<InputBindingOverride>(3);
        listOverrides.Add(new InputBindingOverride { action = "action3", binding = "/gamepad/buttonSouth" });
        listOverrides.Add(new InputBindingOverride { action = "action2", binding = "/gamepad/rightTrigger" });
        listOverrides.Add(new InputBindingOverride { action = "action1", binding = "/gamepad/leftTrigger" });

        set.ApplyOverrides(listOverrides);
        listOverrides.RemoveAt(1);
        set.RemoveOverrides(listOverrides);

        Assert.That(action1.bindings[0].overridePath, Is.Null);
        Assert.That(action2.bindings[0].overridePath, Is.Not.Null);
        Assert.That(action1.bindings[0].path, Is.Not.EqualTo("/gamepad/leftTrigger"));
        Assert.That(action2.bindings[0].overridePath, Is.EqualTo("/gamepad/rightTrigger"));
    }
}
