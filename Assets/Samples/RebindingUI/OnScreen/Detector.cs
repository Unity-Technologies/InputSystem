using System;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    // Note that gesture event handler delegate takes an 'in' reference when 'ref readonly' would be preferrable.
    // The reason for this is that this construct isn't available until C# 12.0.

    /// <summary>
    /// Base class for gesture detectors.
    /// </summary>
    abstract class Detector : ITouchMonitor
    {
        public delegate void GestureEventHandler(in GestureEvent gestureEvent);

        protected Detector(int maxTouches)
        {
            m_Touches = new TouchState[maxTouches];
            Reset(Rect.zero, AreaShape.Rectangle, null);
        }

        public virtual void Reset(Rect bounds, AreaShape shape, GestureEventHandler handler)
        {
            m_Handler = handler;
            m_Bounds = bounds;
            m_Shape = shape;
            m_Count = 0;
        }

        protected bool Contains(in TouchState touch)
        {
            // TODO Consider transforming touch into normalized instead? E.g. touch / renderingSize

            var display = Display.displays[touch.displayIndex];
            var absoluteBounds = new Rect(
                x: m_Bounds.xMin * display.renderingWidth,
                y: m_Bounds.yMin * display.renderingHeight,
                width: m_Bounds.width * display.renderingWidth,
                height: m_Bounds.height * display.renderingHeight);

            switch (m_Shape)
            {
                case AreaShape.Rectangle:
                {
                    return absoluteBounds.Contains(touch.position);
                }
                case AreaShape.Ellipse:
                {
                    var delta = touch.position - absoluteBounds.center;
                    var radius = absoluteBounds.size / 2;
                    var value = (delta.x * delta.x) / (radius.x * radius.x) + (delta.y * delta.y) / (radius.y * radius.y);
                    return value <= 1f;
                }
            }

            return false;
        }

        protected int FindTouch(int touchId)
        {
            for (var i = 0; i < m_Count; ++i)
                if (m_Touches[i].touchId == touchId)
                    return i;
            return -1;
        }

        public void FireEvent(in GestureEvent gestureEvent)
        {
            m_Handler.Invoke(in gestureEvent);
        }

        public ref readonly TouchState this[int index] => ref m_Touches[index]; // TODO Make sure no defensive copy is created

        //set => SetValue(key, value);
        protected Rect m_Bounds;
        protected AreaShape m_Shape;
        private GestureEventHandler m_Handler;
        protected readonly TouchState[] m_Touches;
        protected int m_Count;

        public abstract void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr,
            long monitorIndex);

        public abstract void NotifyTimerExpired(InputControl control, double time, long monitorIndex,
            int timerIndex);
    }

    // TODO This class is doing way too much error checking. Underlying implementation should be correct but
    // there seem to be some kind of bug resulting in duplicate callbacks.
    internal class Detector<TProcessor> : Detector, ITouchMonitor
        where TProcessor : ITouchProcessor
    {
        private TProcessor m_Processor;

        public Detector(int maxTouches, TProcessor processor)
            : base(maxTouches)
        {
            m_Processor = processor;
        }

        /*public static Detector<TProcessor> Create(int maxTouches, TProcessor processor)
        {
            return new Detector<TProcessor>(maxTouches, processor);
        }*/

        public override void Reset(Rect bounds, AreaShape shape, Detector.GestureEventHandler handler)
        {
            // Make sure we cancel any pending touches. This way processor do not need a reset method.
            for (var i = m_Count; i != 0; --i)
                m_Processor.OnTouchCanceled(this, m_Touches, m_Count, m_Count - 1);

            // Reset base
            base.Reset(bounds, shape, handler);
        }

        public override void NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
        {
            // Ignore state change if it is not a touch state
            if (!OnScreenUnsafeHelpers.TryGetStateCopy<TouchState>(eventPtr, TouchState.Format, out var state))
                return;

            // Intentionally left commented to aid debugging
            //Debug.Log($"monitorIndex: {monitorIndex}, id: {state.touchId}, phase: {state.phase}, position: {state.position}");

            // Delegate touch phase events
            int index;
            switch (state.phase)
            {
                case TouchPhase.Began:
                    // Only add the point for tracking if we do not violate max point constraint.
                    if (FindTouch(state.touchId) < 0 && m_Count < m_Touches.Length && Contains(state))
                    {
                        index = m_Count++;
                        m_Touches[index] = state;

                        m_Processor.OnTouchBegin(this, m_Touches, m_Count, index);
                    }
                    break;

                case TouchPhase.Ended:
                    index = FindTouch(state.touchId);
                    if (index < 0)
                        break;

                    m_Processor.OnTouchEnd(this, m_Touches, m_Count, index);

                    // Remove point
                    if (index != m_Count - 1)
                    {
                        Array.Copy(m_Touches, index + 1,
                            m_Touches, index, m_Count - index - 1);
                    }
                    --m_Count;
                    break;

                case TouchPhase.Canceled:
                    index = FindTouch(state.touchId);
                    if (index < 0)
                        break;

                    m_Processor.OnTouchCanceled(this, m_Touches, m_Count, index);

                    // TODO Need to remove or will we get end?!
                    break;

                case TouchPhase.Moved:
                    index = FindTouch(state.touchId);
                    if (index < 0)
                    {
                        if (m_Count < m_Touches.Length && Contains(state))
                        {
                            // TODO Handle as an addition
                        }

                        break; // Unexpected or not tracked by this processor
                    }

                    m_Touches[index] = state;

                    // TODO Handle capture

                    m_Processor.OnTouchMoved(this, m_Touches, m_Count, index);
                    break;

                case TouchPhase.Stationary: // Not used by touchscreen
                case TouchPhase.None:       // Default initialized (no meaning)
                default:
                    break;
            }
        }

        public override void NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex) {}
    }
}
