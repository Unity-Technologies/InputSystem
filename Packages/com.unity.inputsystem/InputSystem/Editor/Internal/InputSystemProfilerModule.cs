#if UNITY_EDITOR // Input System currently do not have proper asmdef for editor code.

using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A profiler module that integrates Input System with the Profiler editor window.
    /// </summary>
    [ProfilerModuleMetadata("Input System")] 
    internal sealed class InputSystemProfilerModule : ProfilerModule
    {
        /// <summary>
        /// A profiler module detail view that extends the Profiler window and shows details for the selected frame.
        /// </summary>
        private sealed class InputSystemDetailsViewController : ProfilerModuleViewController
        {   
            public InputSystemDetailsViewController(ProfilerWindow profilerWindow) 
                : base(profilerWindow) 
            { }

            private Label m_EventCountLabel;
            private Label m_EventSizeLabel;
            private Label m_AverageLatencyLabel;
            private Label m_MaxLatencyLabel;
            private Label m_EventProcessingTimeLabel;

            private Label CreateLabel()
            {
                return new Label() { style = { paddingTop = 8, paddingLeft = 8 } };
            }
            
            protected override VisualElement CreateView()
            {
                var view = new VisualElement();

                m_EventCountLabel = CreateLabel();
                m_EventSizeLabel = CreateLabel();
                m_AverageLatencyLabel = CreateLabel();
                m_MaxLatencyLabel = CreateLabel();
                m_EventProcessingTimeLabel = CreateLabel();
                
                view.Add(m_EventCountLabel);
                view.Add(m_EventSizeLabel);
                view.Add(m_AverageLatencyLabel);
                view.Add(m_MaxLatencyLabel);
                view.Add(m_EventProcessingTimeLabel);

                // Populate the label with the current data for the selected frame. 
                ReloadData();

                // Be notified when the selected frame index in the Profiler Window changes, so we can update the label.
                ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;

                return view;
            }
            
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Unsubscribe from the Profiler window event that we previously subscribed to.
                    ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
                }
                
                base.Dispose(disposing);
            }

            void ReloadData()
            {
                var selectedFrameIndex = System.Convert.ToInt32(ProfilerWindow.selectedFrameIndex);
                
                var eventCount = ProfilerDriver.GetFormattedCounterValue(selectedFrameIndex, 
                    InputStatistics.Category.Name, InputStatistics.EventCountName);
                var eventSizeBytes = ProfilerDriver.GetFormattedCounterValue(selectedFrameIndex,
                    InputStatistics.Category.Name, InputStatistics.EventSizeName);
                var averageLatency = ProfilerDriver.GetFormattedCounterValue(selectedFrameIndex,
                    InputStatistics.Category.Name, InputStatistics.AverageLatencyName);
                var maxLatency = ProfilerDriver.GetFormattedCounterValue(selectedFrameIndex,
                    InputStatistics.Category.Name, InputStatistics.MaxLatencyName);
                var eventProcessingTime = ProfilerDriver.GetFormattedCounterValue(selectedFrameIndex,
                    InputStatistics.Category.Name, InputStatistics.EventProcessingTimeName);

                m_EventCountLabel.text = $"{InputStatistics.EventCountName}: {eventCount}";
                m_EventSizeLabel.text = $"{InputStatistics.EventSizeName}: {eventSizeBytes}";
                m_AverageLatencyLabel.text = $"{InputStatistics.AverageLatencyName}: {averageLatency}";
                m_MaxLatencyLabel.text = $"{InputStatistics.MaxLatencyName}: {maxLatency}";
                m_EventProcessingTimeLabel.text = $"{InputStatistics.EventProcessingTimeName}: {eventProcessingTime}";
            }
            
            void OnSelectedFrameIndexChanged(long selectedFrameIndex)
            {
                ReloadData();
            }
        }
        
        private static readonly ProfilerCounterDescriptor[] Counters = new ProfilerCounterDescriptor[]
        {
            new (InputStatistics.EventCountName, InputStatistics.Category),
            new (InputStatistics.EventSizeName, InputStatistics.Category),
            new (InputStatistics.AverageLatencyName, InputStatistics.Category),
            new (InputStatistics.MaxLatencyName, InputStatistics.Category),
            new (InputStatistics.EventProcessingTimeName, InputStatistics.Category),
        };
        
        public InputSystemProfilerModule()
            : base(Counters)
        { }
        
        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new InputSystemDetailsViewController(ProfilerWindow);
        }
    }
}

#endif // UNITY_EDITOR