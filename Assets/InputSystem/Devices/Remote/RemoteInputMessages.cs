#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;

namespace ISX
{
    internal static class RemoteInputMessages
    {
        public static readonly Guid kMsgTemplate = new Guid("34d9b47f923142ff847c0d1f8b0554d9");
        public static readonly Guid kMsgDevice = new Guid("fcd9651ded40425995dfa6aeb78f1f1c");
        public static readonly Guid kMsgEvent = new Guid("fccfec2b7369466d88502a9dd38505f4");
    }
}
#endif // DEVELOPMENT_BUILD || UNITY_EDITOR
