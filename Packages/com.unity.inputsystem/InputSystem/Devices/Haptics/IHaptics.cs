////REVIEW: Devices usually will automatically shut down haptics if they haven't received a haptics command in some time.
////        How should we deal with that? Should haptics automatically refresh themselves periodically while they are set?

////REVIEW: Do we need a mute in addition to a pause?

namespace UnityEngine.InputSystem.Haptics
{
    /// <summary>
    /// Base interface for haptics on input devices.
    /// </summary>
    /// <remarks>
    /// To support haptics, an <see cref="InputDevice"/> has to implement one or more
    /// haptics interfaces.
    /// </remarks>
    /// <example>
    /// <code>
    /// class MyDevice : InputDevice, IDualMotorRumble
    /// {
    ///     private DualMotorRumble m_Rumble;
    ///
    ///     public void SetMotorSpeeds(float lowFrequency, float highFrequency)
    ///     {
    ///         m_Rumble.SetMotorSpeeds(lowFrequency, highFrequency);
    ///     }
    ///
    ///     public void PauseHaptics()
    ///     {
    ///         m_Rumble.PauseHaptics();
    ///     }
    ///
    ///     public void ResumeHaptics()
    ///     {
    ///         m_Rumble.ResumeHaptics();
    ///     }
    ///
    ///     public void ResetHaptics()
    ///     {
    ///         m_Rumble.ResetHaptics();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="InputSystem.PauseHaptics"/>
    /// <seealso cref="InputSystem.ResumeHaptics"/>
    /// <seealso cref="InputSystem.ResetHaptics"/>
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
        /// <seealso cref="ResumeHaptics"/>
        void PauseHaptics();

        /// <summary>
        /// Resume haptics playback on the device.
        /// </summary>
        /// <remarks>
        /// Should be called after calling <see cref="PauseHaptics"/>. Otherwise does
        /// nothing.
        /// </remarks>
        void ResumeHaptics();

        /// <summary>
        /// Reset haptics playback on the device to its default state.
        /// </summary>
        /// <remarks>
        /// This will turn off all haptics effects that may be playing on the device.
        /// </remarks>
        void ResetHaptics();
    }
}
