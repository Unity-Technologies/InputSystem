using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A collection of common usage string values as reported by <see cref="InputControl.usages"/>.
    /// </summary>
    public static class CommonUsages
    {
        /// <summary>
        /// Primary 2D motion control.
        /// </summary>
        /// <remarks>
        /// Example: Left stick on a gamepad.
        /// </remarks>
        public static readonly InternedString Primary2DMotion = new InternedString("Primary2DMotion");

        /// <summary>
        /// Secondary 2D motion control.
        /// </summary>
        /// <remarks>
        /// Example: Right stick on a gamepad.
        /// </remarks>
        public static readonly InternedString Secondary2DMotion = new InternedString("Secondary2DMotion");

        /// <summary>
        /// The primary action control on any input device, such as a gamepad, mouse, or keyboard.
        /// </summary>
        /// <remarks>
        /// Example: Primary mouse button (left button on right-handed configuration, right button on left-handed configuration),
        /// south-button on a gamepad.
        /// </remarks>
        public static readonly InternedString PrimaryAction = new InternedString("PrimaryAction");

        /// <summary>
        /// Secondary action control on any input device, such as a gamepad, mouse, or keyboard.
        /// </summary>
        /// <remarks>
        /// Example: Secondary mouse button (right button on right-handed configuration, left button on left-handed configuration),
        /// east-button on a gamepad.
        /// </remarks>
        public static readonly InternedString SecondaryAction = new InternedString("SecondaryAction");

        /// <summary>
        /// The primary trigger control on input devices with triggers.
        /// </summary>
        /// <remarks>
        /// Example: Right trigger-button on a gamepad.
        /// </remarks>
        public static readonly InternedString PrimaryTrigger = new InternedString("PrimaryTrigger");

        /// <summary>
        /// The secondary trigger control on input devices with triggers.
        /// </summary>
        /// <remarks>
        /// Example: Left trigger-button on a gamepad.
        /// </remarks>
        public static readonly InternedString SecondaryTrigger = new InternedString("SecondaryTrigger");

        /// <summary>
        /// A modifier action control that modifies usage of other controls.
        /// </summary>
        /// <remarks>
        /// Example: Keyboard modifier keys like CTRL, SHIFT, ALT, OPTION, etc.
        /// </remarks>
        public static readonly InternedString Modifier = new InternedString("Modifier");

        /// <summary>
        /// The spatial position control on input devices with spatial tracking.
        /// </summary>
        /// <remarks>
        /// Example: User head position in tracking-space using e.g. a head-tracking system. This could for example be a VR tracking system or another user-facing tracking sensor.
        /// </remarks>
        public static readonly InternedString Position = new InternedString("Position");

        /// <summary>
        /// The spatial orientation control on input devices with spatial tracking.
        /// </summary>
        /// <remarks>
        /// Example: User head-orientation in tracking-space using e.g. a head-tracking system. This could for example be a VR tracking system or another user-facing tracking sensor.
        /// </remarks>
        public static readonly InternedString Orientation = new InternedString("Orientation");

        /// <summary>
        /// The primary hat-switch control on input devices with hat-switches such as joysticks or gamepads.
        /// </summary>
        /// <remarks>
        /// Example: Joystick or gamepad hat-switch.
        /// </remarks>
        public static readonly InternedString Hatswitch = new InternedString("Hatswitch");

        /// <summary>
        /// Button to navigate to previous location.
        /// </summary>
        /// <remarks>
        /// Example: Escape on keyboard, B button on gamepad.
        ///
        /// In general, the "Back" control is used for moving backwards in the navigation history
        /// of a UI. This is used, for example, in hierarchical menu structures to move back to parent menus
        /// (e.g. from the "Settings" menu back to the "Main" menu). Consoles generally have stringent requirements
        /// as to which button has to fulfill this role.
        /// </remarks>
        public static readonly InternedString Back = new InternedString("Back");

        /// <summary>
        /// Button to navigate to next location.
        /// </summary>
        public static readonly InternedString Forward = new InternedString("Forward");

        /// <summary>
        /// Button to bring up menu.
        /// </summary>
        public static readonly InternedString Menu = new InternedString("Menu");

        /// <summary>
        /// Button to confirm the current choice.
        /// </summary>
        public static readonly InternedString Submit = new InternedString("Submit");

        ////REVIEW: isn't this the same as "Back"?
        /// <summary>
        /// Button to not accept the current choice.
        /// </summary>
        public static readonly InternedString Cancel = new InternedString("Cancel");

        /// <summary>
        /// Horizontal motion axis.
        /// </summary>
        /// <remarks>
        /// Example: X axis on mouse.
        /// </remarks>
        public static readonly InternedString Horizontal = new InternedString("Horizontal");

        /// <summary>
        /// Vertical motion axis.
        /// </summary>
        /// <remarks>
        /// Example: Y axis on mouse.
        /// </remarks>
        public static readonly InternedString Vertical = new InternedString("Vertical");

        /// <summary>
        /// Rotation around single, fixed axis.
        /// </summary>
        /// <remarks>
        /// Example: twist on joystick or twist of pen (few pens support that).
        /// </remarks>
        public static readonly InternedString Twist = new InternedString("Twist");

        /// <summary>
        /// Pressure level axis.
        /// </summary>
        /// <remarks>
        /// Example: pen pressure.
        /// </remarks>
        public static readonly InternedString Pressure = new InternedString("Pressure");

        /// <summary>
        /// Axis to scroll horizontally.
        /// </summary>
        public static readonly InternedString ScrollHorizontal = new InternedString("ScrollHorizontal");

        /// <summary>
        /// Axis to scroll vertically.
        /// </summary>
        public static readonly InternedString ScrollVertical = new InternedString("ScrollVertical");

        /// <summary>
        /// A screen-space point.
        /// </summary>
        /// <remarks>
        /// Example: Touch contact point.
        /// </remarks>
        public static readonly InternedString Point = new InternedString("Point");

        /// <summary>
        /// Low-frequency haptic motor for force-feedback.
        /// </summary>
        public static readonly InternedString LowFreqMotor = new InternedString("LowFreqMotor");

        /// <summary>
        /// High-frequency haptic motor for force-feedback.
        /// </summary>
        public static readonly InternedString HighFreqMotor = new InternedString("HighFreqMotor");

        /// <summary>
        /// Device in left hand.
        /// </summary>
        /// <remarks>
        /// Example: left hand XR controller.
        /// </remarks>
        public static readonly InternedString LeftHand = new InternedString("LeftHand");

        /// <summary>
        /// Device in right hand.
        /// </summary>
        /// <remarks>
        /// Example: right hand XR controller.
        /// </remarks>
        public static readonly InternedString RightHand = new InternedString("RightHand");

        /// <summary>
        /// Axis representing charge of battery (1=full, 0=empty).
        /// </summary>
        public static readonly InternedString BatteryStrength = new InternedString("BatteryStrength");
    }
}
