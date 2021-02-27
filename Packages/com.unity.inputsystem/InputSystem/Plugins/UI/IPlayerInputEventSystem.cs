#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// An interface that allows event systems to restirct which transforms can a InputSystemUIInputModule's pointer interact with.
    /// </summary>
    public interface IPlayerInputEventSystem
    {
        bool CanPlayerInputPointAtTarget(Transform target);
    }
}
#endif
