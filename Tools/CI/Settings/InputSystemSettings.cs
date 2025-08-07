using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;
using RecipeEngine.Platforms;
using RecipeEngine.Unity.Abstractions.Editors;

namespace InputSystem.Cookbook.Settings;

public class InputSystemSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = ["Packages"];

    private static InputSystemSettings? _instance;
    
    public static readonly string BranchName = "develop";
    public static readonly string InputSystemPackageName = "com.unity.inputsystem";

    // Command to install .NET Framework 4.7.1 Developer Pack which is used by doctools on Windows.
    public static readonly string NetfxInstallCmd = "%GSUDO% choco install netfx-4.7.1-devpack -y --ignore-detected-reboot --ignore-package-codes";
    public static readonly string DoctoolsInstallCmd = "git clone --branch \"2.3.0-preview\" git@github.cds.internal.unity3d.com:unity/com.unity.package-manager-doctools.git Packages/com.unity.package-manager-doctools";

    public WrenchPackage InputSystemPackage => Wrench.Packages[InputSystemPackageName];

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            InputSystemPackageName,
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
    /*private readonly Dictionary<SystemType, Platform> ImageOverrides = new()
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
    };*/

    public static InputSystemSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new InputSystemSettings();
            }
            return _instance;
        }
    }

    public InputSystemSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            wrenchCsProjectPath: "/Tools/CI/InputSystem.Cookbook.csproj",
            useLocalPvpExemptions: true
        );

        // ignore packages listed below in PreviewAPV
        Wrench.Packages["com.unity.inputsystem"].DependantsToIgnoreInPreviewApv = new Dictionary<Editor, ISet<string>>()
        {
            {
                new Editor("6000.3",  ""),
                new HashSet<string>()
                {
                    "com.unity.polyspatial",
                    "com.unity.polyspatial.visionos",
                    "com.unity.polyspatial.extensions",
                    "com.unity.polyspatial.xr",
                    "com.unity.xr.visionos" 
                }
            }
        };
        
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };
    }
    
    public WrenchSettings Wrench { get; private set; }
}
