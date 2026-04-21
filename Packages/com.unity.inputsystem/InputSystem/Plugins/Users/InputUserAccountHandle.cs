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

        /// <summary>The raw platform-specific handle value.</summary>
        public ulong handle
        {
            get { return m_Handle; }
        }

        /// <summary>Initializes a handle with the given platform name and raw value.</summary>
        public InputUserAccountHandle(string apiName, ulong handle)
        {
            if (string.IsNullOrEmpty(apiName))
                throw new ArgumentNullException("apiName");

            m_ApiName = apiName;
            m_Handle = handle;
        }

        /// <summary>Returns a string representation of this handle.</summary>
        public override string ToString()
        {
            if (m_ApiName == null)
                return base.ToString();

            return string.Format("{0}({1})", m_ApiName, m_Handle);
        }

        /// <summary>Returns true if both handles refer to the same platform account.</summary>
        public bool Equals(InputUserAccountHandle other)
        {
            return string.Equals(apiName, other.apiName) && Equals(handle, other.handle);
        }

        /// <summary>Returns true if the given object is an <see cref="InputUserAccountHandle"/> referring to the same account.</summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is InputUserAccountHandle && Equals((InputUserAccountHandle)obj);
        }

        /// <summary>Returns true if both handles refer to the same platform account.</summary>
        public static bool operator==(InputUserAccountHandle left, InputUserAccountHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>Returns true if the two handles refer to different platform accounts.</summary>
        public static bool operator!=(InputUserAccountHandle left, InputUserAccountHandle right)
        {
            return !left.Equals(right);
        }

        /// <summary>Returns a hash code for this handle.</summary>
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
