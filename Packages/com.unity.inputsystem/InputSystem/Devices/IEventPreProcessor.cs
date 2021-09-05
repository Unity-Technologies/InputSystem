using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Gives an opportunity for device to modify event data in-place before it gets propagated to the rest of the system.
    /// Beware that currently events can only shrink or stay the same size.
    /// </summary>
    /// <remarks>
    /// If device also implements <see cref="IEventMerger"/> it will run first, because we don't process events ahead-of-time.
    /// </remarks>
    internal interface IEventPreProcessor
    {
        // return false to skip the event
        bool PreProcessEvent(InputEventPtr currentEventPtr);
    }
}