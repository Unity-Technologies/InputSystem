#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using Resolver = UnityEngine.Rendering.VirtualTexturing.Resolver;

namespace UnityEngine.InputSystem.Editor
{
    sealed class InputActionAssetRequirementManager
    {
        private readonly Dictionary<string, List<InputActionAssetRequirements>> m_Dictionary;
        private readonly List<InputActionAssetRequirements> m_Requirements;

        public InputActionAssetRequirementManager()
        {
            this.m_Dictionary = new Dictionary<string, List<InputActionAssetRequirements>>();
            this.m_Requirements = new List<InputActionAssetRequirements>();
        }

        /// <summary>
        /// Register requirements on the Project-wide <c>InputActionAsset</c>.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <returns>true if successfully registered and not previously registered, else false.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="requirements"/>is <c>null</c>.</exception>
        public bool Register(InputActionAssetRequirements requirements)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            // Attempt to register requirements
            if (m_Requirements.Contains(requirements))
            {
                Debug.LogError($"Failed to register requirements for \"{requirements.owner}\". Requirements instance already registered.");
                return false;
            }
            m_Requirements.Add(requirements);

            // Register each requirement with its associated action-path for O(1) look-ups later
            foreach (var requirement in requirements)
            {
                var path = requirement.actionPath;
                if (!m_Dictionary.TryGetValue(path, out List<InputActionAssetRequirements> list))
                {
                    list = new List<InputActionAssetRequirements>();
                    m_Dictionary.Add(path, list);
                }
                list.Add(requirements);
            }

            return true;
        }

        /// <summary>
        /// Unregisters requirements that have been previously registered.
        /// </summary>
        /// <param name="requirements">The requirements to be unregistered.</param>
        /// <returns><c>true</c> if the requirements where successfully unregistered, else <c>false</c>.</returns>
        public bool Unregister(InputActionAssetRequirements requirements)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            // Attempt to unregister requirements
            var result = m_Requirements.Remove(requirements);
            if (result)
            {
                // Remove from dictionary
                foreach (var requirement in requirements)
                {
                    var path = requirement.actionPath;
                    var list = m_Dictionary[path];
                    list.Remove(requirements);
                    if (list.Count == 0)
                        m_Dictionary.Remove(path);
                }
                //Debug.Log($"Unregistered requirements for \"{requirements.owner}\".");
                // TODO We should update current set of failures and remove them from the set
            }
            else
            {
                Debug.LogError($"Failed to unregister requirements for \"{requirements.owner}\"");
            }

            return result;
        }

        // TODO Might want to skip these or create them as extensions?

        public InputActionAssetRequirementVerifier.Result Verify(InputActionAsset asset)
        {
            return m_Requirements.Count == 0 ? InputActionAssetRequirementVerifier.Result.Valid :
                new InputActionAssetRequirementVerifier(m_Requirements).Verify(asset);
        }

        public void Verify(InputActionAsset asset, IInputActionAssetRequirementFailureReporter reporter,
            InputActionAssetRequirementVerifier.ReportPolicy reportPolicy = InputActionAssetRequirementVerifier.DefaultReportPolicy)
        {
            foreach (var requirements in m_Requirements)
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
    }

    /// <summary>
    /// Represents a set of requirements on an <c>InputActionAsset</c>.
    /// </summary>
    sealed class InputActionAssetRequirements : IEnumerable<InputActionRequirement>
    {
        private static readonly Dictionary<string, List<InputActionAssetRequirements>> s_Dictionary =
            new Dictionary<string, List<InputActionAssetRequirements>>();

        // Global list of registered requirements
        private static readonly List<InputActionAssetRequirements> s_Requirements = new List<InputActionAssetRequirements>();

        //private readonly List<InputActionAssetResolution> s_Resolvers = new List<InputActionAssetResolution>();

        public InputActionAssetRequirements(string owner, IEnumerable<InputActionRequirement> requirements,
                                            IEnumerable<InputActionAssetRequirementFailureResolver> resolvers,
                                            string implicationOfFailedRequirements)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (string.Empty.Equals(owner))
                throw new ArgumentException(nameof(owner));

            if (implicationOfFailedRequirements == null)
                throw new ArgumentNullException(nameof(implicationOfFailedRequirements));
            if (string.Empty.Equals(implicationOfFailedRequirements))
                throw new ArgumentException(nameof(implicationOfFailedRequirements));

            this.owner = owner;
            this.implication = implicationOfFailedRequirements;
            this.requirements = requirements != null ? requirements.ToArray() : Array.Empty<InputActionRequirement>();
            this.resolvers = resolvers != null ? resolvers.ToArray() : Array.Empty<InputActionAssetRequirementFailureResolver>();
        }

        /// <summary>
        /// Retrieves a read-only list of the requirements in this set of requirements.
        /// </summary>
        public IReadOnlyList<InputActionRequirement> requirements { get; }

        /// <summary>
        /// Retrieves a read-only list of the resolvers associated with this set of requirements.
        /// </summary>
        public IReadOnlyList<InputActionAssetRequirementFailureResolver> resolvers { get; }

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
        /// Register requirements on the Project-wide <c>InputActionAsset</c>.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <returns>true if successfully registered and not previously registered, else false.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="requirements"/>is <c>null</c>.</exception>
        public static bool Register(InputActionAssetRequirements requirements)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            // Attempt to register requirements
            if (s_Requirements.Contains(requirements))
            {
                Debug.LogError($"Failed to register requirements for \"{requirements.owner}\". Requirements instance already registered.");
                return false;
            }
            s_Requirements.Add(requirements);

            // Register each requirement with its associated action-path for O(1) look-ups later
            foreach (var requirement in requirements)
            {
                var path = requirement.actionPath;
                if (!s_Dictionary.TryGetValue(path, out List<InputActionAssetRequirements> list))
                {
                    list = new List<InputActionAssetRequirements>();
                    s_Dictionary.Add(path, list);
                }
                list.Add(requirements);
            }

            return true;
        }

        /// <summary>
        /// Unregisters requirements that have been previously registered.
        /// </summary>
        /// <param name="requirements">The requirements to be unregistered.</param>
        /// <returns><c>true</c> if the requirements where successfully unregistered, else <c>false</c>.</returns>
        public static bool Unregister(InputActionAssetRequirements requirements)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            // Attempt to unregister requirements
            var result = s_Requirements.Remove(requirements);
            if (result)
            {
                // Remove from dictionary
                foreach (var requirement in requirements)
                {
                    var path = requirement.actionPath;
                    var list = s_Dictionary[path];
                    list.Remove(requirements);
                    if (list.Count == 0)
                        s_Dictionary.Remove(path);
                }
                //Debug.Log($"Unregistered requirements for \"{requirements.owner}\".");
                // TODO We should update current set of failures and remove them from the set
            }
            else
            {
                Debug.LogError($"Failed to unregister requirements for \"{requirements.owner}\"");
            }

            return result;
        }

        // TODO Count check here should not be needed, it should be handled by verifier which allows removing this method
        public static InputActionAssetRequirementVerifier.Result Verify(InputActionAsset asset)
        {
            return s_Requirements.Count == 0 ? InputActionAssetRequirementVerifier.Result.Valid :
                new InputActionAssetRequirementVerifier(s_Requirements).Verify(asset);
        }

        // TODO This should be similar to verifiers ability to report errors on a set of requirements so remove this?
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

        /// <summary>
        /// Enumerates all <see cref="InputActionRequirement"/> instances that applies to the given action path.
        /// </summary>
        /// <param name="actionPath">The action path to be evaluated.</param>
        /// <returns>Enumerable list of <see cref="InputActionRequirement"/> instances.</returns>
        public IEnumerable<InputActionRequirement> EnumerateRequirements(string actionPath)
        {
            foreach (var requirement in requirements)
            {
                if (actionPath == null || requirement.actionPath.Contains(actionPath))
                    yield return requirement;
            }
        }

        private static readonly ReadOnlyCollection<InputActionAssetRequirements> empty =
            new ReadOnlyCollection<InputActionAssetRequirements>(new List<InputActionAssetRequirements>());

        // TODO Note that modifications to underlying list will be reflected in returned collection, this is desirable
        /// <summary>
        /// Enumerates all sets of requirements, optionally matching the specified action path.
        /// </summary>
        /// <param name="actionPath">Optional action path to filter the enumeration.</param>
        /// <returns>Enumerable requirements.</returns>
        public static ReadOnlyCollection<InputActionAssetRequirements> Get(string actionPath = null) // TODO Consider return a filtered object keeping the actionPath?
        {
            if (actionPath == null)
                return s_Requirements.AsReadOnly();
            return s_Dictionary.TryGetValue(actionPath, out List<InputActionAssetRequirements> list) ? list.AsReadOnly() : empty;
        }

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

        public IEnumerator<InputActionRequirement> GetEnumerator()
        {
            return requirements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return requirements.GetEnumerator();
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
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
