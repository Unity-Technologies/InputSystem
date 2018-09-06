using System;

namespace UnityEngine.Experimental.Input.Plugins.Users
{
    public static class InputUserSupport
    {
        /// <summary>
        /// Initialize input user management.
        /// </summary>
        /// <remarks>
        /// User management is an optional feature that is not initialized by default.
        /// </remarks>
        public static void Initialize()
        {
        }

        private static bool s_Enabled;

        private static void ThrowIfNotEnabled()
        {
            if (!s_Enabled)
                throw new InvalidOperationException(
                    "User management has not been initialized; call InputUserSupport.Initialize() if you want to use the feature");
        }
    }
}
