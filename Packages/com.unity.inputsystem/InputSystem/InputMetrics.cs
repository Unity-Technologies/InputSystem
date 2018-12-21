using System;

namespace UnityEngine.Experimental.Input
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
        public int totalFrameCount;

        public double totalEventProcessingTime;

        public float averageEventBytesPerFrame
        {
            get { return (float)totalEventBytes / totalFrameCount; }
        }

        public double averageProcessingTimePerEvent
        {
            get { return totalEventProcessingTime / totalEventCount; }
        }

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
