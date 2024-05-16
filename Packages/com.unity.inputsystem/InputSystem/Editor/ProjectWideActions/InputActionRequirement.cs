#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Represents a requirement imposed on an <see cref="InputActionAsset"/> configuration.
    /// </summary>
    sealed class InputActionRequirement
    {
        /// <summary>
        /// Constructs a new <c>InputActionActionRequirement</c>.
        /// </summary>
        /// <param name="actionPath">The <c>InputAction</c> path (including action map name).</param>
        /// <param name="actionType">The expected <c>InputActionType</c> affecting change detection, phase behavior, etc.</param>
        /// <param name="expectedControlType">The expected control type that may be bound to this action.</param>
        /// <param name="implication">A user-friendly message explaining the implication of not fulfilling this requirement.</param>
        /// <exception cref="ArgumentNullException">If any input argument is <c>null</c>.</exception>
        /// <see cref="InputAction"/>
        /// <see cref="InputActionType"/>
        /// <see cref="InputActionAsset"/>
        public InputActionRequirement(string actionPath, InputActionType actionType, string expectedControlType, string implication)
        {
            this.actionPath = actionPath ?? throw new ArgumentNullException(nameof(actionPath));
            if (actionPath == string.Empty)
                throw new ArgumentException($"{nameof(actionPath)} may not be the empty string");

            this.actionType = actionType;
            this.expectedControlType = expectedControlType ?? throw new ArgumentNullException(nameof(expectedControlType));

            this.implication = implication ?? throw new ArgumentNullException(nameof(implication));
            if (implication == string.Empty)
                throw new ArgumentException($"{nameof(implication)} may not be the empty string");
        }

        /// <summary>
        /// The expected action path associated with the action for which this requirement applies.
        /// </summary>
        public string actionPath { get; }

        /// <summary>
        /// The required or expected action map name.
        /// </summary>
        public string actionMapName => GetActionMapName(actionPath);

        /// <summary>
        /// The required action type.
        /// </summary>
        public InputActionType actionType { get; }

        /// <summary>
        /// The expected control type (if any).
        /// </summary>
        /// <remarks>
        /// In case this is the empty string the requirement is considered to not have any expectations on control type.
        /// </remarks>
        public string expectedControlType { get; }

        /// <summary>
        /// A user-friendly message describing the implication of not fulfilling this requirement.
        /// </summary>
        public string implication { get; }

        private static string GetActionMapName(string actionPath)
        {
            var index = actionPath.IndexOf('/');
            return index > 0 ? actionPath.Substring(0, index) : null;
        }
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
