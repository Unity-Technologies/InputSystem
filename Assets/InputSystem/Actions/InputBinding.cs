using System;

namespace ISX
{
    // Correlates an input action with one or more source controls.
    [Serializable]
    public struct InputBinding
    {
        public string action;
        public string sources;


        public string modifier;////REVIEW: allow this?
    }
}
