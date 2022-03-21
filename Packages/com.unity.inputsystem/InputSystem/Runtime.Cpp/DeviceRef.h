#pragma once

#include "PAL.LibraryAPI.h"

#include <stdint.h>

INPUT_C_INTERFACE
{

struct InputDeviceRef
{
    uint32_t _opaque;

    inline bool operator==(const InputDeviceRef o) const noexcept
    {
        return _opaque == o._opaque;
    }

#ifndef INPUT_BINDING_GENERATION
    inline bool operator!=(const InputDeviceRef o) const noexcept
    {
        return !(*this == o);
    }
#endif
};

static const InputDeviceRef InputDeviceRefInvalid = { 0};

}