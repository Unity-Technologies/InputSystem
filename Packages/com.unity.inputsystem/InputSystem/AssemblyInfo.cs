using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;

[assembly: AssemblyVersion(InputSystem.kAssemblyVersion)]
[assembly: InternalsVisibleTo("Unity.InputSystem.TestFramework")]
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")]
[assembly: InternalsVisibleTo("Unity.InputSystem.IntegrationTests")]
[assembly: InternalsVisibleTo("Unity.InputSystem.ForUI")] // To avoid minor bump

namespace UnityEngine.InputSystem
{
    public static partial class InputSystem
    {
        // Keep this in sync with "Packages/com.unity.inputsystem/package.json".
        // NOTE: Unfortunately, System.Version doesn't use semantic versioning so we can't include
        //       "-preview" suffixes here.
        internal const string kAssemblyVersion = "1.9.0";
        internal const string kDocUrl = "https://docs.unity3d.com/Packages/com.unity.inputsystem@1.9";
    }
}
