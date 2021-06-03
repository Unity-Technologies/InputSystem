using System;

namespace UnityEngine.InputSystem.Users
{
    /// <summary>
    /// Handle for a user account in an external API.
    /// </summary>
    public struct InputUserAccountHandle : IEquatable<InputUserAccountHandle>
    {
        /// <summary>
        /// Symbolic name of the API that owns the handle.
        /// </summary>
        /// <remarks>
        /// This essentially provides a namespace for <see cref="handle"/>.
        ///
        /// On PS4, for example, this will read "PS4" for user handles corresponding
        /// to <c>sceUserId</c>.
        ///
        /// This will not be null or empty except if the handle is invalid.
        /// </remarks>
        public string apiName
        {
            get { return m_ApiName; }
        }

        public ulong handle
        {
            get { return m_Handle; }
        }

        public InputUserAccountHandle(string apiName, ulong handle)
        {
            if (string.IsNullOrEmpty(apiName))
                throw new ArgumentNullException("apiName");

            m_ApiName = apiName;
            m_Handle = handle;
        }

        public override string ToString()
        {
            if (m_ApiName == null)
                return base.ToString();

            return string.Format("{0}({1})", m_ApiName, m_Handle);
        }

        public bool Equals(InputUserAccountHandle other)
        {
            return string.Equals(apiName, other.apiName) && Equals(handle, other.handle);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputUserAccountHandle && Equals((InputUserAccountHandle)obj);
        }

        public static bool operator==(InputUserAccountHandle left, InputUserAccountHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputUserAccountHandle left, InputUserAccountHandle right)
        {
            return !left.Equals(right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((apiName != null ? apiName.GetHashCode() : 0) * 397) ^ handle.GetHashCode();
            }
        }

        private string m_ApiName;
        private ulong m_Handle;
    }
}
