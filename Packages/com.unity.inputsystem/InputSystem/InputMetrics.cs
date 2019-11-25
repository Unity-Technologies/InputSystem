using System;

////TODO: provide total metric for amount of unmanaged memory (device state + action state)

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Provides information on the level of throughput going through the system.
    /// </summary>
    /// <seealso cref="InputSystem.metrics"/>
    [Serializable]
    public struct InputMetrics
    {
        /// <summary>
        /// Maximum number of devices that were concurrently added to the system.
        /// </summary>
        /// <seealso cref="InputSystem.devices"/>
        public int maxNumDevices { get; set; }

        /// <summary>
        /// Number of devices currently added to the system.
        /// </summary>
        /// <seealso cref="InputSystem.devices"/>
        public int currentNumDevices { get; set; }

        /// <summary>
        /// The largest the combined state memory for all devices got.
        /// </summary>
        public int maxStateSizeInBytes { get; set; }

        /// <summary>
        /// Total size of the combined state memory for all current devices.
        /// </summary>
        public int currentStateSizeInBytes { get; set; }

        /// <summary>
        /// Total number of <see cref="InputControl"/>s currently alive in
        /// devices in the system.
        /// </summary>
        public int currentControlCount { get; set; }

        /// <summary>
        /// Total number of currently registered layouts.
        /// </summary>
        public int currentLayoutCount { get; set; }

        /// <summary>
        /// Total number of bytes of <see cref="InputEvent"/>s consumed so far.
        /// </summary>
        public int totalEventBytes { get; set; }

        /// <summary>
        /// Total number of <see cref="InputEvent"/>s consumed so far.
        /// </summary>
        public int totalEventCount { get; set; }

        /// <summary>
        /// Total number of input system updates run so far.
        /// </summary>
        /// <seealso cref="InputSystem.Update"/>
        public int totalUpdateCount { get; set; }

        /// <summary>
        /// Total time in seconds spent processing <see cref="InputEvent"/>s so far.
        /// </summary>
        /// <remarks>
        /// Event processing usually amounts for the majority of time spent in <see cref="InputSystem.Update"/>
        /// but not necessarily for all of it.
        /// </remarks>
        /// <seealso cref="InputSystem.Update"/>
        public double totalEventProcessingTime { get; set; }

        /// <summary>
        /// Total accumulated time that has passed between when events were generated (see <see cref="InputEvent.time"/>)
        /// compared to when they were processed.
        /// </summary>
        public double totalEventLagTime { get; set; }

        /// <summary>
        /// Average size of the event buffer received on every <see cref="InputSystem.Update"/>.
        /// </summary>
        public float averageEventBytesPerFrame => (float)totalEventBytes / totalUpdateCount;

        ////REVIEW: we probably want better averaging than we get with this method; ideally, we should take averages
        ////        each frame and then compute weighted averages as we go; the current method disregards updating spacing
        ////        and event clustering entirely
        /// <summary>
        /// Average time in seconds spend on processing each individual <see cref="InputEvent"/>.
        /// </summary>
        public double averageProcessingTimePerEvent => totalEventProcessingTime / totalEventCount;

        /// <summary>
        /// Average time it takes from when an event is generated to when it is processed.
        /// </summary>
        /// <seealso cref="totalEventLagTime"/>
        public double averageLagTimePerEvent => totalEventLagTime / totalEventCount;
    }
}
