using Unity.Profiling;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Input Statistics for Unity Profiler integration.
    /// </summary>
    internal static class InputStatistics
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Input;

        public const string EventCountName = "Input Event Count"; 
        public const string EventSizeName = "Input Event Size";
        public const string AverageLatencyName = "Average Latency";
        public const string MaxLatencyName = "Max Latency";
        public const string EventProcessingTimeName = "Event Processing Time";

        /// <summary>
        /// Counter reflecting the number of input events. 
        /// </summary>
        public static readonly ProfilerCounterValue<int> EventCount = new ProfilerCounterValue<int>(
            Category, EventCountName, ProfilerMarkerDataUnit.Count,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
        
        /// <summary>
        /// Counter reflecting the accumulated input event size in bytes.
        /// </summary>
        public static readonly ProfilerCounterValue<int> EventSize = new ProfilerCounterValue<int>(
            Category, EventSizeName, ProfilerMarkerDataUnit.Bytes,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the average input latency.
        /// </summary>
        public static readonly ProfilerCounterValue<double> AverageLatency = new ProfilerCounterValue<double>(
            Category, AverageLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the maximum input latency.
        /// </summary>
        public static readonly ProfilerCounterValue<double> MaxLatency = new ProfilerCounterValue<double>(
            Category, MaxLatencyName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        /// <summary>
        /// Counter value reflecting the accumulated event processing time (Update) during a rendering frame.
        /// </summary>
        public static readonly ProfilerCounterValue<double> EventProcessingTime = new ProfilerCounterValue<double>(
            Category, EventProcessingTimeName, ProfilerMarkerDataUnit.TimeNanoseconds,
            ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
    }
}