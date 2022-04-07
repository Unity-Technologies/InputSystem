using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

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
                    //InputSystem.TouchPhase.None  // These are filtered out and shouldn't be seen
            }
            throw new ArgumentOutOfRangeException(nameof(phase), $"Not expected phase value: {phase}");
        }

        private static (float radius, float variance) RadiusValues(Vector2 vRadius)
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

        private static Touch ToTouch(InputSystem.Controls.TouchControl control)
        {
            Touch touch = new Touch();
            touch.fingerId = control.touchId.ReadValue();
            touch.position = control.position.ReadValue();
            touch.rawPosition = control.startPosition.ReadValue();
            touch.deltaPosition = control.delta.ReadValue();
            //touch.deltaTime = TODO: How to implement this? Device has lastUpdateTime() but only for single touch
            touch.tapCount = control.tapCount.ReadValue();
            touch.phase = ToLegacy(control.phase.ReadValue());
            touch.pressure = control.pressure.ReadValue();
            touch.maximumPossiblePressure = 1.0f;
            touch.type = control.indirectTouch.IsPressed() ? TouchType.Indirect : TouchType.Direct;
            touch.altitudeAngle = 0;  // TODO: Not available in TouchControl
            touch.azimuthAngle = 0;   // TODO: Not available in TouchControl

            var(radius, variance) = RadiusValues(control.radius.ReadValue());
            touch.radius = radius;
            touch.radiusVariance = variance;

            return touch;
        }

        public static Touch GetTouch(int index)
        {
            if (index >= touchCount)
                throw new IndexOutOfRangeException();

            return ToTouch(Touchscreen.current.touches[index]);
        }

        public static int touchCount
        {
            get
            {
                var count = 0;
                if (Touchscreen.current != null)
                {
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        if (touch.phase.ReadValue() != InputSystem.TouchPhase.None)
                            count++;
                    }
                }

                return count;
            }
        }
        public static Touch[] touches
        {
            get
            {
                var result = new List<Touch>();
                if (Touchscreen.current != null)
                {
                    var convertedTouches = from t in Touchscreen.current.touches
                        where t.phase.ReadValue() != InputSystem.TouchPhase.None
                        select ToTouch(t);
                    result.AddRange(convertedTouches);
                }
                return result.ToArray();
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
