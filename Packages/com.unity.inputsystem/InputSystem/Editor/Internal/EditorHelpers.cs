#if UNITY_EDITOR
using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class EditorHelpers
    {
        public static Action<string> SetSystemCopyBufferContents = s => EditorGUIUtility.systemCopyBuffer = s;
        public static Func<string> GetSystemCopyBufferContents = () => EditorGUIUtility.systemCopyBuffer;

        // It seems we're getting instabilities on the farm from using EditorGUIUtility.systemCopyBuffer directly in tests.
        // Ideally, we'd have a mocking library to just work around that but well, we don't. So this provides a solution
        // locally to tests.
        public class FakeSystemCopyBuffer : IDisposable
        {
            private string m_Contents;
            private readonly Action<string> m_OldSet;
            private readonly Func<string> m_OldGet;

            public FakeSystemCopyBuffer()
            {
                m_OldGet = GetSystemCopyBufferContents;
                m_OldSet = SetSystemCopyBufferContents;
                SetSystemCopyBufferContents = s => m_Contents = s;
                GetSystemCopyBufferContents = () => m_Contents;
            }

            public void Dispose()
            {
                SetSystemCopyBufferContents = m_OldSet;
                GetSystemCopyBufferContents = m_OldGet;
            }
        }
    }
}
#endif // UNITY_EDITOR
