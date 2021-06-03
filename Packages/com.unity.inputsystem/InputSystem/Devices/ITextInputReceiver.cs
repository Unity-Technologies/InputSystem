namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Interface for <see cref="InputDevice"/> classes that can receive text input events.
    /// </summary>
    /// <remarks>
    /// This interface should be implemented by devices that are meant to receive text
    /// input through <see cref="TextEvent"/>.
    /// </remarks>
    /// <seealso cref="TextEvent"/>
    /// <seealso cref="IMECompositionEvent"/>
    public interface ITextInputReceiver
    {
        /// <summary>
        /// A single, fully-formed Unicode character has been typed on the device.
        /// </summary>
        /// <param name="character">Character that was typed. Note that in case the character is part of
        /// a surrogate pair, this method is called first with the high surrogate and then with the
        /// low surrogate character.</param>
        /// <remarks>
        /// This method is called on a device when a <see cref="TextEvent"/> is received
        /// for the device. <paramref name="character"/> is the <see cref="TextEvent.character"/>
        /// from the event.
        ///
        /// Note that this method will be called *twice* for a single <see cref="TextEvent"/>
        /// in case the given UTF-32 (encoding in the event) needs to be represented as UTF-16
        /// (encoding of <c>char</c> in C#) surrogate.
        /// </remarks>
        void OnTextInput(char character);

        /// <summary>
        /// Called when an IME composition is in-progress or finished.
        /// </summary>
        /// <param name="compositionString">The current composition.</param>
        /// <seealso cref="IMECompositionEvent"/>
        /// <seealso cref="Keyboard.onIMECompositionChange"/>
        /// <remarks>
        /// The method will be repeatedly called with the current string while composition is in progress.
        /// Once composition finishes, the method will be called one more time with a blank composition
        /// string.
        /// </remarks>
        void OnIMECompositionChanged(IMECompositionString compositionString);
    }
}
