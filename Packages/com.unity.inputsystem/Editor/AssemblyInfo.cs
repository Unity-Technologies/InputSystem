using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;

[assembly: AssemblyVersion(InputSystem.kAssemblyVersion)]
[assembly: InternalsVisibleTo("Unity.InputSystem.TestFramework")]
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")]
[assembly: InternalsVisibleTo("Unity.InputSystem.IntegrationTests")]
[assembly: InternalsVisibleTo("Unity.InputSystem.ForUI")] // To avoid minor bump

