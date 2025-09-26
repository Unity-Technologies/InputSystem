using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    // TODO Should maybe be split into GestureArea and OnScreenControl
    // TODO Might need to handle delta state events
    public class CustomOnScreenControl : UnityEngine.InputSystem.OnScreen.OnScreenControl
    {
        #region Properties

        /// <summary>
        /// Gets or set the geometric shape of the touch clipping area.
        /// </summary>
        private AreaShape shape
        {
            get => m_Shape;
            set
            {
                if (m_Shape == value)
                    return;

                m_Shape = value;
                OnConfigurationChanged();
            }
        }

        /// <summary>
        /// Gets or sets the geometric clipping area position.
        /// </summary>
        public Vector2 position
        {
            get => m_NormalizedBounds.position;
            set
            {
                if (m_NormalizedBounds.position.Equals(value))
                    return;

                m_NormalizedBounds.position = value;
                OnConfigurationChanged();
            }
        }

        /// <summary>
        /// Gets or sets the size of the geometric clipping area.
        /// </summary>
        public Vector2 size
        {
            get => m_NormalizedBounds.size;
            set
            {
                if (m_NormalizedBounds.size.Equals(value))
                    return;

                m_NormalizedBounds.size = value;
                OnConfigurationChanged();
            }
        }

        /// <summary>
        /// Gets or sets the bounds of the geometric clipping area.
        /// </summary>
        public Rect bounds
        {
            get => m_NormalizedBounds;
            set
            {
                if (m_NormalizedBounds.Equals(value))
                    return;

                m_NormalizedBounds = value;
                OnConfigurationChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mapping curve.
        /// </summary>
        public PredefinedCurve curve
        {
            get => m_Curve;
            set
            {
                if (m_Curve.Equals(value))
                    return;

                m_Curve = value;
                OnConfigurationChanged();
            }
        }

        [Header("Bounds")]
        [Tooltip("The geometric clipping area shape")]
        [SerializeField]
        private AreaShape m_Shape = AreaShape.Rectangle;

        [Tooltip("The geometric clipping area shape")]
        [SerializeField] private Rect m_NormalizedBounds;

        [Header("Control")]
        [Tooltip("The output control path")]
        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        [Tooltip("Response curve to be applied to the control value")]
        [SerializeField]
        private PredefinedCurve m_Curve = PredefinedCurve.Linear;

        [Tooltip("The stick radius in millimeters when used as an on-screen stick. " +
            "This defaults to 7.2 millimeters which is similar to the mechanical stick displacement of popular " +
            "gamepads.")]
        [SerializeField]
        private float m_StickRadiusMillimeters = 7.2f;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        #endregion // Properties

        private Touchscreen m_Touchscreen;

        protected override void OnEnable()
        {
            base.OnEnable();

            // TODO This should really be reacting to changes of control path and properties
            const float threshold = 10.0f;
            if (control is ButtonControl)
                m_ChangeMonitor = new ChangeMonitor<ActiveDetector>(1, new ActiveDetector());
            else if (control is StickControl)
                m_ChangeMonitor = new ChangeMonitor<DragDetector>(1, new DragDetector(threshold));
            else
                throw new Exception($"Unsupported control type: {control.GetType()}");

            ChangeDevice();

            InputSystem.onDeviceChange += OnDeviceChange;
            //InputSystem.onEvent += OnEvent;
        }

        protected override void OnDisable()
        {
            //InputSystem.onEvent -= OnEvent;
            InputSystem.onDeviceChange -= OnDeviceChange;

            base.OnDisable();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    // We can ignore added devices if its an unrelated device.
                    if (device is Touchscreen)
                        ChangeDevice();
                    break;
                case InputDeviceChange.Removed:
                    // We can ignore removed device if its not the device we are using.
                    if (device == m_Touchscreen)
                        ChangeDevice();
                    break;
                case InputDeviceChange.ConfigurationChanged:
                    // The device configuration changed, we currently ignore this.
                    break;
                case InputDeviceChange.Enabled:
                    // A touchscreen got enabled so we attempt to change device
                    if (device is Touchscreen)
                        ChangeDevice();
                    break;
                case InputDeviceChange.Disabled:
                    // We can ignore disable events if it is an unrelated device
                    if (device == m_Touchscreen)
                        ChangeDevice();
                    break;
                case InputDeviceChange.UsageChanged:
                    // The device usages changed, we currently ignore this.
                    break;
                case InputDeviceChange.HardReset:
                    // The device was reset, we currently ignore this.
                    break;
                case InputDeviceChange.SoftReset:
                    // The device was reset, we currently ignore this.
                    break;
                case InputDeviceChange.Reconnected:
                    ChangeDevice();
                    break;
                case InputDeviceChange.Disconnected:
                    // We can ignore disconnect event if it is an unrelated device
                    if (device == m_Touchscreen)
                        ChangeDevice();
                    break;
                default:
                    Debug.LogWarning($"Unhandled device change, device={device}, change={change}.");
                    break;
            }
        }

        private void ChangeDevice() => ChangeDevice(Touchscreen.current);

        private ChangeMonitor m_ChangeMonitor;

        private void ChangeDevice(Touchscreen current)
        {
            // If touchscreen device have not changed, return immediately.
            if (m_Touchscreen == current)
                return;

            // Stop monitoring the previous device
            if (m_Touchscreen != null && m_ChangeMonitor != null)
                InputState.RemoveChangeMonitor(m_Touchscreen, m_ChangeMonitor);

            // Update current device
            m_Touchscreen = current;

            // Start monitoring the new device
            if (m_ChangeMonitor != null)
            {
                m_ChangeMonitor.Reset(m_NormalizedBounds, m_Shape, OnGestureEvent);

                if (current != null && m_ChangeMonitor != null)
                    InputState.AddChangeMonitor(current, m_ChangeMonitor);
            }
        }

        private void OnGestureEvent(in GestureEvent gestureEvent)
        {
            // TODO Button control should be whether touch is inside area

            // For button control, we consider the whole clip region as a button area.
            if (control is ButtonControl)
            {
                var value = 0.0f;
                if (gestureEvent.flags.HasFlag(GestureFlags.PhaseStart))
                {
                    value = 1.0f;
                }

                value = CurveExtensions.Transform(value, m_Curve);

                SendValueToControl(value);
            }
            // For stick control, we map drag gesture delta as stick actuation from initial press point
            // and reset virtual stick back to zero if touch ends or is cancelled. Note that we transform
            // pixels to physical distance since on-screen controls are expected to be consistent regardless
            // of the displays physical size.
            else if (control is StickControl && gestureEvent.flags.HasFlag(GestureFlags.DragGesture))
            {
                var value = Vector2.zero;
                if (gestureEvent.flags.HasFlag(GestureFlags.PhaseStart) ||
                    gestureEvent.flags.HasFlag(GestureFlags.PhaseChange))
                {
                    // A DualSense has a mechanical displacement of ~7.4 mm.
                    // A DualShock has a mechanical displacement of ~6.9 mm.

                    const float kMillimetersPerInch = 25.4f;
                    var deltaPx = gestureEvent.delta;
                    var dpi = Screen.dpi;
                    var deltaInches = new Vector2(deltaPx.x / dpi, deltaPx.y / dpi);
                    var deltaMillimeters = deltaInches * kMillimetersPerInch;
                    var stickRadius = Vector2.ClampMagnitude(deltaMillimeters, m_StickRadiusMillimeters);
                    value = stickRadius / m_StickRadiusMillimeters;
                    Debug.Log($"{deltaMillimeters} mm, stick={value}");
                }

                // Transform to polar form before we apply curve and then inverse transform.
                value = CurveExtensions.Transform(value, m_Curve);

                SendValueToControl(value);
            }
        }

        // private void OnEvent(InputEventPtr eventPtr, InputDevice device)
        // {
        //     /*Touchscreen.current.GetStatePtrFromStateEvent()
        //     if (m_Touchscreen != null && eventPtr.type == StateEvent.Type)
        //     {
        //         TouchState state;
        //         device.GetStatePtrFromStateEvent(eventPtr);
        //     }*/
        // }

        private static Material mat;

        // public void OnRenderObject()
        // {
        //     if (!mat)
        //     {
        //         Shader shader = Shader.Find("Hidden/Internal-Colored");
        //         mat = new Material(shader);
        //         mat.hideFlags = HideFlags.HideAndDontSave;
        //         // Turn on alpha blending
        //         mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        //         mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        //         // Turn backface culling off
        //         mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        //         // Turn off depth writes
        //         mat.SetInt("_ZWrite", 0);
        //         return;
        //     }
        //     GL.PushMatrix();
        //     mat.SetPass(0);
        //     GL.LoadOrtho();
        //
        //     GL.Begin(GL.QUADS);
        //
        //     GL.Color(new Color(1.0f, 0.0f, 0.0f, 0.5f));
        //     GL.Vertex3(m_NormalizedBounds.xMin, m_NormalizedBounds.yMin, 0);
        //     GL.Vertex3(m_NormalizedBounds.xMin, m_NormalizedBounds.yMax, 0);
        //     GL.Vertex3(m_NormalizedBounds.xMax, m_NormalizedBounds.yMax, 0);
        //     GL.Vertex3(m_NormalizedBounds.xMax, m_NormalizedBounds.yMin, 0);
        //
        //     GL.End();
        //     GL.PopMatrix();
        // }

        private void OnConfigurationChanged()
        {
        }

        // TODO Fix SendValueToControl instead
        // private void ChangeValueOfControl<TValue>(TValue value) where TValue : struct
        // {
        //     var x = base.control;
        //     if (x == null)
        //         return;
        //     if (x is not InputControl<TValue>)
        //     {
        //         throw new ArgumentException($"The control path {controlPath} yields a control of type "+
        //                                     "{m_Control.GetType().Name} which is not an InputControl with value type " +
        //                                     "{typeof(TValue).Name}", nameof(value));
        //     }
        //
        //     m
        // }

        // private void Foo(in Camera camera, in Vector3 position)
        // {
        //     var orthoSize = camera.orthographicSize;
        //     var horizontalExtent = orthoSize * camera.aspect;
        //     return (position.x >= -horizontalExtent - margin) &&
        //            (position.x <= horizontalExtent + margin) &&
        //            (position.y >= -orthoSize - margin) &&
        //            (position.y <= orthoSize + margin);
        // }

        /*private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1.0f, 0.0f, 0.75f);
            Gizmos.DrawLine(new Vector3(0,0,0), new Vector3(100,0,0));
        }*/

        void OnDrawGizmosSelected()
        {
            //Gizmos.color = new Color(1f, 1.0f, 0.0f, 0.75f);

            // Convert the local coordinate values into world
            // coordinates for the matrix transformation.
            //Gizmos.matrix = transform.localToWorldMatrix;
            //Gizmos.DrawCube(Vector3.zero, Vector3.one);

            //Gizmos.DrawWireCube(new Vector3(m_Bounds.center.x, m_Bounds.center.y, 0.01f), new Vector3(m_Bounds.size.x, m_Bounds.size.y, 0.01f));
        }
    }

    //#if UNITY_EDITOR


    //[CustomEditor(typeof(CustomOnScreenControl))]
    // internal class CustomOnScreenControlEditor : UnityEditor.Editor
    // {
    //     public void OnEnable()
    //     {
    //         m_ControlPathInternal = serializedObject.FindProperty("m_ControlPath");
    //         m_Bounds = serializedObject.FindProperty("m_NormalizedBounds");
    //     }
    //
    //     public override void OnInspectorGUI()
    //     {
    //         EditorGUI.BeginChangeCheck();
    //
    //         Debug.Log(m_Bounds);
    //         //EditorGUILayout.PropertyField(m_Bounds);
    //         EditorGUILayout.PropertyField(m_ControlPathInternal);
    //
    //         if (EditorGUI.EndChangeCheck())
    //             serializedObject.ApplyModifiedProperties();
    //     }
    //
    //     private SerializedProperty m_ControlPathInternal;
    //     private SerializedProperty m_Bounds;
    // }

    //#endif // UNITY_EDITOR
}
