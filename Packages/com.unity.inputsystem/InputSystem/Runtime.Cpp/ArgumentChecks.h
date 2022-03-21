#pragma once

#include "Context.h"

#ifndef INPUT_BINDING_GENERATION

#include "PAL.Callbacks.h"

static inline bool ArgumentCheck(const InputFramebufferRef framebufferRef)
{
    const uint32_t given = framebufferRef.transparent;
    const uint32_t count = _GetCtx()->framebuffersCount;
    const bool valid = given < count;
    InputAssertFormatted(valid, "framebufferRef is outside of range, expected less than '%u', was '%u'", count, given);
    return valid;
}

static inline bool _NullPtrCheck(const void* ptr, const char* argName)
{
    const bool valid = ptr != nullptr;
    InputAssertFormatted(valid, "Argument '%s' should be non-null", argName);
    return valid;
}

#define NullPtrCheck(arg) _NullPtrCheck((arg), #arg)

static inline bool _NonZeroCountCheck(const uint32_t count, const char* argName)
{
    const bool valid = count > 0;
    InputAssertFormatted(valid, "Argument '%s' should be >0", argName);
    return valid;
}

#define NonZeroCountCheck(arg) _NonZeroCountCheck((arg), #arg)

#endif