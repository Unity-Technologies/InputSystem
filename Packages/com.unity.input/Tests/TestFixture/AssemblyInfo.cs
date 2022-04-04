using System.Reflection;
using System.Runtime.CompilerServices;

// Keep this in sync with "Packages/com.unity.inputsystem/package.json".
// NOTE: Unfortunately, System.Version doesn't use semantic versioning so we can't include
//       "-preview" suffixes here.
[assembly: AssemblyVersion("2.0.0")]
[assembly: InternalsVisibleTo("UnityEngine.Input.Tests.Editor")]
[assembly: InternalsVisibleTo("UnityEngine.Input.Tests")]
[assembly: InternalsVisibleTo("UnityEngine.Input.IntegrationTests")]
