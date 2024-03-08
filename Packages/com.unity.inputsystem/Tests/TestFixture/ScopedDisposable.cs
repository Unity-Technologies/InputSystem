using System;
using UnityEditor;

namespace UnityEngine.InputSystem
{
    // Utility allowing access to object T as well as a dispose delegate to clean-up any resources associated with it.
    // Useful with the 'using' keyword to provide scoped RAII-like cleanup of objects in tests without having a
    // dedicated fixture handling the clean-up.
    internal sealed class ScopedDisposable<T> : IDisposable
        where T : UnityEngine.Object
    {
        private Action<T> m_Dispose;

        public ScopedDisposable(T obj, Action<T> dispose)
        {
            value = obj;
            m_Dispose = dispose;
        }

        public T value
        {
            get;
            private set;
        }

        public void Dispose()
        {
            if (m_Dispose == null)
                return;
            if (value != null)
                m_Dispose(value);
            m_Dispose = null;
            value = null;
        }
    }

    // Convenience API for scoped objects.
    internal static class Scoped
    {
        public static ScopedDisposable<T> Object<T>(T obj) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return new ScopedDisposable<T>(obj, UnityEngine.Object.DestroyImmediate);
#else
            return new ScopedDisposable<T>(obj, UnityEngine.Object.Destroy);
#endif
        }

#if UNITY_EDITOR
        public static ScopedDisposable<T> Asset<T>(T obj) where T : UnityEngine.Object
        {
            return new ScopedDisposable<T>(obj, (o) => AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(o)));
        }

#endif
    }
}
