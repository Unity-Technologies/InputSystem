using System;

namespace ISX.Remote
{
    // Mirrors all input data over the local EditorConnection.
    internal class RemoteInputNetworkTransportToEditor
    {
        //find better place for these
        public static readonly Guid kMsgDevice = new Guid("fcd9651ded40425995dfa6aeb78f1f1c");
        public static readonly Guid kMsgEvent = new Guid("fccfec2b7369466d88502a9dd38505f4");
        public static readonly Guid kGuid = new Guid("34d9b47f923142ff847c0d1f8b0554d9");
    }
}
