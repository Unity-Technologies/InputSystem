#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.VersionControl;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorHelpers
    {
        public static Action<string> SetSystemCopyBufferContents = s => EditorGUIUtility.systemCopyBuffer = s;
        public static Func<string> GetSystemCopyBufferContents = () => EditorGUIUtility.systemCopyBuffer;

        public static void RestartEditorAndRecompileScripts(bool dryRun = false)
        {
            // The APIs here are not public. Use reflection to get to them.

            #if UNITY_2020_2_OR_NEWER

            var editorApplicationType = typeof(EditorApplication);
            var restartEditorAndRecompileScripts =
                editorApplicationType.GetMethod("RestartEditorAndRecompileScripts",
                    BindingFlags.NonPublic | BindingFlags.Static);
            if (!dryRun)
                restartEditorAndRecompileScripts.Invoke(null, null);
            else if (restartEditorAndRecompileScripts == null)
                throw new MissingMethodException(editorApplicationType.FullName, "RestartEditorAndRecompileScripts");

            #else

            // Delete compilation output.
            var editorAssembly = typeof(EditorApplication).Assembly;
            var editorCompilationInterfaceType =
                editorAssembly.GetType("UnityEditor.Scripting.ScriptCompilation.EditorCompilationInterface");
            var editorCompilationInstance = editorCompilationInterfaceType.GetProperty("Instance").GetValue(null);
            var cleanScriptAssembliesMethod = editorCompilationInstance.GetType().GetMethod("CleanScriptAssemblies");
            if (!dryRun)
                cleanScriptAssembliesMethod.Invoke(editorCompilationInstance, null);
            else if (cleanScriptAssembliesMethod == null)
                throw new MissingMethodException(editorCompilationInterfaceType.FullName, "CleanScriptAssemblies");

            // Restart editor.
            var editorApplicationType = typeof(EditorApplication);
            var requestCloseAndRelaunchWithCurrentArgumentsMethod =
                editorApplicationType.GetMethod("RequestCloseAndRelaunchWithCurrentArguments",
                    BindingFlags.NonPublic | BindingFlags.Static);
            if (!dryRun)
                requestCloseAndRelaunchWithCurrentArgumentsMethod.Invoke(null, null);
            else if (requestCloseAndRelaunchWithCurrentArgumentsMethod == null)
                throw new MissingMethodException(editorApplicationType.FullName, "RequestCloseAndRelaunchWithCurrentArguments");

            #endif
        }

        public static void CheckOut(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            // Make path relative to project folder.
            var projectPath = Application.dataPath;
            if (path.StartsWith(projectPath) && path.Length > projectPath.Length &&
                (path[projectPath.Length] == '/' || path[projectPath.Length] == '\\'))
                path = path.Substring(0, projectPath.Length + 1);

            #if UNITY_2019_3_OR_NEWER
            AssetDatabase.MakeEditable(path);
            #else
            if (!Provider.isActive)
                return;
            var asset = Provider.GetAssetByPath(path);
            if (asset == null)
                return;
            var task = Provider.Checkout(asset, CheckoutMode.Asset);
            task.Wait();
            #endif
        }

        public static void CheckOut(Object asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            var path = AssetDatabase.GetAssetPath(asset);
            CheckOut(path);
        }

        // It seems we're getting instabilities on the farm from using EditorGUIUtility.systemCopyBuffer directly in tests.
        // Ideally, we'd have a mocking library to just work around that but well, we don't. So this provides a solution
        // locally to tests.
        public class FakeSystemCopyBuffer : IDisposable
        {
            private string m_Contents;
            private readonly Action<string> m_OldSet;
            private readonly Func<string> m_OldGet;

            public FakeSystemCopyBuffer()
            {
                m_OldGet = GetSystemCopyBufferContents;
                m_OldSet = SetSystemCopyBufferContents;
                SetSystemCopyBufferContents = s => m_Contents = s;
                GetSystemCopyBufferContents = () => m_Contents;
            }

            public void Dispose()
            {
                SetSystemCopyBufferContents = m_OldSet;
                GetSystemCopyBufferContents = m_OldGet;
            }
        }
    }
}
#endif // UNITY_EDITOR
