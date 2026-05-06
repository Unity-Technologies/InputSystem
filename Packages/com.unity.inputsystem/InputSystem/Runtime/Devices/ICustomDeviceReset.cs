namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// A device that implements its own reset logic for when <see cref="InputSystem.ResetDevice"/>
    /// is called.
    /// </summary>
    internal interface ICustomDeviceReset
    {
        /// <summary>
        /// Reset the current device state.
        /// </summary>
        void Reset();
    }
}
