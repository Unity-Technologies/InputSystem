using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;

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
    
    // You can either use a platform.json file or specify custom yamato VM images for each package in code.
    private readonly Dictionary<SystemType, Platform> ImageOverrides = new()
    {
        {
            SystemType.Windows,
            new Platform(new Agent("package-ci/win10:v4", FlavorType.BuildLarge, ResourceType.Vm), SystemType.Windows)
        },
        {
            SystemType.MacOS,
            new Platform(new Agent("package-ci/macos-13:v4", FlavorType.BuildExtraLarge, ResourceType.VmOsx),
                SystemType.MacOS)
        },
        {
            SystemType.Ubuntu,
            new Platform(new Agent("package-ci/ubuntu-20.04:v4", FlavorType.BuildLarge, ResourceType.Vm),
                SystemType.Ubuntu)
        }
    };

    public InputSystemSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            wrenchCsProjectPath: "/Tools/CI/InputSystem.Cookbook.csproj",
            useLocalPvpExemptions: true
        );
        
        // change default ubuntu image.
        Wrench.Packages["com.unity.inputsystem"].EditorPlatforms = ImageOverrides;
        
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
    }
    
    public WrenchSettings Wrench { get; private set; }
}
