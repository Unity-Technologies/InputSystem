#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ProjectWideActionsAsset
    {
        private const string kDefaultAssetName = "InputSystem_Actions";
        private const string kDefaultAssetPath = "Assets/" + kDefaultAssetName + ".inputactions";
        private const string kDefaultTemplateAssetPath = "Packages/com.unity.inputsystem/InputSystem/Editor/ProjectWideActions/ProjectWideActionsTemplate.json";

        internal static class ProjectSettingsProjectWideActionsAssetConverter
        {
            private const string kAssetPathInputManager = "ProjectSettings/InputManager.asset";
            private const string kAssetNameProjectWideInputActions = "ProjectWideInputActions";

            class ProjectSettingsPostprocessor : AssetPostprocessor
            {
                private static bool migratedInputActionAssets = false;

#if UNITY_2021_2_OR_NEWER
                private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
                private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#endif
                {
                    if (!migratedInputActionAssets)
                    {
                        MoveInputManagerAssetActionsToProjectWideInputActionAsset();
                        migratedInputActionAssets = true;
                    }

                    if (!Application.isPlaying)
                    {
                        // If the Library folder is deleted, InputSystem will fail to retrieve the assigned Project-wide Asset because this look-up occurs
                        // during initialization while the Library is being rebuilt. So, afterwards perform another check and assign PWA asset if needed.
                        var pwaAsset = ProjectWideActionsBuildProvider.actionsToIncludeInPlayerBuild;
                        if (InputSystem.actions == null && pwaAsset != null)
                            InputSystem.actions = pwaAsset;
                    }
                }
            }

            private static void MoveInputManagerAssetActionsToProjectWideInputActionAsset()
            {
                var objects = AssetDatabase.LoadAllAssetsAtPath(EditorHelpers.GetPhysicalPath(kAssetPathInputManager));
                if (objects == null)
                    return;

                var inputActionsAsset = objects.FirstOrDefault(o => o != null && o.name == kAssetNameProjectWideInputActions) as InputActionAsset;
                if (inputActionsAsset != default)
                {
                    // Found some actions in the InputManager.asset file
                    //
                    string path = ProjectWideActionsAsset.kDefaultAssetPath;

                    if (File.Exists(EditorHelpers.GetPhysicalPath(path)))
                    {
                        // We already have a path containing inputactions, find a new unique filename
                        //
                        //  eg  Assets/InputSystem_Actions.inputactions ->
                        //      Assets/InputSystem_Actions (1).inputactions ->
                        //      Assets/InputSystem_Actions (2).inputactions ...
                        //
                        string[] files = Directory.GetFiles("Assets", "*.inputactions");
                        List<string> names = new List<string>();
                        for (int i = 0; i < files.Length; i++)
                        {
                            names.Add(System.IO.Path.GetFileNameWithoutExtension(files[i]));
                        }
                        string unique = ObjectNames.GetUniqueName(names.ToArray(), kDefaultAssetName);
                        path = "Assets/" + unique + ".inputactions";
                    }

                    var json = inputActionsAsset.ToJson();
                    InputActionAssetManager.SaveAsset(EditorHelpers.GetPhysicalPath(path), json);

                    Debug.Log($"Migrated Project-wide Input Actions from '{kAssetPathInputManager}' to '{path}' asset");

                    // Update current project-wide settings if needed (don't replace if already set to something else)
                    //
                    if (InputSystem.actions == null || InputSystem.actions.name == kAssetNameProjectWideInputActions)
                    {
                        InputSystem.actions = (InputActionAsset)AssetDatabase.LoadAssetAtPath(path, typeof(InputActionAsset));
                        Debug.Log($"Loaded Project-wide Input Actions from '{path}' asset");
                    }
                }

                // Handle deleting all InputActionAssets as older 1.8.0 pre release could create more than one project wide input asset in the file
                foreach (var obj in objects)
                {
                    if (obj is InputActionReference)
                    {
                        var actionReference = obj as InputActionReference;
                        AssetDatabase.RemoveObjectFromAsset(obj);
                        Object.DestroyImmediate(actionReference);
                    }
                    else if (obj is InputActionAsset)
                    {
                        AssetDatabase.RemoveObjectFromAsset(obj);
                    }
                }

                AssetDatabase.SaveAssets();
            }
        }

        // Returns the default asset path for where to create project-wide actions asset.
        internal static string defaultAssetPath => kDefaultAssetPath;

        // Returns the default template JSON content.
        internal static string GetDefaultAssetJson()
        {
            return File.ReadAllText(EditorHelpers.GetPhysicalPath(kDefaultTemplateAssetPath));
        }

        // Creates an asset at the given path containing the default template JSON.
        internal static InputActionAsset CreateDefaultAssetAtPath(string assetPath = kDefaultAssetPath)
        {
            return CreateAssetAtPathFromJson(assetPath, File.ReadAllText(EditorHelpers.GetPhysicalPath(kDefaultTemplateAssetPath)));
        }

        // These may be moved out to internal types if decided to extend validation at a later point.

        /// <summary>
        /// Interface for reporting asset verification errors.
        /// </summary>
        internal interface IReportInputActionAssetVerificationErrors
        {
            /// <summary>
            /// Reports a failure to comply to requirements with a message meaningful to the user.
            /// </summary>
            /// <param name="message">User-friendly error message.</param>
            void Report(string message);
        }

        /// <summary>
        /// Interface for asset verification.
        /// </summary>
        internal interface IInputActionAssetVerifier
        {
            /// <summary>
            /// Verifies the given asset.
            /// </summary>
            /// <param name="asset">The asset to be verified</param>
            /// <param name="reporter">The reporter to be used to report failure to meet requirements.</param>
            public void Verify(InputActionAsset asset, IReportInputActionAssetVerificationErrors reporter);
        }

        /// <summary>
        /// Verifier managing verification and reporting of asset compliance with external requirements.
        /// </summary>
        class Verifier : IReportInputActionAssetVerificationErrors
        {
            private readonly IReportInputActionAssetVerificationErrors m_Reporter;

            // Default verification error reporter which generates feedback as debug warnings.
            private class DefaultInputActionAssetVerificationReporter : IReportInputActionAssetVerificationErrors
            {
                public void Report(string message)
                {
                    Debug.LogWarning(message);
                }
            }

            /// <summary>
            /// Constructs a an instance associated with the given reporter.
            /// </summary>
            /// <param name="reporter">The associated reporter instance. If null, a default reporter will be constructed.</param>
            public Verifier(IReportInputActionAssetVerificationErrors reporter = null)
            {
                m_Reporter = reporter ?? new DefaultInputActionAssetVerificationReporter();
                errors = 0;
            }

            #region IReportInputActionAssetVerificationErrors interface

            /// <inheritdoc cref="IReportInputActionAssetVerificationErrors"/>
            public void Report(string message)
            {
                ++errors;

                try
                {
                    m_Reporter.Report(message);
                }
                catch (Exception e)
                {
                    // Only log unexpected but non-fatal exception
                    Debug.LogException(e);
                }
            }

            #endregion

            /// <summary>
            /// Returns the total number of errors seen in verification (accumulative).
            /// </summary>
            public int errors { get; private set; }

            /// <summary>
            /// Returns <c>true</c> if the number of reported errors in verification is zero, else <c>false</c>.
            /// </summary>
            public bool isValid => errors == 0;

            private static List<Func<IInputActionAssetVerifier>> s_VerifierFactories;

            /// <summary>
            /// Registers a factory instance.
            /// </summary>
            /// <param name="factory">The factory instance.</param>
            /// <returns>true if successfully added, <c>false</c> if the factory have already been registered.</returns>
            public static bool RegisterFactory(Func<IInputActionAssetVerifier> factory)
            {
                if (s_VerifierFactories == null)
                    s_VerifierFactories = new List<Func<IInputActionAssetVerifier>>(1);
                if (s_VerifierFactories.Contains(factory))
                    return false;
                s_VerifierFactories.Add(factory);
                return true;
            }

            /// <summary>
            /// Unregisters a factory instance that has previously been registered.
            /// </summary>
            /// <param name="factory">The factory instance to be removed.</param>
            /// <returns>true if successfully unregistered, <c>false</c> if the given factory instance could not be found.</returns>
            public static bool UnregisterFactory(Func<IInputActionAssetVerifier> factory)
            {
                return s_VerifierFactories.Remove(factory);
            }

            /// <summary>
            /// Verifies the given project-wide input action asset using all registered verifiers.
            /// </summary>
            /// <param name="asset">The asset to be verified.</param>
            /// <returns><c>true</c> if no verification errors occurred, else <c>false</c>.</returns>
            /// <remarks>
            /// Throws <c>System.ArgumentNullException</c> if <c>asset</c> is <c>null</c>.
            ///
            /// If any registered factory and/or verifier instance throws an exception this will be evaluated
            /// as a verification error since the execution of the verifier could not continue. However, any
            /// exceptions thrown will be caught and logged but not stop execution of the calling thread.
            /// </remarks>
            bool Verify(InputActionAsset asset)
            {
                if (asset == null)
                    throw new ArgumentNullException(nameof(asset));

                if (s_VerifierFactories == null || s_VerifierFactories.Count == 0)
                    return true;

                var instance = new Verifier(m_Reporter);
                foreach (var factory in s_VerifierFactories)
                {
                    try
                    {
                        factory.Invoke().Verify(asset, instance);
                    }
                    catch (Exception e)
                    {
                        // Only log unexpected but non-fatal exception and count to fail verification
                        ++errors;
                        Debug.LogException(e);
                    }
                }

                return errors == 0;
            }

            /// <summary>
            /// Verifies the given project-wide input action asset using all registered verifiers.
            /// </summary>
            /// <param name="asset">The asset to be verified.</param>
            /// <param name="reporter">The reporter to be used. If this argument is <c>null</c> the default reporter will be used.</param>
            /// <returns><c>true</c> if no verification errors occurred, else <c>false</c>.</returns>
            /// <remarks>Throws <c>System.ArgumentNullException</c> if <c>asset</c> is <c>null</c>.</remarks>
            public static bool Verify(InputActionAsset asset, IReportInputActionAssetVerificationErrors reporter = null)
            {
                return (s_VerifierFactories == null || s_VerifierFactories.Count == 0) || new Verifier(reporter).Verify(asset);
            }
        }

        internal static bool Verify(InputActionAsset asset, IReportInputActionAssetVerificationErrors reporter = null)
        {
            return Verifier.Verify(asset, reporter);
        }

        internal static bool RegisterInputActionAssetVerifier(Func<IInputActionAssetVerifier> factory)
        {
            return Verifier.RegisterFactory(factory);
        }

        internal static bool UnregisterInputActionAssetVerifier(Func<IInputActionAssetVerifier> factory)
        {
            return Verifier.UnregisterFactory(factory);
        }

        // Creates an asset at the given path containing the given JSON content.
        private static InputActionAsset CreateAssetAtPathFromJson(string assetPath, string json)
        {
            // Note that the extra work here is to override the JSON name from the source asset
            var inputActionAsset = InputActionAsset.FromJson(json);
            inputActionAsset.name = InputActionImporter.NameFromAssetPath(assetPath);
            InputActionAssetManager.SaveAsset(assetPath, inputActionAsset.ToJson());
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        }
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
