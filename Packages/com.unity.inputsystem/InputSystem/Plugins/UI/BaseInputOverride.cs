#if UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    internal class BaseInputOverride : BaseInput
    {
        public override string compositionString { get; }
    }
}
#endif
