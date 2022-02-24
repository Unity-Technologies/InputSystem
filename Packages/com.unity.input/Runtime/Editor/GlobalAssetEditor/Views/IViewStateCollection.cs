using System.Collections;

namespace UnityEngine.InputSystem.Editor
{
    internal interface IViewStateCollection : IEnumerable
    {
        bool SequenceEqual(IViewStateCollection other);
    }
}
