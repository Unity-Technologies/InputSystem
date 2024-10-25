using Unity.Profiling;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Input Statistics for Unity Profiler integration.
    /// </summary>
    internal static class InputStatistics
    {
        /// <summary>
        /// The Profiler Category to be used.
        /// </summary>
        internal static readonly ProfilerCategory Category = ProfilerCategory.Input;

        internal const string EventCountName = "Total Input Event Count";
        internal const string EventSizeName = "Total Input Event Size";
        internal const string AverageLatencyName = "Average Input Latency";
        internal const string MaxLatencyName = "Max Input Latency";
        internal const string EventProcessingTimeName = "Total Input Event Processing Time";
        internal const string DeviceCountName = "Input Device Count";
        internal const string ControlCountName = "Active Control Count";
        internal const string CurrentStateMemoryBytesName = "Current State Memory Bytes";
        internal const string StateBufferSizeBytesName = "Total State Buffer Size";
        internal const string UpdateCountName = "Update Count";

        /// <summary>
        /// Counter reflecting the number of input events.
        /// </summary>
        /// <remarks>
        /// We use ProfilerCounterValue instead of ProfilerCounter since there may be multiple Input System updates
        /// per frame and we want it to accumulate for the profilers perspective on what a frame is but auto-reset
        /// when outside the profilers perspective of a frame.
        /// </remarks>
        public static readonly ProfilerCounterValue<int> EventCount = new ProfilerCounterValue<int>(
            Category, EventCountName, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter reflecting the accumulated input event size in bytes.
        /// </summary>
        /// <remarks>
        /// We use ProfilerCounterValue instead of ProfilerCounter since there may be multiple Input System updates
        /// per frame and we want it to accumulate for the profilers perspective on what a frame is but auto-reset
        /// when outside the profilers perspective of a frame.
        /// </remarks>
        public static readonly ProfilerCounterValue<int> EventSize = new ProfilerCounterValue<int>(
            Category, EventSizeName, ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the average input latency.
        /// </summary>
        /// <remarks>
        /// We use ProfilerCounterValue instead of ProfilerCounter since there may be multiple Input System updates
        /// per frame and we want it to accumulate for the profilers perspective on what a frame is but auto-reset
        /// when outside the profilers perspective of a frame.
        /// </remarks>
        public static readonly ProfilerCounterValue<double> AverageLatency = new ProfilerCounterValue<double>(
            Category, AverageLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the maximum input latency.
        /// </summary>
        /// <remarks>
        /// We use ProfilerCounterValue instead of ProfilerCounter since there may be multiple Input System updates
        /// per frame and we want it to accumulate for the profilers perspective on what a frame is but auto-reset
        /// when outside the profilers perspective of a frame.
        /// </remarks>
        public static readonly ProfilerCounterValue<double> MaxLatency = new ProfilerCounterValue<double>(
            Category, MaxLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the accumulated event processing time (Update) during a rendering frame.
        /// </summary>
        /// <remarks>
        /// We use ProfilerCounterValue instead of ProfilerCounter since there may be multiple Input System updates
        /// per frame and we want it to accumulate for the profilers perspective on what a frame is but auto-reset
        /// when outside the profilers perspective of a frame.
        /// </remarks>
        public static readonly ProfilerCounterValue<double> EventProcessingTime = new ProfilerCounterValue<double>(
            Category, EventProcessingTimeName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// The number of devices currently added to the Input System.
        /// </summary>
        public static readonly ProfilerCounter<int> DeviceCount = new ProfilerCounter<int>(
            Category, DeviceCountName, ProfilerMarkerDataUnit.Count);

        /// <summary>
        /// The total number of device controls currently in the Input System.
        /// </summary>
        public static readonly ProfilerCounter<int> ControlCount = new ProfilerCounter<int>(
            Category, ControlCountName, ProfilerMarkerDataUnit.Count);

        /// <summary>
        /// The total state buffer size in bytes.
        /// </summary>
        public static readonly ProfilerCounter<int> StateBufferSizeBytes = new ProfilerCounter<int>(
            Category, StateBufferSizeBytesName, ProfilerMarkerDataUnit.Bytes);

        /// <summary>
        /// The total update count.
        /// </summary>
        /// <remarks>
        /// Update may get called multiple times, e.g. either via manual updates, dynamic update, fixed update
        /// or editor update while running in the editor.
        /// </remarks>
        public static readonly ProfilerCounterValue<int> UpdateCount = new ProfilerCounterValue<int>(
            Category, UpdateCountName, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
    }
}
