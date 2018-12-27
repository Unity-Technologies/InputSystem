using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.TestTools;
using UnityEditorInternal;
using UnityEngine;

using ieu = UnityEditorInternal.InternalEditorUtility;

public class ScriptCompilersTests
{
#if UNITY_2018_3_OR_NEWER && (UNITY_EDITOR_OSX || UNITY_EDITOR_WIN)

    [Test]
    public void InputSystemSourceCodeCompilesWithoutWarnings()
    {
        var messages = CompileCSharp();

        Assert.That(messages, Has.None.Matches<CompilerMessage>(x => x.type == CompilerMessageType.Warning),
            CompilerMessagesToString(messages));
    }

    [Test]
    public void InputSystemSourceCodeCompilesWithoutErrors()
    {
        var messages = CompileCSharp();
        Assert.That(messages, Has.None.Matches<CompilerMessage>(x => x.type == CompilerMessageType.Error),
            CompilerMessagesToString(messages));
    }

    private static string CompilerMessagesToString(CompilerMessage[] messages)
    {
        var builder = new StringBuilder();

        foreach (var message in messages)
        {
            var indexOfFirstColon = message.message.IndexOf(':');
            var indexOfSecondColon = message.message.IndexOf(':', indexOfFirstColon + 1);
            var messageTextWithoutFilenameLineAndMessageNumber = message.message.Substring(indexOfSecondColon + 2); // Skip space, too.
            builder.Append(string.Format("{0}:{1}: {2}", Path.GetFileNameWithoutExtension(message.file), message.line,
                messageTextWithoutFilenameLineAndMessageNumber));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static CompilerMessage[] CompileCSharp()
    {
        var supportedLanguage = (SupportedLanguage)Activator.CreateInstance(typeof(CSharpLanguage));
        var island = CreateMonoIsland(supportedLanguage);

        using (var compiler = new MonoCSharpCompiler(island, false))
        {
            return Compile(compiler, island);
        }
    }

    private static MonoIsland CreateMonoIsland(SupportedLanguage language)
    {
        const string kInputFilePath = "Packages/com.unity.inputsystem/InputSystem";
        var outputAssemblyPath = Path.GetTempFileName();

        var options = EditorScriptCompilationOptions.BuildingForEditor;
        var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = UnityEditor.EditorUserBuildSettings.activeBuildTargetGroup;
        var defines = ieu.GetCompilationDefines(options, buildTargetGroup, buildTarget);

        var references = new List<string>();
        references.Add(ieu.GetEngineAssemblyPath());
        references.Add(ieu.GetEngineCoreModuleAssemblyPath());
        references.Add(ieu.GetEditorAssemblyPath());
        references.AddRange(ModuleUtils.GetAdditionalReferencesForUserScripts());
#if UNITY_EDITOR_OSX
        references.Add(Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll"));
#elif UNITY_EDITOR_WIN
        references.Add(Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data/UnityExtensions/Unity/GUISystem/UnityEngine.UI.dll"));
#endif
        var unityAssemblies = InternalEditorUtility.GetUnityAssemblies(true, buildTargetGroup, buildTarget);
        foreach (var asm in unityAssemblies)
        {
            references.Add(asm.Path);
        }

        var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.activeBuildTargetGroup);

        // Hopefully the churn on these mono library helpers is over, this is going to be a bit a pain to
        // always chase.
#if UNITY_2018_3_OR_NEWER && !(UNITY_2019_1_OR_NEWER)
        var scriptAssembly = new ScriptAssembly
        {
            Filename = AssetPath.GetFileName(outputAssemblyPath),
            Flags = AssemblyFlags.None
        };
        references.AddRange(MonoLibraryHelpers.GetSystemLibraryReferences(apiCompatibilityLevel, buildTarget, language, true, scriptAssembly));
#elif UNITY_2019_1_OR_NEWER
        references.AddRange(MonoLibraryHelpers.GetSystemLibraryReferences(apiCompatibilityLevel, buildTarget, language));
#endif

        var sources = new List<string>();
        sources.AddRange(Directory.GetFiles(kInputFilePath, "*.cs", SearchOption.AllDirectories));

        var island = new MonoIsland(buildTarget, apiCompatibilityLevel, true, sources.ToArray(),
            references.ToArray(), defines, outputAssemblyPath);

        return island;
    }

    private static CompilerMessage[] Compile(ScriptCompilerBase compiler, MonoIsland island)
    {
        var assemblyOutputPath = island._output;

        compiler.BeginCompiling();
        compiler.WaitForCompilationToFinish();

        Assert.IsTrue(compiler.Poll(), "Compilation is not finished");

        var messages = compiler.GetCompilerMessages();

        if (messages.Count(m => m.type == CompilerMessageType.Error) == 0)
            Assert.True(File.Exists(assemblyOutputPath), "Output assembly does not exist after successful compile");

        File.Delete(assemblyOutputPath);

        return messages;
    }

#endif
}
