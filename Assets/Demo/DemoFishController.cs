using UnityEngine;

////REVIEW: have more than one fish popping up?

/// <summary>
/// Controller for the big fish that pops up randomly.
/// </summary>
public class DemoFishController : MonoBehaviour
{
    public enum State
    {
        Disabled,
        WaitingToAppear,
        Appearing,
        Feeding,
        Disappearing,
    }

    public void Reset()
    {
    }
}
