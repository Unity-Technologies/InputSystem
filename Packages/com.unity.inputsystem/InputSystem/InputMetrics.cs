using System;

////TODO: provide total metric for amount of unmanaged memory (device state + action state)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Provides information on the level of throughput going through the system.
    /// </summary>
    [Serializable]
    public struct InputMetrics
    {
        /// <summary>
        /// Maximum number of devices that were concurrently added to the system.
        /// </summary>
        public int maxNumDevices;

        /// <summary>
        /// Number of devices currently added to the system.
        /// </summary>
        public int currentNumDevices;

        /// <summary>
        /// The largest the combined state memory for all devices got.
        /// </summary>
        public int maxStateSizeInBytes;

        /// <summary>
        /// Total size of the combined state memory for all current devices.
        /// </summary>
        public int currentStateSizeInBytes;

        public int currentControlCount;
        public int currentLayoutCount;

        public int totalEventBytes;
        public int totalEventCount;
        public int totalUpdateCount;

        public double totalEventProcessingTime;
        public double totalEventLagTime;

        public float averageEventBytesPerFrame => (float)totalEventBytes / totalUpdateCount;

        ////REVIEW: we probably want better averaging than we get with this method; ideally, we should take averages
        ////        each frame and then compute weighted averages as we go; the current method disregards updating spacing
        ////        and event clustering entirely
        public double averageProcessingTimePerEvent => totalEventProcessingTime / totalEventCount;

        /// <summary>
        /// Average time it takes from when an event is generated to when it is processed.
        /// </summary>
        public double averageLagTimePerEvent => totalEventLagTime / totalEventCount;

        ////REVIEW: see how detailed it makes sense to be
        /*
        public TypeCount[] eventCounts;
        public TypeCount[] commandCounts;

        [Serializable]
        public struct TypeCount
        {
            public FourCC typeCode;
            public FourCC formatCode;
            public int count;
        }
        */
    }
}
