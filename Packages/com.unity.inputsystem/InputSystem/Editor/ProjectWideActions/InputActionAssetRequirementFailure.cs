#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Represents a failed requirement.
    /// </summary>
    sealed class InputActionAssetRequirementFailure
    {
        /// <summary>
        /// Represents the cause of the failure.
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// The expected <c>InputActionMap</c> do not exist.
            /// </summary>
            InputActionMapDoNotExist,

            /// <summary>
            /// The expected <c>InputAction</c> do not exist.
            /// </summary>
            InputActionDoNotExist,

            /// <summary>
            /// The required <c>InputAction</c> is not bound.
            /// </summary>
            InputActionNotBound,

            /// <summary>
            /// There is a type mismatch between expected action type and the actual <see cref="InputAction.type"/>.
            /// </summary>
            InputActionInputActionTypeMismatch,

            /// <summary>
            /// There is a type mismatch between the expected control type and the actual <see cref="InputAction.expectedControlType"/>.
            /// </summary>
            InputActionExpectedControlTypeMismatch
        }

        /// <summary>
        /// Constructs a new <c>InputActionAssetRequirementFailure</c>.
        /// </summary>
        /// <param name="asset">The asset being verified to which this failure is associated.</param>
        /// <param name="reason">The reason behind this requirement verification failure.</param>
        /// <param name="requirement">The associated requirement that failed verification checks.</param>
        /// <param name="actual">The actual <see cref="InputAction"/> that caused the failure. May be <c>null</c> if not applicable.</param>
        public InputActionAssetRequirementFailure(InputActionAsset asset, Reason reason,
                                                  InputActionRequirement requirement, InputAction actual)
        {
            this.asset = asset;
            this.reason = reason;
            this.requirement = requirement;
            this.inputActionType = actual?.type ?? InputActionType.Value;
            this.expectedControlType = actual?.expectedControlType;
        }

        /// <summary>
        /// Returns the asset associated with this requirement verification failure.
        /// </summary>
        public readonly InputActionAsset asset;

        /// <summary>
        /// Returns the reason behind this failure.
        /// </summary>
        public readonly Reason reason;

        /// <summary>
        /// The actual <see cref="InputAction.type"/> associated with the failure.
        /// </summary>
        public readonly InputActionType inputActionType;

        /// <summary>
        /// The actual <see cref="InputAction.expectedControlType"/> associated with the failure.
        /// </summary>
        public readonly string expectedControlType;

        /// <summary>
        /// The associated requirement that failed verification.
        /// </summary>
        public readonly InputActionRequirement requirement;

        /*public string Describe(bool includeAssetReference = true, bool includeImplication = true)
        {
            return DefaultInputActionRequirementFailureFormatter.Format(this, requirement.implication, includeAssetReference, includeImplication);
        }*/

        /*public override string ToString()
        {
            return Describe();
        }*/
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
