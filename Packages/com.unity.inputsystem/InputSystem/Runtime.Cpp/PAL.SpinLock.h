#pragma once

#ifndef INPUT_BINDING_GENERATION

#include <atomic>

// TODO can we kill this include here? it pollutes all over other header files that should otherwise be platform independent
#if defined(_MSC_VER)
#define WIN32_LEAN_AND_MEAN 1
#define NOMINMAX
#include <windows.h>
#endif

// Based on https://rigtorp.se/spinlock/
struct InputFastSpinlock {
    std::atomic<bool> lock_ = {false};

    inline void lock() noexcept
    {
        while(true)
        {
            if (!lock_.exchange(true, std::memory_order_acquire))
                return;

            while (lock_.load(std::memory_order_relaxed))
            {
                #if defined(_MSC_VER)
                    YieldProcessor();
                #elif defined(__x86_64__) || defined(_M_X64) || defined(__x86__) || defined(__i386__) || defined(_M_IX86)
                    __asm__ __volatile__("pause" ::: "memory");
                #elif (defined(__arm64__) || defined(__aarch64__) || defined(__arm__)) && (defined(__clang__) || defined(__GNUC__))
                    __asm__ __volatile__("yield");
                #else
                    #error "Please implement processor yield"
                #endif
            }
        }
    }

    inline bool try_lock() noexcept
    {
        return !lock_.load(std::memory_order_relaxed) && !lock_.exchange(true, std::memory_order_acquire);
    }

    inline void unlock() noexcept
    {
        lock_.store(false, std::memory_order_release);
    }
};

#endif