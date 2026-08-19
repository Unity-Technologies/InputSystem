#if UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Answer to a platform capability query, meaning what the platform can deliver rather than
    /// what is currently connected.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>CapabilityState</c> in the engine's <c>Modules/Input/InputDeviceIOCTL.h</c>, whose
    /// wire values are pinned by tests on both sides. <see cref="Unknown"/> is zero so that an
    /// unwritten payload, or a platform that has not implemented a query, reads as "we do not know"
    /// rather than as a confident <see cref="NotSupported"/>.
    ///
    /// The value space is open. Treat anything other than <see cref="Supported"/> as not supported
    /// rather than rejecting it, because a newer engine may answer with a value this version of the
    /// package does not know about.
    /// </remarks>
    internal enum InputCapabilitySupport : byte
    {
        /// <summary>
        /// The platform has no answer, typically because it has not implemented the query yet.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The platform definitively cannot deliver it.
        /// </summary>
        NotSupported = 1,

        /// <summary>
        /// The platform definitively can deliver it.
        /// </summary>
        Supported = 2
    }
}
#endif // UNITY_INPUTSYSTEM_SUPPORTS_CAPABILITY_QUERIES
