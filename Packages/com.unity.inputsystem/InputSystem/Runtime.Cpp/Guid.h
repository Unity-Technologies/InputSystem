#pragma once

#include "PAL.LibraryAPI.h"
#include <stdint.h>

INPUT_C_INTERFACE
{

//  RFC 4122 Version 4
struct InputGuid
{
    uint64_t a, b;
};

static const InputGuid InputGuidInvalid = { 0, 0 };

INPUT_LIB_API InputGuid InputGuidFromString(const char* guid);
INPUT_LIB_API void InputGuidToString(const InputGuid guid, char* buffer, const uint32_t bufferSize);

}