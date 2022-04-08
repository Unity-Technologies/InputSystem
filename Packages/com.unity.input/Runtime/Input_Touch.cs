using System;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

namespace UnityEngine
{
    public static partial class Input
    {
        private static TouchPhase ToLegacy(InputSystem.TouchPhase phase)
        {
            switch (phase)
            {
                case InputSystem.TouchPhase.Began: return TouchPhase.Began;
                case InputSystem.TouchPhase.Moved: return TouchPhase.Moved;
                case InputSystem.TouchPhase.Ended: return TouchPhase.Ended;
                case InputSystem.TouchPhase.Canceled: return TouchPhase.Canceled;
                case InputSystem.TouchPhase.Stationary: return TouchPhase.Stationary;
                    // InputSystem.TouchPhase.None  // These are shouldn't be seen
            }
            throw new ArgumentOutOfRangeException(nameof(phase), $"Not expected phase value: {phase}");
        }

        private static (float radius, float variance) SplitRadiusValues(Vector2 vRadius)
        {
            var x = vRadius.x;
            var y = vRadius.y;
            var max = Mathf.Max(x, y);
            var min = Mathf.Min(x, y);
            var variance = (max - min) / 2;
            var radius = min + variance;

            return (radius, variance);
        }

        private static (float altitudeAngle, float azimuthAngle) TiltValues(Vector2 vTilt)
        {
            var azimuthAngle = Mathf.Acos(vTilt.x);
            var altitudeAngle = Mathf.Acos(vTilt.y);
            return (altitudeAngle, azimuthAngle);
        }

        private static Touch ToTouch(EnhancedTouch.Touch input)
        {
            Touch touch = new Touch();
            touch.fingerId = input.touchId;
            touch.position = input.screenPosition;
            touch.rawPosition = input.startScreenPosition;
            touch.deltaPosition = input.delta;
            touch.deltaTime = (input.history.Count == 0) ? 0.0f : (float)(input.time - input.history[0].time);
            touch.tapCount = input.tapCount;
            touch.phase = ToLegacy(input.phase);
            touch.pressure = input.pressure;
            touch.maximumPossiblePressure = 1.0f;
            touch.type = TouchType.Direct; // TODO: What about indirect and Stylus
            touch.altitudeAngle = 0;       // TODO: Not available in TouchControl
            touch.azimuthAngle = 0;        // TODO: Not available in TouchControl

            var(radius, variance) = SplitRadiusValues(input.radius);
            touch.radius = radius;
            touch.radiusVariance = variance;

            return touch;
        }

        public static Touch GetTouch(int index)
        {
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            if (index >= touchCount)
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid touch index.");

            return ToTouch(EnhancedTouch.Touch.activeTouches[index]);
        }

        public static int touchCount
        {
            get => EnhancedTouch.Touch.activeTouches.Count;
        }
        public static Touch[] touches
        {
            get
            {
                if (!EnhancedTouchSupport.enabled)
                    EnhancedTouchSupport.Enable();

                var convertedTouches = from t in EnhancedTouch.Touch.activeTouches
                    select ToTouch(t);

                return convertedTouches.ToArray();
            }
        }
        public static bool touchSupported => Touchscreen.current != null;
        public static bool multiTouchEnabled
        {
            get => true;
            set
            {
                if (!value)
                    throw new NotSupportedException("Disabling multi-touch is not supported");
            }
        }
    }
}
