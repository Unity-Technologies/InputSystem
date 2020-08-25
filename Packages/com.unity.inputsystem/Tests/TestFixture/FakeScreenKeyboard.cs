using System.Collections;

namespace UnityEngine.InputSystem
{
    // TODO: Maybe have it input system package as a placeholder if no implementation is provided
    class FakeScreenKeyboard : ScreenKeyboard
    {
        string m_InputFieldText;
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
            m_InputFieldText = string.Empty;
            // Delay keyboard show
            Dispatcher.StartCoroutine(QueueStatusChangeVisible());
        }

        private IEnumerator QueueStatusChangeVisible()
        {
            yield return new WaitForEndOfFrame();
            ReportStatusChange(ScreenKeyboardStatus.Visible);
        }

        protected override void InternalHide()
        {
            // Delay keyboard hide
            Dispatcher.StartCoroutine(QueueStatusChangeDone());
        }

        private IEnumerator QueueStatusChangeDone()
        {
            yield return new WaitForEndOfFrame();
            ReportStatusChange(ScreenKeyboardStatus.Done);
        }

        public override string inputFieldText
        {
            get => m_InputFieldText;
            set
            {
                m_InputFieldText = value;
                ReportInputFieldChange(value);
                ReportSelectionChange(0, m_InputFieldText.Length);
            }
        }
    }
}
