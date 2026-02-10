using InputSystem.Cookbook.Recipes;
using Newtonsoft.Json.Linq;
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

    // Mobile platforms which run build jobs
    public readonly Dictionary<SystemType, Platform> MobileBuildPlatforms = new();
    
    // Mobile platforms which run tests jobs
    public readonly Dictionary<SystemType, Platform> MobileTestPlatforms = new();

    // iOS platform with iOS 15 device (iPhone SE 3rd generation) for 6000.3+ editors
    public readonly Platform iOS15Platform;
    
    public readonly string[] AndroidExtraCommands = new[]
    {
        //Establish an ADB connection with the device
        "start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%",
        //List the connected devices
        "start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe devices"
    };

    public readonly string[] AndroidExtraAfterCommands = new[]
    {
        "start %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe connect %BOKKEN_DEVICE_IP%",
        "if not exist build\\test-results mkdir build\\test-results",
        "powershell %ANDROID_SDK_ROOT%\\platform-tools\\adb.exe logcat -d > build/test-results/device_log.txt"
    };

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
    
    readonly string mobileConfigFilePath = ".yamato/mobile_config.json";

    public InputSystemSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions,
            wrenchCsProjectPath: "/Tools/CI/InputSystem.Cookbook.csproj",
            useLocalPvpExemptions: true
        );
        
        var defaultUbuntuPlatform = WrenchPackage.DefaultEditorPlatforms[SystemType.Ubuntu];
        // Use Ubuntu image package-ci/ubuntu-22.04 which is required by 6000.0+ versions.
        InputSystemPackage.EditorPlatforms[SystemType.Ubuntu] = new Platform(new Agent("package-ci/ubuntu-22.04:default",
            defaultUbuntuPlatform.Agent.Flavor, defaultUbuntuPlatform.Agent.Resource), defaultUbuntuPlatform.System);

        // ignore packages listed below in PreviewAPV
        InputSystemPackage.DependantsToIgnoreInPreviewApv = new Dictionary<Editor, ISet<string>>()
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
            },
            {
                new Editor("6000.5",  ""),
                new HashSet<string>()
                {
                    "com.unity.polyspatial",
                    "com.unity.polyspatial.visionos",
                    "com.unity.polyspatial.extensions",
                    "com.unity.polyspatial.xr",
                    "com.unity.xr.visionos",
                    "com.unity.charactercontroller"
                }
            }
        };

        InputSystemPackage.CoverageCommands.Enabled = true;
        var assemblies = InputSystemPackage.CoverageAssemblies;
        var assebmliesNames = InputSystemPackage.CoverageAssemblyNames();
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };

        ReadMobileConfig();
        
        var oldIOSAgent = MobileTestPlatforms[SystemType.IOS].Agent;
        iOS15Platform = new Platform(new Agent(oldIOSAgent.Image, oldIOSAgent.Flavor, oldIOSAgent.Resource, "SE-Gen3"), SystemType.IOS);
    }
    
    public WrenchSettings Wrench { get; private set; }

    void ReadMobileConfig()
    {
        if (!File.Exists(mobileConfigFilePath))
            throw new FileNotFoundException("Mobile config file could not be found.");

        var configs = File.ReadAllText(mobileConfigFilePath);
        var jsonObject = JObject.Parse(configs);

        foreach (var (k, v) in jsonObject)
        {
            SystemType platform;
            switch (k)
            {
                case "android":
                    platform = SystemType.Android;
                    break;
                case "ios":
                    platform = SystemType.IOS;
                    break;
                case "tvos":
                    platform = SystemType.TvOS;
                    break;
                default:
                    platform = SystemType.Unknown;
                    break;
            }
            
            MobileBuildPlatforms.Add(platform, new Platform(
                new Agent(v?["build"]?["image"]?.ToString() ?? string.Empty, 
                    Utilities.GetEnumValue<FlavorType>(v?["build"]?["flavor"]?.ToString() ?? string.Empty), 
                    Utilities.GetEnumValue<ResourceType>(v?["build"]?["type"]?.ToString() ?? string.Empty)),
                platform));
            
            MobileTestPlatforms.Add(platform, new Platform(
                new Agent(v?["run"]?["image"]?.ToString() ?? string.Empty, 
                    Utilities.GetEnumValue<FlavorType>(v?["run"]?["flavor"]?.ToString() ?? string.Empty), 
                    Utilities.GetEnumValue<ResourceType>(v?["run"]?["type"]?.ToString() ?? string.Empty),
                    v?["run"]?["model"]?.ToString()),
                platform));
        }
    }
}
