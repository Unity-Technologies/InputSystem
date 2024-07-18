
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
using Unity.Profiling;
#endif

namespace UnityEngine.InputSystem.Profiler
{
    /// <summary>
    /// Wrapper class to encapsulate the use of Profiler and ProfilerMarker in the Input System based
    /// on the Unity version and the presence of the com.unity.profiling.core package in a project.
    /// </summary>
    internal class InputProfilerMarker
    {
        readonly string m_Name;
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
        readonly ProfilerMarker m_ProfilerMarker;
#endif

        public InputProfilerMarker(string name)
        {
            m_Name = name;
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
            m_ProfilerMarker = new ProfilerMarker(name);
#endif
        }

        /// <summary>
        /// Begin tracking time for the profiler marker.
        /// </summary>
        public void Begin()
        {
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
            m_ProfilerMarker.Begin();
#else
            Profiling.Profiler.BeginSample(m_Name);
#endif
        }

        /// <summary>
        /// End tracking time for the profiler marker.
        /// </summary>
        public void End()
        {
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
            m_ProfilerMarker.End();
#else
            Profiling.Profiler.EndSample();
#endif
        }
    }
}