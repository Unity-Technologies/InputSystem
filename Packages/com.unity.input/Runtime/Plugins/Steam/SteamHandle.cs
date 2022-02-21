#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;

namespace UnityEngine.InputSystem.Steam
{
    /// <summary>
    /// A handle for a Steam controller API object typed <typeparamref name="TObject"/>.
    /// </summary>
    /// <typeparam name="TObject">A type used to type the Steam handle. The type itself isn't used other than
    /// for providing type safety to the Steam handle.</typeparam>
    public struct SteamHandle<TObject> : IEquatable<SteamHandle<TObject>>
    {
        private ulong m_Handle;

        public SteamHandle(ulong handle)
        {
            m_Handle = handle;
        }

        public override string ToString()
        {
            return string.Format("Steam({0}): {1}", typeof(TObject).Name, m_Handle);
        }

        public bool Equals(SteamHandle<TObject> other)
        {
            return m_Handle == other.m_Handle;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is SteamHandle<TObject> && Equals((SteamHandle<TObject>)obj);
        }

        public override int GetHashCode()
        {
            return m_Handle.GetHashCode();
        }

        public static bool operator==(SteamHandle<TObject> a, SteamHandle<TObject> b)
        {
            return a.m_Handle == b.m_Handle;
        }

        public static bool operator!=(SteamHandle<TObject> a, SteamHandle<TObject> b)
        {
            return !(a == b);
        }

        public static explicit operator ulong(SteamHandle<TObject> handle)
        {
            return handle.m_Handle;
        }
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
