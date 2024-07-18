
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
using Unity.Profiling;
#endif

namespace UnityEngine.InputSystem.Profiler
{
    public class InputProfilerMarker
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

        public void Begin()
        {
#if UNITY_2020_3_OR_NEWER && UNITY_INPUT_SYSTEM_USE_PROFILER_MARKERS
            m_ProfilerMarker.Begin();
#else
            Profiling.Profiler.BeginSample(m_Name);
#endif
        }

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