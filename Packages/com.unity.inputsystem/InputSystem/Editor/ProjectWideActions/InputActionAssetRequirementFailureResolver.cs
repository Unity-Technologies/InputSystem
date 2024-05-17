#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// An abstraction of a failed requirement resolver that may modify an asset to fix issues with failed requirements.
    /// </summary>
    sealed class InputActionAssetRequirementFailureResolver
    {
        private readonly Action<SerializedObject> m_Resolver;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="name">A user-friendly name describing the action of this resolver.</param>
        /// <param name="resolver">A wrapped resolver function.</param>
        /// <param name="description">A user-friendly elaborate description of the action carried out by this resolver.</param>
        public InputActionAssetRequirementFailureResolver(string name, Action<SerializedObject> resolver, string description)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.Empty.Equals(name)) throw new ArgumentException(nameof(name));

            if (description == null) throw new ArgumentNullException(nameof(description));
            if (string.Empty.Equals(description)) throw new ArgumentException(nameof(description));

            this.m_Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.name = name;
            this.description = description;
        }

        /// <summary>
        /// Attempts to resolve issues with the asset represented by the given serialized object.
        /// </summary>
        /// <param name="obj">Asset represented as an serialized object to be modified.</param>
        public void Resolve(SerializedObject obj)
        {
            m_Resolver.Invoke(obj);
        }

        /// <summary>
        /// Returns a user-friendly name describing the action of this resolver.
        /// </summary>
        /// <remarks>
        /// Intended to be used for e.g. UI button or menu text.
        /// </remarks>
        public string name { get; }

        /// <summary>
        /// Returns a user-friendly elaborate description of the action of this resolver.
        /// </summary>
        /// <remarks>
        /// Intended to be used for e.g. help descriptions or tool-tip text.
        /// </remarks>
        public string description { get; }
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
