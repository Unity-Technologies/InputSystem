using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static bool IsDesktopPlatform(this RuntimePlatform platform)
    {
        switch (platform)
        {
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WebGLPlayer:
                return true;
        }

        return false;
    }

    public static void Each<TValue>(this IEnumerable<TValue> enumerable, Action<TValue> action)
    {
        foreach (var element in enumerable.ToArray()) // Wasteful but allows modifying the list we're iterating over.
            action(element);
    }
}
