using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Extensions;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Platforms;

namespace InputSystem.Cookbook.Recipes.Extensions;

internal static class CommandExtensions
{
    public static Command WithPlatform(this Command command, Platform platform)
    {
        switch (platform.System)
        {
            case SystemType.Android:
                return command.Concat("--platform=android");
            case SystemType.IOS:
                return command.Concat("--platform=ios");
            case SystemType.TvOS:
                return command.Concat("--platform=tvOS");
            default:
                return command;
        }
    }
}