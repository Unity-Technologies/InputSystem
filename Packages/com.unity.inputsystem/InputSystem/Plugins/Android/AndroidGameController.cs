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
    /// <summary>
    /// Default state layout for Android game controller.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct AndroidGameControllerState : IInputStateTypeInfo
    {
        public const int MaxAxes = 48;
        public const int MaxButtons = 220;

        public class Variants
        {
            public const string Gamepad = "Gamepad";
            public const string Joystick = "Joystick";
            public const string DPadAxes = "DpadAxes";
            public const string DPadButtons = "DpadButtons";
        }

        internal const uint kAxisOffset = sizeof(uint) * (uint)((MaxButtons + 31) / 32);

        public static FourCC kFormat = new FourCC('A', 'G', 'C', ' ');

        [InputControl(name = "dpad", layout = "Dpad", bit = (uint)AndroidKeyCode.DpadUp, sizeInBits = 4, variants = Variants.DPadButtons)]
        [InputControl(name = "dpad/up", bit = (uint)AndroidKeyCode.DpadUp, variants = Variants.DPadButtons)]
        [InputControl(name = "dpad/down", bit = (uint)AndroidKeyCode.DpadDown, variants = Variants.DPadButtons)]
        [InputControl(name = "dpad/left", bit = (uint)AndroidKeyCode.DpadLeft, variants = Variants.DPadButtons)]
        [InputControl(name = "dpad/right", bit = (uint)AndroidKeyCode.DpadRight, variants = Variants.DPadButtons)]
        [InputControl(name = "buttonSouth", bit = (uint)AndroidKeyCode.ButtonA, variants = Variants.Gamepad)]
        [InputControl(name = "buttonWest", bit = (uint)AndroidKeyCode.ButtonX, variants = Variants.Gamepad)]
        [InputControl(name = "buttonNorth", bit = (uint)AndroidKeyCode.ButtonY, variants = Variants.Gamepad)]
        [InputControl(name = "buttonEast", bit = (uint)AndroidKeyCode.ButtonB, variants = Variants.Gamepad)]
        [InputControl(name = "leftStickPress", bit = (uint)AndroidKeyCode.ButtonThumbl, variants = Variants.Gamepad)]
        [InputControl(name = "rightStickPress", bit = (uint)AndroidKeyCode.ButtonThumbr, variants = Variants.Gamepad)]
        [InputControl(name = "leftShoulder", bit = (uint)AndroidKeyCode.ButtonL1, variants = Variants.Gamepad)]
        [InputControl(name = "rightShoulder", bit = (uint)AndroidKeyCode.ButtonR1, variants = Variants.Gamepad)]
        [InputControl(name = "start", bit = (uint)AndroidKeyCode.ButtonStart, variants = Variants.Gamepad)]
        [InputControl(name = "select", bit = (uint)AndroidKeyCode.ButtonSelect, variants = Variants.Gamepad)]
        public fixed uint buttons[(MaxButtons + 31) / 32];

        [InputControl(name = "dpad", layout = "Dpad", offset = (uint)AndroidAxis.HatX * sizeof(float) + kAxisOffset, format = "VEC2", sizeInBits = 64, variants = Variants.DPadAxes)]
        [InputControl(name = "dpad/right", offset = 0, bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=0,clampMax=1", variants = Variants.DPadAxes)]
        [InputControl(name = "dpad/left", offset = 0, bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert", variants = Variants.DPadAxes)]
        [InputControl(name = "dpad/down", offset = ((uint)AndroidAxis.HatY - (uint)AndroidAxis.HatX) * sizeof(float), bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=0,clampMax=1", variants = Variants.DPadAxes)]
        [InputControl(name = "dpad/up", offset = ((uint)AndroidAxis.HatY - (uint)AndroidAxis.HatX) * sizeof(float), bit = 0, sizeInBits = 32, format = "FLT", parameters = "clamp=3,clampConstant=0,clampMin=-1,clampMax=0,invert", variants = Variants.DPadAxes)]
        [InputControl(name = "leftTrigger", offset = (uint)AndroidAxis.Brake * sizeof(float) + kAxisOffset, parameters = "clamp=1,clampMin=0,clampMax=1.0", variants = Variants.Gamepad)]
        [InputControl(name = "rightTrigger", offset = (uint)AndroidAxis.Gas * sizeof(float) + kAxisOffset, parameters = "clamp=1,clampMin=0,clampMax=1.0", variants = Variants.Gamepad)]
        [InputControl(name = "leftStick", variants = Variants.Gamepad)]
        [InputControl(name = "leftStick/y", variants = Variants.Gamepad, parameters = "invert")]
        [InputControl(name = "leftStick/up", variants = Variants.Gamepad, parameters = "invert,clamp=1,clampMin=-1.0,clampMax=0.0")]
        [InputControl(name = "leftStick/down", variants = Variants.Gamepad, parameters = "invert=false,clamp=1,clampMin=0,clampMax=1.0")]
        ////FIXME: state for this control is not contiguous
        [InputControl(name = "rightStick", offset = (uint)AndroidAxis.Z * sizeof(float) + kAxisOffset, sizeInBits = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z + 1) * sizeof(float) * 8, variants = Variants.Gamepad)]
        [InputControl(name = "rightStick/x", variants = Variants.Gamepad)]
        [InputControl(name = "rightStick/y", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = Variants.Gamepad, parameters = "invert")]
        [InputControl(name = "rightStick/up", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = Variants.Gamepad, parameters = "invert,clamp=1,clampMin=-1.0,clampMax=0.0")]
        [InputControl(name = "rightStick/down", offset = ((uint)AndroidAxis.Rz - (uint)AndroidAxis.Z) * sizeof(float), variants = Variants.Gamepad, parameters = "invert=false,clamp=1,clampMin=0,clampMax=1.0")]
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
    /// <summary>
    /// Represents a gamepad device on Android, providing unified support for various controller types.
    /// </summary>
    /// <remarks>
    /// This layout covers multiple Android-supported gamepads, including but not limited to:
    /// - ELAN PLAYSTATION(R)3 Controller
    /// - My-Power CO., LTD. PS(R) Controller Adaptor
    /// - Sony Interactive Entertainment Wireless (PS4 DualShock)
    /// - Xbox Wireless Controller (Xbox One)
    /// - NVIDIA Controller v01.03/v01.04
    /// - (More may be added later)
    ///
    /// ### Typical Android Axis and Key Mappings
    /// | Control                | Mapping                                            |
    /// |------------------------|----------------------------------------------------|
    /// | Left Stick             | AXIS_X(0) / AXIS_Y(1)                              |
    /// | Right Stick            | AXIS_Z(11) / AXIS_RZ(14)                           |
    /// | L1                     | KEYCODE_BUTTON_L1(102)                             |
    /// | R1                     | KEYCODE_BUTTON_R1(103)                             |
    /// | L2                     | AXIS_BRAKE(23)                                     |
    /// | R2                     | AXIS_GAS(22)                                       |
    /// | Left Thumb             | KEYCODE_BUTTON_THUMBL(106)                         |
    /// | Right Thumb            | KEYCODE_BUTTON_THUMBR(107)                         |
    /// | X                      | KEYCODE_BUTTON_X(99)                               |
    /// | Y                      | KEYCODE_BUTTON_Y(100)                              |
    /// | B                      | KEYCODE_BUTTON_B(97)                               |
    /// | A                      | KEYCODE_BUTTON_A(96)                               |
    /// | DPAD                   | AXIS_HAT_X(15), AXIS_HAT_Y(16) or KEYCODE_DPAD_*   |
    ///
    /// ### Notes
    /// - **NVIDIA Shield Console**
    ///   - The L2 and R2 triggers generate both `AXIS_BRAKE` / `AXIS_GAS` and `AXIS_LTRIGGER` / `AXIS_RTRIGGER` events.
    ///   - On most Android phones, only `AXIS_BRAKE` and `AXIS_GAS` are reported; `AXIS_LTRIGGER` and `AXIS_RTRIGGER` are not invoked.
    ///   - For consistency across devices, triggers are mapped exclusively to `AXIS_BRAKE` and `AXIS_GAS`.
    ///   - The Shield also reports `KEYCODE_BACK` instead of `KEYCODE_BUTTON_SELECT`, causing the **Options** (Xbox), **View** (DualShock), or **Select** buttons to be non-functional.
    ///
    /// - **PS4 Controller Compatibility**
    ///   - Official PS4 controller support is available starting from **Android 10 and later**
    ///     (see: https://playstation.com/en-us/support/hardware/ps4-pair-dualshock-4-wireless-with-sony-xperia-and-android).
    ///   - On older Android versions, driver implementations vary by manufacturer. Some vendors have partially fixed DualShock support in custom drivers, leading to inconsistent mappings.
    ///
    /// - **Driver-Dependent Behavior**
    ///   - Gamepad mappings may differ even between devices running the *same Android version*.
    ///     - For example, on **Android 8.0**:
    ///       - **NVIDIA Shield Console:** buttons map correctly according to `AndroidGameControllerState` (for example, `L1 → ButtonL1`, `R1 → ButtonR1`).
    ///       - **Samsung Galaxy S9 / S8** and **Xiaomi Mi Note2:** mappings are inconsistent (for example, `L1 → ButtonY`, `R1 → ButtonZ`).
    ///   - These discrepancies stem from device-specific **driver differences**, not the Android OS itself.
    ///
    /// Because mapping inconsistencies depend on vendor-specific drivers, it’s impractical to maintain per-device remaps.
    /// </remarks>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.Variants.Gamepad)]
    public class AndroidGamepad : Gamepad
    {
    }

    /// <summary>
    /// Generic controller with Dpad axes
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), hideInUI = true,
        variants = AndroidGameControllerState.Variants.Gamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.Variants.DPadAxes)]
    public class AndroidGamepadWithDpadAxes : AndroidGamepad
    {
    }

    /// <summary>
    /// Generic controller with Dpad buttons
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), hideInUI = true,
        variants = AndroidGameControllerState.Variants.Gamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.Variants.DPadButtons)]
    public class AndroidGamepadWithDpadButtons : AndroidGamepad
    {
    }

    /// <summary>
    /// Joystick on Android.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), variants = AndroidGameControllerState.Variants.Joystick)]
    public class AndroidJoystick : Joystick
    {
    }

    /// <summary>
    /// A PlayStation DualShock 4 controller connected to an Android device.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), displayName = "Android DualShock 4 Gamepad",
        variants = AndroidGameControllerState.Variants.Gamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.Variants.DPadAxes)]
    public class DualShock4GamepadAndroid : DualShockGamepad
    {
    }

    /// <summary>
    /// An XboxOne controller connected to an Android device.
    /// </summary>
    [InputControlLayout(stateType = typeof(AndroidGameControllerState), displayName = "Android Xbox One Controller",
        variants = AndroidGameControllerState.Variants.Gamepad + InputControlLayout.VariantSeparator + AndroidGameControllerState.Variants.DPadAxes)]
    public class XboxOneGamepadAndroid : XInput.XInputController
    {
    }
}
#endif // UNITY_EDITOR || UNITY_ANDROID
