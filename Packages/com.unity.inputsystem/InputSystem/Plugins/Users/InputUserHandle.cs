using System;

namespace UnityEngine.Experimental.Input.Plugins.Users
{
    /// <summary>
    /// Handle for a user in an external API.
    /// </summary>
    public struct InputUserHandle : IEquatable<InputUserHandle>
    {
        /// <summary>
        /// Symbolic name of the API that assigned the handle.
        /// </summary>
        /// <remarks>
        /// On PS4, for example, this will read "PS4" for user handles corresponding
        /// to <c>sceUserId</c>.
        /// </remarks>
        public string apiName
        {
            get { return m_ApiName; }
        }

        public object handle
        {
            get { return m_Handle; }
        }

        public InputUserHandle(string apiName, object handle)
        {
            if (string.IsNullOrEmpty(apiName))
                throw new ArgumentNullException("apiName");
            if (handle == null)
                throw new ArgumentNullException("handle");

            m_ApiName = apiName;
            m_Handle = handle;
        }

        public override string ToString()
        {
            if (m_ApiName == null || m_Handle == null)
                return base.ToString();

            return string.Format("{0}({1})", m_ApiName, m_Handle);
        }

        public bool Equals(InputUserHandle other)
        {
            return string.Equals(apiName, other.apiName) && Equals(handle, other.handle);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is InputUserHandle && Equals((InputUserHandle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((apiName != null ? apiName.GetHashCode() : 0) * 397) ^ (handle != null ? handle.GetHashCode() : 0);
            }
        }

        private string m_ApiName;
        private object m_Handle;
    }
}
