#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM && UNITY_2023_2_OR_NEWER
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.Plugins.InputForUI
{
    /// <summary>
    /// Edit-mode only class managing requirements imposed by the <c>InputSystemProvider</c> implementation.
    /// <remarks>
    /// Note that the following procedure is required when actions supported by &lt;c&gt;InputSystemProvider&lt;/c&gt;:
    /// 1. Update the &lt;c&gt;InputSystemProvider.Configuration&lt;c/&gt; struct to include the required action path.
    /// 2. Make sure the new action is registered in &lt;c&gt;InputSystemProvider.RegisterActions()&lt;/c&gt;
    ///    and &lt;c&gt;InputSystemProvider.UnregisterActions()&lt;/c&gt;.
    /// 3. Add missing logic to &lt;c&gt;InputSystemProvider&lt;/c&gt; to handle input related to the added action.
    /// 4. Update the requirements in &lt;c&gt;RegisterRequirements(...)&lt;/c&gt; function of this class to reflect the
    ///    requirements of the consuming code related to this plugin.
    /// 5. Update test cases in &lt;c&gt;InputForUITests&lt;/c&gt; to include to added action.
    /// </remarks>
    /// </summary>
    [InitializeOnLoad]
    internal static class InputSystemProviderRequirements
    {
        // Provides a user-facing message explaining implication of failed requirements for this integration plugin.
        private const string kImplicationOfFailedRequirements =
            "Run-time UI interactivity (input) may not work as expected. See " +
            "<a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/UISupport.html\">Input System Manual - UI Support</a>" +
            " for guidance on required actions for UI integration or see " +
            "<a href=\"https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html?subfolder=/manual/ProjectWideActions.html#the-default-actions\">UI Support</a> for information on how to revert to defaults.";

        // Holds the current applied requirements for this integration plugin.
        private static InputActionAssetRequirements s_Requirements;

        // Static constructor handling initial registration of requirements but also revision when configuration changes.
        // Note that [InitializeOnLoad] drives the invocation of this constructor.
        static InputSystemProviderRequirements()
        {
            InputSystemProvider.SetOnConfigurationChanged((InputSystemProvider.ConfigurationState state,
                ref InputSystemProvider.Configuration config) =>
                {
                    switch (state)
                    {
                        case InputSystemProvider.ConfigurationState.Register:
                            // Make sure any initial registered requirements are removed and then register new requirements.
                            if (s_Requirements != null)
                                UnregisterRequirements();
                            RegisterRequirements(ref config);
                            break;

                        case InputSystemProvider.ConfigurationState.Unregister:
                            // Unregister requirements
                            UnregisterRequirements();
                            break;
                    }
                });

            // Register initial requirements for this plugin.
            var cfg = InputSystemProvider.Configuration.GetDefaultConfiguration();
            RegisterRequirements(ref cfg);
        }

        private static void RegisterRequirements(ref InputSystemProvider.Configuration cfg)
        {
            // Register action requirements to allow the system to perform verification and user-feedback
            const string kOwner = "UI Toolkit Input System Integration";
            s_Requirements = new InputActionAssetRequirements(kOwner,
                new InputActionRequirement[]
                {
                    new InputActionRequirement(cfg.PointAction, actionType: InputActionType.PassThrough,
                        expectedControlType: nameof(Vector2), kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.MoveAction, actionType: InputActionType.PassThrough,
                        expectedControlType: nameof(Vector2), kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.SubmitAction, actionType: InputActionType.Button,
                        expectedControlType: "Button", kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.CancelAction, actionType: InputActionType.Button,
                        expectedControlType: "Button", kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.LeftClickAction, actionType: InputActionType.PassThrough,
                        expectedControlType: "Button", kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.MiddleClickAction, actionType: InputActionType.PassThrough,
                        expectedControlType: "Button", kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.RightClickAction, actionType: InputActionType.PassThrough,
                        expectedControlType: "Button", kImplicationOfFailedRequirements),
                    new InputActionRequirement(cfg.ScrollWheelAction, actionType: InputActionType.PassThrough,
                        expectedControlType: nameof(Vector2), kImplicationOfFailedRequirements)
                }, kImplicationOfFailedRequirements);

            // Register requirements driven by this implementation to enable system to perform verification
            InputActionAssetRequirements.Register(s_Requirements);

            // Explicitly verify requirements for asset and output warnings to console since it may not be
            // the project-wide input action asset but rather another asset.
            InputActionAssetRequirements.Verify(cfg.ActionAsset, new LoggingInputActionAssetRequirementFailureReporter());
        }

        private static void UnregisterRequirements()
        {
            // Unregister requirements driven by this implementation to disable system from further verification
            InputActionAssetRequirements.Unregister(s_Requirements);
            s_Requirements = null;
        }
    }
}

#endif // UNITY_EDITOR && ENABLE_INPUT_SYSTEM && UNITY_2023_2_OR_NEWER
