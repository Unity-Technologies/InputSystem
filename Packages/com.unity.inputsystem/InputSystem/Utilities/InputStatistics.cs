using Unity.Profiling;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Input Statistics for Unity Profiler integration.
    /// </summary>
    internal class InputStatistics
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Input;

        public const string kEventCountName = "Input Event Count"; 
        public const string kEventSizeName = "Input Event Size";
        public const string kAverageLatencyName = "Average Latency";
        public const string kMaxLatencyName = "Max Latency";
        public const string kEventProcessingTimeName = "Event Processing Time";

        /// <summary>
        /// Counter reflecting the number of input events. 
        /// </summary>
        public static readonly ProfilerCounterValue<int> EventCount = new ProfilerCounterValue<int>(
            Category, kEventCountName, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        
        /// <summary>
        /// Counter reflecting the accumulated input event size in bytes.
        /// </summary>
        public static readonly ProfilerCounterValue<int> EventSize = new ProfilerCounterValue<int>(
            Category, kEventSizeName, ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the average input latency.
        /// </summary>
        public static readonly ProfilerCounterValue<float> AverageLatency = new ProfilerCounterValue<float>(
            Category, kAverageLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the maximum input latency.
        /// </summary>
        public static readonly ProfilerCounterValue<float> MaxLatency = new ProfilerCounterValue<float>(
            Category, kMaxLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the accumulated event processing time (Update) during a rendering frame.
        /// </summary>
        public static readonly ProfilerCounterValue<double> EventProcessingTime = new ProfilerCounterValue<double>(
            Category, kEventProcessingTimeName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush
            );
    }
}