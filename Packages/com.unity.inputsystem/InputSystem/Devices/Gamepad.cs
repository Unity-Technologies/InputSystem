using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

////TODO: come up with consistent naming for buttons; (xxxButton? xxx?)

////REVIEW: should we add a gyro as a standard feature of gamepads?

////TODO: allow to be used for mouse simulation

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Default state layout for gamepads.
    /// </summary>
    /// <remarks>
    /// Be aware that unlike some other devices such as <see cref="Mouse"/> or <see cref="Touchscreen"/>,
    /// gamepad devices tend to have wildly varying state formats, i.e. forms in which they internally
    /// store their input data. In practice, even on the same platform gamepads will often store
    /// their data in different formats. This means that <see cref="GamepadState"/> will often <em>not</em>
    /// be the format in which a particular gamepad (such as <see cref="XInput.XInputController"/>,
    /// for example) stores its data.
    ///
    /// If your gamepad data is arriving in a different format, you should extend the "Gamepad" layout and customize its Controls.
    ///
    /// A real-world example of this is the Xbox Controller on macOS, which is supported through HID. Its layout looks like this:
    ///
    /// <example>
    /// <code>
    /// {
    ///     "name" : "XboxGamepadOSX",
    ///     "extend" : "Gamepad",
    ///     "format" : "HID",
    ///     "device" : { "interface" : "HID", "product" : "Xbox.*Controller" },
    ///     "controls" : [
    ///         { "name" : "leftShoulder", "offset" : 2, "bit" : 8 },
    ///         { "name" : "rightShoulder", "offset" : 2, "bit" : 9 },
    ///         { "name" : "leftStickPress", "offset" : 2, "bit" : 14 },
    ///         { "name" : "rightStickPress", "offset" : 2, "bit" : 15 },
    ///         { "name" : "buttonSouth", "offset" : 2, "bit" : 12 },
    ///         { "name" : "buttonEast", "offset" : 2, "bit" : 13 },
    ///         { "name" : "buttonWest", "offset" : 2, "bit" : 14 },
    ///         { "name" : "buttonNorth", "offset" : 2, "bit" : 15 },
    ///         { "name" : "dpad", "offset" : 2 },
    ///         { "name" : "dpad/up", "offset" : 0, "bit" : 8 },
    ///         { "name" : "dpad/down", "offset" : 0, "bit" : 9 },
    ///         { "name" : "dpad/left", "offset" : 0, "bit" : 10 },
    ///         { "name" : "dpad/right", "offset" : 0, "bit" : 11 },
    ///         { "name" : "start", "offset" : 2, "bit" : 4 },
    ///         { "name" : "select", "offset" : 2, "bit" : 5 },
    ///         { "name" : "xbox", "offset" : 2, "bit" : 2, "layout" : "Button" },
    ///         { "name" : "leftTrigger", "offset" : 4, "format" : "BYTE" },
    ///         { "name" : "rightTrigger", "offset" : 5, "format" : "BYTE" },
    ///         { "name" : "leftStick", "offset" : 6, "format" : "VC2S" },
    ///         { "name" : "leftStick/x", "offset" : 0, "format" : "SHRT", "parameters" : "normalize,normalizeMin=-0.5,normalizeMax=0.5" },
    ///         { "name" : "leftStick/y", "offset" : 2, "format" : "SHRT", "parameters" : "invert,normalize,normalizeMin=-0.5,normalizeMax=0.5" },
    ///         { "name" : "rightStick", "offset" : 10, "format" : "VC2S" },
    ///         { "name" : "rightStick/x", "offset" : 0, "format" : "SHRT", "parameters" : "normalize,normalizeMin=-0.5,normalizeMax=0.5" },
    ///         { "name" : "rightStick/y", "offset" : 2, "format" : "SHRT", "parameters" : "invert,normalize,normalizeMin=-0.5,normalizeMax=0.5" }
    ///     ]
    /// }
    /// </code>
    /// </example>
    ///
    /// The same principle applies if some buttons on your Device are swapped, for example. In this case, you can remap their offsets.
    ///
    ///
    ///
    ///
    /// </remarks>
    /// <seealso cref="Gamepad"/>
    // NOTE: Must match GamepadInputState in native.
    [StructLayout(LayoutKind.Explicit, Size = 28)]
    public struct GamepadState : IInputStateTypeInfo
    {
        public static FourCC Format => new FourCC('G', 'P', 'A', 'D');

        // On Sony consoles, we use the platform defaults as the gamepad-wide short default names.
        #if UNITY_PS4 || UNITY_PS5
        internal const string ButtonSouthShortDisplayName = "Cross";
        internal const string ButtonNorthShortDisplayName = "Triangle";
        internal const string ButtonWestShortDisplayName = "Square";
        internal const string ButtonEastShortDisplayName = "Circle";
        #elif UNITY_SWITCH
        internal const string ButtonSouthShortDisplayName = "B";
        internal const string ButtonNorthShortDisplayName = "X";
        internal const string ButtonWestShortDisplayName = "Y";
        internal const string ButtonEastShortDisplayName = "A";
        #else
        internal const string ButtonSouthShortDisplayName = "A";
        internal const string ButtonNorthShortDisplayName = "Y";
        internal const string ButtonWestShortDisplayName = "X";
        internal const string ButtonEastShortDisplayName = "B";
        #endif

        /// <summary>
        /// Button bit mask.
        /// </summary>
        /// <value>Button bit mask.</value>
        /// <seealso cref="GamepadButton"/>
        /// <seealso cref="Gamepad.buttonSouth"/>
        /// <seealso cref="Gamepad.buttonNorth"/>
        /// <seealso cref="Gamepad.buttonWest"/>
        /// <seealso cref="Gamepad.buttonSouth"/>
        /// <seealso cref="Gamepad.leftShoulder"/>
        /// <seealso cref="Gamepad.rightShoulder"/>
        /// <seealso cref="Gamepad.startButton"/>
        /// <seealso cref="Gamepad.selectButton"/>
        /// <seealso cref="Gamepad.leftStickButton"/>
        /// <seealso cref="Gamepad.rightStickButton"/>
        ////REVIEW: do we want the name to correspond to what's actually on the device?
        [InputControl(name = "dpad", layout = "Dpad", usage = "Hatswitch", displayName = "D-Pad", format = "BIT", sizeInBits = 4, bit = 0)]
        [InputControl(name = "buttonSouth", layout = "Button", bit = (uint)GamepadButton.South, usages = new[] { "PrimaryAction", "Submit" }, aliases = new[] { "a", "cross" }, displayName = "Button South", shortDisplayName = ButtonSouthShortDisplayName)]
        [InputControl(name = "buttonWest", layout = "Button", bit = (uint)GamepadButton.West, usage = "SecondaryAction", aliases = new[] { "x", "square" }, displayName = "Button West", shortDisplayName = ButtonWestShortDisplayName)]
        [InputControl(name = "buttonNorth", layout = "Button", bit = (uint)GamepadButton.North, aliases = new[] { "y", "triangle" }, displayName = "Button North", shortDisplayName = ButtonNorthShortDisplayName)]
        [InputControl(name = "buttonEast", layout = "Button", bit = (uint)GamepadButton.East, usages = new[] { "Back", "Cancel" }, aliases = new[] { "b", "circle" }, displayName = "Button East", shortDisplayName = ButtonEastShortDisplayName)]
        ////FIXME: 'Press' naming is inconsistent with 'Button' naming
        [InputControl(name = "leftStickPress", layout = "Button", bit = (uint)GamepadButton.LeftStick, displayName = "Left Stick Press")]
        [InputControl(name = "rightStickPress", layout = "Button", bit = (uint)GamepadButton.RightStick, displayName = "Right Stick Press")]
        [InputControl(name = "leftShoulder", layout = "Button", bit = (uint)GamepadButton.LeftShoulder, displayName = "Left Shoulder", shortDisplayName = "LB")]
        [InputControl(name = "rightShoulder", layout = "Button", bit = (uint)GamepadButton.RightShoulder, displayName = "Right Shoulder", shortDisplayName = "RB")]
        ////REVIEW: seems like these two should get less ambiguous names as well
        [InputControl(name = "start", layout = "Button", bit = (uint)GamepadButton.Start, usage = "Menu", displayName = "Start")]
        [InputControl(name = "select", layout = "Button", bit = (uint)GamepadButton.Select, displayName = "Select")]
        [FieldOffset(0)]
        public uint buttons;

        /// <summary>
        /// Left stick position. Each axis goes from -1 to 1 with
        /// 0 being center position.
        /// </summary>
        /// <value>Left stick position.</value>
        /// <seealso cref="Gamepad.leftStick"/>
        [InputControl(layout = "Stick", usage = "Primary2DMotion", processors = "stickDeadzone", displayName = "Left Stick", shortDisplayName = "LS")]
        [FieldOffset(4)]
        public Vector2 leftStick;

        /// <summary>
        /// Right stick position. Each axis from -1 to 1 with
        /// 0 being center position.
        /// </summary>
        /// <value>Right stick position.</value>
        /// <seealso cref="Gamepad.rightStick"/>
        [InputControl(layout = "Stick", usage = "Secondary2DMotion", processors = "stickDeadzone", displayName = "Right Stick", shortDisplayName = "RS")]
        [FieldOffset(12)]
        public Vector2 rightStick;

        ////REVIEW: should left and right trigger get deadzones?

        /// <summary>
        /// Position of the left trigger. Goes from 0 (not pressed) to 1 (fully pressed).
        /// </summary>
        /// <value>Position of left trigger.</value>
        /// <seealso cref="Gamepad.leftTrigger"/>
        [InputControl(layout = "Button", format = "FLT", usage = "SecondaryTrigger", displayName = "Left Trigger", shortDisplayName = "LT")]
        [FieldOffset(20)]
        public float leftTrigger;

        /// <summary>
        /// Position of the right trigger. Goes from 0 (not pressed) to 1 (fully pressed).
        /// </summary>
        /// <value>Position of right trigger.</value>
        /// <seealso cref="Gamepad.rightTrigger"/>
        [InputControl(layout = "Button", format = "FLT", usage = "SecondaryTrigger", displayName = "Right Trigger", shortDisplayName = "RT")]
        [FieldOffset(24)]
        public float rightTrigger;

        /// <summary>
        /// State format tag for GamepadState.
        /// </summary>
        /// <value>Returns "GPAD".</value>
        public FourCC format => Format;

        /// <summary>
        /// Create a gamepad state with the given buttons being pressed.
        /// </summary>
        /// <param name="buttons">Buttons to put into pressed state.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buttons"/> is <c>null</c>.</exception>
        public GamepadState(params GamepadButton[] buttons)
            : this()
        {
            if (buttons == null)
                throw new ArgumentNullException(nameof(buttons));

            foreach (var button in buttons)
            {
                Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
                var bit = 1U << (int)button;
                this.buttons |= bit;
            }
        }

        /// <summary>
        /// Set the specific buttons to be pressed or unpressed.
        /// </summary>
        /// <param name="button">A gamepad button.</param>
        /// <param name="value">Whether to set <paramref name="button"/> to be pressed or not pressed in
        /// <see cref="buttons"/>.</param>
        /// <returns>GamepadState with a modified <see cref="buttons"/> mask.</returns>
        public GamepadState WithButton(GamepadButton button, bool value = true)
        {
            Debug.Assert((int)button < 32, $"Expected button < 32, so we fit into the 32 bit wide bitmask");
            var bit = 1U << (int)button;
            if (value)
                buttons |= bit;
            else
                buttons &= ~bit;
            return this;
        }
    }

    ////NOTE: The bit positions here based on the enum value are also used in native.
    /// <summary>
    /// Enum of common gamepad buttons.
    /// </summary>
    /// <remarks>
    /// Can be used as an array indexer on the <see cref="Gamepad"/> class to get individual button controls.
    /// </remarks>
    public enum GamepadButton
    {
        // Dpad buttons. Important to be first in the bitfield as we'll
        // point the DpadControl to it.
        // IMPORTANT: Order has to match what is expected by DpadControl.

        /// <summary>
        /// The up button on a gamepad's dpad.
        /// </summary>
        DpadUp = 0,

        /// <summary>
        /// The down button on a gamepad's dpad.
        /// </summary>
        DpadDown = 1,

        /// <summary>
        /// The left button on a gamepad's dpad.
        /// </summary>
        DpadLeft = 2,

        /// <summary>
        /// The right button on a gamepad's dpad.
        /// </summary>
        DpadRight = 3,

        // Face buttons. We go with a north/south/east/west naming as that
        // clearly disambiguates where we expect the respective button to be.

        /// <summary>
        /// The upper action button on a gamepad.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="Y"/> and <see cref="Triangle"/> which are the Xbox and PlayStation controller names for this button.
        /// </remarks>
        North = 4,

        /// <summary>
        /// The right action button on a gamepad.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="B"/> and <see cref="Circle"/> which are the Xbox and PlayStation controller names for this button.
        /// </remarks>
        East = 5,

        /// <summary>
        /// The lower action button on a gamepad.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="A"/> and <see cref="Cross"/> which are the Xbox and PlayStation controller names for this button.
        /// </remarks>
        South = 6,

        /// <summary>
        /// The left action button on a gamepad.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="X"/> and <see cref="Square"/> which are the Xbox and PlayStation controller names for this button.
        /// </remarks>
        West = 7,


        /// <summary>
        /// The button pressed by pressing down the left stick on a gamepad.
        /// </summary>
        LeftStick = 8,

        /// <summary>
        /// The button pressed by pressing down the right stick on a gamepad.
        /// </summary>
        RightStick = 9,

        /// <summary>
        /// The left shoulder button on a gamepad.
        /// </summary>
        LeftShoulder = 10,

        /// <summary>
        /// The right shoulder button on a gamepad.
        /// </summary>
        RightShoulder = 11,

        /// <summary>
        /// The start button.
        /// </summary>
        Start = 12,

        /// <summary>
        /// The select button.
        /// </summary>
        Select = 13,

        // For values that are not part of the buttons bitmask in GamepadState, assign large values that are outside
        // the 32bit bit range.

        /// <summary>
        /// The left trigger button on a gamepad.
        /// </summary>
        LeftTrigger = 32,

        /// <summary>
        /// The right trigger button on a gamepad.
        /// </summary>
        RightTrigger = 33,

        /// <summary>
        /// The X button on an Xbox controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="West"/>, which is the generic name of this button.
        /// </remarks>
        X = West,
        /// <summary>
        /// The Y button on an Xbox controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="North"/>, which is the generic name of this button.
        /// </remarks>
        Y = North,
        /// <summary>
        /// The A button on an Xbox controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="South"/>, which is the generic name of this button.
        /// </remarks>
        A = South,
        /// <summary>
        /// The B button on an Xbox controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="East"/>, which is the generic name of this button.
        /// </remarks>
        B = East,

        /// <summary>
        /// The cross button on a PlayStation controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="South"/>, which is the generic name of this button.
        /// </remarks>
        Cross = South,
        /// <summary>
        /// The square button on a PlayStation controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="West"/>, which is the generic name of this button.
        /// </remarks>
        Square = West,
        /// <summary>
        /// The triangle button on a PlayStation controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="North"/>, which is the generic name of this button.
        /// </remarks>
        Triangle = North,
        /// <summary>
        /// The circle button on a PlayStation controller.
        /// </summary>
        /// <remarks>
        /// Identical to <see cref="East"/>, which is the generic name of this button.
        /// </remarks>
        Circle = East,
    }
}

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// An Xbox-style gamepad with two sticks, a D-Pad, four face buttons, two triggers,
    /// two shoulder buttons, and two menu buttons that usually sit in the midsection of the gamepad.
    /// </summary>
    /// <remarks>
    /// The Gamepad layout provides a standardized layouts for gamepads. Generally, if a specific
    /// device is represented as a Gamepad, the controls, such as the face buttons, are guaranteed
    /// to be mapped correctly and consistently. If, based on the set of supported devices available
    /// to the input system, this cannot be guaranteed, a given device is usually represented as a
    /// generic <see cref="Joystick"/> or as just a plain <see cref="HID.HID"/> instead.
    ///
    /// <example>
    /// <code>
    /// // Show all gamepads in the system.
    /// Debug.Log(string.Join("\n", Gamepad.all));
    ///
    /// // Check whether the X button on the current gamepad is pressed.
    /// if (Gamepad.current.xButton.wasPressedThisFrame)
    ///     Debug.Log("Pressed");
    ///
    /// // Rumble the left motor on the current gamepad slightly.
    /// Gamepad.current.SetMotorSpeeds(0.2f, 0.
    /// </code>
    /// </example>
    /// </remarks>
    [InputControlLayout(stateType = typeof(GamepadState), isGenericTypeOfDevice = true)]
    public class Gamepad : InputDevice, IDualMotorRumble
    {
        /// <summary>
        /// The left face button of the gamepad.
        /// </summary>
        /// <value>Control representing the X/Square face button.</value>
        /// <remarks>
        /// On an Xbox controller, this is the X button and on the PS4 controller, this is the
        /// square button.
        /// </remarks>
        /// <seealso cref="xButton"/>
        /// <seealso cref="squareButton"/>
        public ButtonControl buttonWest { get; protected set; }

        /// <summary>
        /// The top face button of the gamepad.
        /// </summary>
        /// <value>Control representing the Y/Triangle face button.</value>
        /// <remarks>
        /// On an Xbox controller, this is the Y button and on the PS4 controller, this is the
        /// triangle button.
        /// </remarks>
        /// <seealso cref="yButton"/>
        /// <seealso cref="triangleButton"/>
        public ButtonControl buttonNorth { get; protected set; }

        /// <summary>
        /// The bottom face button of the gamepad.
        /// </summary>
        /// <value>Control representing the A/Cross face button.</value>
        /// <remarks>
        /// On an Xbox controller, this is the A button and on the PS4 controller, this is the
        /// cross button.
        /// </remarks>
        /// <seealso cref="aButton"/>
        /// <seealso cref="crossButton"/>
        public ButtonControl buttonSouth { get; protected set; }

        /// <summary>
        /// The right face button of the gamepad.
        /// </summary>
        /// <value>Control representing the B/Circle face button.</value>
        /// <remarks>
        /// On an Xbox controller, this is the B button and on the PS4 controller, this is the
        /// circle button.
        /// </remarks>
        /// <seealso cref="bButton"/>
        /// <seealso cref="circleButton"/>
        public ButtonControl buttonEast { get; protected set; }

        /// <summary>
        /// The button that gets triggered when <see cref="leftStick"/> is pressed down.
        /// </summary>
        /// <value>Control representing a click with the left stick.</value>
        public ButtonControl leftStickButton { get; protected set; }

        /// <summary>
        /// The button that gets triggered when <see cref="rightStick"/> is pressed down.
        /// </summary>
        /// <value>Control representing a click with the right stick.</value>
        public ButtonControl rightStickButton { get; protected set; }

        /// <summary>
        /// The right button in the middle section of the gamepad (called "menu" on Xbox
        /// controllers and "options" on PS4 controllers).
        /// </summary>
        /// <value>Control representing the right button in midsection.</value>
        public ButtonControl startButton { get; protected set; }

        /// <summary>
        /// The left button in the middle section of the gamepad (called "view" on Xbox
        /// controllers and "share" on PS4 controllers).
        /// </summary>
        /// <value>Control representing the left button in midsection.</value>
        public ButtonControl selectButton { get; protected set; }

        /// <summary>
        /// The 4-way directional pad on the gamepad.
        /// </summary>
        /// <value>Control representing the d-pad.</value>
        public DpadControl dpad { get; protected set; }

        /// <summary>
        /// The left shoulder/bumper button that sits on top of <see cref="leftTrigger"/>.
        /// </summary>
        /// <value>Control representing the left shoulder button.</value>
        /// <remarks>
        /// On Xbox controllers, this is usually called "left bumper" whereas on PS4
        /// controllers, this button is referred to as "L1".
        /// </remarks>
        public ButtonControl leftShoulder { get; protected set; }

        /// <summary>
        /// The right shoulder/bumper button that sits on top of <see cref="rightTrigger"/>.
        /// </summary>
        /// <value>Control representing the right shoulder button.</value>
        /// <remarks>
        /// On Xbox controllers, this is usually called "right bumper" whereas on PS4
        /// controllers, this button is referred to as "R1".
        /// </remarks>
        public ButtonControl rightShoulder { get; protected set; }

        /// <summary>
        /// The left thumbstick on the gamepad.
        /// </summary>
        /// <value>Control representing the left thumbstick.</value>
        public StickControl leftStick { get; protected set; }

        /// <summary>
        /// The right thumbstick on the gamepad.
        /// </summary>
        /// <value>Control representing the right thumbstick.</value>
        public StickControl rightStick { get; protected set; }

        /// <summary>
        /// The left trigger button sitting below <see cref="leftShoulder"/>.
        /// </summary>
        /// <value>Control representing the left trigger button.</value>
        /// <remarks>
        /// On PS4 controllers, this button is referred to as "L2".
        /// </remarks>
        public ButtonControl leftTrigger { get; protected set; }

        /// <summary>
        /// The right trigger button sitting below <see cref="rightShoulder"/>.
        /// </summary>
        /// <value>Control representing the right trigger button.</value>
        /// <remarks>
        /// On PS4 controllers, this button is referred to as "R2".
        /// </remarks>
        public ButtonControl rightTrigger { get; protected set; }

        /// <summary>
        /// Same as <see cref="buttonSouth"/>. Xbox-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonSouth"/>.</value>
        public ButtonControl aButton => buttonSouth;

        /// <summary>
        /// Same as <see cref="buttonEast"/>. Xbox-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonEast"/>.</value>
        public ButtonControl bButton => buttonEast;

        /// <summary>
        /// Same as <see cref="buttonWest"/> Xbox-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonWest"/>.</value>
        public ButtonControl xButton => buttonWest;

        /// <summary>
        /// Same as <see cref="buttonNorth"/>. Xbox-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonNorth"/>.</value>
        public ButtonControl yButton => buttonNorth;

        /// <summary>
        /// Same as <see cref="buttonNorth"/>. PS4-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonNorth"/>.</value>
        public ButtonControl triangleButton => buttonNorth;

        /// <summary>
        /// Same as <see cref="buttonWest"/>. PS4-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonWest"/>.</value>
        public ButtonControl squareButton => buttonWest;

        /// <summary>
        /// Same as <see cref="buttonEast"/>. PS4-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonEast"/>.</value>
        public ButtonControl circleButton => buttonEast;

        /// <summary>
        /// Same as <see cref="buttonSouth"/>. PS4-style alias.
        /// </summary>
        /// <value>Same as <see cref="buttonSouth"/>.</value>
        public ButtonControl crossButton => buttonSouth;

        /// <summary>
        /// Retrieve a gamepad button by its <see cref="GamepadButton"/> enumeration
        /// constant.
        /// </summary>
        /// <param name="button">Button to retrieve.</param>
        /// <exception cref="ArgumentException"><paramref name="button"/> is not a valid gamepad
        /// button value.</exception>
        public ButtonControl this[GamepadButton button]
        {
            get
            {
                switch (button)
                {
                    case GamepadButton.North: return buttonNorth;
                    case GamepadButton.South: return buttonSouth;
                    case GamepadButton.East: return buttonEast;
                    case GamepadButton.West: return buttonWest;
                    case GamepadButton.Start: return startButton;
                    case GamepadButton.Select: return selectButton;
                    case GamepadButton.LeftShoulder: return leftShoulder;
                    case GamepadButton.RightShoulder: return rightShoulder;
                    case GamepadButton.LeftTrigger: return leftTrigger;
                    case GamepadButton.RightTrigger: return rightTrigger;
                    case GamepadButton.LeftStick: return leftStickButton;
                    case GamepadButton.RightStick: return rightStickButton;
                    case GamepadButton.DpadUp: return dpad.up;
                    case GamepadButton.DpadDown: return dpad.down;
                    case GamepadButton.DpadLeft: return dpad.left;
                    case GamepadButton.DpadRight: return dpad.right;
                    default:
                        throw new InvalidEnumArgumentException(nameof(button), (int)button, typeof(GamepadButton));
                }
            }
        }

        /// <summary>
        /// The gamepad last used/connected by the player or <c>null</c> if there is no gamepad connected
        /// to the system.
        /// </summary>
        /// <remarks>
        /// When added, a device is automatically made current (see <see cref="InputDevice.MakeCurrent"/>), so
        /// when connecting a gamepad, it will also become current. After that, it will only become current again
        /// when input change on non-noisy controls (see <see cref="InputControl.noisy"/>) is received.
        ///
        /// For local multiplayer scenarios (or whenever there are multiple gamepads that need to be usable
        /// in a concurrent fashion), it is not recommended to rely on this property. Instead, it is recommended
        /// to use <see cref="PlayerInput"/> or <see cref="Users.InputUser"/>.
        /// </remarks>
        /// <seealso cref="InputDevice.MakeCurrent"/>
        /// <seealso cref="all"/>
        public static Gamepad current { get; private set; }

        /// <summary>
        /// A list of gamepads currently connected to the system.
        /// </summary>
        /// <value>All currently connected gamepads.</value>
        /// <remarks>
        /// Does not cause GC allocation.
        ///
        /// Do <em>not</em> hold on to the value returned by this getter but rather query it whenever
        /// you need it. Whenever the gamepad setup changes, the value returned by this getter
        /// is invalidated.
        /// </remarks>
        /// <seealso cref="current"/>
        public new static ReadOnlyArray<Gamepad> all => new ReadOnlyArray<Gamepad>(s_Gamepads, 0, s_GamepadCount);

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            ////REVIEW: what's actually faster/better... storing these in properties or doing the lookup on the fly?
            buttonWest = GetChildControl<ButtonControl>("buttonWest");
            buttonNorth = GetChildControl<ButtonControl>("buttonNorth");
            buttonSouth = GetChildControl<ButtonControl>("buttonSouth");
            buttonEast = GetChildControl<ButtonControl>("buttonEast");

            startButton = GetChildControl<ButtonControl>("start");
            selectButton = GetChildControl<ButtonControl>("select");

            leftStickButton = GetChildControl<ButtonControl>("leftStickPress");
            rightStickButton = GetChildControl<ButtonControl>("rightStickPress");

            dpad = GetChildControl<DpadControl>("dpad");

            leftShoulder = GetChildControl<ButtonControl>("leftShoulder");
            rightShoulder = GetChildControl<ButtonControl>("rightShoulder");

            leftStick = GetChildControl<StickControl>("leftStick");
            rightStick = GetChildControl<StickControl>("rightStick");

            leftTrigger = GetChildControl<ButtonControl>("leftTrigger");
            rightTrigger = GetChildControl<ButtonControl>("rightTrigger");

            base.FinishSetup();
        }

        /// <summary>
        /// Make the gamepad the <see cref="current"/> gamepad.
        /// </summary>
        /// <remarks>
        /// This is called automatically by the system when there is input on a gamepad.
        /// </remarks>
        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        /// <summary>
        /// Called when the gamepad is added to the system.
        /// </summary>
        protected override void OnAdded()
        {
            ArrayHelpers.AppendWithCapacity(ref s_Gamepads, ref s_GamepadCount, this);
        }

        /// <summary>
        /// Called when the gamepad is removed from the system.
        /// </summary>
        protected override void OnRemoved()
        {
            if (current == this)
                current = null;

            // Remove from `all`.
            var index = ArrayHelpers.IndexOfReference(s_Gamepads, this, s_GamepadCount);
            if (index != -1)
                ArrayHelpers.EraseAtWithCapacity(s_Gamepads, ref s_GamepadCount, index);
            else
            {
                Debug.Assert(false,
                    $"Gamepad {this} seems to not have been added but is being removed (gamepad list: {string.Join(", ", all)})"); // Put in else to not allocate on normal path.
            }
        }

        /// <summary>
        /// Pause rumble effects on the gamepad. Resume with <see cref="ResumeHaptics"/>.
        /// </summary>
        /// <seealso cref="IDualMotorRumble"/>
        public virtual void PauseHaptics()
        {
            m_Rumble.PauseHaptics(this);
        }

        /// <summary>
        /// Resume rumble affects on the gamepad that have been paused with <see cref="PauseHaptics"/>.
        /// </summary>
        /// <seealso cref="IDualMotorRumble"/>
        public virtual void ResumeHaptics()
        {
            m_Rumble.ResumeHaptics(this);
        }

        /// <summary>
        /// Reset rumble effects on the gamepad. Puts the gamepad rumble motors back into their
        /// default state.
        /// </summary>
        /// <seealso cref="IDualMotorRumble"/>
        public virtual void ResetHaptics()
        {
            m_Rumble.ResetHaptics(this);
        }

        /// <inheritdoc />
        public virtual void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            m_Rumble.SetMotorSpeeds(this, lowFrequency, highFrequency);
        }

        private DualMotorRumble m_Rumble;

        private static int s_GamepadCount;
        private static Gamepad[] s_Gamepads;
    }
}
