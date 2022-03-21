#pragma once

#include "PAL.LibraryAPI.h"
#include "FramebufferRef.h"
#include "DeviceRef.h"

#include <stdint.h>

INPUT_C_INTERFACE
{

struct InputControlTypeRef
{
    uint32_t transparent;

    inline bool operator==(const InputControlTypeRef o) const noexcept
    {
        return transparent == o.transparent;
    }

    inline bool operator!=(const InputControlTypeRef o) const noexcept
    {
        return transparent != o.transparent;
    }
};

static const InputControlTypeRef InputControlTypeRefInvalid = {0};

struct InputControlUsage
{
    uint32_t transparent;

    inline bool operator==(const InputControlUsage o) const noexcept
    {
        return transparent == o.transparent;
    }

    inline bool operator!=(const InputControlUsage o) const noexcept
    {
        return transparent != o.transparent;
    }
};

INPUT_INLINE_API InputControlUsage InputControlGetVirtualControlUsage(const InputControlUsage parentControl, const uint32_t virtualControlOffset)
{
    return {parentControl.transparent + virtualControlOffset};
}

static const InputControlUsage InputControlUsageInvalid = {0};

struct InputControlDescr
{
    char displayName[256];
    // TODO what are we missing even?
};

enum class InputControlRecordingMode
{
    Disabled,
    LatestOnly,
    AllMerged,
    AllAsIs
};

struct InputControlRef
{
    InputControlUsage usage;
    InputDeviceRef deviceRef;

    static inline InputControlRef Setup(const InputControlUsage usage, const InputDeviceRef deviceRef)
    {
        return {usage, deviceRef};
    }

    inline bool operator==(const InputControlRef o) const noexcept
    {
        return usage == o.usage && deviceRef == o.deviceRef;
    }

    inline bool operator!=(const InputControlRef o) const noexcept
    {
        return !(usage == o.usage && deviceRef == o.deviceRef);
    }
};

static const InputControlRef InputControlRefInvalid = {InputControlUsageInvalid, InputDeviceRefInvalid};

struct InputControlTimestamp
{
    uint64_t timestamp;
    uint16_t timeline;

    inline bool operator==(const InputControlTimestamp o) const noexcept
    {
        return timestamp == o.timestamp && timeline == o.timeline;
    }

    inline bool operator!=(const InputControlTimestamp o) const noexcept
    {
        return !(timestamp == o.timestamp && timeline == o.timeline);
    }
};

struct InputControlVisitorGenericState
{
    void* controlState;
    InputControlTimestamp* latestRecordedTimestamp;
    void* latestRecordedSample;
};

struct InputControlVisitorGenericRecordings
{
    InputControlTimestamp* allRecordedTimestamps;
    void* allRecordedSamples;
    uint32_t allRecordedCount;
};

void InputInstantiateControlsInternal(
    const InputControlRef* controlRefs,
    const uint32_t controlRefsCount
);

void InputRemoveControlsInternal(
    const InputControlRef* controlRefs,
    const uint32_t controlRefsCount
);

INPUT_LIB_API void InputSetControlDescr(
    const InputControlRef controlRef,
    const InputControlDescr descr
);

INPUT_LIB_API bool InputGetControlDescr(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlDescr* outDescr
);

INPUT_LIB_API void InputSetRecordingMode(
    const InputControlRef controlRef,
    const InputControlRecordingMode recordingMode
);

INPUT_LIB_API InputControlRecordingMode InputGetRecordingMode(
    const InputControlRef controlRef
);

INPUT_LIB_API void InputForceSyncControlInFrontbufferWithBackbuffer(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef
);

INPUT_LIB_API void InputGetControlVisitorGenericState(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlVisitorGenericState* outVisitor
);

INPUT_LIB_API void InputGetControlVisitorGenericRecordings(
    const InputControlRef controlRef,
    const InputFramebufferRef framebufferRef,
    InputControlVisitorGenericRecordings* outVisitor
);

INPUT_LIB_API void InputGetCurrentTime(InputControlTimestamp* outTimestamp);

// TODO iterator over samples from two or more controls

};
