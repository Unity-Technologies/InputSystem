#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like vec2, but have a strong typedef
struct InputStickControlSample
{
    float x;
    float y;
};
#pragma pack(pop)

static const InputStickControlSample InputStickControlSampleDefault = {0.0f, 0.0f};

struct InputStickControlState
{
    uint8_t _reserved;
};

INPUT_LIB_API void InputStickControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_INLINE_API void InputStickFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputStickControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputStickControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
){}

}