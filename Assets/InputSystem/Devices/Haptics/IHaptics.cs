namespace ISX.Haptics
{
    /// <summary>
    /// Base interface for haptics on input devices.
    /// </summary>
    /// <remarks>
    /// To support haptics, an <see cref="InputDevice"/> has to implement one or more
    /// haptics interfaces.
    /// </remarks>
    public interface IHaptics
    {
        /// <summary>
        /// Pause haptics playback on the device.
        /// </summary>
        /// <remarks>
        /// This should preserve current playback settings (such as motor speed levels
        /// or effect playback positions) but shut down feedback effects on the device.
        ///
        /// If proper resumption of effects is not possible, playback should be stopped
        /// and <see cref="ResumeHaptics"/> is allowed to be a no-operation.
        ///
        /// Note that haptics playback states are not required to survive domain reloads
        /// in the editor.
        /// </remarks>
        void PauseHaptics();

        void ResumeHaptics();
    }
}
