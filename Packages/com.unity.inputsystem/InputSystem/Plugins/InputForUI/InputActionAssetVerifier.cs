#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Plugins.InputForUI
{
    internal class InputActionAssetVerifier : ProjectWideActionsAsset.IInputActionAssetVerifier
    {
        static InputActionAssetVerifier()
        {
            // Register an InputActionAsset verifier for this plugin.
            ProjectWideActionsAsset.RegisterInputActionAssetVerifier(() => new InputActionAssetVerifier());
        }

        [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Bootstrap() {} // Empty function. Exists only to invoke the static class constructor in Runtime Players

        private static void VerifyAction(InputActionAsset asset, string actionNameOrId,
            InputActionType actionType, string expectedControlType, ref HashSet<string> missingPaths,
            ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter)
        {
            var action = asset.FindAction(actionNameOrId);

            string GetAssetReference()
            {
                var path = AssetDatabase.GetAssetPath(asset);
                return (path == null)
                    ? '"' + asset.name + '"'
                    : "<a href=\"" + path + $">{path}</a>";
            }

            const string kErrorSuffix = "Run-time UI interactivity (input) may not work as expected. See <a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/UISupport.html\">Input System Manual - UI Support</a> for guidance on required actions for UI integration or see <a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/ProjectWideActions.html#the-default-actions\">how to revert to defaults</a>.";
            void ActionMapWarning(string actionMap, string problem)
            {
                reporter.Report($"InputActionMap with path '{actionMap}' in asset '{GetAssetReference()}' {problem}. {kErrorSuffix}");
            }

            void ActionWarning(string actionNameOrId, string problem)
            {
                reporter.Report($"InputAction with path '{actionNameOrId}' in asset '{GetAssetReference()}' {problem}. {kErrorSuffix}");
            }

            if (action == null)
            {
                const string kCouldNotBeFound = "could not be found";
                
                // Check if the map (if any) exists
                var index = actionNameOrId.IndexOf('/');
                if (index > 0)
                {
                    var path = actionNameOrId.Substring(0, index);
                    if (asset.FindActionMap(path) == null)
                    {
                        if (missingPaths == null)
                            missingPaths = new HashSet<string>(1);
                        if (missingPaths.Add(path))
                            ActionMapWarning(path, kCouldNotBeFound);
                    }
                }

                ActionWarning(actionNameOrId, kCouldNotBeFound);
            }
            else if (action.bindings.Count == 0)
                ActionWarning(actionNameOrId, "do not have any configured bindings");
            else if (action.type != actionType)
                ActionWarning(actionNameOrId, $" has 'Action Type' {action.type}, but {actionType} was expected");
            else if (expectedControlType != string.Empty && action.expectedControlType != expectedControlType)
                ActionWarning(actionNameOrId, $" has 'Expected Control Type' '{action.expectedControlType}', but '{expectedControlType}' was expected");
        }

        #region ProjectWideActionsAsset.IInputActionAssetVerifier

        public void Verify(InputActionAsset asset,
            ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter)
        {
            // Note that we never cache this to guarantee we have the current configuration.
            var config = InputSystemProvider.Configuration.GetDefaultConfiguration();
            Verify(asset, ref config, reporter);
        }

        #endregion

        internal static void Verify(InputActionAsset asset, ref InputSystemProvider.Configuration config,
            ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter)
        {
            // Temporary set used to track missing paths to avoid multiple similar warnings.
            // Initialize to null to avoid allocation completely if no errors are found.
            HashSet<string> missingPaths = null;

            VerifyAction(
                asset: asset,
                actionNameOrId: config.PointAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: nameof(Vector2),
                missingPaths: ref missingPaths,
                reporter: reporter); // initial state check true in PWA, false in DefaultActions, does it matter?
            VerifyAction(
                asset: asset,
                actionNameOrId: config.MoveAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: nameof(Vector2),
                missingPaths: ref missingPaths,
                reporter: reporter);
            VerifyAction(
                asset: asset,
                actionNameOrId: config.SubmitAction,
                actionType: InputActionType.Button,
                expectedControlType: string.Empty,
                missingPaths: ref missingPaths,
                reporter: reporter);
            VerifyAction(
                asset: asset,
                actionNameOrId: config.CancelAction,
                actionType: InputActionType.Button,
                expectedControlType: string.Empty,
                missingPaths: ref missingPaths,
                reporter: reporter);
            VerifyAction(
                asset: asset,
                actionNameOrId: config.LeftClickAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: "Button",
                missingPaths: ref missingPaths,
                reporter: reporter); // initial state check true in PWA, false in DefaultActions, does it matter?
            VerifyAction(
                asset: asset,
                actionNameOrId: config.MiddleClickAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: "Button",
                missingPaths: ref missingPaths,
                reporter: reporter);
            VerifyAction(
                asset: asset,
                actionNameOrId: config.RightClickAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: "Button",
                missingPaths: ref missingPaths,
                reporter: reporter);
            VerifyAction(
                asset: asset,
                actionNameOrId: config.ScrollWheelAction,
                actionType: InputActionType.PassThrough,
                expectedControlType: nameof(Vector2),
                missingPaths: ref missingPaths,
                reporter: reporter);
        }
    }
}

#endif // UNITY_EDITOR && ENABLE_INPUT_SYSTEM
