using System;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;

////TODO: expose user index

////TODO: set displayNames of the controls according to Xbox controller standards

namespace UnityEngine.Experimental.Input.Plugins.XInput
{
    /// <summary>
    /// An XInput compatible game controller.
    /// </summary>
    /// <remarks>
    /// Note that on non-Microsoft platforms, XInput controllers will not actually use the XInput interface
    /// but will rather be interfaced with through different APIs -- on OSX, for example, HID is used to
    /// interface with Xbox controlllers. In those cases, XInput-specific functionality (like <see cref="Capabilities"/>)
    /// will not be available.
    /// </remarks>
    public class XInputController : Gamepad
    {
        public ButtonControl aButton { get; private set; }
        public ButtonControl bButton { get; private set; }
        public ButtonControl xButton { get; private set; }
        public ButtonControl yButton { get; private set; }

        public ButtonControl menu { get; private set; }
        public ButtonControl view { get; private set; }

        /// <summary>
        /// What specific kind of XInput controller this is.
        /// </summary>
        /// <remarks>
        /// When the controller is picked up through interfaces other than XInput or through old versions of
        /// XInput, this will always be <see cref="DeviceSubType.Unknown"/>. Put another way, this value is
        /// meaningful only on recent Microsoft platforms.
        /// </remarks>
        public DeviceSubType subType
        {
            get
            {
                if (!m_HaveParsedCapabilities)
                    ParseCapabilities();
                return m_SubType;
            }
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            aButton = buttonSouth;
            bButton = buttonEast;
            xButton = buttonWest;
            yButton = buttonNorth;

            menu = startButton;
            view = selectButton;
        }

        private bool m_HaveParsedCapabilities;
        private DeviceSubType m_SubType;

        private void ParseCapabilities()
        {
            if (!string.IsNullOrEmpty(description.capabilities))
            {
                var capabilities = JsonUtility.FromJson<Capabilities>(description.capabilities);
                m_SubType = capabilities.subType;
            }
            m_HaveParsedCapabilities = true;
        }

        public enum DeviceType
        {
            Gamepad = 0x00
        }

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

        [Flags]
        public enum CapabilityFlags
        {
            ForceFeedbackSupported = 0x01,
            Wireless = 0x02,
            VoiceSupported = 0x04,
            PluginModulesSupported = 0x08,
            NoNavigation = 0x10,
        }

        [Serializable]
        public struct Capabilities
        {
            public DeviceType type;
            public DeviceSubType subType;
            public CapabilityFlags flags;
        }
    }
}
