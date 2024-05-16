#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// An interface for reporting <c>InputActionAssetRequirementFailure</c> verification failures.
    /// </summary>
    interface IInputActionAssetRequirementFailureReporter
    {
        /// <summary>
        /// Reports a requirement verification failure.
        /// </summary>
        /// <param name="failure">The failure to be reported. May not be <c>null</c>.</param>
        void Report(InputActionAssetRequirementFailure failure);
    }

    /// <summary>
    /// A failure reporter that simply convert failures to their string representation and log them as warnings.
    /// </summary>
    internal sealed class LoggingInputActionAssetRequirementFailureReporter : IInputActionAssetRequirementFailureReporter
    {
        /// <inheritdoc/>
        public void Report(InputActionAssetRequirementFailure failure)
        {
            Debug.LogWarning(failure);
        }
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
