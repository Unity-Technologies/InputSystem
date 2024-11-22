using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace InputSystem.Cookbook.Settings;

public class InputSystemSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = {"Packages"};

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.inputsystem",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true },
                PackJobOptions = new PackJobOptions()
                {
                    PrePackCommands = new List<Command>()
                    {
                        // We keep the samples in Assets/ as they otherwise won't get imported and you can't
                        // really work with them. Move them into the package for when we pack the package.
                        new Command("mv ./Assets/Samples ./Packages/com.unity.inputsystem"),
                        new Command("mv ./Assets/Samples.meta ./Packages/com.unity.inputsystem"),
                    }
                }
            }
        }
    };

    public InputSystemSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            useLocalPvpExemptions: true
        );
        
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
    }
    
    public WrenchSettings Wrench { get; private set; }
}
