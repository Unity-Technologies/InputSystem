using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Plugins.UI;

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

    ////REVIEW: should this detect UWP running on Xbox?
    public static bool IsConsolePlatform(this RuntimePlatform platform)
    {
        switch (platform)
        {
            case RuntimePlatform.Switch:
            case RuntimePlatform.PS4:
            case RuntimePlatform.XboxOne:
                return true;
        }

        return false;
    }

    public static bool IsXbox(this RuntimePlatform platform)
    {
        return platform == RuntimePlatform.XboxOne;
    }

    public static bool IsPlayStation(this RuntimePlatform platform)
    {
        return platform == RuntimePlatform.PS4;
    }

    public static void Each<TValue>(this IEnumerable<TValue> enumerable, Action<TValue> action)
    {
        foreach (var element in enumerable.ToArray()) // Wasteful but allows modifying the list we're iterating over.
            action(element);
    }

    public static void BindUIActions(this UIActionInputModule uiInputModule, DemoControls.MenuActions menuActions)
    {
        uiInputModule.move = new InputActionProperty(menuActions.navigate);
        uiInputModule.leftClick = new InputActionProperty(menuActions.click);
    }
}
