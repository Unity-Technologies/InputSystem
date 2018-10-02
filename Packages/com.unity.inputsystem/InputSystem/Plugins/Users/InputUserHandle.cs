namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Handle for a user in an external API.
    /// </summary>
    public struct InputUserHandle
    {
        /// <summary>
        /// Symbolic name of the API that assigned the handle.
        /// </summary>
        /// <remarks>
        /// On PS4, for example, this will read "PS4" for user handles corresponding
        /// to <c>sceUserId</c>.
        /// </remarks>
        public string apiName { get; private set; }

        public object handle { get; private set; }
    }
}
