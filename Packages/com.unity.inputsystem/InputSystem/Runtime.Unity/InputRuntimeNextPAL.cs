using System;
using AOT;
using Unity.InputSystem.Runtime;
using UnityEngine;

public static class InputRuntimeNextPAL
{
    [MonoPInvokeCallback(typeof(PALCallbacksContainer.Log))]
    public static unsafe void Log(sbyte* ptr)
    {
        Debug.Log($"RuntimeNext: {new string(ptr)}");
    }

    [MonoPInvokeCallback(typeof(PALCallbacksContainer.DebugBreak))]
    public static void DebugBreak()
    {
        throw new InvalidOperationException();
    }

    internal static unsafe PALCallbacksContainer Create()
    {
        return new PALCallbacksContainer(Log, DebugBreak);
    }
}