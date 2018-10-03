using UnityEngine;

public static class UnityExtensions
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
}
