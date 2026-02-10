using System.Reflection;
using System.Runtime.Serialization;
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
    
    public static string GetEditorDownloadCommand(string unityBranch, Platform platform, string? backend=null)
    {
        if (IsEditorPlatform(platform))
        {
            if (backend == "Il2Cpp")
                return $"unity-downloader-cli -u {unityBranch} -c Editor -c StandaloneSupport-IL2CPP --fast --wait";

            return $"unity-downloader-cli -u {unityBranch} -c Editor --fast --wait";
        }

        return $"unity-downloader-cli -u {unityBranch} -c Editor -c {Utilities.GetPlatformName(platform)} --fast --wait";
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

    public static TEnum GetEnumValue<TEnum>(string value) where TEnum : Enum
    {
        var type = typeof(TEnum);
        foreach (var field in type.GetFields())
        {
            var attribute = field.GetCustomAttribute<EnumMemberAttribute>();

            // Ensure both the attribute and its value match
            if (attribute?.Value == value)
            {
                var val = field.GetValue(null);
                if (val is TEnum result)
                {
                    return result;
                }
            }
        }

        throw new ArgumentException($"Value '{value}' not found in {type.Name}.");
    }
}