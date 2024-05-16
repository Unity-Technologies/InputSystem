#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    readonly struct InputActionAssetResolution
    {
        private readonly Action<SerializedObject> m_Resolver;

        public InputActionAssetResolution(string name, Action<SerializedObject> resolver, string description)
        {
            this.name = name;
            this.m_Resolver = resolver;
            this.description = description;
        }

        public void Resolve(SerializedObject obj)
        {
            m_Resolver.Invoke(obj);
        }

        public string name { get; }
        public string description { get; }
    }
}

#endif // UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
