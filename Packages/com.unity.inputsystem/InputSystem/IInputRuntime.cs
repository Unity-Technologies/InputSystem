using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Layouts;

#if UNITY_EDITOR
using UnityEditor;
#endif

////TODO: add API to send events in bulk rather than one by one

namespace UnityEngine.InputSystem.LowLevel
{
    internal delegate void InputUpdateDelegate(InputUpdateType updateType, ref InputEventBuffer eventBuffer);

    /// <summary>
    /// Input functions that have to be performed by the underlying input runtime.
    /// </summary>
    /// <remarks>
    /// The runtime owns the input event queue, reports device discoveries, and runs
    /// periodic updates that flushes out events from the queue. Updates can also be manually
    /// triggered by calling <see cref="Update"/>.
    /// </remarks>
    internal unsafe interface IInputRuntime
    {
        /// <summary>
        /// Allocate a new unique device ID.
        /// </summary>
        /// <returns>A numeric device ID that is not <see cref="InputDevice.InvalidDeviceId"/>.</returns>
        /// <remarks>
        /// Device IDs are managed by the runtime. This method allows creating devices that
        /// can use the same ID system but are not known to the underlying runtime.
        /// </remarks>
        int AllocateDeviceId();

        /// <summary>
        /// Manually trigger an update.
        /// </summary>
        /// <param name="type">Type of update to run. If this is a combination of updates, each flag
        /// that is set in the mask will run a separate update.</param>
        /// <remarks>
        /// Updates will flush out events and trigger <see cref="onBeforeUpdate"/> and <see cref="onUpdate"/>.
        /// Also, newly discovered devices will be reported by an update is run.
        /// </remarks>
        void Update(InputUpdateType type);

        /// <summary>
        /// Queue an input event.
        /// </summary>
        /// <remarks>
        /// This method has to be thread-safe.
        /// </remarks>
        /// <param name="ptr">Pointer to the event data. Uses the <see cref="InputEvent"/> format.</param>
        /// <remarks>
        /// Events are copied into an internal buffer. Thus the memory referenced by this method does
        /// not have to persist until the event is processed.
        /// </remarks>
        void QueueEvent(InputEvent* ptr);

        //NOTE: This method takes an IntPtr instead of a generic ref type parameter (like InputDevice.ExecuteCommand)
        //      to avoid issues with AOT where generic interface methods can lead to problems. Il2cpp can handle it here
        //      just fine but Mono will run into issues.
        /// <summary>
        /// Perform an I/O transaction directly against a specific device.
        /// </summary>
        /// <remarks>
        /// This function is used to set up device-specific communication controls between
        /// a device and the user of a device. The interface does not dictate a set of supported
        /// IOCTL control codes.
        /// </remarks>
        /// <param name="deviceId">Device to send the command to.</param>
        /// <param name="commandPtr">Pointer to the command buffer.</param>
        /// <returns>Negative value on failure, >=0 on success. Meaning of return values depends on the
        /// command sent to the device.</returns>
        long DeviceCommand(int deviceId, InputDeviceCommand* commandPtr);

        /// <summary>
        /// Set delegate to be called on input updates.
        /// </summary>
        InputUpdateDelegate onUpdate { get; set; }

        /// <summary>
        /// Set delegate to be called right before <see cref="onUpdate"/>.
        /// </summary>
        /// <remarks>
        /// This delegate is meant to allow events to be queued that should be processed right
        /// in the upcoming update.
        /// </remarks>
        Action<InputUpdateType> onBeforeUpdate { get; set; }

        Func<InputUpdateType, bool> onShouldRunUpdate { get; set; }

        #if UNITY_EDITOR
        /// <summary>
        /// Set delegate to be called during player loop initialization callbacks.
        /// </summary>
        Action onPlayerLoopInitialization { get; set; }
        #endif

        /// <summary>
        /// Set delegate to be called when a new device is discovered.
        /// </summary>
        /// <remarks>
        /// The runtime should delay reporting of already present devices until the delegate
        /// has been put in place and then call the delegate for every device already in the system.
        ///
        /// First parameter is the ID assigned to the device, second parameter is a description
        /// in JSON format of the device (see <see cref="InputDeviceDescription.FromJson"/>).
        /// </remarks>
        Action<int, string> onDeviceDiscovered { get; set; }

        /// <summary>
        /// Set delegate to call when the application changes focus.
        /// </summary>
        /// <seealso cref="Application.onFocusChanged"/>
        Action<bool> onPlayerFocusChanged { get; set; }

        /// <summary>
        // Is true when the player or game view has focus.
        /// </summary>
        /// <seealso cref="Application.isFocused"/>
        bool isPlayerFocused { get; }

        /// <summary>
        /// Set delegate to invoke when system is shutting down.
        /// </summary>
        Action onShutdown { get; set; }

        /// <summary>
        /// Set the background polling frequency for devices that have to be polled.
        /// </summary>
        /// <remarks>
        /// The frequency is in Hz. A value of 60 means that polled devices get sampled
        /// 60 times a second.
        /// </remarks>
        float pollingFrequency { get; set; }

        /// <summary>
        /// The current time on the same timeline that input events are delivered on.
        /// </summary>
        /// <remarks>
        /// This is used to timestamp events that are not explicitly supplied with timestamps.
        ///
        /// Time in the input system progresses linearly and in real-time and relates to when Unity was started.
        /// In the editor, this always corresponds to <see cref="EditorApplication.timeSinceStartup"/>.
        ///
        /// Input time, however, is offset in relation to <see cref="Time.realtimeSinceStartup"/>. This is because
        /// in the player, <see cref="Time.realtimeSinceStartup"/> is reset to 0 upon loading the first scene and
        /// in the editor, <see cref="Time.realtimeSinceStartup"/> is reset to 0 whenever the editor enters play
        /// mode. As the resetting runs counter to the need of linearly progressing time for input, the input
        /// system will not reset time along with <see cref="Time.realtimeSinceStartup"/>.
        /// </remarks>
        double currentTime { get; }

        /// <summary>
        /// The current time on the same timeline that input events are delivered on, for the current FixedUpdate.
        /// </summary>
        /// <remarks>
        /// This should be used inside FixedUpdate calls instead of currentTime, as FixedUpdates are simulated at times
        /// not matching the real time the simulation corresponds to.
        /// </remarks>
        double currentTimeForFixedUpdate { get; }

        /// <summary>
        /// The value of <c>Time.unscaledTime</c>.
        /// </summary>
        float unscaledGameTime { get; }

        /// <summary>
        /// The time offset that <see cref="currentTime"/> currently has to <see cref="Time.realtimeSinceStartup"/>.
        /// </summary>
        double currentTimeOffsetToRealtimeSinceStartup { get; }

        bool runInBackground { get; }

        Vector2 screenSize { get; }
        ScreenOrientation screenOrientation { get; }

        // If analytics are enabled, the runtime receives analytics events from the input manager.
        // See InputAnalytics.
        #if UNITY_ANALYTICS || UNITY_EDITOR
        void RegisterAnalyticsEvent(string name, int maxPerHour, int maxPropertiesPerEvent);
        void SendAnalyticsEvent(string name, object data);
        #endif

        bool isInBatchMode { get; }

        #if UNITY_EDITOR
        Action<PlayModeStateChange> onPlayModeChanged { get; set; }
        Action onProjectChange { get; set; }
        bool isInPlayMode { get;  }
        bool isPaused { get; }
        bool isEditorActive { get; }

        // Functionality related to the Unity Remote.
        Func<IntPtr, bool> onUnityRemoteMessage { set; }
        void SetUnityRemoteGyroEnabled(bool value);
        void SetUnityRemoteGyroUpdateInterval(float interval);
        #endif
    }

    internal static class InputRuntime
    {
        public static IInputRuntime s_Instance;
        public static double s_CurrentTimeOffsetToRealtimeSinceStartup;
    }

    internal static class InputRuntimeExtensions
    {
        public static unsafe long DeviceCommand<TCommand>(this IInputRuntime runtime, int deviceId, ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            return runtime.DeviceCommand(deviceId, (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command));
        }
    }
}
