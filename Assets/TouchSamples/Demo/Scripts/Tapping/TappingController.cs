using InputSamples.Gestures;
using UnityEngine;

namespace InputSamples.Demo.Tapping
{
    /// <summary>
    /// Controller that responds to taps.
    /// </summary>
    public class TappingController : MonoBehaviour
    {
        /// <summary>
        /// Reference to gesture input manager.
        /// </summary>
        [SerializeField]
        private GestureController gestureController;

        private int castLayerMask;
        private Camera cachedCamera;

        protected virtual void Awake()
        {
            castLayerMask = LayerMask.GetMask("Default");
            cachedCamera = Camera.main;
        }

        protected virtual void OnEnable()
        {
            gestureController.Tapped += OnTapped;
        }

        private void OnDisable()
        {
            gestureController.Tapped -= OnTapped;
        }

        private void OnTapped(TapInput input)
        {
            // Try find tapped target
            Vector2 worldCurrent = cachedCamera.ScreenToWorldPoint(input.ReleasePosition);

            // Try find target to grab for this touch
            Collider2D collider = Physics2D.OverlapPoint(worldCurrent, castLayerMask);
            if (collider != null)
            {
                // Try find a TapTarget object on this hit component
                var target = collider.GetComponent<TapTarget>();
                if (target != null)
                {
                    // Remember that this swipe went over this object
                    target.Hit((float)input.TimeStamp);
                }
            }
        }
    }
}
