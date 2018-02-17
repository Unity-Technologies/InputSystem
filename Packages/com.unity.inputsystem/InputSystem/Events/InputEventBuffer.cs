//have fixed-size event buffer *per device*
//allow associating processing logic with buffer
//have base processing logic that can be extended
//should do the right thing exactly for that device (resets, accumulation, setting current, etc)

//No need to run "make current device" repeatedly; run once for entire event buffer
//No need to look up device repeatedly
//No need to go over events twice due to before-render updates
//Can get rid of device IDs in events (and maybe even in entire system) entirely if buffers are per device
//Nicely fits with the need for per-device output event buffers

////REVIEW: turn the core of InputEventTrace into InputEventBuffer?

//solve threading with indirection
//one fixed size buffer of N events of size T
//fixed array of event points used as ring buffer
//threaded freelist

//have two structs: InputEventBuffer which is a plain memory buffer containing InputEvents and InputEventQueue which
//                  implements threaded queueing and dequeuing on fixed size buffers

using System;

namespace ISX.LowLevel
{
    /// <summary>
    /// A buffer containing InputEvents.
    /// </summary>
    /// <remarks>
    /// Stored in unmanaged memory. Safe to pass to jobs.
    ///
    /// Event buffers can be used either to simply hold N events of maximum size M
    /// or they can be used to incrementally write events to. In the latter case,
    /// an event buffer functions like a ring buffer which, when running out of space,
    /// starts overwriting older events.
    /// </remarks>
    public struct InputEventBuffer
    {
        /// <summary>
        /// Copy the given event data to the event buffer.
        /// </summary>
        /// <remarks>
        /// The copying is thread-safe such the event may be overwritten concurrently by
        /// another thread in which case the write will fail and not add data to the buffer.
        /// This, however, relies on two invariants in order to work: overwriting has to
        /// overwrite the header of the event with another header
        /// ..... this won't be thread-safe; we may see partial writes
        ///
        /// Note that this is not thread-safe in the way that it would allow concurrent
        /// writes by multiple threads.
        /// </remarks>
        /// <param name="eventPtr"></param>
        public bool Write(InputEventPtr eventPtr)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
        }
    }
}
