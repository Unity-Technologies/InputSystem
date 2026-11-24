using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

/// <summary>
/// Compiles the Player scripts for the active build target under several combinations of
/// development build and scripting defines. Only scripts are compiled, no Player content is built.
/// </summary>
/// <remarks>
/// Test runs only build development Players, which always define DEBUG, so code that compiles only in
/// some combinations can break customer builds without failing any test job. For example,
/// UNITY_INCLUDE_TESTS without DEBUG used to fail with CS0117 on InputSystem.remoteConnection.
///
/// CI runs this with:
///   Unity -batchmode -nographics -projectPath . -executeMethod PlayerCompileCheck.Run -logFile -
/// The Editor exits with code 1 if any combination fails to compile.
/// </remarks>
internal static class PlayerCompileCheck
{
    // Add a define here when users are known to set it in their projects.
    static readonly string[][] k_DefineSets =
    {
        new string[0],
        new[] { "UNITY_INCLUDE_TESTS" },
    };

    const string k_PackageAssembly = "Unity.InputSystem.dll";

    public static void Run()
    {
        // Keep the CI log readable: the stack traces of these log lines carry no information.
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

        var target = EditorUserBuildSettings.activeBuildTarget;
        var group = BuildPipeline.GetBuildTargetGroup(target);

        var failures = new List<string>();
        foreach (var development in new[] { false, true })
        {
            foreach (var defines in k_DefineSets)
            {
                var name = $"{(development ? "Development" : "Release")} [{string.Join(", ", defines)}]";
                if (!Compile(target, group, development, defines, name))
                    failures.Add(name);
            }
        }

        if (failures.Count > 0)
        {
            Debug.LogError($"PlayerCompileCheck: {failures.Count} combination(s) failed to compile for {target}:\n  {string.Join("\n  ", failures)}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"PlayerCompileCheck: all combinations compiled for {target}.");
        EditorApplication.Exit(0);
    }

    static bool Compile(BuildTarget target, BuildTargetGroup group, bool development, string[] defines, string name)
    {
        var errors = new List<string>();
        void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error && message.Contains("error CS"))
                lock (errors)
                    errors.Add(message);
        }

        var settings = new ScriptCompilationSettings
        {
            target = target,
            group = group,
            options = development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None,
            extraScriptingDefines = defines,
        };

        Application.logMessageReceivedThreaded += OnLog;
        ScriptCompilationResult result;
        try
        {
            result = PlayerBuildInterface.CompilePlayerScripts(settings, $"Temp/PlayerCompileCheck/{(development ? "Development" : "Release")}{defines.Length}");
        }
        finally
        {
            Application.logMessageReceivedThreaded -= OnLog;
        }

        var compiledPackage = result.assemblies != null && result.assemblies.Contains(k_PackageAssembly);
        var ok = compiledPackage && errors.Count == 0;

        Debug.Log($"PlayerCompileCheck: {name}: {(ok ? "OK" : "FAILED")}");
        if (!compiledPackage)
            Debug.Log($"PlayerCompileCheck: {name}: {k_PackageAssembly} was not produced.");
        foreach (var error in errors.Distinct())
            Debug.Log($"PlayerCompileCheck: {name}: {error}");

        return ok;
    }
}
