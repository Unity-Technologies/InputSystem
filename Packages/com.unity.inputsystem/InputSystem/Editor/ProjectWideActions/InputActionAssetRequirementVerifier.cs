#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// A verifier of <see cref="InputActionRequirement"/>s that generates <see cref="InputActionAssetRequirementFailure"/>s
    /// for failed requirements given an <see cref="InputActionAsset"/> to be verified.
    /// </summary>
    sealed class InputActionAssetRequirementVerifier
    {
        /// <summary>
        /// Represents a requirement failure report policy.
        /// </summary>
        public enum ReportPolicy
        {
            /// <summary>
            /// Report all verification failures.
            /// </summary>
            ReportAll,

            /// <summary>
            /// Suppress child errors if a parent entity has verification failures.
            /// </summary>
            SuppressChildErrors
        }

        /// <summary>
        /// The default reporting policy.
        /// </summary>
        public const ReportPolicy DefaultReportPolicy = ReportPolicy.SuppressChildErrors;

        /// <summary>
        /// Represents the verification result.
        /// </summary>
        public sealed class Result
        {
            /// <summary>
            /// Represents a report of requirements and failures that applies to the associated entity.
            /// </summary>
            public struct Report
            {
                public readonly IReadOnlyList<InputActionRequirement> requirements;
                public readonly IReadOnlyList<InputActionAssetRequirementFailure> failures;
            }

            /// <summary>
            /// Represents a list of requirement failures associated with a certain set of requirements.
            /// </summary>
            public readonly struct RequirementFailures
            {
                public RequirementFailures(InputActionAssetRequirements requirements,
                                           IReadOnlyList<InputActionAssetRequirementFailure> failures)
                {
                    this.requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
                    this.failures = failures ?? throw new ArgumentNullException(nameof(failures));
                }

                public readonly InputActionAssetRequirements requirements;
                public readonly IReadOnlyList<InputActionAssetRequirementFailure> failures;
            }

            private readonly List<RequirementFailures> m_RequirementFailures;
            private List<InputActionAssetRequirementFailure> m_Failures;

            private Result()
            {
                m_RequirementFailures = new List<RequirementFailures>(0);
                m_Failures = new List<InputActionAssetRequirementFailure>();
            }

            public static readonly Result Valid = new Result();

            public Result(List<RequirementFailures> requirementFailures)
            {
                this.m_RequirementFailures = requirementFailures ?? throw new ArgumentNullException(nameof(requirementFailures));
                this.m_Failures = new List<InputActionAssetRequirementFailure>();

                foreach (var pair in requirementFailures)
                    m_Failures.AddRange(pair.failures);
            }

            public IReadOnlyList<RequirementFailures> parts => m_RequirementFailures;

            public bool hasFailures => m_Failures.Count > 0;
            public IReadOnlyList<InputActionAssetRequirementFailure> failures => m_Failures;

            public void Append(Result other)
            {
                if (m_Failures == null && other.m_Failures != null)
                    m_Failures = new List<InputActionAssetRequirementFailure>(other.m_Failures);
                else if (m_Failures != null && other.m_Failures != null)
                {
                    foreach (var failure in other.m_Failures)
                        m_Failures.Add(failure);
                }
            }

            public IReadOnlyList<InputActionAssetRequirementFailure> GetActionFailures(string actionPath)
            {
                if (!hasFailures)
                    return null;

                List<InputActionAssetRequirementFailure> failureList = null;
                foreach (var failure in failures)
                {
                    if (actionPath.Equals(failure.requirement.actionPath))
                    {
                        if (failureList == null)
                            failureList = new List<InputActionAssetRequirementFailure>();
                        failureList.Add(failure);
                    }
                }

                return failureList;
            }

            /// <summary>
            /// Returns a dictionary mapping action map names to a list of requirement verification failures, if any.
            /// </summary>
            /// <remarks>Only entries with active failures will be included in the resulting container.</remarks>
            /// <returns>Read-only dictionary of failures per action map name. Never null.</returns>
            public IReadOnlyDictionary<string, IReadOnlyList<InputActionAssetRequirementFailure>> GetActionMapFailures()
            {
                var map = new Dictionary<string, List<InputActionAssetRequirementFailure>>();
                if (hasFailures)
                {
                    foreach (var failure in failures)
                    {
                        var name = failure.requirement.actionMapName;
                        if (!map.TryGetValue(name, out var list))
                        {
                            list = new List<InputActionAssetRequirementFailure>();
                            map.Add(name, list);
                        }
                        list.Add(failure);
                    }
                }
                return map.AsReadOnly<string, List<InputActionAssetRequirementFailure>, IReadOnlyList<InputActionAssetRequirementFailure>>();
            }
        }

        private readonly List<InputActionAssetRequirements> m_Requirements;

        private List<InputActionAssetRequirementFailure> m_Failures;
        private HashSet<string> m_MissingPaths;

        public InputActionAssetRequirementVerifier(InputActionAssetRequirements requirements)
            : this(new List<InputActionAssetRequirements> { requirements })
        {}

        public InputActionAssetRequirementVerifier(IEnumerable<InputActionAssetRequirements> requirements)
        {
            m_Requirements = new List<InputActionAssetRequirements>(requirements);
            m_Failures = new List<InputActionAssetRequirementFailure>();
            m_MissingPaths = new HashSet<string>();
        }

        /// <summary>
        /// Verifies all applicable registered requirements against <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">The asset to be verified.</param>
        /// <returns>Verification result indicating whether requirements where fulfilled or not.</returns>
        /// <seealso cref="InputActionAssetRequirements.Register"/>
        /// <seealso cref="InputActionAssetRequirements.Unregister"/>
        /// <exception cref="System.ArgumentNullException">If <paramref name="asset"/> is <c>null</c>.</exception>
        public Result Verify(InputActionAsset asset)
        {
            m_Failures.Clear();

            List<Result.RequirementFailures> pairs = null;
            foreach (var requirements in m_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                    VerifyRequirement(asset, requirement);
                if (m_Failures.Count <= 0)
                    continue;
                pairs ??= new List<Result.RequirementFailures>();
                pairs.Add(new Result.RequirementFailures(requirements, new List<InputActionAssetRequirementFailure>(m_Failures)));
            }

            return pairs == null ? Result.Valid : new Result(pairs);
        }

        private void ReportFailure(InputActionAsset asset, InputActionRequirement requirement,
            InputActionAssetRequirementFailure.Reason reason, InputAction action)
        {
            m_Failures ??= new List<InputActionAssetRequirementFailure>(); // lazy construction if needed
            m_Failures.Add(new InputActionAssetRequirementFailure(asset, reason, requirement, action));
        }

        private void VerifyRequirement(InputActionAsset asset, InputActionRequirement requirement)
        {
            var path = requirement.actionPath;
            var action = asset.FindAction(path);
            if (action == null)
            {
                // Check if the map (if any) exists
                var index = path.IndexOf('/');
                if (index > 0)
                {
                    var actionMap = path.Substring(0, index);
                    if (asset.FindActionMap(actionMap) == null)
                    {
                        m_MissingPaths ??= new HashSet<string>();
                        if (m_MissingPaths.Add(path))
                            ReportFailure(asset, requirement, InputActionAssetRequirementFailure.Reason.InputActionMapDoNotExist, null);
                    }
                }

                ReportFailure(asset, requirement, InputActionAssetRequirementFailure.Reason.InputActionDoNotExist, null);
            }
            else if (action.bindings.Count == 0)
            {
                ReportFailure(asset, requirement, InputActionAssetRequirementFailure.Reason.InputActionNotBound, action);
            }
            else if (action.type != requirement.actionType)
            {
                ReportFailure(asset, requirement, InputActionAssetRequirementFailure.Reason.InputActionInputActionTypeMismatch, action);
            }
            else if (!string.IsNullOrEmpty(requirement.expectedControlType) &&
                     !string.IsNullOrEmpty(action.expectedControlType) &&
                     action.expectedControlType != requirement.expectedControlType)
            {
                ReportFailure(asset, requirement, InputActionAssetRequirementFailure.Reason.InputActionExpectedControlTypeMismatch, action);
            }
        }
    }
}
#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
