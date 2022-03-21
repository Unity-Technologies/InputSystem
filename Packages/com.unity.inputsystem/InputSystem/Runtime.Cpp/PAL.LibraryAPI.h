#pragma once

#define INPUT_C_INTERFACE   extern "C"
#define INPUT_CPP_INTERFACE extern "C++"
#define INPUT_INLINE_API    static inline

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64) || defined(WINAPI_FAMILY) || defined(__CYGWIN32__)
    #define INPUT_LIB_API_IMPORT __declspec(dllimport)
    #define INPUT_LIB_API_EXPORT __declspec(dllexport)
#elif defined(__MACH__) || defined(__ANDROID__) || defined(__linux__) || defined(LUMIN)
    #define INPUT_LIB_API_IMPORT
    #define INPUT_LIB_API_EXPORT __attribute__ ((visibility ("default")))
#else
    #define INPUT_LIB_API_IMPORT
    #define INPUT_LIB_API_EXPORT
#endif

#if defined(INPUT_USE_DYNAMICLIBRARY)
    #define INPUT_LIB_API INPUT_LIB_API_IMPORT
#elif defined(INPUT_BUILD_DYNAMICLIBRARY)
    #define INPUT_LIB_API INPUT_LIB_API_EXPORT
#else
    #define INPUT_LIB_API
#endif
