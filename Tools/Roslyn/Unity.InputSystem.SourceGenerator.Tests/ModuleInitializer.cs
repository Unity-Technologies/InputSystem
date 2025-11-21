namespace Unity.InputSystem.SourceGenerator.Tests;

internal static class ModuleInitializer
{
    // Verify requires initialization. This is the recommended way.
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
