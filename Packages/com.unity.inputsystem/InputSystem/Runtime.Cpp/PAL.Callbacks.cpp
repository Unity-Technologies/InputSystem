#include "PAL.Callbacks.h"
#include "Context.h"

#include <stdarg.h>
#include <stdio.h>

void InputSetPALCallbacks(const InputPALCallbacks callbacks)
{
    *_GetPALCallbacks() = callbacks;
}

void InputLog(const char* fmt, ...)
{
    if (_GetPALCallbacks()->Log == nullptr)
        return;

    va_list args;
    va_start(args, fmt);
    char buffer[1024]; // TODO fix me, integrate fmt instead
    vsnprintf(buffer, sizeof(buffer), fmt, args); va_end(args);

    _GetPALCallbacks()->Log(buffer);
}

void _InputAssert(bool expr, const char* fmt, ...)
{
    if(expr)
        return;

    if(_GetPALCallbacks()->Log)
    {
        va_list args;
        va_start(args, fmt);
        char buffer[1024]; // TODO fix me, integrate fmt instead
        vsnprintf(buffer, sizeof(buffer), fmt, args);
        va_end(args);

        _GetPALCallbacks()->Log(buffer);
    }

    if(_GetPALCallbacks()->DebugTrap)
        _GetPALCallbacks()->DebugTrap();
}
