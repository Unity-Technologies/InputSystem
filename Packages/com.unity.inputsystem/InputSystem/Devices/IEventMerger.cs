using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem
{
    internal interface IEventMerger
    {
        bool MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr);
    }
}
