using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;

[assembly: AssemblyVersion(InputSystem.kAssemblyVersion)]
[assembly: InternalsVisibleTo("UnityEngine.Input.TestFramework")]
[assembly: InternalsVisibleTo("UnityEngine.Input.Tests.Editor")]
[assembly: InternalsVisibleTo("UnityEngine.Input.Tests")]
[assembly: InternalsVisibleTo("UnityEngine.Input.IntegrationTests")]

namespace UnityEngine.InputSystem
{
    public static partial class InputSystem
    {
        // Keep this in sync with "Packages/com.unity.input/package.json".
        // NOTE: Unfortunately, System.Version doesn't use semantic versioning so we can't include
        //       "-preview" suffixes here.
        internal const string kAssemblyVersion = "2.0.0";
        internal const string kDocUrl = "https://docs.unity3d.com/Packages/com.unity.input@2.0";
    }
}
