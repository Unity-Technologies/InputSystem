////REVIEW: move everything from InputControlExtensions here?

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// Various useful extension methods.
    /// </summary>
    public static class InputExtensions
    {
        public static bool IsEndedOrCancelled(this TouchPhase phase)
        {
            return phase == TouchPhase.Cancelled || phase == TouchPhase.Ended;
        }

        public static bool IsActive(this TouchPhase phase)
        {
            switch (phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    return true;
            }
            return false;
        }
    }
}
