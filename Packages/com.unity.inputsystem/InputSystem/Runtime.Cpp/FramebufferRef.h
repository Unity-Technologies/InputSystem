#pragma once

#include "PAL.LibraryAPI.h"

#include <stdint.h>

INPUT_C_INTERFACE
{

struct InputFramebufferRef
{
    uint32_t transparent; // 0,1,2,3 whatever <ioFramesCount

    inline bool operator==(InputFramebufferRef const& o) const noexcept
    {
        return transparent == o.transparent;
    }
};

}