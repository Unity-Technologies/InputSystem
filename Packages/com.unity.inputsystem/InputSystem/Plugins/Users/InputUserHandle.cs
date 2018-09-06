namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Handle for a user in an external API.
    /// </summary>
    public struct InputUserHandle
    {
        public string apiName { get; private set; }
        public object handle { get; private set; }
    }
}
