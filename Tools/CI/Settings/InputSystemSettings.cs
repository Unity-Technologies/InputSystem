using InputSystem.Cookbook.Recipes;
using System.Text.Json;
using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Platforms;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Platforms;
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
                ValidationOptions = new ValidationOptions()
                {
                    AdditionalUtrArguments = ["--coverage-pkg-version=1.3.0"]
                },
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

        // Ignore packages listed below in PreviewAPV because they cause instability in the editor
        // We don't want to block our development on fixing those as they are not related to our package.
        // We can remove them from this list once the issues are fixed on their end.
        InputSystemPackage.DependantsToIgnoreInPreviewApv = new Dictionary<Editor, ISet<string>>()
        {
            {
                new Editor("6000.5",  ""),
                new HashSet<string>()
                {
                    "com.unity.charactercontroller"
                }
            },
            {
                new Editor("6000.6",  ""),
                new HashSet<string>()
                {
                    "com.unity.charactercontroller"
                }
            }
        };

        InputSystemPackage.CoverageCommands.Enabled = true;

        Wrench.PvpProfilesToCheck = new HashSet<string>() { "supported" };

        OverridePackagePlatform(InputSystemPackage);

        foreach ((string name, WrenchPackage package) in Wrench.Packages)
        {
            Wrench.Packages[name].EditorPlatforms[SystemType.MacOS] = new Platform(new Agent("package-ci/macos-13-arm64:v4", FlavorType.MacDefault, ResourceType.VmOsx, "M1"), SystemType.MacOS);
        }

        ReadMobileConfig();

        var oldIOSAgent = MobileTestPlatforms[SystemType.IOS].Agent;
        iOS15Platform = new Platform(new Agent(oldIOSAgent.Image, oldIOSAgent.Flavor, oldIOSAgent.Resource, "SE-Gen3"), SystemType.IOS);
    }

    // Default FlavorType was changed for Win & Mac in Wrench 2.0.
    // Overriding this to keep them the same esp. for performance jobs.
    // Ubuntu2204 is overridden to use a GPU VM to avoid software rendering slowdowns
    // when running standalone player tests.
    private void OverridePackagePlatform(WrenchPackage package)
    {
        foreach (UnityEditor unityEditor in package.UnityEditors)
        {
            unityEditor.EditorPlatforms.Items[EditorPlatformType.Win10]
                = new EditorPlatform(EditorPlatformType.Win10,
                new Agent("package-ci/win10:v4", FlavorType.BuildLarge, ResourceType.Vm));

            // Unity 6.6 defaults to MacOs13Arm in Wrench 2.12.0 — don't override it back to Intel.
            if (unityEditor.Version.Version != "6000.6")
            {
                unityEditor.EditorPlatforms.Items[EditorPlatformType.MacOs13]
                    = new EditorPlatform(EditorPlatformType.MacOs13,
                    new Agent("package-ci/macos-13:v4", FlavorType.BuildExtraLarge, ResourceType.VmOsx));
            }

            unityEditor.EditorPlatforms.Items[EditorPlatformType.Ubuntu2204]
                = new EditorPlatform(EditorPlatformType.Ubuntu2204,
                new Agent("package-ci/ubuntu-22.04:v4", FlavorType.BuildLarge, ResourceType.VmGpu));
        }
    }

    public WrenchSettings Wrench { get; private set; }

    void ReadMobileConfig()
    {
        if (!File.Exists(mobileConfigFilePath))
            throw new FileNotFoundException("Mobile config file could not be found.");

        var configs = File.ReadAllText(mobileConfigFilePath);
        using var jsonDocument = JsonDocument.Parse(configs);
        var jsonObject = jsonDocument.RootElement;

        foreach (var property in jsonObject.EnumerateObject())
        {
            var k = property.Name;
            var v = property.Value;

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
                new Agent(
                    v.TryGetProperty("build", out var build) && build.TryGetProperty("image", out var buildImage) ? buildImage.GetString() ?? string.Empty : string.Empty,
                    Utilities.GetEnumValue<FlavorType>(v.TryGetProperty("build", out build) && build.TryGetProperty("flavor", out var buildFlavor) ? buildFlavor.GetString() ?? string.Empty : string.Empty),
                    Utilities.GetEnumValue<ResourceType>(v.TryGetProperty("build", out build) && build.TryGetProperty("type", out var buildType) ? buildType.GetString() ?? string.Empty : string.Empty)),
                platform));

            MobileTestPlatforms.Add(platform, new Platform(
                new Agent(
                    v.TryGetProperty("run", out var run) && run.TryGetProperty("image", out var runImage) ? runImage.GetString() ?? string.Empty : string.Empty,
                    Utilities.GetEnumValue<FlavorType>(v.TryGetProperty("run", out run) && run.TryGetProperty("flavor", out var runFlavor) ? runFlavor.GetString() ?? string.Empty : string.Empty),
                    Utilities.GetEnumValue<ResourceType>(v.TryGetProperty("run", out run) && run.TryGetProperty("type", out var runType) ? runType.GetString() ?? string.Empty : string.Empty),
                    v.TryGetProperty("run", out run) && run.TryGetProperty("model", out var runModel) ? runModel.GetString() : null),
                platform));
        }
    }
}
