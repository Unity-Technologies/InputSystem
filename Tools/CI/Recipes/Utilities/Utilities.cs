using InputSystem.Cookbook.Settings;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;

namespace InputSystem.Cookbook.Recipes;

internal static class Utilities
{
    
    public static bool IsEditorPlatform(Platform platform)
    {
        return InputSystemSettings.Instance.InputSystemPackage.EditorPlatforms.ContainsKey(platform.System);
    }
    
    public static string GetEditorDownloadCommand(string editor, Platform platform)
    {
        if (IsEditorPlatform(platform))
            return $"unity-downloader-cli -u {editor} -c Editor --fast --wait";
        else
            return $"unity-downloader-cli -u {editor} -c Editor  -c {Utilities.GetPlatformName(platform)} --fast --wait";
    }

    public static string GetPlatformName(Platform platform)
    {
#pragma warning disable CS8603 // Possible null reference return.
        return platform.System switch
        {
            SystemType.Ps4 => "ps4",
            SystemType.Ps5 => "ps5",
            SystemType.Xbox => "xbox",
            SystemType.XboxOne => "GameCoreXboxOne",
            SystemType.XboxSeriesS => "GameCoreScarlett",
            SystemType.XboxSeriesX => "GameCoreScarlett",
            SystemType.Switch => "switch",
            SystemType.IOS => "iOS",
            SystemType.TvOS => "AppleTV",
            SystemType.Android => "Android",
            _ => Enum.GetName(typeof(SystemType), platform.System)
        };
#pragma warning restore CS8603 // Possible null reference return.
    }

}