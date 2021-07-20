#if UNITY_EDITOR || UNITY_ANDROID || PACKAGE_DOCS_GENERATION
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Android.LowLevel;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Android.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct AndroidGameControllerState : IInputStateTypeInfo
    {
        public const int MaxAxes = 48;
        public const int MaxButtons = 220;

        internal const string kVariantGamepad = "Gamepad";
        internal const string kVariantJoystick = "Joystick";
        internal const string kVariantDPadAxes = "DpadAxes";
        internal const string kVariantDPadButtons = "DpadButtons";

        internal const uint kAxisOffset = sizeof(uint) * (uint)((MaxButtons + 31) / 32);

        public static FourCC kFormat = new FourCC('A', 'G', 'C', ' ');

        [InputControl(name = "dpad", layout = "Dpad", bit = (uint)AndroidKeyCode.DpadUp, sizeInBits = 4, variants = kVariantDPadButtons)]
        [InputControl(name = "dpad/up", bit = (uint)AndroidKeyCode.DpadUp, variants = kVariantDPadButtons)]
        [InputControl(name = "dpad/down", bit = (uint)AndroidKeyCode.DpadDown, variants = kVariantDPadButtons)]
        [InputControl(name = "dpad/left", bit = (uint)AndroidKeyCode.DpadLeft, variants = kVariantDPadButtons)]
        [InputControl(name = "dpad/right", bit = (uint)AndroidKeyCode.DpadRight, variants = kVariantDPadButtons)]
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
        public fixed uint buttons[(MaxButtons + 31) / 32];

        [InputControl(name = "dpad", layout = "Dpad", offset = (uint)AndroidAxis.HatX * sizeof(float) + kAxisOffset, format = "VEC2", sizeInBits = 64, variants = kVariantDPadAxes)]
        [InputControl(name = "dpad/right", offset = 0, bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=0,clampMax=1", variants = kVariantDPadAxes)]
        [InputControl(name = "dpad/left", offset = 0, bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert", variants = kVariantDPadAxes)]
        [InputControl(name = "dpad/down", offset = ((uint)AndroidAxis.HatY - (uint)AndroidAxis.HatX) * sizeof(float), bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=0,clampMax=1", variants = kVariantDPadAxes)]
        [InputControl(name = "dpad/up", offset = ((uint)AndroidAxis.HatY - (uint)AndroidAxis.HatX) * sizeof(float), bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert", variants = kVariantDPadAxes)]
        [InputControl(name = "leftTrigger", offset = (uint)AndroidAxis.Brake * sizeof(float) + kAxisOffset, parameters = "clamp=1,clampMin=0,clampMax=1.0", variants = kVariantGamepad)]
        [InputControl(name = "rightTrigger", offset = (uint)AndroidAxis.Gas * sizeof(float) + kAxisOffset, parameters = "clamp=1,clampMin=0,clampMax=1.0", variants = kVariantGamepad)]
        [InputControl(name = "leftStick", variants = kVariantGamepad)]
        [InputControl(name = "leftStick/y", variants = kVariantGamepad, parameters = "invert")]
        [InputControl(name = "leftStick/up", variants = kVariantGamepad, parameters = "invert,clamp=1,clampMin=-1.0,clampMax=0.0")]
        [InputControl(name = "leftStick/down", variants = kVariantGamepad, parameters = "invert=false,clamp=1,clampMin=0,clampMax=1.0")]
        ////FIXME: state for this control is not contiguous
        [InputControl(name = "rightStick", offset = (uint)AndroidAxis.Z * sizeof(float) + kAxisOffset, sizeInBits = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float) * 8, variants = kVariantGamepad)]
        [InputControl(name = "rightStick/x", variants = kVariantGamepad)]
        [InputControl(name = "rightStick/y", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = kVariantGamepad, parameters = "invert")]
        [InputControl(name = "rightStick/up", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = kVariantGamepad, parameters = "invert,clamp=1,clampMin=-1.0,clampMax=0.0")]
        [InputControl(name = "rightStick/down", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = kVariantGamepad, parameters = "invert=false,clamp=1,clampMin=0,clampMax=1.0")]
        public fixed float axis[MaxAxes];

        public FourCC format
        {
            get { return kFormat; }
        }

        public AndroidGameControllerState WithButton(AndroidKeyCode code, bool value = true)
        {
            fixed(uint* buttonsPtr = buttons)
            {
                if (value)
                    buttonsPtr[(int)code / 32] |= 1U << ((int)code % 32);
                else
                    buttonsPtr[(int)code / 32] &= ~(1U << ((int)code % 32));
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
    internal enum AndroidInputSource
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
    internal struct AndroidDeviceCapabilities
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
                throw new ArgumentNullException(nameof(json));
            return JsonUtility.FromJson<AndroidDeviceCapabilities>(json);
        }

        public override string ToString()
        {
            return
                $"deviceDescriptor = {deviceDescriptor}, productId = {productId}, vendorId = {vendorId}, isVirtual = {isVirtual}, motionAxes = {(motionAxes == null ? "<null>" : String.Join(",", motionAxes.Select(i => i.ToString()).ToArray()))}, inputSources = {inputSources}";
        }
    }
}

namespace UnityEngine.InputSystem.Android
{
    ////FIXME: This is messy! Clean up!
    /// <summary>
    /// Gamepad on Android. Comprises all types of gamepads supported on Android.
    /// </summary>
    /// <remarks>
    /// Most of the gamepads:
    /// - ELAN PLAYSTATION(R)3 Controller
    /// - My-Power CO.,LTD. PS(R) Controller Adaptor
    /// (Following tested and work with: Nvidia Shield TV (OS 9); Galaxy Note 20 Ultra (OS 11); Galaxy S9+ (OS 10.0))
    /// - Sony Interactive Entertainment Wireless (PS4 DualShock) (Also tested and DOES NOT WORK with Galaxy S9 (OS 8.0); Galaxy S8 (OS 7.0); Xiaomi Mi Note2 (OS 8.0))
    /// - Xbox Wireless Controller (Xbox One) (Also tested and works on Samsung Galaxy S8 (OS 7.0); Xiaomi Mi Note2 (OS 8.0))
    /// - NVIDIA Controller v01.03/v01.04
    /// - (Add more)
    /// map buttons in the following way:
    ///  Left Stick -> AXIS_X(0) / AXIS_Y(1)
    ///  Right Stick -> AXIS_Z (11) / AXIS_RZ(14)
    ///  Right Thumb -> KEYCODE_BUTTON_THUMBR(107)
    ///  Left Thumb -> KEYCODE_BUTTON_THUMBL(106)
    ///  L1 (Left shoulder) -> KEYCODE_BUTTON_L1(102)
    ///  R1 (Right shoulder) -> KEYCODE_BUTTON_R1(103)
    ///  L2 (Left trigger) -> AXIS_BRAKE(23)
    ///  R2 (Right trigger) -> AXIS_GAS(22)
    ///  X -> KEYCODE_BUTTON_X(99)
    ///  Y -> KEYCODE_BUTTON_Y(100)
    ///  B -> KEYCODE_BUTTON_B(97)
    ///  A -> KEYCODE_BUTTON_A(96)
    ///  DPAD -> AXIS_HAT_X(15),AXIS_HAT_Y(16) or KEYCODE_DPAD_LEFT(21), KEYCODE_DPAD_RIGHT(22), KEYCODE_DPAD_UP(19), KEYCODE_DPAD_DOWN(20),
    /// Note: On Nvidia Shield Console, L2/R2 additionally invoke key events for AXIS_LTRIGGER, AXIS_RTRIGGER (in addition to AXIS_BRAKE, AXIS_GAS)
    ///       If you connect gamepad to a phone for L2/R2 only AXIS_BRAKE/AXIS_GAS come. AXIS_LTRIGGER, AXIS_RTRIGGER are not invoked.
    ///       That's why we map triggers only to AXIS_BRAKE/AXIS_GAS
    ///       Nvidia Shield also reports KEYCODE_BACK instead of KEYCODE_BUTTON_SELECT, so Options(XboxOne Controller)/View(DualShock)/Select buttons do not work
    /// PS4 controller is officially supported from Android 10 and higher (https://playstation.com/en-us/support/hardware/ps4-pair-dualshock-4-wireless-with-sony-xperia-and-android)
    /// However, some devices with older OS have fixed PS4 controller support on their drivers this leads to following situation:
    /// Some gamepads on Android devices (with same Android number version) might have different mappings
    ///  For ex., Dualshock, on NVidia Shield Console (OS 8.0) all buttons correctly map according to rules in AndroidGameControllerState
    ///           when clicking left shoulder it will go to AndroidKeyCode.ButtonL1, rightShoulder -> AndroidKeyCode.ButtonR1, etc
    ///           But, on Samsung Galaxy S9 (OS 8.0), the mapping is different (Same for Xiaomi Mi Note2 (OS 8.0), Samsung Galaxy S8 (OS 7.0))
    ///           when clicking left shoulder it will go to AndroidKeyCode.ButtonY, rightShoulder -> AndroidKeyCode.ButtonZ, etc
    ///  So even though Android version is 8.0 in both cases, Dualshock will only correctly work on NVidia Shield Console
    ///  It's obvious that this depends on the driver and not Android OS, thus we can only assume Samsung in this case doesn't properly support Dualshock in their drivers
    ///  While we can do custom mapping for Samsung, we can never now when will they try to update the driver for Dualshock or some other gamepad
    /// </remarks>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.kVariantGamepad)]
    [Scripting.Preserve]
    public class AndroidGamepad : Gamepad
    {
    }

    /// <summary>
    /// Generic controller with Dpad axes
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), hideInUI = true,
        variants = AndroidGameControllerState.kVariantGamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.kVariantDPadAxes)]
    [Scripting.Preserve]
    public class AndroidGamepadWithDpadAxes : AndroidGamepad
    {
    }

    /// <summary>
    /// Generic controller with Dpad buttons
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), hideInUI = true,
        variants = AndroidGameControllerState.kVariantGamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.kVariantDPadButtons)]
    [Scripting.Preserve]
    public class AndroidGamepadWithDpadButtons : AndroidGamepad
    {
    }

    /// <summary>
    /// Joystick on Android.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.kVariantJoystick)]
    [Scripting.Preserve]
    public class AndroidJoystick : Joystick
    {
    }

    /// <summary>
    /// A PlayStation DualShock 4 controller connected to an Android device.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), displayName = "Android DualShock 4 Gamepad",
        variants = AndroidGameControllerState.kVariantGamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.kVariantDPadAxes)]
    [Scripting.Preserve]
    public class DualShock4GamepadAndroid : DualShockGamepad
    {
    }

    /// <summary>
    /// A PlayStation DualShock 4 controller connected to an Android device.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), displayName = "Android Xbox One Controller",
        variants = AndroidGameControllerState.kVariantGamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.kVariantDPadAxes)]
    [Scripting.Preserve]
    public class XboxOneGamepadAndroid : XInput.XInputController
    {
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
