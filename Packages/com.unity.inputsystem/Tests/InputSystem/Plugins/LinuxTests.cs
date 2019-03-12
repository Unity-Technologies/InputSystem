#if UNITY_EDITOR || UNITY_STANDALONE // UNITY_STANDALONE_LINUX
using NUnit.Framework;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.Linux;
using UnityEngine.Experimental.Input.Controls;

internal class LinuxTests : InputTestFixture
{
    [StructLayout(LayoutKind.Explicit)]
    unsafe struct TestSDLJoystick : IInputStateTypeInfo
    {
        [FieldOffset(0)] public byte buttonMask1;
        [FieldOffset(1)] public uint buttonMask2;
        [FieldOffset(4)] public int xAxis;
        [FieldOffset(8)] public int yAxis;
        [FieldOffset(12)] public int rotateZAxis;
        [FieldOffset(16)] public int ThrottleAxis;
        [FieldOffset(20)] public int hat1;
        [FieldOffset(24)] public int hat2;

        public void SetButtonValue(SDLButtonUsage usage, bool value)
        {
            bool useButtonMask2 = false;
            byte bitflag = 0;
            switch (usage)
            {
                case SDLButtonUsage.Trigger:
                {
                    bitflag = 1 >> 0;
                }
                break;
                case SDLButtonUsage.Thumb:
                {
                    bitflag = 1 >> 1;
                }
                break;
                case SDLButtonUsage.Thumb2:
                {
                    bitflag = 1 >> 2;
                }
                break;
                case SDLButtonUsage.Top:
                {
                    bitflag = 1 >> 3;
                }
                break;
                case SDLButtonUsage.Top2:
                {
                    bitflag = 1 >> 4;
                }
                break;
                case SDLButtonUsage.Pinkie:
                {
                    bitflag = 1 >> 5;
                }
                break;
                case SDLButtonUsage.Base:
                {
                    bitflag = 1 >> 6;
                }
                break;
                case SDLButtonUsage.Base2:
                {
                    bitflag = 1 >> 7;
                }
                break;
                case SDLButtonUsage.Base3:
                {
                    useButtonMask2 = true;
                    bitflag = 1 >> 0;
                }
                break;
                case SDLButtonUsage.Base4:
                {
                    useButtonMask2 = true;
                    bitflag = 1 >> 1;
                }
                break;
                case SDLButtonUsage.Base5:
                {
                    useButtonMask2 = true;
                    bitflag = 1 >> 2;
                }
                break;
                case SDLButtonUsage.Base6:
                {
                    useButtonMask2 = true;
                    bitflag = 1 >> 3;
                }
                break;
                default:
                    //Do Nothing
                    break;
            }

            if (bitflag != 0)
            {
                if (value)
                {
                    if (useButtonMask2)
                    {
                        buttonMask2 |= bitflag;
                    }
                    else
                    {
                        buttonMask1 |= bitflag;
                    }
                }
                else
                {
                    if (useButtonMask2)
                    {
                        buttonMask2 &= (byte)~bitflag;
                    }
                    else
                    {
                        buttonMask1 &= (byte)~bitflag;
                    }
                }
            }
        }

        public FourCC GetFormat()
        {
            return new FourCC('L', 'J', 'O', 'Y');
        }

        public static readonly string descriptorString = "{\"interface\":\"Linux\",\"type\":\"Joystick\",\"product\":\"TestProduct\",\"manufacturer\":\"TestManufacturer\",\"serial\":\"030000006d04000015c2000010010000\",\"version\":\"272\",\"capabilities\":\"{\\\"controls\\\":[{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":1,\\\"bit\\\":0},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":2,\\\"bit\\\":1},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":3,\\\"bit\\\":2},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":4,\\\"bit\\\":3},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":5,\\\"bit\\\":4},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":6,\\\"bit\\\":5},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":7,\\\"bit\\\":6},{\\\"offset\\\":0,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":8,\\\"bit\\\":7},{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":9,\\\"bit\\\":0},{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":10,\\\"bit\\\":1},{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":11,\\\"bit\\\":2},{\\\"offset\\\":1,\\\"featureSize\\\":1,\\\"featureType\\\":3,\\\"usageHint\\\":12,\\\"bit\\\":3},{\\\"offset\\\":4,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":1,\\\"min\\\":-32768,\\\"max\\\":32767},{\\\"offset\\\":8,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":2,\\\"min\\\":-32768,\\\"max\\\":32767},{\\\"offset\\\":12,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":6,\\\"min\\\":-32768,\\\"max\\\":32767},{\\\"offset\\\":16,\\\"featureSize\\\":4,\\\"featureType\\\":1,\\\"usageHint\\\":7,\\\"min\\\":-32768,\\\"max\\\":32767},{\\\"offset\\\":20,\\\"featureSize\\\":4,\\\"featureType\\\":4,\\\"usageHint\\\":12,\\\"min\\\":-1,\\\"max\\\":1},{\\\"offset\\\":24,\\\"featureSize\\\":4,\\\"featureType\\\":4,\\\"usageHint\\\":13,\\\"min\\\":-1,\\\"max\\\":1}]}\"}";
    }

    [Test]
    public void Layout_LinuxLayoutTest()
    {
        runtime.ReportNewInputDevice(TestSDLJoystick.descriptorString);

        InputSystem.Update();

        var generatedLayout = InputSystem.TryLoadLayout("Linux::TestManufacturer::TestProduct");

        Assert.That(generatedLayout, Is.Not.Null);
        Assert.That(generatedLayout.controls, Has.Count.EqualTo(16));

        var triggerControl = generatedLayout.controls[0];
        Assert.That(triggerControl.name, Is.EqualTo(SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Trigger)));
        Assert.That(triggerControl.offset, Is.EqualTo(0));
        Assert.That(triggerControl.layout, Is.EqualTo(new InternedString("Button")));
    }

    [Test]
    public void State_LinuxDeviceTest()
    {
        runtime.ReportNewInputDevice(TestSDLJoystick.descriptorString);

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var createdDevice = InputSystem.devices[0];

        Assert.That(createdDevice.allControls, Has.Count.EqualTo(16));

        InputControl triggerControl = createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Trigger)];
        Assert.That(triggerControl, Is.TypeOf(typeof(ButtonControl)));

        InputControl thumbControl = createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Thumb)];
        Assert.That(thumbControl, Is.TypeOf(typeof(ButtonControl)));

        InputControl thumb2Control = createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Thumb2)];
        Assert.That(thumb2Control, Is.TypeOf(typeof(ButtonControl)));

        InputControl topControl = createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Top)];;
        Assert.That(topControl, Is.TypeOf(typeof(ButtonControl)));

        InputControl top2Control = createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Top2)];
        Assert.That(top2Control, Is.TypeOf(typeof(ButtonControl)));

        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Top)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Top2)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Pinkie)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base2)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base3)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base4)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base5)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Base6)], Is.Not.Null);

        Assert.That(createdDevice[SDLSupport.GetAxisNameFromUsage(SDLAxisUsage.X)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetAxisNameFromUsage(SDLAxisUsage.Y)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetAxisNameFromUsage(SDLAxisUsage.RotateZ)], Is.Not.Null);
        Assert.That(createdDevice[SDLSupport.GetAxisNameFromUsage(SDLAxisUsage.Throttle)], Is.Not.Null);
    }

    [Test]
    public void State_LinuxStateTest()
    {
        runtime.ReportNewInputDevice(TestSDLJoystick.descriptorString);

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var device = InputSystem.devices[0];

        TestSDLJoystick joystickState = new TestSDLJoystick();

        ButtonControl triggerControl = device[SDLSupport.GetButtonNameFromUsage(SDLButtonUsage.Trigger)] as ButtonControl;
        Assert.That(triggerControl.isPressed, Is.False);

        joystickState.SetButtonValue(SDLButtonUsage.Trigger, true);
        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        Assert.That(triggerControl.isPressed, Is.True);

        joystickState.SetButtonValue(SDLButtonUsage.Trigger, false);
        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        Assert.That(triggerControl.isPressed, Is.False);
    }

    [Test]
    public void State_LinuxJoystickStickAxesReportProperValues()
    {
        runtime.ReportNewInputDevice(TestSDLJoystick.descriptorString);

        InputSystem.Update();

        Assert.That(InputSystem.devices, Has.Count.EqualTo(1));
        var device = InputSystem.devices[0];

        ButtonControl leftButton = device["stick/left"] as ButtonControl;
        ButtonControl rightButton = device["stick/right"] as ButtonControl;
        AxisControl xAxis = device["stick/x"] as AxisControl;
        ButtonControl downButton = device["stick/down"] as ButtonControl;
        ButtonControl upButton = device["stick/up"] as ButtonControl;
        AxisControl yAxis = device["stick/y"] as AxisControl;

        TestSDLJoystick joystickState = new TestSDLJoystick();

        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        joystickState.yAxis = short.MaxValue;
        joystickState.xAxis = short.MaxValue;

        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        float leftValue = leftButton.ReadValue();
        float rightValue = rightButton.ReadValue();
        float xValue = xAxis.ReadValue();
        float downValue = downButton.ReadValue();
        float upValue = upButton.ReadValue();
        float yValue = yAxis.ReadValue();
        Assert.That(downValue, Is.EqualTo(1.0f));
        Assert.That(upValue, Is.EqualTo(0.0f));
        Assert.That(yValue, Is.EqualTo(-1.0f));
        Assert.That(leftValue, Is.EqualTo(0.0f));
        Assert.That(rightValue, Is.EqualTo(1.0f));
        Assert.That(xValue, Is.EqualTo(1.0f));

        joystickState.yAxis = short.MinValue;
        joystickState.xAxis = short.MinValue;

        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        leftValue = leftButton.ReadValue();
        rightValue = rightButton.ReadValue();
        xValue = xAxis.ReadValue();
        downValue = downButton.ReadValue();
        upValue = upButton.ReadValue();
        yValue = yAxis.ReadValue();
        Assert.That(downValue, Is.EqualTo(0.0f));
        Assert.That(upValue, Is.EqualTo(1.0f));
        Assert.That(yValue, Is.EqualTo(1.0f));
        Assert.That(leftValue, Is.EqualTo(1.0f));
        Assert.That(rightValue, Is.EqualTo(0.0f));
        Assert.That(xValue, Is.EqualTo(-1.0f));

        joystickState.yAxis = 0;
        joystickState.xAxis = 0;

        InputSystem.QueueStateEvent(device, joystickState);
        InputSystem.Update();

        leftValue = leftButton.ReadValue();
        rightValue = rightButton.ReadValue();
        xValue = xAxis.ReadValue();
        downValue = downButton.ReadValue();
        upValue = upButton.ReadValue();
        yValue = yAxis.ReadValue();
        Assert.That(downValue, Is.EqualTo(0.0f));
        Assert.That(upValue, Is.EqualTo(0.0f));
        Assert.That(yValue, Is.EqualTo(0.0f));
        Assert.That(leftValue, Is.EqualTo(0.0f));
        Assert.That(rightValue, Is.EqualTo(0.0f));
        Assert.That(xValue, Is.EqualTo(0.0f));
    }
}
#endif //UNITY_EDITOR || UNITY_STANDALONE // UNITY_STANDALONE_LINUX
