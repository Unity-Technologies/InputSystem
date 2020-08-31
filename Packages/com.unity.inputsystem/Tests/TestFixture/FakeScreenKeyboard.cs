using System;
using System.Collections;

namespace UnityEngine.InputSystem
{
    // TODO: Maybe have it input system package as a placeholder if no implementation is provided
    class FakeScreenKeyboard : ScreenKeyboard
    {
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

        public override string inputFieldText
        {
            get => m_InputFieldText;
            set
            {
                if (m_InputFieldText.Equals(value))
                    return;
                m_InputFieldText = value;
                ReportInputFieldChange(value);
                m_Selection = new RangeInt(m_InputFieldText.Length, 0);
                ReportSelectionChange(m_Selection.start, m_Selection.length);
            }
        }

        public override RangeInt selection
        {
            get => m_Selection;
            set
            {
                if (m_Selection.Equals(value))
                    return;
                m_Selection = value;
                m_Selection.start = Math.Min(m_InputFieldText.Length, m_Selection.start);
                m_Selection.length = Mathf.Clamp(m_Selection.length, 0, m_InputFieldText.Length - m_Selection.start);
                ReportSelectionChange(m_Selection.start, m_Selection.length);
            }
        }
    }
}
