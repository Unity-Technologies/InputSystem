using System;

namespace UnityEngine.Experimental.Input
{
    [Flags]
    public enum InputUpdateType
    {
        None = 0,
        Dynamic = 1 << 0,
        Fixed = 1 << 1,
        BeforeRender = 1 << 2,
        Editor = 1 << 3
    }

    internal static class InputUpdate
    {
        public static InputUpdateType lastUpdateType;
        public static uint dynamicUpdateCount;
        public static uint fixedUpdateCount;

        [Serializable]
        public struct SerializedState
        {
            public InputUpdateType lastUpdateType;
            public uint dynamicUpdateCount;
            public uint fixedUpdateCount;
        }

        public static SerializedState Save()
        {
            return new SerializedState
            {
                lastUpdateType = lastUpdateType,
                dynamicUpdateCount = dynamicUpdateCount,
                fixedUpdateCount = fixedUpdateCount,
            };
        }

        public static void Restore(SerializedState state)
        {
            lastUpdateType = state.lastUpdateType;
            dynamicUpdateCount = state.dynamicUpdateCount;
            fixedUpdateCount = state.fixedUpdateCount;
        }
    }
}
