using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Gives an opportunity for device to modify event data in-place before it gets propagated to the rest of the system.
    /// </summary>
    /// <remarks>
    /// If device also implements <see cref="IEventMerger"/> it will run first, because we don't process events ahead-of-time.
    /// </remarks>
    internal interface IEventPreProcessor
    {
        /// <summary>
        /// Preprocess the event. !!! Beware !!! currently events can only shrink or stay the same size.
        /// </summary>
        /// <param name="currentEventPtr">The event to preprocess.</param>
        /// <returns>True if event should be processed further, false if event should be skipped and ignored.</returns>
        bool PreProcessEvent(InputEventPtr currentEventPtr);
    }
}
