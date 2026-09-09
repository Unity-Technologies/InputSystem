namespace DocCodeSamples.Tests
{
    using UnityEngine.InputSystem;

    internal class GamepadPolling
    {
        void Example()
        {
            #region pollingFrequency
            // Poll gamepads at 120 Hz.
            InputSystem.pollingFrequency = 120;
            #endregion
        }
    }
}
