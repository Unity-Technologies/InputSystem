namespace UnityEngine.InputSystem
{
    // TODO: Maybe have it input system package as a placeholder if no implementation is provided
    class FakeScreenKeyboard : ScreenKeyboard
    {
        protected override void InternalShow()
        {
            ReportStatusChange(ScreenKeyboardStatus.Visible);
        }

        protected override void InternalHide()
        {
            ReportStatusChange(ScreenKeyboardStatus.Done);
        }

        /// <summary>
        /// Simulates a method as if user would close the keyboard from UI
        /// </summary>
        internal void SimulateKeybordClose()
        {
            ReportStatusChange(ScreenKeyboardStatus.Done);
        }

        /// <summary>
        /// Simulates a method as if user would would open the keyboard from UI
        /// </summary>
        internal void SimulateKeybordOpen()
        {
            ReportStatusChange(ScreenKeyboardStatus.Visible);
        }
    }
}
