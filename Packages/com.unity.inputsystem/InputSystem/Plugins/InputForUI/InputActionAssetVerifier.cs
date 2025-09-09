#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM && UNITY_2023_2_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Plugins.InputForUI
{
    // Unlike InputSystemProvider we want the verifier to register itself directly on domain reload in editor.
    [InitializeOnLoad]
    internal class InputActionAssetVerifier : ProjectWideActionsAsset.IInputActionAssetVerifier
    {
        public enum ReportPolicy
        {
            ReportAll,
            SuppressChildErrors
        }

        // Note: This is intentionally not a constant to avoid dead code warning in tests while this remains
        // as a setting type of value.
        public static ReportPolicy DefaultReportPolicy = ReportPolicy.SuppressChildErrors;

        static InputActionAssetVerifier()
        {
            // Register an InputActionAsset verifier for this plugin.
            ProjectWideActionsAsset.RegisterInputActionAssetVerifier(() => new InputActionAssetVerifier());

            InputSystemProvider.SetOnRegisterActions((asset) => { ProjectWideActionsAsset.Verify(asset); });
        }

        #region ProjectWideActionsAsset.IInputActionAssetVerifier

        public void Verify(InputActionAsset asset,
            ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter)
        {
            // We don't want to log warnings if no UI action map is present as we default in this case.
            if (asset.FindActionMap("UI", false) == null)
            {
                return;
            }

            // Note:
            // PWA has initial state check true for "Point" action, DefaultActions do not, does it matter?
            //
            // Additionally note that "Submit" and "Cancel" are indirectly expected to be of Button action type.
            // This is not available in UI configuration, but InputActionRebindingExtensions suggests this.
            //
            // Additional "LeftClick" has initial state check set in PWA, but not "MiddleClick" and "RightClick".
            // Is this intentional? Are requirements different?
            var context = new Context(asset, reporter, DefaultReportPolicy);
            context.Verify(actionNameOrId: InputSystemProvider.Actions.PointAction, actionType: InputActionType.PassThrough, expectedControlType: nameof(Vector2));
            context.Verify(actionNameOrId: InputSystemProvider.Actions.MoveAction, actionType: InputActionType.PassThrough, expectedControlType: nameof(Vector2));
            context.Verify(actionNameOrId: InputSystemProvider.Actions.SubmitAction, actionType: InputActionType.Button, expectedControlType: "Button");
            context.Verify(actionNameOrId: InputSystemProvider.Actions.CancelAction, actionType: InputActionType.Button, expectedControlType: "Button");
            context.Verify(actionNameOrId: InputSystemProvider.Actions.LeftClickAction, actionType: InputActionType.PassThrough, expectedControlType: "Button");
            context.Verify(actionNameOrId: InputSystemProvider.Actions.MiddleClickAction, actionType: InputActionType.PassThrough, expectedControlType: "Button");
            context.Verify(actionNameOrId: InputSystemProvider.Actions.RightClickAction, actionType: InputActionType.PassThrough, expectedControlType: "Button");
            context.Verify(actionNameOrId: InputSystemProvider.Actions.ScrollWheelAction, actionType: InputActionType.PassThrough, expectedControlType: nameof(Vector2));
        }

        #endregion

        private struct Context
        {
            const string errorSuffix = "The Input System's runtime UI integration relies on certain required input action definitions, some of which are missing. This means some runtime UI input may not work correctly. See <a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/UISupport.html#required-actions-for-ui\">Input System Manual - UI Support</a> for guidance on required actions for UI integration or see <a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/ProjectWideActions.html#the-default-actions\">how to revert to defaults</a>.";

            public Context(InputActionAsset asset,
                           ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter,
                           ReportPolicy policy)
            {
                this.asset = asset;
                this.missingPaths = new HashSet<string>();
                this.reporter = reporter;
                this.policy = policy;
            }

            private string GetAssetReference()
            {
                var path = AssetDatabase.GetAssetPath(asset);
                return string.IsNullOrEmpty(path) ? asset.name : path;
            }

            private void ActionMapWarning(string actionMap, string problem)
            {
                reporter.Report($"InputActionMap with path '{actionMap}' in asset \"{GetAssetReference()}\" {problem}. {errorSuffix}");
            }

            private void ActionWarning(string actionNameOrId, string problem)
            {
                reporter.Report($"InputAction with path '{actionNameOrId}' in asset \"{GetAssetReference()}\" {problem}. {errorSuffix}");
            }

            public void Verify(string actionNameOrId, InputActionType actionType, string expectedControlType)
            {
                var action = asset.FindAction(actionNameOrId);
                if (action == null)
                {
                    const string kCouldNotBeFound = "could not be found";

                    // Check if the map (if any) exists
                    var noMapOrMapExists = true;
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
                            noMapOrMapExists = false;
                        }
                    }

                    if (!noMapOrMapExists && policy == ReportPolicy.SuppressChildErrors)
                        return;

                    ActionWarning(actionNameOrId, kCouldNotBeFound);
                }
                else if (action.bindings.Count == 0)
                    ActionWarning(actionNameOrId, "do not have any configured bindings");
                else if (action.type != actionType)
                    ActionWarning(actionNameOrId, $"has 'type' set to '{nameof(InputActionType)}.{action.type}', but '{nameof(InputActionType)}.{actionType}' was expected");
                else if (!string.IsNullOrEmpty(expectedControlType) && !string.IsNullOrEmpty(action.expectedControlType) && action.expectedControlType != expectedControlType)
                    ActionWarning(actionNameOrId, $"has 'expectedControlType' set to '{action.expectedControlType}', but '{expectedControlType}' was expected");
            }

            private readonly InputActionAsset asset;
            private readonly ProjectWideActionsAsset.IReportInputActionAssetVerificationErrors reporter;

            private HashSet<string> missingPaths; // Avoids generating multiple warnings around missing map
            private ReportPolicy policy;
        }
    }
}

#endif // UNITY_EDITOR && ENABLE_INPUT_SYSTEM && UNITY_2023_2_OR_NEWER
