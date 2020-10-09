using System;
using System.Collections;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Mock screen keyboard class, which simulates screen keyboard behavior and is validated by the tests in ScreenKeyboardTests.cs
    /// </summary>
    class EmulatedScreenKeyboard : ScreenKeyboard
    {
#if UNITY_EDITOR
        private EmulatedScreenKeyboardVisualization m_Visualization;
#endif

        string m_InputFieldText;
        RangeInt m_Selection;

        class FakeScreenKeyboardDispatcher : MonoBehaviour
        {
        }

        internal EmulatedScreenKeyboard()
        {
        }

        public override void Dispose()
        {
        }

        private FakeScreenKeyboardDispatcher Dispatcher
        {
            get
            {
                var go = GameObject.Find("Unity" + nameof(EmulatedScreenKeyboard));
                if (go == null)
                {
                    go = new GameObject(nameof(EmulatedScreenKeyboard));
                    go.AddComponent<FakeScreenKeyboardDispatcher>();
                }
                return go.GetComponent<FakeScreenKeyboardDispatcher>();
            }
        }

        protected override void InternalShow()
        {
            m_InputFieldText = m_ShowParams.initialText;
            m_Selection = new RangeInt(m_InputFieldText.Length, 0);
            // Delay keyboard show
            Dispatcher.StartCoroutine(QueueStatusChangeVisible());
        }

        IEnumerator Waiting()
        {
#if UNITY_EDITOR
            // WaitForEndOfFrame doesn't work in batch mode
            int startFrame = Time.frameCount;
            return new WaitUntil(() => Time.frameCount - startFrame >= 1);
#else
            yield return new WaitForEndOfFrame();
#endif
        }

#if UNITY_EDITOR
        private EmulatedScreenKeyboardVisualization Visualization
        {
            get
            {
                if (!Application.isPlaying)
                    return null;
                if (m_Visualization != null)
                    return m_Visualization;
                var go = new GameObject("Unity" + nameof(EmulatedScreenKeyboardVisualization));
                m_Visualization = go.AddComponent<EmulatedScreenKeyboardVisualization>();
                m_Visualization.SetCallbacks(this, OnSelectionChange);
                return m_Visualization;
            }
        }
#endif

        private IEnumerator QueueStatusChangeVisible()
        {
            yield return Waiting();
            ReportStateChange(ScreenKeyboardState.Visible);
#if UNITY_EDITOR
            Visualization.Show(m_ShowParams);
#endif
        }

        protected override void InternalHide()
        {
            // Delay keyboard hide
            Dispatcher.StartCoroutine(QueueStatusChangeDone());
        }

        private IEnumerator QueueStatusChangeDone()
        {
            yield return Waiting();
            ReportStateChange(ScreenKeyboardState.Done);
        }

        private IEnumerator QueueStatusChangeCancel()
        {
            yield return Waiting();
            ReportStateChange(ScreenKeyboardState.Canceled);
        }

        private bool IsSelectionEqual(RangeInt a, RangeInt b)
        {
            return a.end == b.end && a.start == b.start;
        }

        private void OnSelectionChange(RangeInt newSelection)
        {
            if (IsSelectionEqual(newSelection, m_Selection))
                return;
            m_Selection = newSelection;
            ReportSelectionChange(m_Selection.start, m_Selection.length);
        }

        public override string inputFieldText
        {
            get => m_InputFieldText;
            set
            {
                if (m_InputFieldText.Equals(value))
                    return;
                m_InputFieldText = value;
                // Note: Order is important, the selection is reported first
                OnSelectionChange(new RangeInt(m_InputFieldText.Length, 0));
                ReportInputFieldChange(value);
            }
        }

        public override RangeInt selection
        {
            get => m_Selection;
            set
            {
                if (m_InputFieldText.Length >= value.end && m_InputFieldText.Length >= value.start)
                    OnSelectionChange(value);
            }
        }

        internal override void SimulateKeyEvent(int keyCode)
        {
            if (keyCode == (int)KeyCode.Escape)
                Dispatcher.StartCoroutine(QueueStatusChangeCancel());
        }
    }
}
