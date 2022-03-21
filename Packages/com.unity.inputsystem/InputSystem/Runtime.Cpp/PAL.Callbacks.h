#pragma once

#include "PAL.LibraryAPI.h"

INPUT_C_INTERFACE
{

// ---------------------------------------------------------------------------------------------------------------------
// PAL via callbacks (via reverse PInvoke to managed)
// ---------------------------------------------------------------------------------------------------------------------

struct InputPALCallbacks
{
    void (* Log)(const char* msg);
    void (* DebugTrap)();
};

INPUT_LIB_API void InputSetPALCallbacks(const InputPALCallbacks callbacks);

// Currently we can't generate bindings for variadic arguments functions
#ifndef INPUT_BINDING_GENERATION

// use printf attribute on compilers that support it
// TODO kill it with moving to fmt
#if __clang__ || __GNUC__ || __GCC__
INPUT_LIB_API void InputLog(const char* fmt, ...) __attribute__((format(printf, 1, 2)));
INPUT_LIB_API void _InputAssert(bool expr, const char* fmt, ...) __attribute__((format(printf, 2, 3)));
#else
INPUT_LIB_API void InputLog(const char* fmt, ...);
INPUT_LIB_API void _InputAssert(bool expr, const char* fmt, ...);
#endif

#define InputAssertFormatted(expr, msg, ...) _InputAssert((expr), "%s(%d): Assertion failed (%s) - " msg "\n", __FILE__, __LINE__, #expr, __VA_ARGS__)
#define InputAssert(expr, msg) _InputAssert((expr), "%s(%d): Assertion failed (%s) - " msg "\n", __FILE__, __LINE__, #expr)

#endif

}
