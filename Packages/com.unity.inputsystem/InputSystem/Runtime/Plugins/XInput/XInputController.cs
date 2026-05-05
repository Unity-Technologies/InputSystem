using System;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;

////TODO: expose user index

////TODO: set displayNames of the controls according to Xbox controller standards

namespace UnityEngine.InputSystem.XInput
{
    /// <summary>
    /// An XInput-compatible game controller.
    /// </summary>
    /// <remarks>
    /// Note that on non-Microsoft platforms, XInput controllers will not actually use the XInput interface
    /// but will rather be interfaced with through different APIs -- on OSX, for example, HID is used to
    /// interface with Xbox controlllers. In those cases, XInput-specific functionality (like <see cref="Capabilities"/>)
    /// will not be available.
    ///
    /// On Windows, XInput controllers will be reported with <see cref="InputDeviceDescription.interfaceName"/>
    /// set to <c>"XInput"</c> and with a JSON representation of <a
    /// href="https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_capabilities">XINPUT_CAPABILITIES</a>
    /// available in <see cref="InputDeviceDescription.capabilities"/>. This means that you match on those
    /// <c>subType</c> and/or <c>flags</c> for example.
    ///
    /// <example>
    /// <code>
    /// // Create an XInput-specific guitar layout subtype.
    /// // NOTE: Works only on Windows.
    /// InputSystem.RegisterLayout(@"
    ///     {
    ///         ""name"" : ""XInputGuitar"",
    ///         ""displayName"" : ""Guitar"",
    ///         ""extend"" : ""XInputController"",
    ///         ""device"" : {
    ///             ""interface"" : ""XInput"",
    ///             ""capabilities"" : [
    ///                 { ""path"" : ""subType"", ""value"" : ""6"" }
    ///             ]
    ///         }
    ///     }
    /// ");
    /// </code>
    /// </example>
    ///
    /// Now, when an XInput controller is connected and reports itself with the
    /// subtype "Guitar", it is turned into an "XInputGuitar" instead of an
    /// "XInputController".
    /// </remarks>
    [InputControlLayout(displayName = "Xbox Controller")]
    public class XInputController : Gamepad
    {
        /// <summary>
        /// Same as <see cref="Gamepad.startButton"/>.
        /// </summary>
        /// <value>Same control as <see cref="Gamepad.startButton"/>.</value>
        // Change the display names for the buttons to conform to Xbox conventions.
        [InputControl(name = "buttonSouth", displayName = "A")]
        [InputControl(name = "buttonEast", displayName = "B")]
        [InputControl(name = "buttonWest", displayName = "X")]
        [InputControl(name = "buttonNorth", displayName = "Y")]
        [InputControl(name = "leftShoulder", displayName = "Left Bumper", shortDisplayName = "LB")]
        [InputControl(name = "rightShoulder", displayName = "Right Bumper", shortDisplayName = "RB")]
        [InputControl(name = "leftTrigger", shortDisplayName = "LT")]
        [InputControl(name = "rightTrigger", shortDisplayName = "RT")]
        // This follows Xbox One conventions; on Xbox 360, this is start=start and select=back.
        [InputControl(name = "start", displayName = "Menu", alias = "menu")]
        [InputControl(name = "select", displayName = "View", alias = "view")]
        public ButtonControl menu { get; protected set; }

        /// <summary>
        /// Same as <see cref="Gamepad.selectButton"/>
        /// </summary>
        /// <value>Same control as <see cref="Gamepad.selectButton"/>.</value>
        public ButtonControl view { get; protected set; }

        /// <summary>
        /// What specific kind of XInput controller this is.
        /// </summary>
        /// <value>XInput device subtype.</value>
        /// <remarks>
        /// When the controller is picked up through interfaces other than XInput or through old versions of
        /// XInput, this will always be <see cref="DeviceSubType.Unknown"/>. Put another way, this value is
        /// meaningful only on recent Microsoft platforms.
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/windows/win32/xinput/xinput-and-controller-subtypes"/>
        public DeviceSubType subType
        {
            get
            {
                if (!m_HaveParsedCapabilities)
                    ParseCapabilities();
                return m_SubType;
            }
        }

        /// <summary>
        /// Return the device flags as reported by XInput.
        /// </summary>
        /// <value>XInput device flags.</value>
        /// <seealso href="https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_capabilities"/>
        public DeviceFlags flags
        {
            get
            {
                if (!m_HaveParsedCapabilities)
                    ParseCapabilities();
                return m_Flags;
            }
        }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();

            menu = startButton;
            view = selectButton;
        }

        private bool m_HaveParsedCapabilities;
        private DeviceSubType m_SubType;
        private DeviceFlags m_Flags;

        private void ParseCapabilities()
        {
            if (!string.IsNullOrEmpty(description.capabilities))
            {
                var capabilities = JsonUtility.FromJson<Capabilities>(description.capabilities);
                m_SubType = capabilities.subType;
                m_Flags = capabilities.flags;
            }
            m_HaveParsedCapabilities = true;
        }

        /// <summary>
        /// Controller type enumeration in <c>Type</c> field of <c>XINPUT_CAPABILITIES</c>.
        /// </summary>
        /// <remarks>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_capabilities">MSDN</a>.
        /// </remarks>
        internal enum DeviceType
        {
            Gamepad = 0x00
        }

        /// <summary>
        /// Controller subtype enumeration in <c>SubType</c> field of <c>XINPUT_CAPABILITIES</c>.
        /// </summary>
        /// <remarks>
        /// Provides additional detail about the underlying hardware being used and how it maps physical to logical
        /// controls.
        ///
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/xinput/xinput-and-controller-subtypes">MSDN</a>
        /// for additional details.
        /// </remarks>
        public enum DeviceSubType
        {
            /// <summary>
            /// The controller type is unknown.
            /// </summary>
            Unknown = 0x00,

            /// <summary>
            /// Gamepad controller.
            /// </summary>
            /// <remarks>
            /// Includes left and right stick as <see cref="Gamepad.leftStick" /> and <see cref="Gamepad.rightStick"/>,
            /// left and right trigger as <see cref="Gamepad.leftTrigger"/> and <see cref="Gamepad.rightTrigger"/>,
            /// directional pad as <see cref="Gamepad.dpad"/>,
            /// and all standard buttons (<see cref="Gamepad.buttonSouth"/>, <see cref="Gamepad.buttonEast"/>,
            /// <see cref="Gamepad.buttonWest"/>, <see cref="Gamepad.buttonNorth"/>,
            /// <see cref="Gamepad.startButton"/>, <see cref="Gamepad.selectButton"/>,
            /// <see cref="Gamepad.leftShoulder"/>, <see cref="Gamepad.rightShoulder"/>,
            /// <see cref="Gamepad.leftStickButton"/>, <see cref="Gamepad.rightStickButton"/>).
            /// </remarks>
            Gamepad = 0x01,

            /// <summary>
            /// Racing wheel controller.
            /// </summary>
            /// <remarks>
            /// <see cref="UnityEngine.InputSystem.Gamepad.leftStick" /> x-axis reports the wheel rotation,
            /// <see cref="Gamepad.rightTrigger"/> is the acceleration pedal, and
            /// <see cref="Gamepad.leftTrigger"/>Left Trigger is the brake pedal.
            /// Includes Directional Pad as <see cref="Gamepad.dpad"/> and most standard buttons
            /// (<see cref="Gamepad.buttonSouth"/>, <see cref="Gamepad.buttonEast"/>,
            /// <see cref="Gamepad.buttonWest"/>, <see cref="Gamepad.buttonNorth"/>,
            /// <see cref="Gamepad.startButton"/>, <see cref="Gamepad.selectButton"/>,
            /// <see cref="Gamepad.leftShoulder"/>, <see cref="Gamepad.rightShoulder"/>).
            /// <see cref="Gamepad.leftStickButton"/> and <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            Wheel = 0x02,

            /// <summary>
            /// Arcade stick controller.
            /// </summary>
            /// <remarks>
            /// Includes a Digital Stick that reports as a <see cref="Gamepad.dpad"/> (up, down, left, right),
            /// and most standard buttons (<see cref="Gamepad.buttonSouth"/>, <see cref="Gamepad.buttonEast"/>,
            /// <see cref="Gamepad.buttonWest"/>, <see cref="Gamepad.buttonNorth"/>,
            /// <see cref="Gamepad.startButton"/>, <see cref="Gamepad.selectButton"/>).
            /// The <see cref="Gamepad.leftTrigger"/> and <see cref="Gamepad.rightTrigger"/> are implemented as digital
            /// buttons and report either 0.0f or 1.0f.
            /// The <see cref="Gamepad.leftShoulder"/>, <see cref="Gamepad.rightShoulder"/> and
            /// <see cref="Gamepad.leftStickButton"/>, <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            ArcadeStick = 0x03,

            /// <summary>
            /// Flight stick controller.
            /// </summary>
            /// <remarks>
            /// Includes a pitch and roll stick that reports as the <see cref="Gamepad.leftStick"/>, a POV Hat which
            /// reports as the <see cref="Gamepad.rightStick"/>, a rudder (handle twist or rocker) that reports as
            /// <see cref="Gamepad.leftTrigger"/>, and a throttle control as the <see cref="Gamepad.rightTrigger"/>.
            /// Includes support for a primary weapon (<see cref="Gamepad.buttonSouth"/>), secondary weapon
            /// (<see cref="Gamepad.buttonEast"/>), and other standard buttons (<see cref="Gamepad.buttonWest"/>,
            /// <see cref="Gamepad.buttonNorth"/>, <see cref="Gamepad.startButton"/>,
            /// <see cref="Gamepad.selectButton"/>).
            /// <see cref="Gamepad.leftShoulder"/>, <see cref="Gamepad.rightShoulder"/> and
            /// <see cref="Gamepad.leftStickButton"/>, <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            FlightStick = 0x04,

            /// <summary>
            /// Dance pad controller.
            /// </summary>
            /// <remarks>
            /// Includes the <see cref="Gamepad.dpad"/> and standard buttons (<see cref="Gamepad.buttonSouth"/>,
            /// <see cref="Gamepad.buttonEast"/>, <see cref="Gamepad.buttonWest"/>,
            /// <see cref="Gamepad.buttonNorth"/>) on the pad, plus <see cref="Gamepad.startButton"/> and
            /// <see cref="Gamepad.selectButton"/>.
            /// </remarks>
            DancePad = 0x05,

            /// <summary>
            /// Guitar controller.
            /// </summary>
            /// <remarks>
            /// The strum bar maps to <see cref="Gamepad.dpad"/> (up and down), and the frets are assigned to
            /// <see cref="Gamepad.buttonSouth"/> (green), <see cref="Gamepad.buttonEast"/> (red),
            /// <see cref="Gamepad.buttonNorth"/> (yellow), <see cref="Gamepad.buttonWest"/> (blue), and
            /// <see cref="Gamepad.leftShoulder"/> (orange).
            /// <see cref="Gamepad.rightStick"/> y-axis is associated with a vertical orientation sensor;
            /// <see cref="Gamepad.rightStick"/> x-axis is the whammy bar.
            /// Includes support for <see cref="Gamepad.selectButton"/>, <see cref="Gamepad.startButton"/>,
            /// <see cref="Gamepad.dpad"/> (left, right).
            /// <see cref="Gamepad.leftTrigger"/> (pickup selector), <see cref="Gamepad.rightTrigger"/>,
            /// <see cref="Gamepad.rightShoulder"/>, <see cref="Gamepad.leftStickButton"/> (fret modifier),
            /// <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            Guitar = 0x06,

            /// <summary>
            /// Alternate guitar controller.
            /// </summary>
            /// <remarks>
            /// Similar to <see cref="Guitar"/> but supports a larger range of movement for the vertical orientation
            /// sensor.
            /// </remarks>
            GuitarAlternate = 0x07,

            /// <summary>
            /// Drum kit controller.
            /// </summary>
            /// <remarks>
            /// The drum pads are assigned to buttons: <see cref="Gamepad.buttonSouth"/> for green (Floor Tom),
            /// <see cref="Gamepad.buttonEast"/> for red (Snare Drum),
            /// <see cref="Gamepad.buttonWest"/> for blue (Low Tom),
            /// <see cref="Gamepad.buttonNorth"/> for yellow (High Tom),
            /// and <see cref="Gamepad.leftShoulder"/> for the pedal (Bass Drum).
            /// Includes <see cref="Gamepad.dpad"/>, <see cref="Gamepad.selectButton"/>, and
            /// <see cref="Gamepad.startButton"/>. <see cref="Gamepad.rightShoulder"/>,
            /// <see cref="Gamepad.leftStickButton"/>, and <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            DrumKit = 0x08,

            /// <summary>
            /// Bass guitar controller.
            /// </summary>
            /// <remarks>
            /// Identical to <see cref="Guitar" />, with the distinct subtype to simplify setup.
            /// </remarks>
            GuitarBass = 0x0B,

            /// <summary>
            /// Arcade pad controller.
            /// </summary>
            /// <remarks>
            /// Includes Directional Pad and most standard buttons
            /// (<see cref="Gamepad.buttonSouth"/>, <see cref="Gamepad.buttonEast"/>,
            /// <see cref="Gamepad.buttonWest"/>, <see cref="Gamepad.buttonNorth"/>,
            /// <see cref="Gamepad.startButton"/>, <see cref="Gamepad.selectButton"/>,
            /// <see cref="Gamepad.leftShoulder"/>, <see cref="Gamepad.rightShoulder"/>).
            /// The <see cref="Gamepad.leftTrigger"/>, <see cref="Gamepad.rightTrigger"/> are implemented as digital
            /// buttons and report either 0.0f or 1.0f.
            /// <see cref="Gamepad.leftStick"/>, <see cref="Gamepad.rightStick"/>,
            /// <see cref="Gamepad.leftStickButton"/> and <see cref="Gamepad.rightStickButton"/> are optional.
            /// </remarks>
            ArcadePad = 0x13
        }

        /// <summary>
        /// Controller flags in <c>Flags</c> field of <c>XINPUT_CAPABILITIES</c>.
        /// </summary>
        /// <remarks>
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_capabilities">MSDN</a>.
        /// </remarks>
        [Flags]
        public new enum DeviceFlags
        {
            ForceFeedbackSupported = 0x01,
            Wireless = 0x02,
            VoiceSupported = 0x04,
            PluginModulesSupported = 0x08,
            NoNavigation = 0x10,
        }

        [Serializable]
        internal struct Capabilities
        {
            public DeviceType type;
            public DeviceSubType subType;
            public DeviceFlags flags;
        }
    }
}
