using System.Runtime.InteropServices;
using AOT;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.iOS.LowLevel
{
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
    /// Note: You need to add 'Privacy - Motion Usage Description to Info.plist to use this sensor.
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
        private static extern int _iOSStepCounterIsAuthorized();

        [MonoPInvokeCallback(typeof(OnDataReceivedDelegate))]
        private static void OnDataReceived(int deviceId, int numberOfSteps)
        {
            // Note: this is called on non main thread.
            Debug.Log($"Received data {deviceId} {numberOfSteps}");
            var stepCounter = InputSystem.GetDevice<iOSStepCounter>();

            InputSystem.QueueStateEvent(stepCounter, new iOSStepCounterState(){stepCounter = numberOfSteps});
        }

        public override unsafe long ExecuteCommand<TCommand>(ref TCommand command)
        {
            var ptr = UnsafeUtility.AddressOf(ref command);
            var t = command.typeStatic;
            if (t == QueryEnabledStateCommand.Type)
            {
                ((QueryEnabledStateCommand*)ptr)->isEnabled = _iOSStepCounterIsEnabled(deviceId) != 0;
                return kCommandSuccess;
            }

            if (t == EnableDeviceCommand.Type)
            {
                var callbacks = new iOSStepCounterCallbacks();
                callbacks.onData = OnDataReceived;
                return _iOSStepCounterEnable(deviceId, ref callbacks, Marshal.SizeOf(callbacks));
            }

            if (t == DisableDeviceCommand.Type)
            {
                return _iOSStepCounterDisable(deviceId);
            }

            if (t == QueryCanRunInBackground.Type)
            {
                ((QueryCanRunInBackground*)ptr)->canRunInBackground = false;
                return kCommandSuccess;
            }

            if (t == RequestResetCommand.Type)
            {
                _iOSStepCounterDisable(deviceId);
                return kCommandFailure;
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
        /// Did user authorize the usage of the pedometer?
        /// </summary>
        /// <returns></returns>
        public static bool IsAuthorized()
        {
#if UNITY_EDITOR
            return false;
#else
            return _iOSStepCounterIsAuthorized() != 0;
#endif
        }
    }
}
