using System;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.Plugins.Users
{
    public enum InputDeviceAssignmentPolicy
    {
        AutomaticallyAssignAnyUsedDevice,

        /// <summary>
        /// On platforms (such as consoles) where user management is built-in, reflect device
        /// assignments as they exist at the platform level.
        /// </summary>
        AutomaticallyAssignDeviceAssociatedWithUserByPlatform,

        /// <summary>
        /// Never automatically assign a device to a user.
        /// </summary>
        ManuallyAssignDevicesOnly,
    }

    public interface IInputUserManager
    {
        /// <summary>
        /// List of currently known/active users.
        /// </summary>
        //ReadOnlyArray<IInputUser> users { get; }
    }

    /*
    /// <summary>
    /// Helper/convenience methods for working with <see cref="InputUser">input users</see>.
    /// </summary>
    public static class InputUserExtensions
    {
        /// <summary>
        /// Based
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <remarks>
        /// Does not allocate!
        /// </remarks>
        /// <example>
        /// <code>
        /// class MyBehaviour : MonoBehaviour
        /// {
        ///
        /// }
        /// </code>
        /// </example>
        public static string GetActionDisplayText(this IInputUser user, InputAction action)
        {
            throw new NotImplementedException();
        }

        public static string GetCustomBindingsAsJson(this IInputUser user)
        {
            throw new NotImplementedException();
        }

        public static void RestoreCustomBindingsFromJson(this IInputUser user, string json)
        {
            throw new NotImplementedException();
        }
    }
    */
}
