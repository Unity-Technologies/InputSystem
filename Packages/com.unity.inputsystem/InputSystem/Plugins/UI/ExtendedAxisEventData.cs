#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    // AxisEventData has no ToString. But that's the only thing we add so keeping
    // it internal.
    internal class ExtendedAxisEventData : AxisEventData
    {
        public ExtendedAxisEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }

        public override string ToString()
        {
            return $"MoveDir: {moveDir}\nMoveVector: {moveVector}";
        }
    }
}
#endif
