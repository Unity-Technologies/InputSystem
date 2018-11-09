using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        static CompilerMessage[] CompileCSharp(string code)
        {
            var supportedLanguage = (SupportedLanguage)Activator.CreateInstance(typeof(CSharpLanguage));
            var island = CreateMonoIsland(supportedLanguage);

            using (var compiler = new MonoCSharpCompiler(island, false))
            {
                return Compile(compiler, island, code);
            }
        }

        static MonoIsland CreateMonoIsland(SupportedLanguage language)
        {
            var inputFilePath = Path.GetTempFileName();
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

            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.activeBuildTargetGroup);

            references.AddRange(MonoLibraryHelpers.GetSystemLibraryReferences(apiCompatibilityLevel, buildTarget, language, true, outputAssemblyPath));

            MonoIsland island = new MonoIsland(buildTarget, apiCompatibilityLevel, true, new[] { inputFilePath },
                references.ToArray(), defines, outputAssemblyPath);

            return island;
        }

        static CompilerMessage[] Compile(ScriptCompilerBase compiler, MonoIsland island, string code)
        {
            var inputSourcePath = island._files[0];
            var assemblyOutputPath = island._output;

            File.WriteAllText(inputSourcePath, code);

            compiler.BeginCompiling();
            compiler.WaitForCompilationToFinish();

            Assert.IsTrue(compiler.Poll(), "Compilation is not finished");

            var messages = compiler.GetCompilerMessages();

            if (messages.Count(m => m.type == CompilerMessageType.Error) == 0)
                Assert.True(File.Exists(assemblyOutputPath), "Output assembly does not exist after successful compile");

            File.Delete(inputSourcePath);
            File.Delete(assemblyOutputPath);

            return messages;
        }
}
