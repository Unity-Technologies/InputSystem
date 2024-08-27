#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.InputSystem.Editor;
#endif
////TODO: custom icon for OnScreenStick component

namespace UnityEngine.InputSystem.OnScreen
{
    /// <summary>
    /// A stick control displayed on screen and moved around by touch or other pointer
    /// input.
    /// </summary>
    /// <remarks>
    /// The <see cref="OnScreenStick"/> works by simulating events from the device specified in the <see cref="OnScreenControl.controlPath"/>
    /// property. Some parts of the Input System, such as the <see cref="PlayerInput"/> component, can be set up to
    /// auto-switch to a new device when input from them is detected. When a device is switched, any currently running
    /// inputs from the previously active device are cancelled. In the case of <see cref="OnScreenStick"/>, this can mean that the
    /// <see cref="IPointerUpHandler.OnPointerUp"/> method will be called and the stick will jump back to center, even though
    /// the pointer input has not physically been released.
    ///
    /// To avoid this situation, set the <see cref="useIsolatedInputActions"/> property to true. This will create a set of local
    /// Input Actions to drive the stick that are not cancelled when device switching occurs.
    /// </remarks>
    [AddComponentMenu("Input/On-Screen Stick")]
    [HelpURL(InputSystem.kDocUrl + "/manual/OnScreen.html#on-screen-sticks")]
    public class OnScreenStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private const string kDynamicOriginClickable = "DynamicOriginClickable";

        /// <summary>
        /// Callback to handle OnPointerDown UI events.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_UseIsolatedInputActions)
                return;

            if (eventData == null)
                throw new System.ArgumentNullException(nameof(eventData));

            BeginInteraction(eventData.position, eventData.pressEventCamera);
        }

        /// <summary>
        /// Callback to handle OnDrag UI events.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (m_UseIsolatedInputActions)
                return;

            if (eventData == null)
                throw new System.ArgumentNullException(nameof(eventData));

            MoveStick(eventData.position, eventData.pressEventCamera);
        }

        /// <summary>
        /// Callback to handle OnPointerUp UI events.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_UseIsolatedInputActions)
                return;

            EndInteraction();
        }

        private void Start()
        {
            if (m_UseIsolatedInputActions)
            {
                // avoid allocations every time the pointer down event fires by allocating these here
                // and re-using them
                m_RaycastResults = new List<RaycastResult>();
                m_PointerEventData = new PointerEventData(EventSystem.current);

                // if the pointer actions have no bindings (the default), add some
                if (m_PointerDownAction == null || m_PointerDownAction.bindings.Count == 0)
                {
                    if (m_PointerDownAction == null)
                        m_PointerDownAction = new InputAction();

                    m_PointerDownAction.AddBinding("<Mouse>/leftButton");
                    m_PointerDownAction.AddBinding("<Pen>/tip");
                    m_PointerDownAction.AddBinding("<Touchscreen>/touch*/press");
                    m_PointerDownAction.AddBinding("<XRController>/trigger");
                }

                if (m_PointerMoveAction == null || m_PointerMoveAction.bindings.Count == 0)
                {
                    if (m_PointerMoveAction == null)
                        m_PointerMoveAction = new InputAction();

                    m_PointerMoveAction.AddBinding("<Mouse>/position");
                    m_PointerMoveAction.AddBinding("<Pen>/position");
                    m_PointerMoveAction.AddBinding("<Touchscreen>/touch*/position");
                }

                m_PointerDownAction.started += OnPointerDown;
                m_PointerDownAction.canceled += OnPointerUp;
                m_PointerDownAction.Enable();
                m_PointerMoveAction.Enable();
            }

            // Unable to setup elements according to settings if a RectTransform is not available (ISXB-915, ISXB-916).
            if (!(transform is RectTransform))
                return;

            m_StartPos = ((RectTransform)transform).anchoredPosition;

            if (m_Behaviour != Behaviour.ExactPositionWithDynamicOrigin) return;

            m_PointerDownPos = m_StartPos;

            var dynamicOrigin = new GameObject(kDynamicOriginClickable, typeof(Image));
            dynamicOrigin.transform.SetParent(transform);
            var image = dynamicOrigin.GetComponent<Image>();
            image.color = new Color(1, 1, 1, 0);
            var rectTransform = (RectTransform)dynamicOrigin.transform;
            rectTransform.sizeDelta = new Vector2(m_DynamicOriginRange * 2, m_DynamicOriginRange * 2);
            rectTransform.localScale = new Vector3(1, 1, 0);
            rectTransform.anchoredPosition3D = Vector3.zero;

            image.sprite = SpriteUtilities.CreateCircleSprite(16, new Color32(255, 255, 255, 255));
            image.alphaHitTestMinimumThreshold = 0.5f;
        }

        private void BeginInteraction(Vector2 pointerPosition, Camera uiCamera)
        {
            var canvasRectTransform = UGUIOnScreenControlUtils.GetCanvasRectTransform(transform);
            if (canvasRectTransform == null)
            {
                Debug.LogError(GetWarningMessage());
                return;
            }

            switch (m_Behaviour)
            {
                case Behaviour.RelativePositionWithStaticOrigin:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, pointerPosition, uiCamera, out m_PointerDownPos);
                    break;
                case Behaviour.ExactPositionWithStaticOrigin:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, pointerPosition, uiCamera, out m_PointerDownPos);
                    MoveStick(pointerPosition, uiCamera);
                    break;
                case Behaviour.ExactPositionWithDynamicOrigin:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, pointerPosition, uiCamera, out var pointerDown);
                    m_PointerDownPos = ((RectTransform)transform).anchoredPosition = pointerDown;
                    break;
            }
        }

        private void MoveStick(Vector2 pointerPosition, Camera uiCamera)
        {
            var canvasRectTransform = UGUIOnScreenControlUtils.GetCanvasRectTransform(transform);
            if (canvasRectTransform == null)
            {
                Debug.LogError(GetWarningMessage());
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, pointerPosition, uiCamera, out var position);
            var delta = position - m_PointerDownPos;

            switch (m_Behaviour)
            {
                case Behaviour.RelativePositionWithStaticOrigin:
                    delta = Vector2.ClampMagnitude(delta, movementRange);
                    ((RectTransform)transform).anchoredPosition = (Vector2)m_StartPos + delta;
                    break;

                case Behaviour.ExactPositionWithStaticOrigin:
                    delta = position - (Vector2)m_StartPos;
                    delta = Vector2.ClampMagnitude(delta, movementRange);
                    ((RectTransform)transform).anchoredPosition = (Vector2)m_StartPos + delta;
                    break;

                case Behaviour.ExactPositionWithDynamicOrigin:
                    delta = Vector2.ClampMagnitude(delta, movementRange);
                    ((RectTransform)transform).anchoredPosition = m_PointerDownPos + delta;
                    break;
            }

            var newPos = new Vector2(delta.x / movementRange, delta.y / movementRange);
            SendValueToControl(newPos);
        }

        private void EndInteraction()
        {
            ((RectTransform)transform).anchoredPosition = m_PointerDownPos = m_StartPos;
            SendValueToControl(Vector2.zero);
        }

        private void OnPointerDown(InputAction.CallbackContext ctx)
        {
            Debug.Assert(EventSystem.current != null);

            var screenPosition = Vector2.zero;
            if (ctx.control?.device is Pointer pointer)
                screenPosition = pointer.position.ReadValue();

            m_PointerEventData.position = screenPosition;
            EventSystem.current.RaycastAll(m_PointerEventData, m_RaycastResults);
            if (m_RaycastResults.Count == 0)
                return;

            var stickSelected = false;
            foreach (var result in m_RaycastResults)
            {
                if (result.gameObject != gameObject) continue;

                stickSelected = true;
                break;
            }

            if (!stickSelected)
                return;

            BeginInteraction(screenPosition, GetCameraFromCanvas());
            m_PointerMoveAction.performed += OnPointerMove;
        }

        private void OnPointerMove(InputAction.CallbackContext ctx)
        {
            // only pointer devices are allowed
            Debug.Assert(ctx.control?.device is Pointer);

            var screenPosition = ((Pointer)ctx.control.device).position.ReadValue();

            MoveStick(screenPosition, GetCameraFromCanvas());
        }

        private void OnPointerUp(InputAction.CallbackContext ctx)
        {
            EndInteraction();
            m_PointerMoveAction.performed -= OnPointerMove;
        }

        private Camera GetCameraFromCanvas()
        {
            var canvas = GetComponentInParent<Canvas>();
            var renderMode = canvas?.renderMode;
            if (renderMode == RenderMode.ScreenSpaceOverlay
                || (renderMode == RenderMode.ScreenSpaceCamera && canvas?.worldCamera == null))
                return null;

            return canvas?.worldCamera ?? Camera.main;
        }

        private void OnDrawGizmosSelected()
        {
            // This will not produce meaningful results unless we have a rect transform (ISXB-915, ISXB-916).
            var parentRectTransform = transform.parent as RectTransform;
            if (parentRectTransform == null)
                return;

            Gizmos.matrix = parentRectTransform.localToWorldMatrix;

            var startPos = parentRectTransform.anchoredPosition;
            if (Application.isPlaying)
                startPos = m_StartPos;

            Gizmos.color = new Color32(84, 173, 219, 255);

            var center = startPos;
            if (Application.isPlaying && m_Behaviour == Behaviour.ExactPositionWithDynamicOrigin)
                center = m_PointerDownPos;

            DrawGizmoCircle(center, m_MovementRange);

            if (m_Behaviour != Behaviour.ExactPositionWithDynamicOrigin) return;

            Gizmos.color = new Color32(158, 84, 219, 255);
            DrawGizmoCircle(startPos, m_DynamicOriginRange);
        }

        private void DrawGizmoCircle(Vector2 center, float radius)
        {
            for (var i = 0; i < 32; i++)
            {
                var radians = i / 32f * Mathf.PI * 2;
                var nextRadian = (i + 1) / 32f * Mathf.PI * 2;
                Gizmos.DrawLine(
                    new Vector3(center.x + Mathf.Cos(radians) * radius, center.y + Mathf.Sin(radians) * radius, 0),
                    new Vector3(center.x + Mathf.Cos(nextRadian) * radius, center.y + Mathf.Sin(nextRadian) * radius, 0));
            }
        }

        private void UpdateDynamicOriginClickableArea()
        {
            var dynamicOriginTransform = transform.Find(kDynamicOriginClickable);
            if (dynamicOriginTransform)
            {
                var rectTransform = (RectTransform)dynamicOriginTransform;
                rectTransform.sizeDelta = new Vector2(m_DynamicOriginRange * 2, m_DynamicOriginRange * 2);
            }
        }

        /// <summary>
        /// The distance from the onscreen control's center of origin, around which the control can move.
        /// </summary>
        public float movementRange
        {
            get => m_MovementRange;
            set => m_MovementRange = value;
        }

        /// <summary>
        /// Defines the circular region where the onscreen control may have it's origin placed.
        /// </summary>
        /// <remarks>
        /// This only applies if <see cref="behaviour"/> is set to <see cref="Behaviour.ExactPositionWithDynamicOrigin"/>.
        /// When the first press is within this region, then the control will appear at that position and have it's origin of motion placed there.
        /// Otherwise, if pressed outside of this region the control will ignore it.
        /// This property defines the radius of the circular region. The center point being defined by the component position in the scene.
        /// </remarks>
        public float dynamicOriginRange
        {
            get => m_DynamicOriginRange;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m_DynamicOriginRange != value)
                {
                    m_DynamicOriginRange = value;
                    UpdateDynamicOriginClickableArea();
                }
            }
        }

        /// <summary>
        /// Prevents stick interactions from getting cancelled due to device switching.
        /// </summary>
        /// <remarks>
        /// This property is useful for scenarios where the active device switches automatically
        /// based on the most recently actuated device. A common situation where this happens is
        /// when using a <see cref="PlayerInput"/> component with Auto-switch set to true. Imagine
        /// a mobile game where an on-screen stick simulates the left stick of a gamepad device.
        /// When the on-screen stick is moved, the Input System will see an input event from a gamepad
        /// and switch the active device to it. This causes any active actions to be cancelled, including
        /// the pointer action driving the on screen stick, which results in the stick jumping back to
        /// the center as though it had been released.
        ///
        /// In isolated mode, the actions driving the stick are not cancelled because they are
        /// unique Input Action instances that don't share state with any others.
        /// </remarks>
        public bool useIsolatedInputActions
        {
            get => m_UseIsolatedInputActions;
            set => m_UseIsolatedInputActions = value;
        }

        [FormerlySerializedAs("movementRange")]
        [SerializeField]
        [Min(0)]
        private float m_MovementRange = 50;

        [SerializeField]
        [Tooltip("Defines the circular region where the onscreen control may have it's origin placed.")]
        [Min(0)]
        private float m_DynamicOriginRange = 100;

        [InputControl(layout = "Vector2")]
        [SerializeField]
        private string m_ControlPath;

        [SerializeField]
        [Tooltip("Choose how the onscreen stick will move relative to it's origin and the press position.\n\n" +
            "RelativePositionWithStaticOrigin: The control's center of origin is fixed. " +
            "The control will begin un-actuated at it's centered position and then move relative to the pointer or finger motion.\n\n" +
            "ExactPositionWithStaticOrigin: The control's center of origin is fixed. The stick will immediately jump to the " +
            "exact position of the click or touch and begin tracking motion from there.\n\n" +
            "ExactPositionWithDynamicOrigin: The control's center of origin is determined by the initial press position. " +
            "The stick will begin un-actuated at this center position and then track the current pointer or finger position.")]
        private Behaviour m_Behaviour;

        [SerializeField]
        [Tooltip("Set this to true to prevent cancellation of pointer events due to device switching. Cancellation " +
            "will appear as the stick jumping back and forth between the pointer position and the stick center.")]
        private bool m_UseIsolatedInputActions;

        [SerializeField]
        [Tooltip("The action that will be used to detect pointer down events on the stick control. Note that if no bindings " +
            "are set, default ones will be provided.")]
        private InputAction m_PointerDownAction;

        [SerializeField]
        [Tooltip("The action that will be used to detect pointer movement on the stick control. Note that if no bindings " +
            "are set, default ones will be provided.")]
        private InputAction m_PointerMoveAction;

        private Vector3 m_StartPos;
        private Vector2 m_PointerDownPos;

        [NonSerialized]
        private List<RaycastResult> m_RaycastResults;
        [NonSerialized]
        private PointerEventData m_PointerEventData;

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        /// <summary>Defines how the onscreen stick will move relative to it's origin and the press position.</summary>
        public Behaviour behaviour
        {
            get => m_Behaviour;
            set => m_Behaviour = value;
        }

        /// <summary>Defines how the onscreen stick will move relative to it's center of origin and the press position.</summary>
        public enum Behaviour
        {
            /// <summary>The control's center of origin is fixed in the scene.
            /// The control will begin un-actuated at it's centered position and then move relative to the press motion.</summary>
            RelativePositionWithStaticOrigin,

            /// <summary>The control's center of origin is fixed in the scene.
            /// The control may begin from an actuated position to ensure it is always tracking the current press position.</summary>
            ExactPositionWithStaticOrigin,

            /// <summary>The control's center of origin is determined by the initial press position.
            /// The control will begin unactuated at this center position and then track the current press position.</summary>
            ExactPositionWithDynamicOrigin
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OnScreenStick))]
        internal class OnScreenStickEditor : UnityEditor.Editor
        {
            private AnimBool m_ShowDynamicOriginOptions;
            private AnimBool m_ShowIsolatedInputActions;

            private SerializedProperty m_UseIsolatedInputActions;
            private SerializedProperty m_Behaviour;
            private SerializedProperty m_ControlPathInternal;
            private SerializedProperty m_MovementRange;
            private SerializedProperty m_DynamicOriginRange;
            private SerializedProperty m_PointerDownAction;
            private SerializedProperty m_PointerMoveAction;

            public void OnEnable()
            {
                m_ShowDynamicOriginOptions = new AnimBool(false);
                m_ShowIsolatedInputActions = new AnimBool(false);

                m_UseIsolatedInputActions = serializedObject.FindProperty(nameof(OnScreenStick.m_UseIsolatedInputActions));

                m_Behaviour = serializedObject.FindProperty(nameof(OnScreenStick.m_Behaviour));
                m_ControlPathInternal = serializedObject.FindProperty(nameof(OnScreenStick.m_ControlPath));
                m_MovementRange = serializedObject.FindProperty(nameof(OnScreenStick.m_MovementRange));
                m_DynamicOriginRange = serializedObject.FindProperty(nameof(OnScreenStick.m_DynamicOriginRange));
                m_PointerDownAction = serializedObject.FindProperty(nameof(OnScreenStick.m_PointerDownAction));
                m_PointerMoveAction = serializedObject.FindProperty(nameof(OnScreenStick.m_PointerMoveAction));
            }

            public void OnDisable()
            {
                // Report analytics
                new InputComponentEditorAnalytic(InputSystemComponent.OnScreenStick).Send();
                new OnScreenStickEditorAnalytic(this).Send();
            }

            public override void OnInspectorGUI()
            {
                // Current implementation has UGUI dependencies (ISXB-915, ISXB-916)
                UGUIOnScreenControlEditorUtils.ShowWarningIfNotPartOfCanvasHierarchy((OnScreenStick)target);

                EditorGUILayout.PropertyField(m_MovementRange);
                EditorGUILayout.PropertyField(m_ControlPathInternal);
                EditorGUILayout.PropertyField(m_Behaviour);

                m_ShowDynamicOriginOptions.target = ((OnScreenStick)target).behaviour ==
                    Behaviour.ExactPositionWithDynamicOrigin;
                if (EditorGUILayout.BeginFadeGroup(m_ShowDynamicOriginOptions.faded))
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_DynamicOriginRange);
                    if (EditorGUI.EndChangeCheck())
                    {
                        ((OnScreenStick)target).UpdateDynamicOriginClickableArea();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();

                EditorGUILayout.PropertyField(m_UseIsolatedInputActions);
                m_ShowIsolatedInputActions.target = m_UseIsolatedInputActions.boolValue;
                if (EditorGUILayout.BeginFadeGroup(m_ShowIsolatedInputActions.faded))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_PointerDownAction);
                    EditorGUILayout.PropertyField(m_PointerMoveAction);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFadeGroup();

                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}
#endif
