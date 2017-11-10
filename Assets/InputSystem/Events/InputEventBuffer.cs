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

namespace ISX
{
    // A buffer containing InputEvents.
    // Stored in unmanaged memory. Safe to pass to jobs.
    public struct InputEventBuffer
    {
    }
}
