namespace UnityEngine.InputSystem.LowLevel
{
    internal interface IEventMerger
    {
        bool MergeForward(InputEventPtr currentEventPtr, InputEventPtr nextEventPtr);
    }
}
