#if UNITY_EDITOR || UNITY_IOS || PACKAGE_DOCS_GENERATION
using System.Runtime.InteropServices;
using AOT;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.iOS.LowLevel
{
    /// <summary>
    /// Describes the access for motion related features.
    /// </summary>
    /// <remarks>Enum values map values from CoreMotion.framework/Headers/CMAuthorization.h</remarks>
    public enum MotionAuthorizationStatus
    {
        /// <summary>
        /// The access status was not yet determined.
        /// </summary>
        NotDetermined = 0,

        /// <summary>
        /// Access was denied due system settings.
        /// </summary>
        Restricted,

        /// <summary>
        /// Access was denied by the user.
        /// </summary>
        Denied,

        /// <summary>
        /// Access was allowed by the user.
        /// </summary>
        Authorized
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct iOSStepCounterState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'S', 'C', 'S');
        public FourCC format => kFormat;

        [InputControl(name = "stepCounter", layout = "Integer")]
        public int stepCounter;
    }

    /// <summary>
    /// Step Counter (also known as pedometer) sensor for iOS.
    /// </summary>
    /// <remarks>
    /// You need to enable Motion Usage in Input System settings (see <see cref="InputSettings.iOSSettings.motionUsage"/>), to be allowed
    /// to access the sensor on the user's device.
    /// <example>
    /// <code>
    /// void Start()
    /// {
    ///     InputSystem.EnableDevice(StepCounter.current);
    /// }
    ///
    /// void OnGUI()
    /// {
    ///     GUILayout.Label(StepCounter.current.stepCounter.ReadValue().ToString());
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputSettings.iOSSettings.motionUsage"/>
    [InputControlLayout(stateType = typeof(iOSStepCounterState), variants = "StepCounter", hideInUI = true)]
    public class iOSStepCounter : StepCounter
    {
        private const int kCommandFailure = -1;
        private const int kCommandSuccess = 1;

        internal delegate void OnDataReceivedDelegate(int deviceId, int numberOfSteps);

        [StructLayout(LayoutKind.Sequential)]
        private struct iOSStepCounterCallbacks
        {
            internal OnDataReceivedDelegate onData;
        }

        [DllImport("__Internal")]
        private static extern int _iOSStepCounterEnable(int deviceId, ref iOSStepCounterCallbacks callbacks, int sizeOfCallbacks);

        [DllImport("__Internal")]
        private static extern int _iOSStepCounterDisable(int deviceId);

        [DllImport("__Internal")]
        private static extern int _iOSStepCounterIsEnabled(int deviceId);

        [DllImport("__Internal")]
        private static extern int _iOSStepCounterIsAvailable();

        [DllImport("__Internal")]
        private static extern int _iOSStepCounterGetAuthorizationStatus();

        [MonoPInvokeCallback(typeof(OnDataReceivedDelegate))]
        private static void OnDataReceived(int deviceId, int numberOfSteps)
        {
            var stepCounter = (iOSStepCounter)InputSystem.GetDeviceById(deviceId);
            InputSystem.QueueStateEvent(stepCounter, new iOSStepCounterState {stepCounter = numberOfSteps});
        }

#if UNITY_EDITOR
        private bool m_Enabled = false;
#endif
        protected override unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
        {
            var t = commandPtr->type;
            if (t == QueryEnabledStateCommand.Type)
            {
#if UNITY_EDITOR
                ((QueryEnabledStateCommand*)commandPtr)->isEnabled = m_Enabled;
#else
                ((QueryEnabledStateCommand*)commandPtr)->isEnabled = _iOSStepCounterIsEnabled(deviceId) != 0;
#endif
                return kCommandSuccess;
            }

            if (t == EnableDeviceCommand.Type)
            {
                if (InputSystem.settings.iOS.motionUsage.enabled == false)
                {
                    Debug.LogError("Please enable Motion Usage in Input Settings before using Step Counter.");
                    return kCommandFailure;
                }
#if UNITY_EDITOR
                m_Enabled = true;
                return kCommandSuccess;
#else
                var callbacks = new iOSStepCounterCallbacks();
                callbacks.onData = OnDataReceived;
                return _iOSStepCounterEnable(deviceId, ref callbacks, Marshal.SizeOf(callbacks));
#endif
            }

            if (t == DisableDeviceCommand.Type)
            {
#if UNITY_EDITOR
                m_Enabled = false;
                return kCommandSuccess;
#else
                return _iOSStepCounterDisable(deviceId);
#endif
            }

            if (t == QueryCanRunInBackground.Type)
            {
                ((QueryCanRunInBackground*)commandPtr)->canRunInBackground = true;
                return kCommandSuccess;
            }

            if (t == RequestResetCommand.Type)
            {
#if UNITY_EDITOR
                m_Enabled = false;
#else
                _iOSStepCounterDisable(deviceId);
#endif
                return kCommandSuccess;
            }

            return kCommandFailure;
        }

        /// <summary>
        /// Does the phone support the pedometer?
        /// </summary>
        public static bool IsAvailable()
        {
#if UNITY_EDITOR
            return false;
#else
            return _iOSStepCounterIsAvailable() != 0;
#endif
        }

        /// <summary>
        /// Indicates whether the app is authorized to gather data for step counter sensor.
        /// </summary>
        public static MotionAuthorizationStatus AuthorizationStatus
        {
            get
            {
#if UNITY_EDITOR
                return MotionAuthorizationStatus.NotDetermined;
#else
                return (MotionAuthorizationStatus)_iOSStepCounterGetAuthorizationStatus();
#endif
            }
        }
    }
}
#endif
