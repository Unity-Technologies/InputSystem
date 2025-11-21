# README - Unity.InputSystem.SourceGenerator

## Overview

This directory contains source generators for the Unity Input System and associated automated tests.

The source generator solution supports the following scenarios:
- Source generation for automatic type registration of custom interaction types implementing 
  [`UnityEngine.InputSystem.IInputInteraction`](../../Packages/com.unity.inputsystem/InputSystem/Actions/IInputInteraction.cs). 
- Source generation for automatic type registration of custom processors derived from 
  [`UnityEngine.InputSystem.InputProcessor`](../../Packages/com.unity.inputsystem/InputSystem/Controls/InputProcessor.cs).
- Source generation for automatic type registration of custom composite bindings derived from
  [`UnityEngine.InputSystem.InputBinding`](../../Packages/com.unity.inputsystem/InputSystem/Actions/InputBinding.cs).

This allows generating required registration boilerplate code at compile-time instead of writing manual registration code.
In addition, it eliminates the need to rely on slow and memory consuming run-time operations using 
[.NET Reflection API](https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/reflection).

## How to build and run tests

The simplest way to build the source generators are via the provided convenience scripts (internally using standard `dotnet` commands):

To build using macOS or *nix to build both `Debug` and `Release` targets:
```
./build.sh
```
To build using Windows Command line prompt to build both `Debug` and `Release` targets:
```
build
```

To run tests and generate a test coverage report using macOS or *nix, use:
```
./test.sh
```
To run tests and generate a test coverage report using Windows, use:
```
test
```

To have more control over the build or to build individual targets, inspect the above mentioned scripts which 
illustrate what `dotnet` commands are used. Consult [.NET CLI tools documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet) 
for additional options.

When building the source generator, binaries are located under `./bin/<Configuration>`. For `Release` builds, the 
resulting binary is also automatically copied into the designated folder location of the Input System package.

Note that you may have to install `.NET Runtime` or add it to path if test run fails with error and prompts you that `Microsoft.NETCore.App` is outdated or missing.

## Dependencies

The source generators themselves do not have any other dependencies than `.NET SDK` (including CLI tooling).

Test packages have additional dependencies as follows:
- `Microsoft.NET.Test.Sdk` - Microsoft .NET test support.
- `NUnit` - NUnit testing framework.
- `NUnit.Analyzers` - Analyzer NUnit support.
- `NUnit3TestAdapter` - Adapter for running NUnit tests.
- `Microsoft.CodeAnalysis.CSharp` - Roslyn support.
- `Verify` - Diff-based verification tool that simplifies source generator testing by reviewing and accepting diffs as part of the development workflow.
- `Verify.NUnit` - NUnit adapter for `Verify` NuGet package.
- `Verify.SourceGenerators` - Source generator adapter for `Verify` NuGet package.

All dependencies are managed via NuGet.

## Distribution

The resulting source generator binary is automatically copied into the Input System package. Note that the binary may
generate a diff even when nothing has changed due to timestamps within binary or due to compiler version differences.
It is recommended that the source generator binary is only commited as part of pull requests actually modifying 
`Tools~/Roslyn/*`.
