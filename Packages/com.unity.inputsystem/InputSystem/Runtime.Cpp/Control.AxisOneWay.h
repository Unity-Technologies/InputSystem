#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like float, but have a strong typedef
struct InputAxisOneWayControlSample
{
    float value;

    inline bool operator==(const InputAxisOneWayControlSample o) const noexcept
    {
        return value == o.value;
    }
};
#pragma pack(pop)

static const InputAxisOneWayControlSample InputAxisOneWayControlSampleDefault = {0.0f};

struct InputAxisOneWayControlState
{
    uint8_t _reserved;
};

INPUT_LIB_API void InputAxisOneWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_INLINE_API void InputAxisOneWayFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputAxisOneWayControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputAxisOneWayControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
){}

}