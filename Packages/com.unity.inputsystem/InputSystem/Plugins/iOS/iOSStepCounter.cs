using System.Runtime.InteropServices;
using AOT;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.iOS.LowLevel
{
    //See CoreMotion.framework/Headers/CMAuthorization.h
    public enum MotionAuthorizationStatus : int
    {
        NotDetermined = 0,
        Restricted,
        Denied,
        Authorized
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct iOSStepCounterState : IInputStateTypeInfo
    {
        public static FourCC kFormat = new FourCC('I', 'S', 'C', 'S');
        public FourCC format => kFormat;

        [InputControl(name = "stepCounter")]
        public int stepCounter;
    }

    /// <summary>
    /// Step Counter (also known as pedometer) sensor for iOS.
    /// Note: You need to add 'Privacy - Motion Usage Description' to Info.plist to use this sensor.
    /// </summary>
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
        public override unsafe long ExecuteCommand<TCommand>(ref TCommand command)
        {
            var ptr = UnsafeUtility.AddressOf(ref command);
            var t = command.typeStatic;
            if (t == QueryEnabledStateCommand.Type)
            {
#if UNITY_EDITOR
                ((QueryEnabledStateCommand*)ptr)->isEnabled = m_Enabled;
#else
                ((QueryEnabledStateCommand*)ptr)->isEnabled = _iOSStepCounterIsEnabled(deviceId) != 0;
#endif
                return kCommandSuccess;
            }

            if (t == EnableDeviceCommand.Type)
            {
#if UNITY_EDITOR
                if (InputSystem.settings.iOS.MotionUsage == false)
                {
                    Debug.LogError("Please enable Motion Usage in Input Settings.");
                    m_Enabled = false;
                    return kCommandFailure;
                }

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
                ((QueryCanRunInBackground*)ptr)->canRunInBackground = true;
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

            Debug.LogWarning($"Unhandled command {command.GetType().Name}");
            return kCommandFailure;
        }

        /// <summary>
        /// Does the phone supports the pedometer?
        /// </summary>
        /// <returns></returns>
        public static bool IsAvailable()
        {
#if UNITY_EDITOR
            return false;
#else
            return _iOSStepCounterIsAvailable() != 0;
#endif
        }

        /// <summary>
        /// Query motion authorization status
        /// </summary>
        /// <returns></returns>
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
