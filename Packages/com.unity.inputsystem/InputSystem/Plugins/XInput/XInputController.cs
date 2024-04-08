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
        /// See <a href="https://docs.microsoft.com/en-us/windows/win32/xinput/xinput-and-controller-subtypes">MSDN</a>.
        /// </remarks>
        public enum DeviceSubType
        {
            Unknown = 0x00,
            Gamepad = 0x01,
            Wheel = 0x02,
            ArcadeStick = 0x03,
            FlightStick = 0x04,
            DancePad = 0x05,
            Guitar = 0x06,
            GuitarAlternate = 0x07,
            DrumKit = 0x08,
            GuitarBass = 0x0B,
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
