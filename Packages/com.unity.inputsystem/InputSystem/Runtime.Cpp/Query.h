#pragma once

#include "PAL.LibraryAPI.h"
#include "Controls.h"

#include <stdint.h>

INPUT_C_INTERFACE
{

struct InputQueryDescr
{
    InputControlTypeRef requiredType;
    InputControlUsage requiredUsage;

    InputDeviceRef deviceSessionId;

    // TODO device usages will go here

    enum class ChangedQueryMode
    {
        Any,
        OnlyChanged,
        OnlyNotChanged
    };

    ChangedQueryMode changed;

    enum class ButtonQueryMode
    {
        Any,
        OnlyWasPressed,
        OnlyWasReleased
    };

    ButtonQueryMode _button;

};

struct InputQueryRef
{
    uint32_t _opaque;
};

struct InputControlsLinkedList
{
    const InputControlRef controlRef;
    const InputControlsLinkedList* next;
};


// TODO does this actually force execute the query?
INPUT_LIB_API InputQueryRef InputRegisterQuery(
    const InputFramebufferRef framebufferRef,
    const InputQueryDescr queryDescr
);

INPUT_LIB_API void InputRemoveQuery(
    const InputQueryRef queryRef
);

// return nullptr is none, pointer is valid until next buffer swap or ForceSyncControlInFrontbufferWithBackbuffer
INPUT_LIB_API const InputControlsLinkedList* InputGetQueryResult(
    const InputQueryRef queryRef
);

}