#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Editor-specific singleton wrapper for RemoteInputPlayerConnection.
    /// In the editor, we need to make sure that we get the same instance after domain reloads.
    /// Otherwise, callbacks we have registered before the reload will no longer be valid, because
    /// the object instance they point to will not deserialize to a valid object.
    /// </summary>
    internal class RemoteInputPlayerConnectionEditor : ScriptableSingleton<RemoteInputPlayerConnectionEditor>
    {
        [SerializeField]
        private RemoteInputPlayerConnection m_Connection;

        public static RemoteInputPlayerConnection GetInstance()
        {
            if (instance.m_Connection == null)
            {
                instance.m_Connection = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
                instance.m_Connection.hideFlags = HideFlags.HideAndDontSave;
            }
            return instance.m_Connection;
        }
    }
}
#endif
