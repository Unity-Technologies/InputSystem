#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like vec2, but have a strong typedef
struct InputDeltaVector2DControlSample
{
    float x;
    float y;
};
#pragma pack(pop)

static const InputDeltaVector2DControlSample InputDeltaVector2DControlSampleDefault = {0.0f, 0.0f};

struct InputDeltaVector2DControlState
{
    uint8_t _reserved;
};

INPUT_LIB_API void InputDeltaVector2DControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_LIB_API void InputDeltaVector2DFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputDeltaVector2DControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputDeltaVector2DControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
);

}