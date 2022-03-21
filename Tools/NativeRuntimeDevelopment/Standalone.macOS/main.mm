#include <stdio.h>

//#include <Input.Context.h>
//#include <Input.PAL.Baselib.h>
//#include <Input.PAL.SDL.h>
//#include <Input.Tests.h>

#include <InputRuntime.h>

#include <_BuiltInDeviceDatabase.h>

int main()
{
    InputSetPALCallbacks(
        {
            [](const char* msg)
            {
              printf("%s\n", msg);
              fflush(stdout);
            },
            []()
            {
              __builtin_debugtrap();
            }
        });

//    Gamepad foo;
//    foo[Gamepad::Buttons::East].controlRef;
//    foo[Gamepad::AxisOneWays::LeftTrigger].controlRef;

    InputRuntimeRunNativeTests();

    InputRuntimeInit(1);

    InputRuntimeDeinit();

//    Input_PAL_Baselib_SetCallbacks();
//
//    Input_Tests_RunAll();
//
//    Input_RuntimeContext ctx = {};
//    Input_SetRuntimeContext_ForNative(&ctx);
//    Input_Init(1);
//
//    // Input_PAL_SDL_Looper();
//
//    Input_Deinit();

    return 0;
}