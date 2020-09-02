using System;
using System.Collections;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Mock screen keyboard class, which simulates screen keyboard behavior and is validated by the tests in ScreenKeyboardTests.cs
    /// </summary>
    class FakeScreenKeyboard : ScreenKeyboard
    {
        private static FakeScreenKeyboard ms_Instance;

        public static FakeScreenKeyboard instance
        {
            get
            {
                if (ms_Instance == null)
                    ms_Instance = new FakeScreenKeyboard();
                return ms_Instance;
            }
        }

        string m_InputFieldText;
        RangeInt m_Selection;
        class FakeScreenKeyboardDispatcher : MonoBehaviour
        {
        }

        private FakeScreenKeyboardDispatcher Dispatcher
        {
            get
            {
                var go = GameObject.Find(nameof(FakeScreenKeyboard));
                if (go == null)
                {
                    go = new GameObject(nameof(FakeScreenKeyboard));
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

        private IEnumerator QueueStatusChangeVisible()
        {
            yield return new WaitForEndOfFrame();
            ReportStateChange(ScreenKeyboardState.Visible);
        }

        protected override void InternalHide()
        {
            // Delay keyboard hide
            Dispatcher.StartCoroutine(QueueStatusChangeDone());
        }

        private IEnumerator QueueStatusChangeDone()
        {
            yield return new WaitForEndOfFrame();
            ReportStateChange(ScreenKeyboardState.Done);
        }

        private IEnumerator QueueStatusChangeCancel()
        {
            yield return new WaitForEndOfFrame();
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
                if (m_ShowParams.inputFieldHidden)
                    return;

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
