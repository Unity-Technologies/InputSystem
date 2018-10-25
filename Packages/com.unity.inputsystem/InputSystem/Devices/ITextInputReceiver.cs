using UnityEngine.Experimental.Input.LowLevel;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Interface for <see cref="InputDevice">devices</see> that can receive text input events.
    /// </summary>
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
