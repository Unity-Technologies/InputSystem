#if UNITY_EDITOR || UNITY_ANDROID
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.Plugins.Android.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.Android.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AndroidGameControllerState : IInputStateTypeInfo
    {
        public const int kMaxAndroidAxes = 48;
        public const int kMaxAndroidButtons = 220;

        public const string kVariantGamepad = "Gamepad";
        public const string kVariantJoystick = "Joystick";

        public const uint kAxisOffset = sizeof(uint) * (uint)((kMaxAndroidButtons + 31) / 32);

        public static FourCC kFormat = new FourCC('A', 'G', 'C', ' ');

        [InputControl(name = "buttonSouth", bit = (uint)AndroidKeyCode.ButtonA, variants = kVariantGamepad)]
        [InputControl(name = "buttonWest", bit = (uint)AndroidKeyCode.ButtonX, variants = kVariantGamepad)]
        [InputControl(name = "buttonNorth", bit = (uint)AndroidKeyCode.ButtonY, variants = kVariantGamepad)]
        [InputControl(name = "buttonEast", bit = (uint)AndroidKeyCode.ButtonB, variants = kVariantGamepad)]
        [InputControl(name = "leftStickPress", bit = (uint)AndroidKeyCode.ButtonThumbl, variants = kVariantGamepad)]
        [InputControl(name = "rightStickPress", bit = (uint)AndroidKeyCode.ButtonThumbr, variants = kVariantGamepad)]
        [InputControl(name = "leftShoulder", bit = (uint)AndroidKeyCode.ButtonL1, variants = kVariantGamepad)]
        [InputControl(name = "rightShoulder", bit = (uint)AndroidKeyCode.ButtonR1, variants = kVariantGamepad)]
        [InputControl(name = "start", bit = (uint)AndroidKeyCode.ButtonStart, variants = kVariantGamepad)]
        [InputControl(name = "select", bit = (uint)AndroidKeyCode.ButtonSelect, variants = kVariantGamepad)]
        public fixed uint buttons[(kMaxAndroidButtons + 31) / 32];

        [InputControl(name = "leftTrigger", offset = (uint)AndroidAxis.Brake * sizeof(float) + kAxisOffset, variants = kVariantGamepad)]
        [InputControl(name = "rightTrigger", offset = (uint)AndroidAxis.Gas * sizeof(float) + kAxisOffset, variants = kVariantGamepad)]
        [InputControl(name = "leftStick", variants = kVariantGamepad)]
        [InputControl(name = "leftStick/y", variants = kVariantGamepad, parameters = "invert")]
        ////FIXME: state for this control is not contiguous
        [InputControl(name = "rightStick", offset = (uint)AndroidAxis.Z * sizeof(float) + kAxisOffset, sizeInBits = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float) * 8, variants = kVariantGamepad)]
        [InputControl(name = "rightStick/x", variants = kVariantGamepad)]
        [InputControl(name = "rightStick/y", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = kVariantGamepad, parameters = "invert")]
        public fixed float axis[kMaxAndroidAxes];

        public FourCC GetFormat()
        {
            return kFormat;
        }

        public AndroidGameControllerState WithButton(AndroidKeyCode code, bool value = true)
        {
            fixed(uint* buttonsPtr = buttons)
            {
                if (value)
                    buttonsPtr[(int)code / 32] |= (uint)1 << ((int)code % 32);
                else
                    buttonsPtr[(int)code / 32] &= ~((uint)1 << ((int)code % 32));
            }
            return this;
        }

        public AndroidGameControllerState WithAxis(AndroidAxis axis, float value)
        {
            fixed(float* axisPtr = this.axis)
            {
                axisPtr[(int)axis] = value;
            }
            return this;
        }
    }

    // See https://developer.android.com/reference/android/view/InputDevice.html for input source values
    [Flags]
    public enum AndroidInputSource
    {
        Keyboard = 257,
        Dpad = 513,
        Gamepad = 1025,
        Touchscreen = 4098,
        Mouse = 8194,
        Stylus = 16386,
        Trackball = 65540,
        Touchpad = 1048584,
        Joystick = 16777232
    }

    [Serializable]
    public struct AndroidDeviceCapabilities
    {
        public string deviceDescriptor;
        public int productId;
        public int vendorId;
        public bool isVirtual;
        public AndroidAxis[] motionAxes;
        public AndroidInputSource inputSources;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static AndroidDeviceCapabilities FromJson(string json)
        {
            if (json == null)
                throw new ArgumentNullException("json");
            return JsonUtility.FromJson<AndroidDeviceCapabilities>(json);
        }

        public override string ToString()
        {
            return string.Format(
                "deviceDescriptor = {0}, productId = {1}, vendorId = {2}, isVirtual = {3}, motionAxes = {4}, inputSources = {5}",
                deviceDescriptor,
                productId,
                vendorId,
                isVirtual,
                motionAxes == null ? "<null>" : String.Join(",", motionAxes.Select(i => i.ToString()).ToArray()),
                inputSources);
        }
    }
}

namespace UnityEngine.Experimental.Input.Plugins.Android
{
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.kVariantGamepad)]
    public class AndroidGamepad : Gamepad
    {
    }

    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.kVariantJoystick)]
    public class AndroidJoystick : Joystick
    {
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
