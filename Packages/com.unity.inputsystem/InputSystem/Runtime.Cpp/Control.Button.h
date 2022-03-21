#pragma once

#include "Controls.h"

INPUT_C_INTERFACE
{

#pragma pack(push, 1) // we want this to behave just like uint8_t, but have a strong typedef
struct InputButtonControlSample
{
    uint8_t value;

    inline bool IsPressed() const
    {
        return value == 1;
    }

    inline bool IsReleased() const
    {
        return value == 0;
    }

    inline bool operator==(const InputButtonControlSample o) const noexcept
    {
        return value == o.value;
    }

    inline bool operator!=(const InputButtonControlSample o) const noexcept
    {
        return value != o.value;
    }
};
#pragma pack(pop)

static const InputButtonControlSample InputButtonControlSampleDefault = {0};
static const InputButtonControlSample InputButtonControlSamplePressed = {1};
static const InputButtonControlSample InputButtonControlSampleReleased = {0};

struct InputButtonControlState
{
    bool wasPressedThisIOFrame;
    bool wasReleasedThisIOFrame;
};

INPUT_LIB_API void InputButtonControlIngress(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef controlRef,
    const InputControlTypeRef samplesTypeRef,
    const InputControlTimestamp* timestamps,
    const void* samples,
    const uint32_t count,
    const InputControlRef fromAnotherControl
);

INPUT_LIB_API void InputButtonControlFrameBegin(
    const InputControlTypeRef controlTypeRef,
    const InputControlRef* controlRefs,
    InputButtonControlState* controlStates,
    InputControlTimestamp* latestRecordedTimestamps,
    InputButtonControlSample* latestRecordedSamples,
    const uint32_t controlStatesCount
);

}