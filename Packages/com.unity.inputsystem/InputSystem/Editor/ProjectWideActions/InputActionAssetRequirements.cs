#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Provides a <see cref="AsReadOnly{TKey, TValue, TReadOnlyValue}(IDictionary{TKey, TValue})"/>
    /// method on any generic dictionary.
    /// </summary>
    public static class DictionaryExtension
    {
        class ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue> : IReadOnlyDictionary<TKey, TReadOnlyValue>
            where TValue : TReadOnlyValue
            where TKey : notnull
        {
            private IDictionary<TKey, TValue> _dictionary;

            public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            {
                if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
                _dictionary = dictionary;
            }

            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

            public IEnumerable<TKey> Keys => _dictionary.Keys;

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TReadOnlyValue value)
            {
                var r = _dictionary.TryGetValue(key, out var v);
                value = v !;
                return r;
            }

            public IEnumerable<TReadOnlyValue> Values => _dictionary.Values.Cast<TReadOnlyValue>();

            public TReadOnlyValue this[TKey key] => _dictionary[key];

            public int Count => _dictionary.Count;

            public IEnumerator<KeyValuePair<TKey, TReadOnlyValue>> GetEnumerator() => _dictionary.Select(x => new KeyValuePair<TKey, TReadOnlyValue>(x.Key, x.Value)).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Creates a wrapper on a dictionary that adapts the type of the values.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key.</typeparam>
        /// <typeparam name="TValue">The dictionary value.</typeparam>
        /// <typeparam name="TReadOnlyValue">The base type of the <typeparamref name="TValue"/>.</typeparam>
        /// <param name="this">This dictionary.</param>
        /// <returns>A dictionary where values are a base type of this dictionary.</returns>
        public static IReadOnlyDictionary<TKey, TReadOnlyValue> AsReadOnly<TKey, TValue, TReadOnlyValue>(this IDictionary<TKey, TValue> @this)
            where TValue : TReadOnlyValue
            where TKey : notnull
        {
            return new ReadOnlyDictionaryWrapper<TKey, TValue, TReadOnlyValue>(@this);
        }
    }

    #region Reporting

    /// <summary>
    /// An interface for reporting <c>InputActionAssetRequirementFailure</c> verification failures.
    /// </summary>
    interface IInputActionAssetRequirementFailureReporter
    {
        void Report(InputActionAssetRequirementFailure failure);
    }

    /// <summary>
    /// A failure reporter that simply failures to their string representation and log them as warnings.
    /// </summary>
    internal sealed class LoggingInputActionAssetRequirementFailureReporter : IInputActionAssetRequirementFailureReporter
    {
        /// <inheritdoc/>
        public void Report(InputActionAssetRequirementFailure failure)
        {
            Debug.LogWarning(failure);
        }
    }

    #endregion

    /// <summary>
    /// Represents a requirement imposed on an <see cref="InputActionAsset"/> configuration.
    /// </summary>
    readonly struct InputActionRequirement
    {
        /// <summary>
        /// Constructs a new <c>InputActionActionRequirement</c>.
        /// </summary>
        /// <param name="actionPath">The <c>InputAction</c> path (including action map name).</param>
        /// <param name="actionType">The expected <c>InputActionType</c> affecting change detection, phase behavior, etc.</param>
        /// <param name="expectedControlType">The expected control type that may be bound to this action.</param>
        /// <param name="implication"></param>
        /// <exception cref="ArgumentNullException">If any input argument is <c>null</c>.</exception>
        /// <see cref="InputAction"/>
        /// <see cref="InputActionType"/>
        /// <see cref="InputActionAsset"/>
        public InputActionRequirement(string actionPath, InputActionType actionType, string expectedControlType, string implication)
        {
            this.actionPath = actionPath ?? throw new ArgumentNullException(nameof(actionPath));
            this.actionType = actionType;
            this.expectedControlType = expectedControlType ?? throw new ArgumentNullException(nameof(expectedControlType));
            this.implication = implication ?? throw new ArgumentNullException(nameof(implication));
            this.actionMapName = GetActionMapName(actionPath);
        }

        public readonly string actionPath;
        public readonly string actionMapName;
        public readonly InputActionType actionType;
        public readonly string expectedControlType;
        public readonly string implication;

        private static string GetActionMapName(string actionPath)
        {
            var index = actionPath.IndexOf('/');
            return index > 0 ? actionPath.Substring(0, index) : null;
        }
    }

    /// <summary>
    /// Represents a set of requirements on an <c>InputActionAsset</c>.
    /// </summary>
    sealed class InputActionAssetRequirements
    {
        // Global list of registered requirements
        private static readonly List<InputActionAssetRequirements> s_Requirements = new List<InputActionAssetRequirements>();

        public InputActionAssetRequirements(string owner, IEnumerable<InputActionRequirement> requirements, string implicationOfFailedRequirements)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.requirements = requirements.ToArray();
            this.implication = implicationOfFailedRequirements ?? throw new ArgumentNullException(nameof(implicationOfFailedRequirements));
        }

        public static IReadOnlyList<InputActionAssetRequirements> Get()
        {
            return new List<InputActionAssetRequirements>(s_Requirements);
        }

        /// <summary>
        /// Retrieves a read-only list of the requirements in this set of requirements.
        /// </summary>
        public IReadOnlyList<InputActionRequirement> requirements { get; }

        /// <summary>
        /// Describes the main implication of not meeting this particular set of requirements.
        /// </summary>
        public string implication { get; }

        /// <summary>
        /// Describes the owner (demander) of this set of requirements.
        /// </summary>
        public string owner { get; }


        // TODO Allow registering listeners
        /*public readonly struct InputActionAssetRequirementFailureChangeEvents
        {
            public readonly InputActionAssetRequirementFailure failure;
            public readonly bool wasRemoved;
        }
        public delegate void InputActionAssetRequirementFailureStatusChange(InputActionAssetRequirementFailure failure);
        public static CallbackArray<InputActionAssetRequirementFailureStatusChange> s_Callbacks;
        public EventHandler<InputActionAssetRequirementFailureStatusChange> OnRequirementFailureStatusChange;

        public static event InputActionAssetRequirementFailureStatusChange onActionsChange
        {
            add => s_Callbacks.AddCallback(value);
            remove => s_Callbacks.RemoveCallback(value);
        }*/

        /// <summary>
        /// Register requirements on <c>InputActionAsset</c>.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <returns>true if successfully registered and not previously registered, else false.</returns>
        public static bool Register(InputActionAssetRequirements requirements)
        {
            // Attempt to register requirements
            if (s_Requirements.Contains(requirements))
            {
                Debug.LogError($"Failed to register requirements for \"{requirements.owner}\". Requirements instance already registered.");
                return false;
            }
            s_Requirements.Add(requirements);

            // Check if Project-wide input actions are configured
            /*var asset = InputSystem.actions;
            if (asset == null)
                return true;

            // Verify requirements against project-wide input actions
            var verifier = new InputActionAssetRequirementVerifier(requirements,
                InputActionAssetRequirementVerifier.DefaultReportPolicy);
            var result = verifier.Verify(asset);
            foreach (var failure in result.failures)
            {
            }
            if (!result.isValid)
            {
                foreach (var failure in result.failures)
                {
                    //Debug.LogWarning(DefaultFormatter.Format(cfg.ActionAsset, failure, kImplicationOfFailedRequirements));
                }
            }*/

            //Debug.Log($"Registered requirements for \"{requirements.owner}\".");

            return true;
        }

        public static bool Unregister(InputActionAssetRequirements requirements)
        {
            // Attempt to unregister requirements
            var result = s_Requirements.Remove(requirements);
            if (result)
            {
                //Debug.Log($"Unregistered requirements for \"{requirements.owner}\".");
                // TODO We should update current set of failures and remove them from the set
            }
            else
            {
                Debug.LogError($"Failed to unregister requirements for \"{requirements.owner}\"");
            }
            return result;
        }

        public static InputActionAssetRequirementVerifier.Result Verify(InputActionAsset asset)
        {
            if (s_Requirements.Count == 0)
                return InputActionAssetRequirementVerifier.Result.Valid;
            var verifier = new InputActionAssetRequirementVerifier(s_Requirements);
            return verifier.Verify(asset);
        }

        public static void Verify(InputActionAsset asset, IInputActionAssetRequirementFailureReporter reporter,
            InputActionAssetRequirementVerifier.ReportPolicy reportPolicy = InputActionAssetRequirementVerifier.DefaultReportPolicy)
        {
            foreach (var requirements in s_Requirements)
            {
                var verifier = new InputActionAssetRequirementVerifier(requirements);
                var result = verifier.Verify(asset);
                if (result.hasFailures)
                {
                    foreach (var failure in result.failures)
                    {
                        reporter.Report(failure);
                    }
                }
            }
        }

        public IEnumerable<InputActionAssetRequirementFailure> GetFailures(string actionPath = null)
        {
            // TODO Considering storing cached result of each verify request into a static map.
            //      This way, it may be obtained later by enumerating failures.

            return null;
        }

        public IEnumerable<InputActionRequirement> EnumerateRequirement(string actionPath)
        {
            foreach (var requirement in requirements)
            {
                if (actionPath == null || requirement.actionPath.Contains(actionPath))
                    yield return requirement;
            }
        }

        /// <summary>
        /// Enumerates all sets of requirements, optionally matching the specified action path.
        /// </summary>
        /// <param name="actionPath">Optional action path to filter the enumeration.</param>
        /// <returns>Enumerable requirements.</returns>
        public static IEnumerable<InputActionAssetRequirements> Enumerate(string actionPath = null) // TODO Consider return a filtered object keeping the actionPath?
        {
            foreach (var requirements in s_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                {
                    if (actionPath == null || requirement.actionPath.Contains(actionPath))
                    {
                        yield return requirements;
                        break;
                    }
                }
            }
        }

        /*public static IReadOnlyDictionary<string, IReadOnlyList<InputActionRequirement>> GetActionRequirementsPerOwner(string actionPath)
        {
            List<InputActionRequirement> result = null;
            foreach (var requirements in s_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                {
                    if (requirement.actionPath.Contains(actionPath))
                    {
                        result ??= new List<InputActionRequirement>();
                        result.Add(requirement);
                    }
                }
            }
            return result;
        }*/

        public static IReadOnlyList<InputActionRequirement> GetActionRequirements(string actionPath)
        {
            List<InputActionRequirement> result = null;
            foreach (var requirements in s_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                {
                    if (requirement.actionPath.Contains(actionPath))
                    {
                        result ??= new List<InputActionRequirement>();
                        result.Add(requirement);
                    }
                }
            }
            return result;
        }

        public static IReadOnlyDictionary<string, IEnumerable<InputActionAssetRequirements>> GetActionMapRequirements()
        {
            var dictionary = new Dictionary<string, List<InputActionAssetRequirements>>();
            foreach (var requirements in s_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                {
                    var actionMapName = requirement.actionMapName;
                    if (!dictionary.TryGetValue(actionMapName,
                        out List<InputActionAssetRequirements> actionMapRequirements))
                    {
                        actionMapRequirements = new List<InputActionAssetRequirements>();
                        dictionary.Add(actionMapName, actionMapRequirements);
                    }
                    actionMapRequirements.Add(requirements);
                    break; // no need to process more requirements for this set of requirements here
                }
            }

            return dictionary
                .AsReadOnly<string, List<InputActionAssetRequirements>, IEnumerable<InputActionAssetRequirements>>();
            //return dictionary.AsReadOnly<string, List<IEnumerable<InputActionAssetRequirements>>, IEnumerable<InputActionAssetRequirements>>();
        }

        public static IReadOnlyList<InputActionRequirement> FindRequirements(string path)
        {
            // TODO Replace by dictionary lookup
            List<InputActionRequirement> result = null;
            foreach (var requirements in s_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                {
                    if (requirement.actionPath.Equals(path))
                    {
                        if (result == null)
                            result = new List<InputActionRequirement>();
                        result.Add(requirement);
                    }
                }
            }
            return result;
        }
    }

    enum InputActionRequirementFailureType
    {
        InputActionMapDoNotExist,
        InputActionDoNotExist,
        InputActionNotBound,
        InputActionInputActionTypeMismatch,
        InputActionExpectedControlTypeMismatch
    }

    sealed class InputActionAssetRequirementFailure
    {
        public InputActionAssetRequirementFailure(InputActionAsset asset, InputActionRequirementFailureType type, InputActionRequirement requirement, InputAction actual)
        {
            this.asset = asset;
            this.type = type;
            this.requirement = requirement;
            this.inputActionType = actual?.type ?? InputActionType.Value;
            this.expectedControlType = actual?.expectedControlType ?? null;
        }

        public readonly InputActionAsset asset;
        public readonly InputActionRequirementFailureType type;
        public readonly InputActionType inputActionType;
        public readonly string expectedControlType;
        public readonly InputActionRequirement requirement;

        public string Describe(bool includeAssetReference = true, bool includeImplication = true)
        {
            return DefaultFormatter.Format(this, requirement.implication, includeAssetReference, includeImplication);
        }

        public override string ToString()
        {
            return Describe();
        }
    }

    static class DefaultFormatter
    {
        public static string Format(InputActionAssetRequirementFailure failure, string implication,
            bool includeAssetReference, bool includeImplication)
        {
            switch (failure.type)
            {
                case InputActionRequirementFailureType.InputActionMapDoNotExist:
                    return FormatActionMapProblem(failure, implication, "could not be found",
                        includeAssetReference, includeImplication);
                case InputActionRequirementFailureType.InputActionDoNotExist:
                    return FormatActionProblem(failure, implication, "could not be found",
                        includeAssetReference, includeImplication);
                case InputActionRequirementFailureType.InputActionNotBound:
                    return FormatActionProblem(failure, implication, "do not have any configured bindings",
                        includeAssetReference, includeImplication);
                case InputActionRequirementFailureType.InputActionInputActionTypeMismatch:
                    return FormatActionProblem(failure, implication,
                        $"has 'type' set to '{nameof(InputActionType)}.{failure.inputActionType}', but '{nameof(InputActionType)}.{failure.requirement.actionType}' was expected",
                        includeAssetReference, includeImplication);
                case InputActionRequirementFailureType.InputActionExpectedControlTypeMismatch:
                    return FormatActionProblem(failure, implication,
                        $"has 'expectedControlType' set to '{failure.expectedControlType}', but '{failure.requirement.expectedControlType}' was expected",
                        includeAssetReference, includeImplication);
                default:
                    throw new ArgumentException(nameof(failure.type));
            }
        }

        private static string GetAssetReference(InputActionAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return (path == null) ? '"' + asset.name + '"' : "<a href=\"" + path + $">{path}</a>";
        }

        private static string FormatActionMapProblem(InputActionAssetRequirementFailure failure,
            string implication, string reason, bool includeAssetReference, bool includeImplication)
        {
            var sb = new StringBuilder($"Required {nameof(InputActionMap)} with path '{failure.requirement.actionMapName}' ");
            if (includeAssetReference)
                sb.Append($"in asset '{GetAssetReference(failure.asset)}' ");
            sb.Append(reason);
            sb.Append('.');
            if (includeImplication)
                sb.Append(implication);
            return sb.ToString();
            //return $"{nameof(InputActionMap)} with path '{failure.requirement.actionMapName}' in asset '{GetAssetReference(failure.asset)}' {reason}. {implication}";
        }

        private static string FormatActionProblem(InputActionAssetRequirementFailure failure,
            string implication, string reason, bool includeAssetReference, bool includeImplication)
        {
            var sb = new StringBuilder($"Required {nameof(InputAction)} with path '{failure.requirement.actionPath}' ");
            if (includeAssetReference)
                sb.Append($"in asset '{GetAssetReference(failure.asset)}' ");
            sb.Append(reason);
            sb.Append('.');
            if (includeImplication)
                sb.Append(implication);
            return sb.ToString();
            //return $"{nameof(InputAction)} with path '{failure.requirement.actionPath}' in asset '{GetAssetReference(failure.asset)}' {reason}. {implication}";
        }
    }

    /// <summary>
    /// Provides support for throttled synchronous verification.
    /// </summary>
    sealed class ThrottledInputActionAssetVerifier
    {
        private readonly InputActionAsset m_Asset;
        private bool m_Invalidated;

        public event Action<InputActionAssetRequirementVerifier.Result> OnVerificationResult;

        public ThrottledInputActionAssetVerifier(InputActionAsset asset)
        {
            m_Asset = asset;
            result = InputActionAssetRequirementVerifier.Result.Valid;
        }

        public InputActionAssetRequirementVerifier.Result result { get; private set; }

        public void Invalidate()
        {
            if (m_Invalidated)
                return;

            m_Invalidated = true;
            EditorApplication.delayCall += DoVerify;
        }

        public InputActionAssetRequirementVerifier.Result Verify()
        {
            m_Invalidated = true;
            DoVerify();
            return result;
        }

        private void DoVerify()
        {
            if (!m_Invalidated)
                return;

            result = new InputActionAssetRequirementVerifier(InputActionAssetRequirements.Get()).Verify(m_Asset);
            m_Invalidated = false;

            var handler = OnVerificationResult;
            handler?.Invoke(result);
        }

        /*public Task<InputActionAssetRequirementVerifier.Result> Verify()
        {
            var verifier = new InputActionAssetRequirementVerifier(InputActionAssetRequirements.Get());
            verifier.Verify(asset)
        }*/
    }

    sealed class InputActionAssetRequirementVerifier
    {
        /// <summary>
        /// Represents a requirement failure report policy.
        /// </summary>
        public enum ReportPolicy
        {
            ReportAll,
            SuppressChildErrors
        }

        public const ReportPolicy DefaultReportPolicy = ReportPolicy.SuppressChildErrors;

        public class Result
        {
            /// <summary>
            /// Represents a report of requirements and failures that applies to the associated entity.
            /// </summary>
            public struct Report
            {
                public readonly IReadOnlyList<InputActionRequirement> requirements;
                public readonly IReadOnlyList<InputActionAssetRequirementFailure> failures;
            }

            public readonly struct Pair
            {
                public Pair(InputActionAssetRequirements requirements,
                            IReadOnlyList<InputActionAssetRequirementFailure> failures)
                {
                    this.requirements = requirements;
                    this.failures = failures;
                }

                public readonly InputActionAssetRequirements requirements;
                public readonly IReadOnlyList<InputActionAssetRequirementFailure> failures;
            }

            private List<Pair> m_RequirementFailures;
            private List<InputActionAssetRequirementFailure> m_Failures;

            private Result()
            {
                m_RequirementFailures = new List<Pair>(0);
                m_Failures = new List<InputActionAssetRequirementFailure>();
            }

            public static readonly Result Valid = new Result();

            public Result(List<Pair> requirementFailures)
            {
                this.m_RequirementFailures = requirementFailures ?? throw new ArgumentNullException(nameof(requirementFailures));
                this.m_Failures = new List<InputActionAssetRequirementFailure>();
                if (requirementFailures != null)
                {
                    foreach (var pair in requirementFailures)
                        m_Failures.AddRange(pair.failures);
                }
            }

            public IReadOnlyList<Pair> parts => m_RequirementFailures;

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

            // TODO Add a way to get requirements per path, as well as failures per path

            public readonly struct Impact
            {
                public readonly string owner;
                public readonly string implication;
            }

            /*public IEnumerable<Impact> Implications()
            {
                Dictionary<string, List<string>> implicationsPerOwner = new Dictionary<string, List<string>>();
                foreach (var failure in failures)
                {
                    if (!implicationsPerOwner.TryGetValue(failure.requirement.owner, out List<string> implications))
                        implications = new List<string>();
                    if (!implications.Contains(failure.requirement.implication))
                        implications.Add(failure.requirement.implication);
                }

            }*/
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

            List<Result.Pair> pairs = null;
            foreach (var requirements in m_Requirements)
            {
                foreach (var requirement in requirements.requirements)
                    VerifyRequirement(asset, requirement);
                if (m_Failures.Count <= 0)
                    continue;
                pairs ??= new List<Result.Pair>();
                pairs.Add(new Result.Pair(requirements, new List<InputActionAssetRequirementFailure>(m_Failures)));
            }

            return pairs == null ? Result.Valid : new Result(pairs);
        }

        private void ReportFailure(InputActionAsset asset, InputActionRequirement requirement, InputActionRequirementFailureType type, InputAction action)
        {
            m_Failures ??= new List<InputActionAssetRequirementFailure>(); // lazy construction
            m_Failures.Add(new InputActionAssetRequirementFailure(asset, type, requirement, action));
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
                        if (m_MissingPaths == null)
                            m_MissingPaths = new HashSet<string>();
                        if (m_MissingPaths.Add(path))
                            ReportFailure(asset, requirement, InputActionRequirementFailureType.InputActionMapDoNotExist, null);
                    }
                }

                ReportFailure(asset, requirement, InputActionRequirementFailureType.InputActionDoNotExist, null);
            }
            else if (action.bindings.Count == 0)
            {
                ReportFailure(asset, requirement, InputActionRequirementFailureType.InputActionNotBound, action);
            }
            else if (action.type != requirement.actionType)
            {
                ReportFailure(asset, requirement, InputActionRequirementFailureType.InputActionInputActionTypeMismatch, action);
            }
            else if (!string.IsNullOrEmpty(requirement.expectedControlType) &&
                     !string.IsNullOrEmpty(action.expectedControlType) &&
                     action.expectedControlType != requirement.expectedControlType)
            {
                ReportFailure(asset, requirement, InputActionRequirementFailureType.InputActionExpectedControlTypeMismatch, action);
            }
        }
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
