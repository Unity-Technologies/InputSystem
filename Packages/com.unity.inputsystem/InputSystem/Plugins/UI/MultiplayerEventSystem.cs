using UnityEngine;
using UnityEngine.EventSystems;

public class MultiplayerEventSystem : EventSystem
{
    [Tooltip("If set, only process mouse events for any game objects which are children of this transform.")]
    public Transform playerRootTransform;

    protected override void Update()
    {
        EventSystem originalCurrent = EventSystem.current;
        current = this; // in order to avoid reimplementing half of the EventSystem class, just temporarily assign this EventSystem to be the globally current one
        base.Update();
        current = originalCurrent;
    }
}
