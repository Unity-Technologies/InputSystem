using System;
using UnityEditor;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

// TODO Consider if we should ditch shape control on this one? Only if we want a square shaped stick it does matter.
//      We could call that joystick in that case.
// TODO Consider not using viewport coordinates for bounds. Instead we want a bounding rect in physical screen space.
//      This is similar but make more sense.
// TODO Consider support for 1D sticks
// TODO Consider support for d-pad.

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
        public Curve curve
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

        /// <summary>
        /// Gets or sets the on-screen stick radius in millimeters.
        /// </summary>
        /// <remarks>Physical gamepads analog sticks have mechanical displacements of 7-8 millimeters.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">If attempting to set the stick radius to a negative value.</exception>
        public float stickRadiusMillimeters
        {
            get => m_StickRadiusMillimeters;
            set
            {
                if (value < 0.0f)
                    throw new ArgumentOutOfRangeException("stickRadiusMillimeters must be greater or equal to zero.");

                m_StickRadiusMillimeters = value;
            }
        }

        public Vector2 stickCenter
        {
            get => m_Actuated ? m_StickCenter : m_NormalizedBounds.center;
        }

        private Vector2 m_StickCenter;
        private bool m_Actuated;

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
        private Curve m_Curve = Curve.Linear;

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

        private Touchscreen m_Touchscreen;
        private Detector m_Detector;

        #endregion // Properties

        protected override void OnEnable()
        {
            base.OnEnable();

            // TODO This should really be reacting to changes of control path and properties
            const float threshold = 10.0f;
            if (control is ButtonControl)
                m_Detector = new Detector<ActiveDetector>(1, new ActiveDetector());
            else if (control is StickControl)
                m_Detector = new Detector<DragDetector>(1, new DragDetector(0.0f));
            else
                throw new Exception($"Unsupported control type: {control.GetType()}");

            ChangeDevice();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        protected override void OnDisable()
        {
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
                case InputDeviceChange.Reconnected:
                    ChangeDevice();
                    break;
                case InputDeviceChange.Disconnected:
                    // We can ignore disconnect event if it is an unrelated device
                    if (device == m_Touchscreen)
                        ChangeDevice();
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
                case InputDeviceChange.ConfigurationChanged:
                case InputDeviceChange.UsageChanged:
                    OnConfigurationChanged();
                    break;
                case InputDeviceChange.HardReset:
                    // The device was reset, we currently ignore this and rely on event propagation.
                    break;
                case InputDeviceChange.SoftReset:
                    // The device was reset, we currently ignore this and rely on event propagation.
                    break;
                default:
                    Debug.LogWarning($"Unhandled device change, device={device}, change={change}.");
                    break;
            }
        }

        private void ChangeDevice() => ChangeDevice(Touchscreen.current);

        private void ChangeDevice(Touchscreen current)
        {
            // If touchscreen device have not changed, return immediately.
            if (m_Touchscreen == current)
                return;

            // Stop monitoring the previous device
            if (m_Touchscreen != null && m_Detector != null)
                InputState.RemoveChangeMonitor(m_Touchscreen, m_Detector);

            // Update current device
            m_Touchscreen = current;

            // Start monitoring the new device
            if (m_Detector != null)
            {
                m_Detector.Reset(m_NormalizedBounds, m_Shape, OnGestureEvent);

                if (current != null && m_Detector != null)
                    InputState.AddChangeMonitor(current, m_Detector);
            }
        }

        private void OnGestureEvent(in GestureEvent gestureEvent)
        {
            // TODO We cannot only use drag for this, we need to also know when it gets "activated" so we
            //      can set stick position at that point

            // For button control, we consider the whole clip region as a button area.
            if (control is ButtonControl)
            {
                var value = (gestureEvent.flags.HasFlag(GestureEvent.Flags.PhaseStart) ? 1.0f : 0.0f);
                value = m_Curve.Transform(value);
                SendValueToControl(value);
            }
            // For stick control, we map drag gesture delta as stick actuation from initial press point
            // and reset virtual stick back to zero if touch ends or is cancelled. Note that we transform
            // pixels to physical distance since on-screen controls are expected to be consistent regardless
            // of the displays physical size.
            else if (control is StickControl && gestureEvent.flags.HasFlag(GestureEvent.Flags.DragGesture))
            {
                var value = Vector2.zero;
                if (gestureEvent.flags.HasFlag(GestureEvent.Flags.PhaseStart) ||
                    gestureEvent.flags.HasFlag(GestureEvent.Flags.PhaseChange))
                {
                    var deltaMillimeters = UnitConverter.PixelsToMillimeters(gestureEvent.delta);
                    var stickRadius = Vector2.ClampMagnitude(deltaMillimeters, m_StickRadiusMillimeters);
                    value = stickRadius / m_StickRadiusMillimeters;

                    if (!m_Actuated)
                    {
                        m_Actuated = true;
                        m_StickCenter = new Vector2(gestureEvent.start.x / Display.displays[0].renderingWidth,
                            gestureEvent.start.y / Display.displays[0].renderingHeight);
                        //m_StickCenter = gestureEvent.start; // TODO Convert to normalized
                    }
                }
                else
                {
                    m_StickCenter = m_NormalizedBounds.center;
                    m_Actuated = false;
                }

                value = m_Curve.Transform(value);
                SendValueToControl(value);
            }
        }

//        private static Material mat;

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
            // TODO Handle configuration change
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

        private Rect GetScreenSpaceRect(Rect rect, int displayIndex)
        {
            var display = Display.displays[displayIndex];
            var width = display.renderingWidth;
            var height = display.renderingHeight;
            return new Rect(
                x: rect.xMin * width,
                y: rect.yMin * height,
                width: rect.width * width,
                height: rect.height * height);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1.0f, 0.0f, 0.75f);
            ScreenGizmos.DrawLine(Camera.current, new Vector2(0, 0), new Vector2(1000, 1000));
            //Gizmos.DrawLine(new Vector3(0,0,0), new Vector3(10000,0,0));
            //Gizmos.DrawCube(Vector3.zero, new Vector3(5,5,1e-3f));
            //
            // // Start drawing in screen coordinates.
            // Handles.BeginGUI();
            //
            // // Get screen space rect
            // //var r = GetScreenSpaceRect(m_NormalizedBounds, Display.activeEditorGameViewTarget);
            //
            // var r = new Rect(x: 0, y: -20, width: 500, height: 500);
            //
            // // Transform to GUI coordinates
            //
            //
            // // Draw a solid rectangle with an outline.
            // Color backgroundColor = new Color(1.0f, 0.0f, 0.0f, 0.25f);
            // Handles.DrawSolidRectangleWithOutline(r, backgroundColor, Color.white);
            //
            // // End drawing in screen coordinates.
            // Handles.EndGUI();
        }

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
