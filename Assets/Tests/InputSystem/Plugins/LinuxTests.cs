#if UNITY_EDITOR || UNITY_STANDALONE_LINUX
using NUnit.Framework;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.Linux;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

internal class LinuxTests : InputTestFixture
{
    [Test]
    [Category("Devices")]
    public void Devices_SupportsLinuxSDLJoysticks()
    {
        runtime.ReportNewInputDevice(TestSDLJoystick.descriptorString);

        InputSystem.Update();

        var device = InputSystem.devices[0];

        Assert.That(device, Is.Not.Null);
        Assert.That(device, Is.TypeOf<Joystick>());

        var joystick = (Joystick)device;

        // Buttons.
        Assert.That(joystick["Trigger"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Thumb"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Thumb2"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Top"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Top2"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Pinkie"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base2"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base3"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base4"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base5"], Is.TypeOf<ButtonControl>());
        Assert.That(joystick["Base6"], Is.TypeOf<ButtonControl>());

        Assert.That(joystick["Trigger"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Thumb"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Thumb2"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Top"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Top2"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Pinkie"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base2"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base3"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base4"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base5"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));
        Assert.That(joystick["Base6"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatBit));

        Assert.That(joystick["Trigger"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Thumb"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Thumb2"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Top"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Top2"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Pinkie"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Base"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Base2"].stateBlock.byteOffset, Is.EqualTo(0));
        Assert.That(joystick["Base3"].stateBlock.byteOffset, Is.EqualTo(1));
        Assert.That(joystick["Base4"].stateBlock.byteOffset, Is.EqualTo(1));
        Assert.That(joystick["Base5"].stateBlock.byteOffset, Is.EqualTo(1));
        Assert.That(joystick["Base6"].stateBlock.byteOffset, Is.EqualTo(1));

        Assert.That(joystick["Trigger"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(joystick["Thumb"].stateBlock.bitOffset, Is.EqualTo(1));
        Assert.That(joystick["Thumb2"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(joystick["Top"].stateBlock.bitOffset, Is.EqualTo(3));
        Assert.That(joystick["Top2"].stateBlock.bitOffset, Is.EqualTo(4));
        Assert.That(joystick["Pinkie"].stateBlock.bitOffset, Is.EqualTo(5));
        Assert.That(joystick["Base"].stateBlock.bitOffset, Is.EqualTo(6));
        Assert.That(joystick["Base2"].stateBlock.bitOffset, Is.EqualTo(7));
        Assert.That(joystick["Base3"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(joystick["Base4"].stateBlock.bitOffset, Is.EqualTo(1));
        Assert.That(joystick["Base5"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(joystick["Base6"].stateBlock.bitOffset, Is.EqualTo(3));

        Assert.That(joystick["Trigger"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Thumb"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Thumb2"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Top"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Top2"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Pinkie"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base2"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base3"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base4"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base5"].stateBlock.sizeInBits, Is.EqualTo(1));
        Assert.That(joystick["Base6"].stateBlock.sizeInBits, Is.EqualTo(1));

        // Stick.
        Assert.That(joystick["Stick"], Is.TypeOf<StickControl>());
        Assert.That(joystick["Stick"].stateBlock.byteOffset, Is.EqualTo(4));
        Assert.That(joystick["Stick"].stateBlock.sizeInBits, Is.EqualTo(64));
        Assert.That(joystick["Stick/x"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Stick/y"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Stick/x"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Stick/y"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Stick/x"].stateBlock.byteOffset, Is.EqualTo(4)); // Parent offset baked in.
        Assert.That(joystick["Stick/y"].stateBlock.byteOffset, Is.EqualTo(8));

        // Axes.
        Assert.That(joystick["RotateZ"], Is.TypeOf<AxisControl>());
        Assert.That(joystick["RotateZ"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["RotateZ"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["RotateZ"].stateBlock.byteOffset, Is.EqualTo(12));
        Assert.That(joystick["Throttle"], Is.TypeOf<AxisControl>());
        Assert.That(joystick["Throttle"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Throttle"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Throttle"].stateBlock.byteOffset, Is.EqualTo(16));

        // Hat.
        Assert.That(joystick["Hat"], Is.TypeOf<DpadControl>());
        Assert.That(joystick["Hat"].stateBlock.byteOffset, Is.EqualTo(20));
        Assert.That(joystick["Hat"].stateBlock.sizeInBits, Is.EqualTo(64));
        Assert.That(joystick["Hat/up"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Hat/down"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Hat/left"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Hat/right"].stateBlock.format, Is.EqualTo(InputStateBlock.FormatInt));
        Assert.That(joystick["Hat/up"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Hat/down"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Hat/left"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Hat/right"].stateBlock.sizeInBits, Is.EqualTo(32));
        Assert.That(joystick["Hat/up"].stateBlock.byteOffset, Is.EqualTo(24)); // Parent offset baked in.
        Assert.That(joystick["Hat/down"].stateBlock.byteOffset, Is.EqualTo(24));
        Assert.That(joystick["Hat/left"].stateBlock.byteOffset, Is.EqualTo(20));
        Assert.That(joystick["Hat/right"].stateBlock.byteOffset, Is.EqualTo(20));

        // Control properties.
        Assert.That(joystick.trigger, Is.SameAs(joystick["Trigger"]));
        Assert.That(joystick.stick, Is.SameAs(joystick["Stick"]));
        Assert.That(joystick.twist, Is.SameAs(joystick["RotateZ"]));

        Assert.That(joystick["Thumb"].stateBlock.bitOffset, Is.EqualTo(1));
        Assert.That(joystick["Thumb2"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(joystick["Top"].stateBlock.bitOffset, Is.EqualTo(3));
        Assert.That(joystick["Top2"].stateBlock.bitOffset, Is.EqualTo(4));
        Assert.That(joystick["Pinkie"].stateBlock.bitOffset, Is.EqualTo(5));
        Assert.That(joystick["Base"].stateBlock.bitOffset, Is.EqualTo(6));
        Assert.That(joystick["Base2"].stateBlock.bitOffset, Is.EqualTo(7));
        Assert.That(joystick["Base3"].stateBlock.bitOffset, Is.EqualTo(0));
        Assert.That(joystick["Base4"].stateBlock.bitOffset, Is.EqualTo(1));
        Assert.That(joystick["Base5"].stateBlock.bitOffset, Is.EqualTo(2));
        Assert.That(joystick["Base6"].stateBlock.bitOffset, Is.EqualTo(3));

        // Check button presses.
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Trigger), (ButtonControl)joystick["Trigger"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Thumb), (ButtonControl)joystick["Thumb"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Thumb2), (ButtonControl)joystick["Thumb2"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Top), (ButtonControl)joystick["Top"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Top2), (ButtonControl)joystick["Top2"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Pinkie), (ButtonControl)joystick["Pinkie"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base), (ButtonControl)joystick["Base"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base2), (ButtonControl)joystick["Base2"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base3), (ButtonControl)joystick["Base3"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base4), (ButtonControl)joystick["Base4"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base5), (ButtonControl)joystick["Base5"]);
        AssertButtonPress(joystick, new TestSDLJoystick().WithButton(SDLButtonUsage.Base6), (ButtonControl)joystick["Base6"]);

        // Check axis motion.
        InputSystem.QueueStateEvent(joystick, new TestSDLJoystick
        {
            hatX = int.MaxValue,
            hatY = int.MinValue,
            rotateZAxis = int.MaxValue,
            xAxis = short.MinValue,
            yAxis = short.MaxValue,
            throttleAxis = int.MinValue,
        });
        InputSystem.Update();

        Assert.That(joystick.stick.x.ReadUnprocessedValue(), Is.EqualTo(-1).Within(0.00001));
        Assert.That(joystick.stick.y.ReadUnprocessedValue(), Is.EqualTo(-1).Within(0.00001));
        Assert.That(joystick.stick.up.isPressed, Is.False);
        Assert.That(joystick.stick.down.isPressed, Is.True);
        Assert.That(joystick.stick.left.isPressed, Is.True);
        Assert.That(joystick.stick.right.isPressed, Is.False);
        Assert.That(joystick.twist.ReadUnprocessedValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(joystick["Throttle"].ReadValueAsObject(), Is.EqualTo(-1).Within(0.00001));

        InputSystem.QueueStateEvent(joystick, new TestSDLJoystick
        {
            hatX = int.MinValue,
            hatY = int.MaxValue,
            rotateZAxis = int.MinValue,
            xAxis = short.MaxValue,
            yAxis = short.MinValue,
            throttleAxis = int.MaxValue,
        });
        InputSystem.Update();

        Assert.That(joystick.stick.x.ReadUnprocessedValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(joystick.stick.y.ReadUnprocessedValue(), Is.EqualTo(1).Within(0.00001));
        Assert.That(joystick.stick.up.isPressed, Is.True);
        Assert.That(joystick.stick.down.isPressed, Is.False);
        Assert.That(joystick.stick.left.isPressed, Is.False);
        Assert.That(joystick.stick.right.isPressed, Is.True);
        Assert.That(joystick.twist.ReadUnprocessedValue(), Is.EqualTo(-1).Within(0.00001));
        Assert.That(joystick["Throttle"].ReadValueAsObject(), Is.EqualTo(1).Within(0.00001));

        InputSystem.QueueStateEvent(joystick, new TestSDLJoystick());
        InputSystem.Update();

        Assert.That(joystick.stick.x.ReadUnprocessedValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(joystick.stick.y.ReadUnprocessedValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(joystick.stick.up.isPressed, Is.False);
        Assert.That(joystick.stick.down.isPressed, Is.False);
        Assert.That(joystick.stick.left.isPressed, Is.False);
        Assert.That(joystick.stick.right.isPressed, Is.False);
        Assert.That(joystick.twist.ReadUnprocessedValue(), Is.EqualTo(0).Within(0.00001));
        Assert.That(joystick["Throttle"].ReadValueAsObject(), Is.EqualTo(0).Within(0.00001));
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct TestSDLJoystick : IInputStateTypeInfo
    {
        [FieldOffset(0)] public int buttons;
        [FieldOffset(4)] public int xAxis;
        [FieldOffset(8)] public int yAxis;
        [FieldOffset(12)] public int rotateZAxis;
        [FieldOffset(16)] public int throttleAxis;
        [FieldOffset(20)] public int hatX;
        [FieldOffset(24)] public int hatY;

        public TestSDLJoystick WithButton(SDLButtonUsage usage, bool value = true)
        {
            Debug.Assert((int)SDLButtonUsage.Count <= 32);
            var bitMask = 1 << ((int)usage - 1);

            if (value)
                buttons |= bitMask;
            else
                buttons &= ~bitMask;
            return this;
        }

        public FourCC format
        {
            get { return new FourCC('L', 'J', 'O', 'Y'); }
        }

        public static readonly string descriptorString =
            "{\"interface\":\"Linux\"," +
            "\"type\":\"Joystick\"," +
            "\"product\":\"TestProduct\"," +
            "\"manufacturer\":\"TestManufacturer\"," +
            "\"serial\":\"030000006d04000015c2000010010000\"," +
            "\"version\":\"272\"," +
            "\"capabilities\":\"{" +
            "\\\"controls\\\":[" +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":1,\\\"bit\\\":0}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":2,\\\"bit\\\":1}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":3,\\\"bit\\\":2}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":4,\\\"bit\\\":3}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":5,\\\"bit\\\":4}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":6,\\\"bit\\\":5}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":7,\\\"bit\\\":6}," +
            "{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":8,\\\"bit\\\":7}," +
            "{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":9,\\\"bit\\\":0}," +
            "{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":10,\\\"bit\\\":1}," +
            "{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":11,\\\"bit\\\":2}," +
            "{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":12,\\\"bit\\\":3}," +
            "{\\\"offset\\\":4,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":1,\\\"min\\\":-32768,\\\"max\\\":32767}," +
            "{\\\"offset\\\":8,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":2,\\\"min\\\":-32768,\\\"max\\\":32767}," +
            "{\\\"offset\\\":12,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":6,\\\"min\\\":-32768,\\\"max\\\":32767}," +
            "{\\\"offset\\\":16,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":7,\\\"min\\\":-32768,\\\"max\\\":32767}," +
            "{\\\"offset\\\":20,\\\"featureSize\\\":4,\\\"featureType\\\":4,\\\"usageHint\\\":12,\\\"min\\\":-1,\\\"max\\\":1}," +
            "{\\\"offset\\\":24,\\\"featureSize\\\":4,\\\"featureType\\\":4,\\\"usageHint\\\":13,\\\"min\\\":-1,\\\"max\\\":1}]}\"}";
    }
}
#endif // UNITY_EDITOR || UNITY_STANDALONE_LINUX
