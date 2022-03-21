#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like float, but have a strong typedef
struct InputAxisTwoWayControlSample
{
    float value;

    inline bool operator==(const InputAxisTwoWayControlSample o) const noexcept
    {
        return value == o.value;
    }
};
#pragma pack(pop)

static const InputAxisTwoWayControlSample InputAxisTwoWayControlSampleDefault = {0.0f};

struct InputAxisTwoWayControlState
{
    uint8_t _reserved;
};

INPUT_LIB_API void InputAxisTwoWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_INLINE_API void InputAxisTwoWayFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputAxisTwoWayControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputAxisTwoWayControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
){}

}