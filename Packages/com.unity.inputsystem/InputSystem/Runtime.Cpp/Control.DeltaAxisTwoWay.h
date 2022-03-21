#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like float, but have a strong typedef
struct InputDeltaAxisTwoWayControlSample
{
    float value;
};
#pragma pack(pop)

static const InputDeltaAxisTwoWayControlSample InputDeltaAxisTwoWayControlSampleDefault = {0.0f};

struct InputDeltaAxisTwoWayControlState
{
    uint8_t _reserved;
};

INPUT_LIB_API void InputDeltaAxisTwoWayControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_LIB_API void InputDeltaAxisTwoWayFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputDeltaAxisTwoWayControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputDeltaAxisTwoWayControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
);

}