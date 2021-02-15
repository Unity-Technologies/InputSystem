// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
namespace UnityEngine.InputSystem
{
    internal static class FourCCs
    {
        // These codes should be kept in sync with the codes from the native side. The unfortunate identifier naming is intentional
        // so that we can search for fourcc usages more easily across all our code bases, as the names match the identifiers on the
        // native side.

        // States
        public const int kInputFourCCMouseState = 0x4D4F5553; // 'MOUS'
        public const int kInputFourCCPenState = 0x50454E20; //'PEN '
        public const int kInputFourCCTouchState = 0x544F5543; //'TOUC'
        public const int kInputFourCCTouchscreenState = 0x54534352; //'TSCR'
        public const int kInputFourCCKeyboardState = 0x4B455953; //'KEYS'
        public const int kInputFourCCTrackingState = 0x504F5345; //'POSE'
        public const int kInputFourCCGamepadState = 0x47504144; //'GPAD'
        public const int kInputFourCCHIDState = 0x48494420; //'HID '
        public const int kInputFourCCAccelerometerState = 0x4143434C; //'ACCL'
        public const int kInputFourCCGyroscopeState = 0x4759524F; //'GYRO'
        public const int kInputFourCCGravityState = 0x47525620; //'GRV '
        public const int kInputFourCCAttitudeState = 0x41545444; //'ATTD'
        public const int kInputFourCCLinearAccelerationState = 0x4C414143; //'LAAC'
        public const int kInputFourCCLinuxJoystickState = 0x4C4A4F59; //'LJOY'
        public const int kInputFourCCXInputState = 0x58494E50; //'XINP'
        public const int kInputFourCCWebGLState = 0x48544D4C; //'HTML'
        public const int kInputFourCCAndroidGameControllerState = 0x41474320; //'AGC '
        public const int kInputFourCCAndroidSensorState = 0x41535320; //'ASS '
        public const int kInputFourCCiOSGameControllerState = 0x49474320; //'IGC '
        
        // XR
        public const int kInputFourCCXRSDKState = 0x58525330; //'XRS0'
        public const int kInputFourCCXREventRecenter = 0x58524330; //'XRC0'
        public const int kInputFourCCXREventHapticSendImpulse = 0x58484930; //'XHI0'
        public const int kInputFourCCXREventHapticSendBuffer = 0x58485530; //'XHU0'
        public const int kInputFourCCXREventHapticGetCapabilities = 0x58484330; //'XHC0'
        public const int kInputFourCCXREventHapticGetState = 0x58485330; //'XHS0'
        public const int kInputFourCCXREventHapticStop = 0x58485354; //'XHST'
    
        // Events
        public const int kInputFourCCEventDeviceRemoved = 0x4452454D; //'DREM'
        public const int kInputFourCCEventDeviceConfigurationChanged = 0x44434647; //'DCFG'
        public const int kInputFourCCEventText = 0x54455854; //'TEXT'
        public const int kInputFourCCEventIMECompositionString = 0x494D4553; //'IMES'
        public const int kInputFourCCEventIMEComposition = 0x494D4543; //'IMEC'
        public const int kInputFourCCEventState = 0x53544154; //'STAT'
        public const int kInputFourCCEventDelta = 0x444C5441; //'DLTA'
        
        // IOCTL
        public const int kInputFourCCIOCTLEnableDevice = 0x454E424C; //'ENBL'
        public const int kInputFourCCIOCTLDisableDevice = 0x4453424C; //'DSBL'
        public const int kInputFourCCIOCTLQueryDeviceEnabled = 0x51454E42; //'QENB'
        public const int kInputFourCCIOCTLRequestResetDevice = 0x52534554; //'RSET'
        public const int kInputFourCCIOCTLRequestSyncDevice = 0x53594E43; //'SYNC'
        public const int kInputFourCCIOCTLQueryRunInBackground = 0x51524942; //'QRIB'
        public const int kInputFourCCIOCTLDualMotorRumble = 0x524D424C; //'RMBL'
        public const int kInputFourCCIOCTLGetKeyInfo = 0x4B594346; //'KYCF'
        public const int kInputFourCCIOCTLGetKeyboardLayout = 0x4B424C54; //'KBLT'
        public const int kInputFourCCIOCTLSetIMEMode = 0x494D454D; //'IMEM'
        public const int kInputFourCCIOCTLSetIMECursorPosition = 0x494D4550; //'IMEP'
        public const int kInputFourCCIOCTLGetEditorWindowCoordinates = 0x45575053; //'EWPS'
        public const int kInputFourCCIOCTLWarpMouse = 0x57504D53; //'WPMS'
        public const int kInputFourCCIOCTLDeviceDimensions = 0x44494D53; //'DIMS'
        public const int kInputFourCCIOCTLUserId = 0x55534552; //'USER'
        public const int kInputFourCCIOCTLGetHIDReportDescriptor = 0x48494444; //'HIDD'
        public const int kInputFourCCIOCTLGetParsedHIDReportDescriptor = 0x48494450; //'HIDP'
        public const int kInputFourCCIOCTLGetHIDReportDescriptorSize = 0x48494453; //'HIDS'
        public const int kInputFourCCIOCTLHIDWriteOutputReport = 0x4849444F; //'HIDO'
        public const int kInputFourCCIOCTLSetSamplingFrequency = 0x5353504C; //'SSPL'
        public const int kInputFourCCIOCTLGetSamplingFrequency = 0x534D504C; //'SMPL'
    }
}