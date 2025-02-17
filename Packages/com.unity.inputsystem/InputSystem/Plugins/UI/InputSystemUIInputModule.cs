#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

////FIXME: The UI is currently not reacting to pointers until they are moved after the UI module has been enabled. What needs to
////       happen is that point, trackedDevicePosition, and trackedDeviceOrientation have initial state checks. However, for touch,
////       we do *not* want to react to the initial value as then we also get presses (unlike with other pointers). Argh.

////REVIEW: I think this would be much better served by having a composite type input for each of the three basic types of input (pointer, navigation, tracked)
////        I.e. there'd be a PointerInput, a NavigationInput, and a TrackedInput composite. This would solve several problems in one go and make
////        it much more obvious which inputs go together.
////        NOTE: This does not actually solve the problem. Even if, for example, we have a PointerInput value struct and a PointerInputComposite
////              that binds the individual inputs to controls, and then we use it to bind touch0 as a pointer input source, there may still be multiple
////              touchscreens and thus multiple touches coming in through the same composite. This leads back to the same situation.

////REVIEW: The current input model has too much complexity for pointer input; find a way to simplify this.

////REVIEW: how does this/uGUI support drag-scrolls on touch? [GESTURES]

////REVIEW: how does this/uGUI support two-finger right-clicks with touch? [GESTURES]

////TODO: add ability to query which device was last used with any of the actions
////REVIEW: also give access to the last/current UI event?

////TODO: ToString() method a la PointerInputModule

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// Input module that takes its input from <see cref="InputAction">input actions</see>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This module processes all UI input based on the Input System. It is the "glue" between UI systems (UGUI, UITK)
    /// and the Input System.
    /// </para>
    /// <para>
    /// When adding this component from code (such as through <c>GameObject.AddComponent</c>), the
    /// resulting module will automatically have <see cref="DefaultInputActions"/> assigned to it.
    /// If you want to use your own actions, you should create an <see cref="InputActionAsset"/> with the necessary
    /// UI actions. You can copy the default actions and edit them as you need. To have editable Input Actions
    /// out-of-the box you can use the project-wide actions through InputSystem.actions. More information about this
    /// can be read in the <a href="../Manual/InputActions.html">manual</a> documentation.
    /// </para>
    /// <para>
    /// This module can be configured in the Editor > Inspector when added as a component to a <c>GameObject</c>.
    /// </para>
    /// <para>
    /// This UI input module has the advantage over other such modules that it doesn't have to know
    /// what devices and types of devices input is coming from. Instead, the actions hide the actual
    /// sources of input from the module.
    /// </para>
    /// </remarks>
    /// <seealso cref="BaseInputModule"/>
    /// <seealso cref="InputActionAsset"/>
    /// <seealso cref="InputAction"/>
    /// <seealso cref="EventSystem"/>
    /// <seealso cref="AssignDefaultActions"/>
    /// <example>
    /// <code>
    /// using UnityEngine;
    /// using UnityEngine.InputSystem;
    /// using UnityEngine.InputSystem.UI;
    /// using UnityEngine.EventSystems;
    ///
    /// class InputSystemUIInputModuleExample : MonoBehaviour
    /// {
    ///     private InputSystemUIInputModule uiModule;
    ///
    ///     // Configure the InputSystemUIInputModule component programmatically on Start()
    ///     // But a lot of this could be done at runtime as well.
    ///     void Start()
    ///     {
    ///         // Find the EventSystem in the scene
    ///         var eventSystem = EventSystem.current;
    ///
    ///         // Get the InputSystemUIInputModule component
    ///         uiModule = eventSystem.GetComponent&lt;InputSystemUIInputModule&gt;();
    ///
    ///         // Using the default input actions just as an example. Another InputActionAsset can be used.
    ///         DefaultInputActions defaultInputActions = new DefaultInputActions();
    ///         // Example on how to assign individual actions programmatically
    ///         uiModule.actionsAsset = defaultInputActions.asset;
    ///         uiModule.leftClick = InputActionReference.Create(defaultInputActions.UI.Click);
    ///         uiModule.scrollWheel = InputActionReference.Create(defaultInputActions.UI.ScrollWheel);
    ///
    ///         // Set other fields programmatically
    ///         uiModule.deselectOnBackgroundClick = true;
    ///         uiModule.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
    ///         uiModule.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.ScreenCenter;
    ///     }
    ///
    ///     // Example on how programmatically set the move repeat delay based on the move repeat rate
    ///     void SetMoveRepeat(float value)
    ///     {
    ///         uiModule.moveRepeatRate = value;
    ///         uiModule.moveRepeatDelay = value * (1.2f);
    ///     }
    /// }
    /// </code>
    /// </example>
    [HelpURL(InputSystem.kDocUrl + "/manual/UISupport.html#setting-up-ui-input")]
    public class InputSystemUIInputModule : BaseInputModule
    {
        /// <summary>
        /// Whether to clear the current selection when a click happens that does not hit any <c>GameObject</c>.
        /// </summary>
        /// <remarks>
        /// If true (default), clicking outside of any GameObject will reset the current selection
        /// By toggling this behavior off, background clicks will keep the current selection. I.e.
        /// <c>EventSystem.currentSelectedGameObject</c> will not be changed.
        /// </remarks>
        public bool deselectOnBackgroundClick
        {
            get => m_DeselectOnBackgroundClick;
            set => m_DeselectOnBackgroundClick = value;
        }

        /// <summary>
        /// How to deal with the presence of pointer-type input from multiple devices.
        /// </summary>
        /// <remarks>
        /// By default, this is set to <see cref="UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack"/> which will
        /// treat input from <see cref="Mouse"/> and <see cref="Pen"/> devices as coming from a single on-screen pointer
        /// but will treat input from devices such as <see cref="XR.XRController"/> and <see cref="Touchscreen"/> as
        /// their own discrete pointers.
        ///
        /// The primary effect of this setting is to determine whether the user can concurrently point at more than
        /// a single UI element or not. Whenever multiple pointers are allowed, more than one element may have a pointer
        /// over it at any one point and thus several elements can be interacted with concurrently.
        /// </remarks>
        public UIPointerBehavior pointerBehavior
        {
            get => m_PointerBehavior;
            set => m_PointerBehavior = value;
        }

        /// <summary>
        /// Where to position the pointer when the cursor is locked.
        /// </summary>
        /// <remarks>
        /// By default, the pointer is positioned at -1, -1 in screen space when the cursor is locked. This has implications
        /// for using ray casters like <see cref="PhysicsRaycaster"/> because the raycasts will be sent from the pointer
        /// position. By setting the value of <see cref="cursorLockBehavior"/> to <see cref="CursorLockBehavior.ScreenCenter"/>,
        /// the raycasts will be sent from the center of the screen. This is useful when trying to interact with world space UI
        /// using the <see cref="IPointerEnterHandler"/> and <see cref="IPointerExitHandler"/> interfaces when the cursor
        /// is locked.
        /// </remarks>
        /// <see cref="Cursor.lockState"/>
        public CursorLockBehavior cursorLockBehavior
        {
            get => m_CursorLockBehavior;
            set => m_CursorLockBehavior = value;
        }

        /// <summary>
        /// A root game object to support correct navigation in local multi-player UIs.
        /// <remarks>
        /// In local multi-player games where each player has their own UI, players should not be able to navigate into
        /// another player's UI. Each player should have their own instance of an InputSystemUIInputModule, and this property
        /// should be set to the root game object containing all UI objects for that player. If set, navigation using the
        /// <see cref="InputSystemUIInputModule.move"/> action will be constrained to UI objects under that root.
        /// </remarks>
        /// </summary>
        internal GameObject localMultiPlayerRoot
        {
            get => m_LocalMultiPlayerRoot;
            set => m_LocalMultiPlayerRoot = value;
        }

        /// <summary>
        /// A multiplier value that allows you to adjust the scroll wheel speed sent to uGUI (Unity UI) components.
        /// </summary>
        /// <remarks>
        /// This value controls the magnitude of the PointerEventData.scrollDelta value, when the scroll wheel is rotated one tick. It acts as a multiplier, so a value of 1 passes through the original value and behaves the same as the legacy Standalone Input Module.
        ///
        /// A value larger than one increases the scrolling speed per tick, and a value less than one decreases the speed.
        ///
        /// You can set this to a negative value to invert the scroll direction. A value of zero prevents mousewheel scrolling from working at all.
        ///
        /// Note: this has no effect on UI Toolkit content, only uGUI components.
        /// </remarks>
        public float scrollDeltaPerTick
        {
            get => m_ScrollDeltaPerTick;
            set => m_ScrollDeltaPerTick = value;
        }

        /// <summary>
        /// Called by <c>EventSystem</c> when the input module is made current.
        /// </summary>
        /// <remarks>
        /// There's no need to call this method directly unless for specific reasons.
        /// It is called by <c>EventSystem</c> when the input module is made current.
        /// It sets <see cref="EventSystem.currentSelectedGameObject"/> to
        /// <see cref="EventSystem.firstSelectedGameObject"/>  if nothing is selected.
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.EventSystems;
        /// using UnityEngine.InputSystem.UI;
        ///
        /// public class ActivateModuleExample : MonoBehaviour
        /// {
        ///     private InputSystemUIInputModule uiModule;
        ///     void Start()
        ///     {
        ///         // Find the EventSystem in the scene
        ///         var eventSystem = EventSystem.current;
        ///
        ///         // Get the InputSystemUIInputModule component
        ///         uiModule = eventSystem.GetComponent&lt;InputSystemUIInputModule&gt;();
        ///
        ///         // Manually activate the module
        ///         uiModule.ActivateModule();
        ///     }
        /// }
        /// </code>
        /// </example>
        public override void ActivateModule()
        {
            base.ActivateModule();

            // Select firstSelectedGameObject if nothing is selected ATM.
            var toSelect = eventSystem.currentSelectedGameObject;
            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;
            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /// <summary>
        /// Check whether the given pointer or touch is currently hovering over a <c>GameObject</c>.
        /// </summary>
        /// <param name="pointerOrTouchId">ID of the pointer or touch. Meaning this should correspond to either
        /// <c>PointerEventData.pointerId</c> or <see cref="ExtendedPointerEventData.touchId"/>. The pointer ID
        /// generally corresponds to the <see cref="InputDevice.deviceId"/> of the pointer device. An exception
        /// to this are touches as a <see cref="Touchscreen"/> may have multiple pointers (one for each active
        /// finger). For touch, you can use the <see cref="TouchControl.touchId"/> of the touch.
        ///
        /// Note that for touch, a pointer will stay valid for one frame before being removed. In other words,
        /// when <see cref="TouchPhase.Ended"/> or <see cref="TouchPhase.Canceled"/> is received for a touch
        /// and the touch was over a <c>GameObject</c>, the associated pointer is still considered over that
        /// object for the frame in which the touch ended.
        ///
        /// To check whether any pointer is over a <c>GameObject</c>, simply pass a negative value such as -1.</param>
        /// <returns>True if the given pointer is currently hovering over a <c>GameObject</c>.</returns>
        /// <remarks>
        /// The result is true if the given pointer has caused an <c>IPointerEnter</c> event to be sent to a
        /// <c>GameObject</c>.
        ///
        /// This method can be invoked via <c>EventSystem.current.IsPointerOverGameObject</c>.
        ///
        /// Be aware that this method relies on state set up during UI event processing that happens in <c>EventSystem.Update</c>,
        /// that is, as part of <c>MonoBehaviour</c> updates. This step happens <em>after</em> input processing.
        /// Thus, calling this method earlier than that in the frame will make it poll state from <em>last</em> frame.
        ///
        /// Calling this method from within an <see cref="InputAction"/> callback (such as <see cref="InputAction.performed"/>)
        /// will result in a warning. See the "UI vs Game Input" sample shipped with the Input System package for
        /// how to deal with this fact.
        /// </remarks>
        /// <example>
        /// <code>
        /// // In general, the pointer ID corresponds to the device ID:
        /// EventSystem.current.IsPointerOverGameObject(XRController.leftHand.deviceId);
        /// EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
        ///
        /// // For touch input, pass the ID of a touch:
        /// EventSystem.current.IsPointerOverGameObject(Touchscreen.primaryTouch.touchId.ReadValue());
        ///
        /// // But can also pass the ID of the entire Touchscreen in which case the result
        /// // is true if any touch is over a GameObject:
        /// EventSystem.current.IsPointerOverGameObject(Touchscreen.current.deviceId);
        ///
        /// // Finally, any negative value will be interpreted as "any pointer" and will
        /// // return true if any one pointer is currently over a GameObject:
        /// EventSystem.current.IsPointerOverGameObject(-1);
        /// EventSystem.current.IsPointerOverGameObject(); // Equivalent.
        /// </code>
        /// </example>
        /// <seealso cref="ExtendedPointerEventData.touchId"/>
        /// <seealso cref="InputDevice.deviceId"/>
        public override bool IsPointerOverGameObject(int pointerOrTouchId)
        {
            if (InputSystem.isProcessingEvents)
                Debug.LogWarning(
                    "Calling IsPointerOverGameObject() from within event processing (such as from InputAction callbacks) will not work as expected; it will query UI state from the last frame");

            var stateIndex = -1;

            if (pointerOrTouchId < 0)
            {
                if (m_CurrentPointerId != -1)
                {
                    stateIndex = m_CurrentPointerIndex;
                }
                else
                {
                    // No current pointer. Can happen, for example, when a touch just ended and its pointer record
                    // was removed as a result. If we still have some active pointer, use it.
                    if (m_PointerStates.length > 0)
                        stateIndex = 0;
                }
            }
            else
            {
                stateIndex = GetPointerStateIndexFor(pointerOrTouchId);
            }

            if (stateIndex == -1)
                return false;

            return m_PointerStates[stateIndex].eventData.pointerEnter != null;
        }

        /// <summary>
        /// Returns the most recent raycast information for a given pointer or touch.
        /// </summary>
        /// <param name="pointerOrTouchId">ID of the pointer or touch. Meaning this should correspond to either
        /// <c>PointerEventData.pointerId</c> or <see cref="ExtendedPointerEventData.touchId"/>. The pointer ID
        /// generally corresponds to the <see cref="InputDevice.deviceId"/> of the pointer device. An exception
        /// to this are touches as a <see cref="Touchscreen"/> may have multiple pointers (one for each active
        /// finger). For touch, you can use the <see cref="TouchControl.touchId"/> of the touch.
        ///
        /// Negative values will return an invalid <see cref="RaycastResult"/>.</param>
        /// <returns>The most recent raycast information.</returns>
        /// <remarks>
        /// This method is for the most recent raycast, but depending on when it's called is not guaranteed to be for the current frame.
        /// This method can be used to determine raycast distances and hit information for visualization.
        /// <br />
        /// Use <see cref="RaycastResult.isValid"/> to determine if pointer hit anything.
        /// </remarks>
        /// <seealso cref="ExtendedPointerEventData.touchId"/>
        /// <seealso cref="InputDevice.deviceId"/>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.EventSystems;
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.UI;
        ///
        /// public class GetLastRaycastResultExample : MonoBehaviour
        /// {
        ///     public InputSystemUIInputModule uiModule;
        ///
        ///     void PrintLastRaycastResult(int pointerId)
        ///     {
        ///         if (uiModule)
        ///         {
        ///             // Retrieve the last raycast result for the given pointer ID
        ///             RaycastResult raycastResult = uiModule.GetLastRaycastResult(pointerId);
        ///
        ///             // Check if the raycast result is valid
        ///             if (raycastResult.isValid)
        ///             {
        ///                 // Print details about the raycast result
        ///                 Debug.Log($"Pointer ID: {pointerId}");
        ///                 Debug.Log($"Hit GameObject: {raycastResult.gameObject.name}");
        ///                 Debug.Log($"Distance: {raycastResult.distance}");
        ///                 Debug.Log($"World Position: {raycastResult.worldPosition}");
        ///             }
        ///         }
        ///
        ///     }
        ///
        ///     void Update()
        ///     {
        ///         PrintLastRaycastResult(Mouse.current.deviceId);
        ///     }
        /// }
        /// </code>
        /// </example>
        public RaycastResult GetLastRaycastResult(int pointerOrTouchId)
        {
            var stateIndex = GetPointerStateIndexFor(pointerOrTouchId);
            if (stateIndex == -1)
                return default;

            return m_PointerStates[stateIndex].eventData.pointerCurrentRaycast;
        }

        private RaycastResult PerformRaycast(ExtendedPointerEventData eventData)
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            // If it's an event from a tracked device, see if we have a TrackedDeviceRaycaster and give it
            // the first shot.
            if (eventData.pointerType == UIPointerType.Tracked && TrackedDeviceRaycaster.s_Instances.length > 0)
            {
                for (var i = 0; i < TrackedDeviceRaycaster.s_Instances.length; ++i)
                {
                    var trackedDeviceRaycaster = TrackedDeviceRaycaster.s_Instances[i];
                    m_RaycastResultCache.Clear();
                    trackedDeviceRaycaster.PerformRaycast(eventData, m_RaycastResultCache);
                    if (m_RaycastResultCache.Count > 0)
                    {
                        var raycastResult = m_RaycastResultCache[0];
                        m_RaycastResultCache.Clear();
                        return raycastResult;
                    }
                }
                return default;
            }

            // Otherwise pass it along to the normal raycasting logic.
            eventSystem.RaycastAll(eventData, m_RaycastResultCache);
            var result = FindFirstRaycast(m_RaycastResultCache);
            m_RaycastResultCache.Clear();
            return result;
        }

        // Mouse, pen, touch, and tracked device pointer input all go through here.
        private void ProcessPointer(ref PointerModel state)
        {
            var eventData = state.eventData;

            // Sync position.
            var pointerType = eventData.pointerType;
            if (pointerType == UIPointerType.MouseOrPen && Cursor.lockState == CursorLockMode.Locked)
            {
                eventData.position = m_CursorLockBehavior == CursorLockBehavior.OutsideScreen ?
                    new Vector2(-1, -1) :
                    new Vector2(Screen.width / 2f, Screen.height / 2f);
                ////REVIEW: This is consistent with StandaloneInputModule but having no deltas in locked mode seems wrong
                eventData.delta = default;
            }
            else if (pointerType == UIPointerType.Tracked)
            {
                var position = state.worldPosition;
                var rotation = state.worldOrientation;
                if (m_XRTrackingOrigin != null)
                {
                    position = m_XRTrackingOrigin.TransformPoint(position);
                    rotation = m_XRTrackingOrigin.rotation * rotation;
                }

                eventData.trackedDeviceOrientation = rotation;
                eventData.trackedDevicePosition = position;
            }
            else
            {
                eventData.delta = state.screenPosition - eventData.position;
                eventData.position = state.screenPosition;
            }

            // Clear the 'used' flag.
            eventData.Reset();

            // Raycast from current position.
            eventData.pointerCurrentRaycast = PerformRaycast(eventData);

            // Sync position for tracking devices. For those, we can only do this
            // after the raycast as the screen-space position is a byproduct of the raycast.
            if (pointerType == UIPointerType.Tracked && eventData.pointerCurrentRaycast.isValid)
            {
                var screenPos = eventData.pointerCurrentRaycast.screenPosition;
                eventData.delta = screenPos - eventData.position;
                eventData.position = eventData.pointerCurrentRaycast.screenPosition;
            }

            ////REVIEW: for touch, we only need the left button; should we skip right and middle button processing? then we also don't need to copy to/from the event

            // Left mouse button. Movement and scrolling is processed with event set left button.
            eventData.button = PointerEventData.InputButton.Left;
            state.leftButton.CopyPressStateTo(eventData);

            // Unlike StandaloneInputModule, we process moves before processing buttons. This way
            // UI elements get pointer enters/exits before they get button ups/downs and clicks.
            ProcessPointerMovement(ref state, eventData);

            // We always need to process move-related events in order to get PointerEnter and Exit events
            // when we change UI state (e.g. show/hide objects) without moving the pointer. This unfortunately
            // also means that we will invariably raycast on every update.
            // However, after that, early out at this point when there's no changes to the pointer state (except
            // for tracked pointers as the tracking origin may have moved).
            if (!state.changedThisFrame && (xrTrackingOrigin == null || state.pointerType != UIPointerType.Tracked))
                return;

            ProcessPointerButton(ref state.leftButton, eventData);
            ProcessPointerButtonDrag(ref state.leftButton, eventData);
            ProcessPointerScroll(ref state, eventData);

            // Right mouse button.
            eventData.button = PointerEventData.InputButton.Right;
            state.rightButton.CopyPressStateTo(eventData);

            ProcessPointerButton(ref state.rightButton, eventData);
            ProcessPointerButtonDrag(ref state.rightButton, eventData);

            // Middle mouse button.
            eventData.button = PointerEventData.InputButton.Middle;
            state.middleButton.CopyPressStateTo(eventData);

            ProcessPointerButton(ref state.middleButton, eventData);
            ProcessPointerButtonDrag(ref state.middleButton, eventData);
        }

        // if we are using a MultiplayerEventSystem, ignore any transforms
        // not under the current MultiplayerEventSystem's root.
        private bool PointerShouldIgnoreTransform(Transform t)
        {
            if (eventSystem is MultiplayerEventSystem multiplayerEventSystem && multiplayerEventSystem.playerRoot != null)
            {
                if (!t.IsChildOf(multiplayerEventSystem.playerRoot.transform))
                    return true;
            }
            return false;
        }

        private void ProcessPointerMovement(ref PointerModel pointer, ExtendedPointerEventData eventData)
        {
            var currentPointerTarget =
                // If the pointer is a touch that was released the *previous* frame, we generate pointer-exit events
                // and then later remove the pointer.
                eventData.pointerType == UIPointerType.Touch && !pointer.leftButton.isPressed && !pointer.leftButton.wasReleasedThisFrame
                ? null
                : eventData.pointerCurrentRaycast.gameObject;

            ProcessPointerMovement(eventData, currentPointerTarget);
        }

        private void ProcessPointerMovement(ExtendedPointerEventData eventData, GameObject currentPointerTarget)
        {
#if UNITY_2021_1_OR_NEWER
            // If the pointer moved, send move events to all UI elements the pointer is
            // currently over.
            var wasMoved = eventData.IsPointerMoving();
            if (wasMoved)
            {
                for (var i = 0; i < eventData.hovered.Count; ++i)
                    ExecuteEvents.Execute(eventData.hovered[i], eventData, ExecuteEvents.pointerMoveHandler);
            }
#endif

            // If we have no target or pointerEnter has been deleted,
            // we just send exit events to anything we are tracking
            // and then exit.
            if (currentPointerTarget == null || eventData.pointerEnter == null)
            {
                for (var i = 0; i < eventData.hovered.Count; ++i)
                    ExecuteEvents.Execute(eventData.hovered[i], eventData, ExecuteEvents.pointerExitHandler);

                eventData.hovered.Clear();

                if (currentPointerTarget == null)
                {
                    eventData.pointerEnter = null;
                    return;
                }
            }

            if (eventData.pointerEnter == currentPointerTarget && currentPointerTarget)
                return;

            Transform commonRoot = FindCommonRoot(eventData.pointerEnter, currentPointerTarget)?.transform;
            Transform pointerParent = ((Component)currentPointerTarget.GetComponentInParent<IPointerExitHandler>())?.transform;

            // We walk up the tree until a common root and the last entered and current entered object is found.
            // Then send exit and enter events up to, but not including, the common root.
            // ** or when !m_SendPointerEnterToParent, stop when meeting a gameobject with an exit event handler
            if (eventData.pointerEnter != null)
            {
                var current = eventData.pointerEnter.transform;
                while (current != null)
                {
                    // if we reach the common root break out!
                    if (sendPointerHoverToParent && current == commonRoot)
                        break;

                    // if we reach a PointerExitEvent break out!
                    if (!sendPointerHoverToParent && current == pointerParent)
                        break;

#if UNITY_2021_3_OR_NEWER
                    eventData.fullyExited = current != commonRoot && eventData.pointerEnter != currentPointerTarget;
#endif
                    ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerExitHandler);
                    eventData.hovered.Remove(current.gameObject);

                    if (sendPointerHoverToParent)
                        current = current.parent;

                    // if we reach the common root break out!
                    if (current == commonRoot)
                        break;

                    if (!sendPointerHoverToParent)
                        current = current.parent;
                }
            }

            // now issue the enter call up to but not including the common root
            Transform oldPointerEnter = eventData.pointerEnter ? eventData.pointerEnter.transform : null;
            eventData.pointerEnter = currentPointerTarget;
            if (currentPointerTarget != null)
            {
                Transform current = currentPointerTarget.transform;
                while (current != null && !PointerShouldIgnoreTransform(current))
                {
#if UNITY_2021_3_OR_NEWER
                    eventData.reentered = current == commonRoot && current != oldPointerEnter;
                    // if we are sending the event to parent, they are already in hover mode at that point. No need to bubble up the event.
                    if (sendPointerHoverToParent && eventData.reentered)
                        break;
#endif

                    ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
#if UNITY_2021_1_OR_NEWER
                    if (wasMoved)
                        ExecuteEvents.Execute(current.gameObject, eventData, ExecuteEvents.pointerMoveHandler);
#endif
                    eventData.hovered.Add(current.gameObject);

                    // stop when encountering an object with the pointerEnterHandler
                    if (!sendPointerHoverToParent && current.GetComponent<IPointerEnterHandler>() != null)
                        break;

                    if (sendPointerHoverToParent)
                        current = current.parent;

                    // if we reach the common root break out!
                    if (current == commonRoot)
                        break;

                    if (!sendPointerHoverToParent)
                        current = current.parent;
                }
            }
        }

        private const float kClickSpeed = 0.3f;

        private void ProcessPointerButton(ref PointerModel.ButtonState button, PointerEventData eventData)
        {
            var currentOverGo = eventData.pointerCurrentRaycast.gameObject;

            if (currentOverGo != null && PointerShouldIgnoreTransform(currentOverGo.transform))
                return;

            // Button press.
            if (button.wasPressedThisFrame)
            {
                button.pressTime = InputRuntime.s_Instance.unscaledGameTime;

                eventData.delta = Vector2.zero;
                eventData.dragging = false;
                eventData.pressPosition = eventData.position;
                eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;
                eventData.eligibleForClick = true;
                eventData.useDragThreshold = true;

                var selectHandler = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);

                // If we have clicked something new, deselect the old thing and leave 'selection handling' up
                // to the press event (except if there's none and we're told to not deselect in that case).
                if (selectHandler != eventSystem.currentSelectedGameObject && (selectHandler != null || m_DeselectOnBackgroundClick))
                    eventSystem.SetSelectedGameObject(null, eventData);

                // Invoke OnPointerDown, if present.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.pointerDownHandler);

                var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // If no GO responded to OnPointerDown, look for one that responds to OnPointerClick.
                // NOTE: This only looks up the handler. We don't invoke OnPointerClick here.
                if (newPressed == null)
                    newPressed = pointerClickHandler;

                // Reset click state if delay to last release was too long or if we didn't
                // press on the same object as last time. The latter part we don't know until
                // we've actually run the press handler.
                button.clickedOnSameGameObject = newPressed == eventData.lastPress && button.pressTime - eventData.clickTime <= kClickSpeed;
                if (eventData.clickCount > 0 && !button.clickedOnSameGameObject)
                {
                    eventData.clickCount = default;
                    eventData.clickTime = default;
                }

                // Set pointerPress. This nukes lastPress. Meaning that after OnPointerDown, lastPress will
                // become null.
                eventData.pointerPress = newPressed;
#if UNITY_2020_1_OR_NEWER // pointerClick doesn't exist before this.
                eventData.pointerClick = pointerClickHandler;
#endif
                eventData.rawPointerPress = currentOverGo;

                // Save the drag handler for drag events during this mouse down.
                eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
            }

            // Button release.
            if (button.wasReleasedThisFrame)
            {
                // Check for click. Release must be on same GO that we pressed on and we must not
                // have moved beyond our move tolerance (doing so will set eligibleForClick to false).
                // NOTE: There's two difference to click handling here compared to StandaloneInputModule.
                //       1) StandaloneInputModule counts clicks entirely on press meaning that clickCount is increased
                //          before a click has actually happened.
                //       2) StandaloneInputModule increases click counts even if something is eventually not deemed a
                //          click and OnPointerClick is thus never invoked.
                var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
#if UNITY_2020_1_OR_NEWER
                var isClick = eventData.pointerClick != null && eventData.pointerClick == pointerClickHandler && eventData.eligibleForClick;
#else
                var isClick = eventData.pointerPress != null && eventData.pointerPress == pointerClickHandler && eventData.eligibleForClick;
#endif
                if (isClick)
                {
                    // Count clicks.
                    if (button.clickedOnSameGameObject)
                    {
                        // We re-clicked on the same UI element within 0.3 seconds so count
                        // it as a repeat click.
                        ++eventData.clickCount;
                    }
                    else
                    {
                        // First click on this object.
                        eventData.clickCount = 1;
                    }
                    eventData.clickTime = InputRuntime.s_Instance.unscaledGameTime;
                }

                // Invoke OnPointerUp.
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                // Invoke OnPointerClick or OnDrop.
                if (isClick)
                {
#if UNITY_2020_1_OR_NEWER
                    ExecuteEvents.Execute(eventData.pointerClick, eventData, ExecuteEvents.pointerClickHandler);
#else
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
#endif
                }
                else if (eventData.dragging && eventData.pointerDrag != null)
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, eventData, ExecuteEvents.dropHandler);

                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;

                if (eventData.dragging && eventData.pointerDrag != null)
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

                eventData.dragging = false;
                eventData.pointerDrag = null;

                button.ignoreNextClick = false;
            }

            button.CopyPressStateFrom(eventData);
        }

        private void ProcessPointerButtonDrag(ref PointerModel.ButtonState button, ExtendedPointerEventData eventData)
        {
            if (!eventData.IsPointerMoving() ||
                (eventData.pointerType == UIPointerType.MouseOrPen && Cursor.lockState == CursorLockMode.Locked) ||
                eventData.pointerDrag == null)
                return;

            // Detect drags.
            if (!eventData.dragging)
            {
                if (!eventData.useDragThreshold || (eventData.pressPosition - eventData.position).sqrMagnitude >=
                    (double)eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold * (eventData.pointerType == UIPointerType.Tracked
                                                                                               ? m_TrackedDeviceDragThresholdMultiplier
                                                                                               : 1))
                {
                    // Started dragging. Invoke OnBeginDrag.
                    ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                    eventData.dragging = true;
                }
            }

            if (eventData.dragging)
            {
                // If we moved from our initial press object, process an up for that object.
                if (eventData.pointerPress != eventData.pointerDrag)
                {
                    ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

                    eventData.eligibleForClick = false;
                    eventData.pointerPress = null;
                    eventData.rawPointerPress = null;
                }

                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
                button.CopyPressStateFrom(eventData);
            }
        }

        private static void ProcessPointerScroll(ref PointerModel pointer, PointerEventData eventData)
        {
            var scrollDelta = pointer.scrollDelta;
            if (!Mathf.Approximately(scrollDelta.sqrMagnitude, 0.0f))
            {
                eventData.scrollDelta = scrollDelta;
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.pointerEnter);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, eventData, ExecuteEvents.scrollHandler);
            }
        }

        internal void ProcessNavigation(ref NavigationModel navigationState)
        {
            var usedSelectionChange = false;
            if (eventSystem.currentSelectedGameObject != null)
            {
                var data = GetBaseEventData();
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
                usedSelectionChange = data.used;
            }

            // Don't send move events if disabled in the EventSystem.
            if (!eventSystem.sendNavigationEvents)
                return;

            // Process move.
            var movement = navigationState.move;
            if (!usedSelectionChange && (!Mathf.Approximately(movement.x, 0f) || !Mathf.Approximately(movement.y, 0f)))
            {
                var time = InputRuntime.s_Instance.unscaledGameTime;
                var moveVector = navigationState.move;

                var moveDirection = MoveDirection.None;
                if (moveVector.sqrMagnitude > 0)
                {
                    if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
                        moveDirection = moveVector.x > 0 ? MoveDirection.Right : MoveDirection.Left;
                    else
                        moveDirection = moveVector.y > 0 ? MoveDirection.Up : MoveDirection.Down;
                }

                ////REVIEW: is resetting move repeats when direction changes really useful behavior?
                if (moveDirection != m_NavigationState.lastMoveDirection)
                    m_NavigationState.consecutiveMoveCount = 0;

                if (moveDirection != MoveDirection.None)
                {
                    var allow = true;
                    if (m_NavigationState.consecutiveMoveCount != 0)
                    {
                        if (m_NavigationState.consecutiveMoveCount > 1)
                            allow = time > m_NavigationState.lastMoveTime + moveRepeatRate;
                        else
                            allow = time > m_NavigationState.lastMoveTime + moveRepeatDelay;
                    }

                    if (allow)
                    {
                        var eventData = m_NavigationState.eventData as ExtendedAxisEventData;
                        if (eventData == null)
                        {
                            eventData = new ExtendedAxisEventData(eventSystem);
                            m_NavigationState.eventData = eventData;
                        }
                        eventData.Reset();

                        eventData.moveVector = moveVector;
                        eventData.moveDir = moveDirection;
                        eventData.device = navigationState.device;

                        if (IsMoveAllowed(eventData))
                        {
                            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, eventData, ExecuteEvents.moveHandler);
                            usedSelectionChange = eventData.used;

                            m_NavigationState.consecutiveMoveCount = m_NavigationState.consecutiveMoveCount + 1;
                            m_NavigationState.lastMoveTime = time;
                            m_NavigationState.lastMoveDirection = moveDirection;
                        }
                    }
                }
                else
                    m_NavigationState.consecutiveMoveCount = 0;
            }
            else
            {
                m_NavigationState.consecutiveMoveCount = 0;
            }

            // Process submit and cancel events.
            if (!usedSelectionChange && eventSystem.currentSelectedGameObject != null)
            {
                // NOTE: Whereas we use callbacks for the other actions, we rely on WasPressedThisFrame() for
                //       submit and cancel. This makes their behavior inconsistent with pointer click behavior where
                //       a click will register on button *up*, but consistent with how other UI systems work where
                //       click occurs on key press. This nuance in behavior becomes important in combination with
                //       action enable/disable changes in response to submit or cancel. We react to button *down*
                //       instead of *up*, so button *up* will come in *after* we have applied the state change.
                var submitAction = m_SubmitAction?.action;
                var cancelAction = m_CancelAction?.action;

                var data = m_SubmitCancelState.eventData as ExtendedSubmitCancelEventData;
                if (data == null)
                {
                    data = new ExtendedSubmitCancelEventData(eventSystem);
                    m_SubmitCancelState.eventData = data;
                }
                data.Reset();

                data.device = m_SubmitCancelState.device;

                if (cancelAction != null && cancelAction.WasPerformedThisFrame())
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.cancelHandler);
                if (!data.used && submitAction != null && submitAction.WasPerformedThisFrame())
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.submitHandler);
            }
        }

        private bool IsMoveAllowed(AxisEventData eventData)
        {
            if (m_LocalMultiPlayerRoot == null)
                return true;

            if (eventSystem.currentSelectedGameObject == null)
                return true;

            var selectable = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();

            if (selectable == null)
                return true;

            Selectable navigationTarget = null;
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    navigationTarget = selectable.FindSelectableOnRight();
                    break;

                case MoveDirection.Up:
                    navigationTarget = selectable.FindSelectableOnUp();
                    break;

                case MoveDirection.Left:
                    navigationTarget = selectable.FindSelectableOnLeft();
                    break;

                case MoveDirection.Down:
                    navigationTarget = selectable.FindSelectableOnDown();
                    break;
            }

            if (navigationTarget == null)
                return true;

            return navigationTarget.transform.IsChildOf(m_LocalMultiPlayerRoot.transform);
        }

        [FormerlySerializedAs("m_RepeatDelay")]
        [Tooltip("The Initial delay (in seconds) between an initial move action and a repeated move action.")]
        [SerializeField]
        private float m_MoveRepeatDelay = 0.5f;

        [FormerlySerializedAs("m_RepeatRate")]
        [Tooltip("The speed (in seconds) that the move action repeats itself once repeating (max 1 per frame).")]
        [SerializeField]
        private float m_MoveRepeatRate = 0.1f;

        [Tooltip("Scales the Eventsystem.DragThreshold, for tracked devices, to make selection easier.")]
        // Hide this while we still have to figure out what to do with this.
        private float m_TrackedDeviceDragThresholdMultiplier = 2.0f;

        [Tooltip("Transform representing the real world origin for tracking devices. When using the XR Interaction Toolkit, this should be pointing to the XR Rig's Transform.")]
        [SerializeField]
        private Transform m_XRTrackingOrigin;

        /// <summary>
        /// Delay in seconds between an initial move action and a repeated move action while <see cref="move"/> is actuated.
        /// </summary>
        /// <remarks>
        /// While <see cref="move"/> is being held down, the input module will first wait for <see cref="moveRepeatDelay"/> seconds
        /// after the first actuation of <see cref="move"/> and then trigger a move event every <see cref="moveRepeatRate"/> seconds.
        /// </remarks>
        /// <seealso cref="moveRepeatRate"/>
        /// <seealso cref="AxisEventData"/>
        public float moveRepeatDelay
        {
            get => m_MoveRepeatDelay;
            set => m_MoveRepeatDelay = value;
        }

        /// <summary>
        /// Delay in seconds between repeated move actions while <see cref="move"/> is actuated.
        /// </summary>
        /// <remarks>
        /// While <see cref="move"/> is being held down, the input module will first wait for <see cref="moveRepeatDelay"/> seconds
        /// after the first actuation of <see cref="move"/> and then trigger a move event every <see cref="moveRepeatRate"/> seconds.
        ///
        /// Note that a maximum of one <see cref="AxisEventData"/> will be sent per frame. This means that even if multiple time
        /// increments of the repeat delay have passed since the last update, only one move repeat event will be generated.
        /// </remarks>
        public float moveRepeatRate
        {
            get => m_MoveRepeatRate;
            set => m_MoveRepeatRate = value;
        }

        private bool explictlyIgnoreFocus => InputSystem.settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus;

        private bool shouldIgnoreFocus
        {
            // By default, key this on whether running the background is enabled or not. Rationale is that
            // if running in the background is enabled, we already have rules in place what kind of input
            // is allowed through and what isn't. And for the input that *IS* allowed through, the UI should
            // react.
            get => explictlyIgnoreFocus || InputRuntime.s_Instance.runInBackground;
        }

        /// <summary>
        /// (Obsolete) <inheritdoc cref="moveRepeatRate"/>
        /// </summary>
        [Obsolete("'repeatRate' has been obsoleted; use 'moveRepeatRate' instead. (UnityUpgradable) -> moveRepeatRate", false)]
        public float repeatRate
        {
            get => moveRepeatRate;
            set => moveRepeatRate = value;
        }

        /// <summary>
        /// (Obsolete) <inheritdoc cref="moveRepeatDelay"/>
        /// </summary>
        [Obsolete("'repeatDelay' has been obsoleted; use 'moveRepeatDelay' instead. (UnityUpgradable) -> moveRepeatDelay", false)]
        public float repeatDelay
        {
            get => moveRepeatDelay;
            set => moveRepeatDelay = value;
        }

        /// <summary>
        /// A <see cref="Transform"/> representing the real world origin for tracking devices.
        /// </summary>
        /// <remarks>
        /// This is used to convert real world positions and rotations for all <see cref="UIPointerType.Tracked"/>
        /// pointers into Unity's global space.
        /// When using the XR Interaction Toolkit, this should be pointing to the XR Rig's Transform.
        /// If unset, or set to null, the Unity world origin will be used as the basis for all tracked positions and
        /// rotations.
        /// </remarks>
        public Transform xrTrackingOrigin
        {
            get => m_XRTrackingOrigin;
            set => m_XRTrackingOrigin = value;
        }

        /// <summary>
        /// Scales the drag threshold of <c>EventSystem</c> for tracked devices to make selection easier.
        /// </summary>
        public float trackedDeviceDragThresholdMultiplier
        {
            get => m_TrackedDeviceDragThresholdMultiplier;
            set => m_TrackedDeviceDragThresholdMultiplier = value;
        }

        private void SwapAction(ref InputActionReference property, InputActionReference newValue, bool actionsHooked, Action<InputAction.CallbackContext> actionCallback)
        {
            if (property == newValue || (property != null && newValue != null && property.action == newValue.action))
                return;

            if (property != null && actionCallback != null && actionsHooked)
            {
                property.action.performed -= actionCallback;
                property.action.canceled -= actionCallback;
            }

            var oldActionNull = property?.action == null;
            var oldActionEnabled = property?.action != null && property.action.enabled;

            TryDisableInputAction(property);
            property = newValue;

            #if DEBUG
            // We source inputs from arbitrary pointers through a set of pointer-related actions (point, click, etc). This means that in any frame,
            // multiple pointers may pipe input through to the same action and we do not want the disambiguation code in InputActionState.ShouldIgnoreControlStateChange()
            // to prevent input from getting to us. Thus, these actions should generally be set to InputActionType.PassThrough.
            //
            // We treat navigation actions differently as there is only a single NavigationModel for the UI that all navigation input feeds into.
            // Thus, those actions should be configured with disambiguation active (i.e. Move should be a Value action and Submit and Cancel should
            // be Button actions). This is especially important for Submit and Cancel as we get proper press and release action this way.
            if (newValue != null && newValue.action != null && newValue.action.type != InputActionType.PassThrough && !IsNavigationAction(newValue))
            {
                Debug.LogWarning("Pointer-related actions used with the UI input module should generally be set to Pass-Through type so that the module can properly distinguish between "
                    + $"input from multiple pointers (action {newValue.action} is set to {newValue.action.type})", this);
            }
            #endif

            if (newValue?.action != null && actionCallback != null && actionsHooked)
            {
                property.action.performed += actionCallback;
                property.action.canceled += actionCallback;
            }

            if (isActiveAndEnabled && newValue?.action != null && (oldActionEnabled || oldActionNull))
                EnableInputAction(property);
        }

        #if DEBUG
        private bool IsNavigationAction(InputActionReference reference)
        {
            return reference == m_SubmitAction || reference == m_CancelAction || reference == m_MoveAction;
        }

        #endif

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <see cref="Vector2"/> 2D screen position
        /// used as a cursor for pointing at UI elements.
        /// </summary>
        /// <remarks>
        /// The values read from this action determine <see cref="PointerEventData.position"/> and <see cref="PointerEventData.delta"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/>, this forms the basis for pointer-type UI input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Vector2"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("Point");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference point
        {
            get => m_PointAction;
            set => SwapAction(ref m_PointAction, value, m_ActionsHooked, m_OnPointDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> scroll wheel value
        /// used for sending <see cref="PointerEventData"/> events.
        /// </summary>
        /// <remarks>
        /// The values read from this action determine <see cref="PointerEventData.scrollDelta"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Vector2"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var scrollAction = map.AddAction("scroll");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// scrollAction.AddBinding("&lt;Mouse&gt;/scroll");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).scrollWheel =
        ///     InputActionReference.Create(scrollAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference scrollWheel
        {
            get => m_ScrollWheelAction;
            set => SwapAction(ref m_ScrollWheelAction, value, m_ActionsHooked, m_OnScrollWheelDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines
        /// whether the left button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Left"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="point"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that together with <see cref="point"/>, this action is necessary for a pointer to be functional. The other clicks
        /// and <see cref="scrollWheel"/> are optional, however.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Button"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var clickAction = map.AddAction("click");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// clickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// clickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference leftClick
        {
            get => m_LeftClickAction;
            set => SwapAction(ref m_LeftClickAction, value, m_ActionsHooked, m_OnLeftClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines
        /// whether the middle button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Middle"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="scrollWheel"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Button"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var leftClickAction = map.AddAction("leftClick");
        /// var middleClickAction = map.AddAction("middleClick");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// leftClickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// leftClickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// middleClickAction.AddBinding("&lt;Mouse&gt;/middleButton");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(leftClickAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).middleClick =
        ///     InputActionReference.Create(middleClickAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference middleClick
        {
            get => m_MiddleClickAction;
            set => SwapAction(ref m_MiddleClickAction, value, m_ActionsHooked, m_OnMiddleClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float"</c> button value that determines
        /// whether the right button of a pointer is pressed.
        /// </summary>
        /// <remarks>
        /// Clicks on this button will use <see cref="PointerEventData.InputButton.Right"/> for <see cref="PointerEventData.button"/>.
        ///
        /// Together with <see cref="leftClick"/>, <see cref="middleClick"/>, <see cref="scrollWheel"/>, and
        /// <see cref="point"/>, this forms the basis for pointer-type UI input.
        ///
        /// Note that the action is optional. A pointer is fully functional with just <see cref="point"/>
        /// and <see cref="leftClick"/> alone.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Button"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("scroll");
        /// var leftClickAction = map.AddAction("leftClick");
        /// var rightClickAction = map.AddAction("rightClick");
        ///
        /// pointAction.AddBinding("&lt;Mouse&gt;/position");
        /// pointAction.AddBinding("&lt;Touchscreen&gt;/touch*/position");
        ///
        /// leftClickAction.AddBinding("&lt;Mouse&gt;/leftButton");
        /// leftClickAction.AddBinding("&lt;Touchscreen&gt;/touch*/press");
        ///
        /// rightClickAction.AddBinding("&lt;Mouse&gt;/rightButton");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).point =
        ///     InputActionReference.Create(pointAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(leftClickAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).rightClick =
        ///     InputActionReference.Create(rightClickAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference rightClick
        {
            get => m_RightClickAction;
            set => SwapAction(ref m_RightClickAction, value, m_ActionsHooked, m_OnRightClickDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector2</c> 2D motion vector
        /// used for sending <see cref="AxisEventData"/> navigation events.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="IMoveHandler.OnMove"/>.
        ///
        /// This action together with <see cref="submit"/> and <see cref="cancel"/> form the sources for navigation-style
        /// UI input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Vector2"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference move
        {
            get => m_MoveAction;
            set => SwapAction(ref m_MoveAction, value, m_ActionsHooked, m_OnMoveDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines when <c>ISubmitHandler</c>
        /// is triggered.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="ISubmitHandler"/>.
        ///
        /// This action together with <see cref="move"/> and <see cref="cancel"/> form the sources for navigation-style
        /// UI input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.Button"/>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference submit
        {
            get => m_SubmitAction;
            set => SwapAction(ref m_SubmitAction, value, m_ActionsHooked, m_OnSubmitCancelDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>float</c> button value that determines when <c>ICancelHandler</c>
        /// is triggered.
        /// </summary>
        /// <remarks>
        /// The events generated from this input will be received by <see cref="ICancelHandler"/>.
        ///
        /// This action together with <see cref="move"/> and <see cref="submit"/> form the sources for navigation-style
        /// UI input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.Button"/>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var pointAction = map.AddAction("move");
        /// var submitAction = map.AddAction("submit");
        /// var cancelAction = map.AddAction("cancel");
        ///
        /// moveAction.AddBinding("&lt;Gamepad&gt;/*stick");
        /// moveAction.AddBinding("&lt;Gamepad&gt;/dpad");
        /// submitAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// cancelAction.AddBinding("&lt;Gamepad&gt;/buttonEast");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).move =
        ///     InputActionReference.Create(moveAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).submit =
        ///     InputActionReference.Create(submitAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).cancelAction =
        ///     InputActionReference.Create(cancelAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference cancel
        {
            get => m_CancelAction;
            set => SwapAction(ref m_CancelAction, value, m_ActionsHooked, m_OnSubmitCancelDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Quaternion</c> value reflecting the orientation of <see cref="TrackedDevice"/>s.
        /// In combination with <see cref="trackedDevicePosition"/>, this is used to determine the transform of tracked devices from which
        /// to raycast into the UI scene.
        /// </summary>
        /// <remarks>
        /// <see cref="trackedDeviceOrientation"/> and <see cref="trackedDevicePosition"/> together replace <see cref="point"/> for
        /// UI input from <see cref="TrackedDevice"/>. Other than that, UI input for tracked devices is no different from "normal"
        /// pointer-type input. This means that <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/> can all be used for tracked device input like for regular pointer input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Quaternion"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var positionAction = map.AddAction("position");
        /// var orientationAction = map.AddAction("orientation");
        /// var clickAction = map.AddAction("click");
        ///
        /// positionAction.AddBinding("&lt;TrackedDevice&gt;/devicePosition");
        /// orientationAction.AddBinding("&lt;TrackedDevice&gt;/deviceRotation");
        /// clickAction.AddBinding("&lt;TrackedDevice&gt;/trigger");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDevicePosition =
        ///     InputActionReference.Create(positionAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDeviceOrientation =
        ///     InputActionReference.Create(orientationAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference trackedDeviceOrientation
        {
            get => m_TrackedDeviceOrientationAction;
            set => SwapAction(ref m_TrackedDeviceOrientationAction, value, m_ActionsHooked, m_OnTrackedDeviceOrientationDelegate);
        }

        /// <summary>
        /// An <see cref="InputAction"/> delivering a <c>Vector3</c> value reflecting the position of <see cref="TrackedDevice"/>s.
        /// In combination with <see cref="trackedDeviceOrientation"/>, this is used to determine the transform of tracked devices from which
        /// to raycast into the UI scene.
        /// </summary>
        /// <remarks>
        /// <see cref="trackedDeviceOrientation"/> and <see cref="trackedDevicePosition"/> together replace <see cref="point"/> for
        /// UI input from <see cref="TrackedDevice"/>. Other than that, UI input for tracked devices is no different from "normal"
        /// pointer-type input. This means that <see cref="leftClick"/>, <see cref="rightClick"/>, <see cref="middleClick"/>, and
        /// <see cref="scrollWheel"/> can all be used for tracked device input like for regular pointer input.
        ///
        /// This action should have its <see cref="InputAction.type"/> set to <see cref="InputActionType.PassThrough"/> and its
        /// <see cref="InputAction.expectedControlType"/> set to <c>"Vector3"</c>.
        ///
        /// <example>
        /// <code>
        /// var asset = ScriptableObject.Create&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("UI");
        /// var positionAction = map.AddAction("position");
        /// var orientationAction = map.AddAction("orientation");
        /// var clickAction = map.AddAction("click");
        ///
        /// positionAction.AddBinding("&lt;TrackedDevice&gt;/devicePosition");
        /// orientationAction.AddBinding("&lt;TrackedDevice&gt;/deviceRotation");
        /// clickAction.AddBinding("&lt;TrackedDevice&gt;/trigger");
        ///
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDevicePosition =
        ///     InputActionReference.Create(positionAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).trackedDeviceOrientation =
        ///     InputActionReference.Create(orientationAction);
        /// ((InputSystemUIInputModule)EventSystem.current.currentInputModule).leftClick =
        ///     InputActionReference.Create(clickAction);
        /// </code>
        /// </example>
        /// </remarks>
        public InputActionReference trackedDevicePosition
        {
            get => m_TrackedDevicePositionAction;
            set => SwapAction(ref m_TrackedDevicePositionAction, value, m_ActionsHooked, m_OnTrackedDevicePositionDelegate);
        }

        /// <summary>
        /// Assigns default input actions asset and input actions, similar to how defaults are assigned when creating UI module in editor.
        /// Useful for creating <see cref="InputSystemUIInputModule"/> at runtime.
        /// </summary>
        /// <remarks>
        /// This instantiates <see cref="DefaultInputActions"/> and assigns it to <see cref="actionsAsset"/>. It also
        /// assigns all the various individual actions such as <see cref="point"/> and <see cref="leftClick"/>.
        ///
        /// Note that if an <c>InputSystemUIInputModule</c> component is programmatically added to a <c>GameObject</c>,
        /// it will automatically receive the default actions as part of its <c>OnEnable</c> method. Use <see cref="UnassignActions"/>
        /// to remove these assignments.
        /// </remarks>
        /// <example>
        /// <code source="../../../DocCodeSamples.Tests/InputSystemUIInputModuleAssignActionsExample.cs"/>
        /// </example>
        public void AssignDefaultActions()
        {
            if (defaultActions == null)
            {
                defaultActions = new DefaultInputActions();
            }
            actionsAsset = defaultActions.asset;
            cancel = InputActionReference.Create(defaultActions.UI.Cancel);
            submit = InputActionReference.Create(defaultActions.UI.Submit);
            move = InputActionReference.Create(defaultActions.UI.Navigate);
            leftClick = InputActionReference.Create(defaultActions.UI.Click);
            rightClick = InputActionReference.Create(defaultActions.UI.RightClick);
            middleClick = InputActionReference.Create(defaultActions.UI.MiddleClick);
            point = InputActionReference.Create(defaultActions.UI.Point);
            scrollWheel = InputActionReference.Create(defaultActions.UI.ScrollWheel);
            trackedDeviceOrientation = InputActionReference.Create(defaultActions.UI.TrackedDeviceOrientation);
            trackedDevicePosition = InputActionReference.Create(defaultActions.UI.TrackedDevicePosition);
        }

        private static DefaultInputActions defaultActions;

        /// <summary>
        /// Remove all action assignments.
        /// </summary>
        /// <remarks>
        /// Resets <see cref="actionsAsset"/> reference as well as all individual
        /// actions references, such as <see cref="leftClick"/> , and removes the correspondent callbacks hooked for
        /// <see cref="InputAction.performed"/> and <see cref="InputAction.canceled"/>
        ///
        /// It also disposes <see cref="defaultActions"/>.
        ///
        /// If the current actions were enabled by the UI input module, they will be disabled in the process.
        /// </remarks>
        /// <seealso cref="AssignDefaultActions"/>
        /// <example>
        /// <code source="../../../DocCodeSamples.Tests/InputSystemUIInputModuleAssignActionsExample.cs"/>
        /// </example>
        public void UnassignActions()
        {
            defaultActions?.Dispose();
            defaultActions = default;
            actionsAsset = default;
            cancel = default;
            submit = default;
            move = default;
            leftClick = default;
            rightClick = default;
            middleClick = default;
            point = default;
            scrollWheel = default;
            trackedDeviceOrientation = default;
            trackedDevicePosition = default;
        }

        /// <summary>
        /// (Obsolete) This API has been obsoleted; use <see cref="leftClick"/> instead.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Obsolete("'trackedDeviceSelect' has been obsoleted; use 'leftClick' instead.", true)]
        public InputActionReference trackedDeviceSelect
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

#if UNITY_EDITOR
        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();

            var asset = (InputActionAsset)AssetDatabase.LoadAssetAtPath(
                UnityEngine.InputSystem.Editor.PlayerInputEditor.kDefaultInputActionsAssetPath,
                typeof(InputActionAsset));
            // Setting default asset and actions when creating via inspector
            Editor.InputSystemUIInputModuleEditor.ReassignActions(this, asset);
        }

#endif

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();

            m_NavigationState.Reset();
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnhookActions();
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_OnControlsChangedDelegate == null)
                m_OnControlsChangedDelegate = OnControlsChanged;
            InputActionState.s_GlobalState.onActionControlsChanged.AddCallback(m_OnControlsChangedDelegate);

            if (HasNoActions())
                AssignDefaultActions();

            ResetPointers();

            HookActions();
            EnableAllActions();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            ResetPointers();

            InputActionState.s_GlobalState.onActionControlsChanged.RemoveCallback(m_OnControlsChangedDelegate);
            DisableAllActions();
            UnhookActions();
            UnassignActions();

            base.OnDisable();
        }

        private void ResetPointers()
        {
            for (var i = 0; i < m_PointerStates.length; ++i)
            {
                if (SendPointerExitEventsAndRemovePointer(i))
                    --i;
            }

            m_CurrentPointerId = -1;
            m_CurrentPointerIndex = -1;
            m_CurrentPointerType = UIPointerType.None;
        }

        private bool HasNoActions()
        {
            if (m_ActionsAsset != null)
                return false;

            return m_PointAction?.action == null
                && m_LeftClickAction?.action == null
                && m_RightClickAction?.action == null
                && m_MiddleClickAction?.action == null
                && m_SubmitAction?.action == null
                && m_CancelAction?.action == null
                && m_ScrollWheelAction?.action == null
                && m_TrackedDeviceOrientationAction?.action == null
                && m_TrackedDevicePositionAction?.action == null;
        }

        private void EnableAllActions()
        {
            EnableInputAction(m_PointAction);
            EnableInputAction(m_LeftClickAction);
            EnableInputAction(m_RightClickAction);
            EnableInputAction(m_MiddleClickAction);
            EnableInputAction(m_MoveAction);
            EnableInputAction(m_SubmitAction);
            EnableInputAction(m_CancelAction);
            EnableInputAction(m_ScrollWheelAction);
            EnableInputAction(m_TrackedDeviceOrientationAction);
            EnableInputAction(m_TrackedDevicePositionAction);
        }

        private void DisableAllActions()
        {
            TryDisableInputAction(m_PointAction, true);
            TryDisableInputAction(m_LeftClickAction, true);
            TryDisableInputAction(m_RightClickAction, true);
            TryDisableInputAction(m_MiddleClickAction, true);
            TryDisableInputAction(m_MoveAction, true);
            TryDisableInputAction(m_SubmitAction, true);
            TryDisableInputAction(m_CancelAction, true);
            TryDisableInputAction(m_ScrollWheelAction, true);
            TryDisableInputAction(m_TrackedDeviceOrientationAction, true);
            TryDisableInputAction(m_TrackedDevicePositionAction, true);
        }

        private void EnableInputAction(InputActionReference inputActionReference)
        {
            var action = inputActionReference?.action;
            if (action == null)
                return;

            if (s_InputActionReferenceCounts.TryGetValue(action, out var referenceState))
            {
                referenceState.refCount++;
                s_InputActionReferenceCounts[action] = referenceState;
            }
            else
            {
                // if the action is already enabled but its reference count is zero then it was enabled by
                // something outside the input module and the input module should never disable it.
                referenceState = new InputActionReferenceState {refCount = 1, enabledByInputModule = !action.enabled};
                s_InputActionReferenceCounts.Add(action, referenceState);
            }

            action.Enable();
        }

        private void TryDisableInputAction(InputActionReference inputActionReference, bool isComponentDisabling = false)
        {
            var action = inputActionReference?.action;
            if (action == null)
                return;

            // Don't decrement refCount when we were not responsible for incrementing it.
            // I.e. when we were not enabled yet. When OnDisabled is called, isActiveAndEnabled will
            // already have been set to false. In that case we pass isComponentDisabling to check if we
            // came from OnDisabled and therefore need to allow disabling.
            if (!isActiveAndEnabled && !isComponentDisabling)
                return;

            if (!s_InputActionReferenceCounts.TryGetValue(action, out var referenceState))
                return;

            if (referenceState.refCount - 1 == 0 && referenceState.enabledByInputModule)
            {
                action.Disable();
                s_InputActionReferenceCounts.Remove(action);
                return;
            }

            referenceState.refCount--;
            s_InputActionReferenceCounts[action] = referenceState;
        }

        private int GetPointerStateIndexFor(int pointerOrTouchId)
        {
            if (pointerOrTouchId == m_CurrentPointerId)
                return m_CurrentPointerIndex;

            for (var i = 0; i < m_PointerIds.length; ++i)
                if (m_PointerIds[i] == pointerOrTouchId)
                    return i;

            // Search for Device or Touch Ids as a fallback
            for (var i = 0; i < m_PointerStates.length; ++i)
            {
                var eventData = m_PointerStates[i].eventData;
                if (eventData.touchId == pointerOrTouchId || (eventData.touchId != 0 && eventData.device.deviceId == pointerOrTouchId))
                    return i;
            }

            return -1;
        }

        private ref PointerModel GetPointerStateForIndex(int index)
        {
            if (index == 0)
                return ref m_PointerStates.firstValue;
            return ref m_PointerStates.additionalValues[index - 1];
        }

        private int GetDisplayIndexFor(InputControl control)
        {
            int displayIndex = 0;
            if (control.device is Pointer pointerCast)
            {
                displayIndex = pointerCast.displayIndex.ReadValue();
                Debug.Assert(displayIndex <= byte.MaxValue, "Display index was larger than expected");
            }
            return displayIndex;
        }

        private int GetPointerStateIndexFor(ref InputAction.CallbackContext context)
        {
            if (CheckForRemovedDevice(ref context))
                return -1;

            var phase = context.phase;
            return GetPointerStateIndexFor(context.control, createIfNotExists: phase != InputActionPhase.Canceled);
        }

        // This is the key method for determining which pointer a particular input is associated with.
        // The principal determinant is the device that is sending the input which, in general, is expected
        // to be a Pointer (Mouse, Pen, Touchscreen) or TrackedDevice.
        //
        // Note, however, that the input is not guaranteed to even come from a pointer-like device. One can
        // bind the space key to a left click, for example. As long as we have an active pointer that can
        // deliver position input, we accept that setup and treat pressing the space key the same as pressing
        // the left button input on the respective pointer.
        //
        // Quite a lot going on in this method but we're dealing with three different UI interaction paradigms
        // here which we all support from a single input path and allow seamless switching between.
        private int GetPointerStateIndexFor(InputControl control, bool createIfNotExists = true)
        {
            Debug.Assert(control != null, "Control must not be null");

            ////REVIEW: Any way we can cut down on the hops all over memory that we're doing here?
            var device = control.device;

            // Determine the pointer (and touch) ID. We default the pointer ID to the device
            // ID of the InputDevice.
            var controlParent = control.parent;
            var pointerId = device.deviceId;
            var touchId = 0;
            var touchPosition = Vector2.zero;

            // Need to check if it's a touch so that we get a correct pointerId.
            if (controlParent is TouchControl touchControl)
            {
                touchId = touchControl.touchId.value;
                touchPosition = touchControl.position.value;
            }
            // Could be it's a toplevel control on Touchscreen (like "<Touchscreen>/position"). In that case,
            // read the touch ID from primaryTouch.
            else if (controlParent is Touchscreen touchscreen)
            {
                touchId = touchscreen.primaryTouch.touchId.value;
                touchPosition = touchscreen.primaryTouch.position.value;
            }

            int displayIndex = GetDisplayIndexFor(control);

            if (touchId != 0)
                pointerId = ExtendedPointerEventData.MakePointerIdForTouch(pointerId, touchId);

            // Early out if it's the last used pointer.
            // NOTE: Can't just compare by device here because of touchscreens potentially having multiple associated pointers.
            if (m_CurrentPointerId == pointerId)
                return m_CurrentPointerIndex;

            // Search m_PointerIds for an existing entry.
            // NOTE: This is a linear search but m_PointerIds is only IDs and the number of concurrent pointers
            //       should be very low at any one point (in fact, we don't generally expect to have more than one
            //       which is why we are using InlinedArrays).
            for (var i = 0; i < m_PointerIds.length; i++)
            {
                if (m_PointerIds[i] == pointerId)
                {
                    // Existing entry found. Make it the current pointer.
                    m_CurrentPointerId = pointerId;
                    m_CurrentPointerIndex = i;
                    m_CurrentPointerType = m_PointerStates[i].pointerType;
                    return i;
                }
            }

            if (!createIfNotExists)
                return -1;

            // Determine pointer type.
            var pointerType = UIPointerType.None;
            if (touchId != 0)
                pointerType = UIPointerType.Touch;
            else if (HaveControlForDevice(device, point))
                pointerType = UIPointerType.MouseOrPen;
            else if (HaveControlForDevice(device, trackedDevicePosition))
                pointerType = UIPointerType.Tracked;

            ////REVIEW: For touch, probably makes sense to force-ignore any input other than from primaryTouch.
            // If the behavior is SingleUnifiedPointer, we only ever create a single pointer state
            // and use that for all pointer input that is coming in.
            if ((m_PointerBehavior == UIPointerBehavior.SingleUnifiedPointer && pointerType != UIPointerType.None) ||
                (m_PointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack && pointerType == UIPointerType.MouseOrPen))
            {
                if (m_CurrentPointerIndex == -1)
                {
                    m_CurrentPointerIndex = AllocatePointer(pointerId, displayIndex, touchId, pointerType, control, device, touchId != 0 ? controlParent : null);
                }
                else
                {
                    // Update pointer record to reflect current device. We know they're different because we checked
                    // m_CurrentPointerId earlier in the method.
                    // NOTE: This path may repeatedly switch the pointer type and ID on the same single event instance.

                    ref var pointer = ref GetPointerStateForIndex(m_CurrentPointerIndex);

                    var eventData = pointer.eventData;
                    eventData.control = control;
                    eventData.device = device;
                    eventData.pointerType = pointerType;
                    eventData.pointerId = pointerId;
                    eventData.touchId = touchId;
#if UNITY_2022_3_OR_NEWER
                    eventData.displayIndex = displayIndex;
#endif

                    // Make sure these don't linger around when we switch to a different kind of pointer.
                    eventData.trackedDeviceOrientation = default;
                    eventData.trackedDevicePosition = default;
                }

                if (pointerType == UIPointerType.Touch)
                    GetPointerStateForIndex(m_CurrentPointerIndex).screenPosition = touchPosition;

                m_CurrentPointerId = pointerId;
                m_CurrentPointerType = pointerType;

                return m_CurrentPointerIndex;
            }

            // No existing record for the device. Find out if the device has the ability to point at all.
            // If not, we need to use a pointer state from a different device (if present).
            var index = -1;
            if (pointerType != UIPointerType.None)
            {
                // Device has an associated position input. Create a new pointer record.
                index = AllocatePointer(pointerId, displayIndex, touchId, pointerType, control, device, touchId != 0 ? controlParent : null);
            }
            else
            {
                // Device has no associated position input. Find a pointer device to route the change into.
                // As a last resort, create a pointer without a position input.

                // If we have a current pointer, route the input into that. The majority of times we end
                // up in this branch, this should settle things.
                if (m_CurrentPointerId != -1)
                    return m_CurrentPointerIndex;

                // NOTE: In most cases, we end up here when there is input on a non-pointer device bound to one of the pointer-related
                //       actions before there is input from a pointer device. In this scenario, we don't have a pointer state allocated
                //       for the device yet.

                // If we have anything bound to the `point` action, create a pointer for it.
                var pointControls = point?.action?.controls;
                var pointerDevice = pointControls.HasValue && pointControls.Value.Count > 0 ? pointControls.Value[0].device : null;
                if (pointerDevice != null && !(pointerDevice is Touchscreen)) // Touchscreen only temporarily allocate pointer states.
                {
                    // Create MouseOrPen style pointer.
                    index = AllocatePointer(pointerDevice.deviceId, displayIndex, 0, UIPointerType.MouseOrPen, pointControls.Value[0], pointerDevice);
                }
                else
                {
                    // Do the same but look at the `position` action.
                    var positionControls = trackedDevicePosition?.action?.controls;
                    var trackedDevice = positionControls.HasValue && positionControls.Value.Count > 0
                        ? positionControls.Value[0].device
                        : null;
                    if (trackedDevice != null)
                    {
                        // Create a Tracked style pointer.
                        index = AllocatePointer(trackedDevice.deviceId, displayIndex, 0, UIPointerType.Tracked, positionControls.Value[0], trackedDevice);
                    }
                    else
                    {
                        // We got input from a non-pointer device and apparently there's no pointer we can route the
                        // input into. Just create a pointer state for the device and leave it at that.
                        index = AllocatePointer(pointerId, displayIndex, 0, UIPointerType.None, control, device);
                    }
                }
            }

            if (pointerType == UIPointerType.Touch)
                GetPointerStateForIndex(index).screenPosition = touchPosition;

            m_CurrentPointerId = pointerId;
            m_CurrentPointerIndex = index;
            m_CurrentPointerType = pointerType;

            return index;
        }

        private int AllocatePointer(int pointerId, int displayIndex, int touchId, UIPointerType pointerType, InputControl control, InputDevice device, InputControl touchControl = null)
        {
            // Recover event instance from previous record.
            var eventData = default(ExtendedPointerEventData);
            if (m_PointerStates.Capacity > m_PointerStates.length)
            {
                if (m_PointerStates.length == 0)
                    eventData = m_PointerStates.firstValue.eventData;
                else
                    eventData = m_PointerStates.additionalValues[m_PointerStates.length - 1].eventData;
            }

            // Or allocate event.
            if (eventData == null)
                eventData = new ExtendedPointerEventData(eventSystem);

            eventData.pointerId = pointerId;
#if UNITY_2022_3_OR_NEWER
            eventData.displayIndex = displayIndex;
#endif
            eventData.touchId = touchId;
            eventData.pointerType = pointerType;
            eventData.control = control;
            eventData.device = device;

            // Allocate state.
            m_PointerIds.AppendWithCapacity(pointerId);
            return m_PointerStates.AppendWithCapacity(new PointerModel(eventData));
        }

        // Returns true if the pointer was successfully removed (ISXB-1258)
        private bool SendPointerExitEventsAndRemovePointer(int index)
        {
            var eventData = m_PointerStates[index].eventData;
            if (eventData.pointerEnter != null)
                ProcessPointerMovement(eventData, null);

            return RemovePointerAtIndex(index);
        }

        private bool RemovePointerAtIndex(int index)
        {
            Debug.Assert(m_PointerStates[index].eventData.pointerEnter == null, "Pointer should have exited all objects before being removed");

            // We don't want to release touch pointers on the same frame they are released (unpressed). They get cleaned up one frame later in Process()
            ref var state = ref GetPointerStateForIndex(index);
            if (state.pointerType == UIPointerType.Touch && (state.leftButton.isPressed || state.leftButton.wasReleasedThisFrame))
            {
                // The pointer was not removed
                return false;
            }

            // Retain event data so that we can reuse the event the next time we allocate a PointerModel record.
            var eventData = m_PointerStates[index].eventData;
            Debug.Assert(eventData != null, "Pointer state should have an event instance!");

            // Update current pointer, if necessary.
            if (index == m_CurrentPointerIndex)
            {
                m_CurrentPointerId = -1;
                m_CurrentPointerIndex = -1;
                m_CurrentPointerType = default;
            }
            else if (m_CurrentPointerIndex == m_PointerIds.length - 1)
            {
                // We're about to move the last entry so update the index it will
                // be at.
                m_CurrentPointerIndex = index;
            }

            // Remove. Note that we may change the order of pointers here. This can save us needless copying
            // and m_CurrentPointerIndex should be the only index we get around for longer.
            m_PointerIds.RemoveAtByMovingTailWithCapacity(index);
            m_PointerStates.RemoveAtByMovingTailWithCapacity(index);
            Debug.Assert(m_PointerIds.length == m_PointerStates.length, "Pointer ID array should match state array in length");

            // Put event instance back in place at one past last entry of array (which we know we have
            // as we just erased one entry). This entry will be the next one that will be used when we
            // allocate a new entry.

            // Wipe the event.
            // NOTE: We only wipe properties here that contain reference data. The rest we rely on
            //       the event handling code to initialize when using the event.
            eventData.hovered.Clear();
            eventData.device = null;
            eventData.pointerCurrentRaycast = default;
            eventData.pointerPressRaycast = default;
            eventData.pointerPress = default; // Twice to wipe lastPress, too.
            eventData.pointerPress = default;
            eventData.pointerDrag = default;
            eventData.pointerEnter = default;
            eventData.rawPointerPress = default;

            if (m_PointerStates.length == 0)
                m_PointerStates.firstValue.eventData = eventData;
            else
                m_PointerStates.additionalValues[m_PointerStates.length - 1].eventData = eventData;

            return true;
        }

        // Remove any pointer that no longer has the ability to point.
        private void PurgeStalePointers()
        {
            for (var i = 0; i < m_PointerStates.length; ++i)
            {
                ref var state = ref GetPointerStateForIndex(i);
                var device = state.eventData.device;
                if (!device.added || // Check if device was removed altogether.
                    (!HaveControlForDevice(device, point) &&
                     !HaveControlForDevice(device, trackedDevicePosition) &&
                     !HaveControlForDevice(device, trackedDeviceOrientation)))
                {
                    // Only decrement 'i' if the pointer was successfully removed
                    if (SendPointerExitEventsAndRemovePointer(i))
                        --i;
                }
            }

            m_NeedToPurgeStalePointers = false;
        }

        private static bool HaveControlForDevice(InputDevice device, InputActionReference actionReference)
        {
            var action = actionReference?.action;
            if (action == null)
                return false;

            var controls = action.controls;
            for (var i = 0; i < controls.Count; ++i)
                if (controls[i].device == device)
                    return true;

            return false;
        }

        // The pointer actions we unfortunately cannot poll as we may be sourcing input from multiple pointers.

        private void OnPointCallback(InputAction.CallbackContext context)
        {
            // When a pointer is removed, there's like a non-zero coordinate on the position control and thus
            // we will see cancellations on the "Point" action. Ignore these as they provide no useful values
            // and we want to avoid doing a read of touch IDs in GetPointerStateFor() on an already removed
            // touchscreen.
            if (CheckForRemovedDevice(ref context) || context.canceled)
                return;

            var index = GetPointerStateIndexFor(context.control);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            state.screenPosition = context.ReadValue<Vector2>();
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        // NOTE: In the click events, we specifically react to the Canceled phase to make sure we do NOT perform
        //       button *clicks* when an action resets. However, we still need to send pointer ups.

        private bool IgnoreNextClick(ref InputAction.CallbackContext context, bool wasPressed)
        {
            // If explicitly ignoring focus due to setting, never ignore clicks
            if (explictlyIgnoreFocus)
                return false;
            // If a currently active click is cancelled (by focus change), ignore next click if device cannot run in background.
            // This prevents the cancelled click event being registered when focus is returned i.e. if
            // the button was released while another window was focused.
            return context.canceled && !InputRuntime.s_Instance.isPlayerFocused && !context.control.device.canRunInBackground && wasPressed;
        }

        private void OnLeftClickCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            bool wasPressed = state.leftButton.isPressed;
            state.leftButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
            if (IgnoreNextClick(ref context, wasPressed))
                state.leftButton.ignoreNextClick = true;
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private void OnRightClickCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            bool wasPressed = state.rightButton.isPressed;
            state.rightButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
            if (IgnoreNextClick(ref context, wasPressed))
                state.rightButton.ignoreNextClick = true;
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private void OnMiddleClickCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            bool wasPressed = state.middleButton.isPressed;
            state.middleButton.isPressed = context.ReadValueAsButton();
            state.changedThisFrame = true;
            if (IgnoreNextClick(ref context, wasPressed))
                state.middleButton.ignoreNextClick = true;
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private bool CheckForRemovedDevice(ref InputAction.CallbackContext context)
        {
            // When a device is removed, we want to simply cancel ongoing pointer
            // operations. Most importantly, we want to prevent GetPointerStateFor()
            // doing ReadValue() on touch ID controls when a touchscreen has already
            // been removed.
            if (context.canceled && !context.control.device.added)
            {
                m_NeedToPurgeStalePointers = true;
                return true;
            }
            return false;
        }

        private void OnScrollCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);

            var scrollDelta = context.ReadValue<Vector2>();

            // ISXB-704: convert input value to BaseInputModule convention.
            state.scrollDelta = (scrollDelta / InputSystem.scrollWheelDeltaPerTick) * scrollDeltaPerTick;

#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private void OnMoveCallback(InputAction.CallbackContext context)
        {
            ////REVIEW: should we poll this? or set the action to not be pass-through? (ps4 controller is spamming this action)
            m_NavigationState.move = context.ReadValue<Vector2>();
            m_NavigationState.device = context.control.device;
        }

        private void OnSubmitCancelCallback(InputAction.CallbackContext context)
        {
            m_SubmitCancelState.device = context.control.device;
        }

        private void OnTrackedDeviceOrientationCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            state.worldOrientation = context.ReadValue<Quaternion>();
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private void OnTrackedDevicePositionCallback(InputAction.CallbackContext context)
        {
            var index = GetPointerStateIndexFor(ref context);
            if (index == -1)
                return;

            ref var state = ref GetPointerStateForIndex(index);
            state.worldPosition = context.ReadValue<Vector3>();
#if UNITY_2022_3_OR_NEWER
            state.eventData.displayIndex = GetDisplayIndexFor(context.control);
#endif
        }

        private void OnControlsChanged(object obj)
        {
            m_NeedToPurgeStalePointers = true;
        }

        private void FilterPointerStatesByType()
        {
            var pointerTypeToProcess = UIPointerType.None;
            // Read all pointers device states
            // Find first pointer that has changed this frame to be processed later
            for (var i = 0; i < m_PointerStates.length; ++i)
            {
                ref var state = ref GetPointerStateForIndex(i);
                state.eventData.ReadDeviceState();
                state.CopyTouchOrPenStateFrom(state.eventData);
                if (state.changedThisFrame && pointerTypeToProcess == UIPointerType.None)
                    pointerTypeToProcess = state.pointerType;
            }

            // For SingleMouseOrPenButMultiTouchAndTrack, we keep a single pointer for mouse and pen but only for as
            // long as there is no touch or tracked input. If we get that kind, we remove the mouse/pen pointer.
            if (m_PointerBehavior == UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack && pointerTypeToProcess != UIPointerType.None)
            {
                // var pointerTypeToProcess = m_PointerStates.firstValue.pointerType;
                if (pointerTypeToProcess == UIPointerType.MouseOrPen)
                {
                    // We have input on a mouse or pen. Kill all touch and tracked pointers we may have.
                    for (var i = 0; i < m_PointerStates.length; ++i)
                    {
                        ref var state = ref GetPointerStateForIndex(i);
                        // Touch pointers need to get forced to no longer be pressed otherwise they will not get released in subsequent frames.
                        if (m_PointerStates[i].pointerType == UIPointerType.Touch)
                        {
                            state.leftButton.isPressed = false;
                        }
                        if (m_PointerStates[i].pointerType != UIPointerType.MouseOrPen && m_PointerStates[i].pointerType != UIPointerType.Touch || (m_PointerStates[i].pointerType == UIPointerType.Touch && !state.leftButton.isPressed && !state.leftButton.wasReleasedThisFrame))
                        {
                            if (SendPointerExitEventsAndRemovePointer(i))
                                --i;
                        }
                    }
                }
                else
                {
                    // We have touch or tracked input. Kill mouse/pen pointer, if we have it.
                    for (var i = 0; i < m_PointerStates.length; ++i)
                    {
                        if (m_PointerStates[i].pointerType == UIPointerType.MouseOrPen)
                        {
                            if (SendPointerExitEventsAndRemovePointer(i))
                                --i;
                        }
                    }
                }
            }
        }

        /// <inheritdoc cref="BaseInputModule.Process"/>
        /// <remarks>
        /// <para>This method is automatically called by <see cref="EventSystem.Update()"/> once per frame.
        /// There is no need to call it manually. Unless for specific use cases.
        /// </para>
        /// It processes all <see cref="UIPointerType"/> and <see cref="UIPointerBehavior"/> pointer types,
        /// as well as navigation input state from <see cref="m_PointerStates"/> and <see cref="m_NavigationState"/>.
        /// These fields hold state based on the actions set up for the UI action map of <see cref="actionsAsset"/>.
        /// The InputAction callbacks are responsible for updating their state, which means state can change multiple
        /// times during a frame, even though it will only be processed once per frame. For example, in case there are
        /// multiple clicks or touches in a single frame, they can allocate multiple pointers in the same frame, which
        /// will all then be processed by frame when <see cref="EventSystem.Update()"/> calls this method.
        /// <para>
        /// Also, this method is responsible for purging stale pointers when a device is removed, and for filtering
        /// pointer states
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem.UI;
        ///
        /// public class CustomInputModuleProcessor : MonoBehaviour
        /// {
        ///     // Reference to the InputSystemUIInputModule, set in the Inspector
        ///     public InputSystemUIInputModule uiModule;
        ///
        ///     void Update()
        ///     {
        ///         // Process the input module in the Update loop for a specific case
        ///         // if this needs to be called outside the EventSystem.Update() event
        ///         if (uiModule != null)
        ///         {
        ///             uiModule.Process();
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="UIPointerType"/>
        /// <seealso cref="UIPointerBehavior"/>
        /// <seealso cref="BaseInputModule.Process"/>
        /// <seealso cref="actionsAsset"/>
        public override void Process()
        {
            if (m_NeedToPurgeStalePointers)
                PurgeStalePointers();

            // Reset devices of changes since we don't want to spool up changes once we gain focus.
            if (!eventSystem.isFocused && !shouldIgnoreFocus)
            {
                for (var i = 0; i < m_PointerStates.length; ++i)
                    m_PointerStates[i].OnFrameFinished();
            }
            else
            {
                // Navigation input.
                ProcessNavigation(ref m_NavigationState);

                FilterPointerStatesByType();

                // Pointer input.
                for (var i = 0; i < m_PointerStates.length; i++)
                {
                    ref var state = ref GetPointerStateForIndex(i);

                    ProcessPointer(ref state);

                    // If it's a touch and the touch has ended, release the pointer state.
                    // NOTE: We defer this by one frame such that OnPointerUp happens in the frame of release
                    //       and OnPointerExit happens one frame later. This is so that IsPointerOverGameObject()
                    //       stays true for the touch in the frame of release (see UI_TouchPointersAreKeptForOneFrameAfterRelease).
                    if (state.pointerType == UIPointerType.Touch && !state.leftButton.isPressed && !state.leftButton.wasReleasedThisFrame)
                    {
                        RemovePointerAtIndex(i);
                        --i;
                        continue;
                    }

                    state.OnFrameFinished();
                }
            }
        }

#if UNITY_2021_1_OR_NEWER
        public override int ConvertUIToolkitPointerId(PointerEventData sourcePointerData)
        {
            // Case 1369081: when using SingleUnifiedPointer, the same (default) pointerId should be sent to UIToolkit
            // regardless of pointer type or finger id.
            if (m_PointerBehavior == UIPointerBehavior.SingleUnifiedPointer)
                return UIElements.PointerId.mousePointerId;

            return sourcePointerData is ExtendedPointerEventData ep
                ? ep.uiToolkitPointerId
                : base.ConvertUIToolkitPointerId(sourcePointerData);
        }

#endif

#if UNITY_INPUT_SYSTEM_INPUT_MODULE_SCROLL_DELTA
        const float kSmallestScrollDeltaPerTick = 0.00001f;
        public override Vector2 ConvertPointerEventScrollDeltaToTicks(Vector2 scrollDelta)
        {
            if (Mathf.Abs(scrollDeltaPerTick) < kSmallestScrollDeltaPerTick)
                return Vector2.zero;

            return scrollDelta / scrollDeltaPerTick;
        }

#endif

#if UNITY_INPUT_SYSTEM_INPUT_MODULE_NAVIGATION_DEVICE_TYPE
        public override NavigationDeviceType GetNavigationEventDeviceType(BaseEventData eventData)
        {
            if (eventData is not INavigationEventData eed)
                return NavigationDeviceType.Unknown;
            if (eed.device is Keyboard)
                return NavigationDeviceType.Keyboard;
            return NavigationDeviceType.NonKeyboard;
        }

#endif

        private void HookActions()
        {
            if (m_ActionsHooked)
                return;

            if (m_OnPointDelegate == null)
                m_OnPointDelegate = OnPointCallback;
            if (m_OnLeftClickDelegate == null)
                m_OnLeftClickDelegate = OnLeftClickCallback;
            if (m_OnRightClickDelegate == null)
                m_OnRightClickDelegate = OnRightClickCallback;
            if (m_OnMiddleClickDelegate == null)
                m_OnMiddleClickDelegate = OnMiddleClickCallback;
            if (m_OnScrollWheelDelegate == null)
                m_OnScrollWheelDelegate = OnScrollCallback;
            if (m_OnMoveDelegate == null)
                m_OnMoveDelegate = OnMoveCallback;
            if (m_OnSubmitCancelDelegate == null)
                m_OnSubmitCancelDelegate = OnSubmitCancelCallback;
            if (m_OnTrackedDeviceOrientationDelegate == null)
                m_OnTrackedDeviceOrientationDelegate = OnTrackedDeviceOrientationCallback;
            if (m_OnTrackedDevicePositionDelegate == null)
                m_OnTrackedDevicePositionDelegate = OnTrackedDevicePositionCallback;

            SetActionCallbacks(true);
        }

        private void UnhookActions()
        {
            if (!m_ActionsHooked)
                return;

            SetActionCallbacks(false);
        }

        private void SetActionCallbacks(bool install)
        {
            m_ActionsHooked = install;
            SetActionCallback(m_PointAction, m_OnPointDelegate, install);
            SetActionCallback(m_MoveAction, m_OnMoveDelegate, install);
            SetActionCallback(m_SubmitAction, m_OnSubmitCancelDelegate, install);
            SetActionCallback(m_CancelAction, m_OnSubmitCancelDelegate, install);
            SetActionCallback(m_LeftClickAction, m_OnLeftClickDelegate, install);
            SetActionCallback(m_RightClickAction, m_OnRightClickDelegate, install);
            SetActionCallback(m_MiddleClickAction, m_OnMiddleClickDelegate, install);
            SetActionCallback(m_ScrollWheelAction, m_OnScrollWheelDelegate, install);
            SetActionCallback(m_TrackedDeviceOrientationAction, m_OnTrackedDeviceOrientationDelegate, install);
            SetActionCallback(m_TrackedDevicePositionAction, m_OnTrackedDevicePositionDelegate, install);
        }

        private static void SetActionCallback(InputActionReference actionReference, Action<InputAction.CallbackContext> callback, bool install)
        {
            if (!install && callback == null)
                return;

            if (actionReference == null)
                return;

            var action = actionReference.action;
            if (action == null)
                return;

            if (install)
            {
                action.performed += callback;
                action.canceled += callback;
            }
            else
            {
                action.performed -= callback;
                action.canceled -= callback;
            }
        }

        private InputActionReference UpdateReferenceForNewAsset(InputActionReference actionReference)
        {
            var oldAction = actionReference?.action;
            if (oldAction == null)
                return null;

            var oldActionMap = oldAction.actionMap;
            Debug.Assert(oldActionMap != null, "Not expected to end up with a singleton action here");

            var newActionMap = m_ActionsAsset?.FindActionMap(oldActionMap.name);
            if (newActionMap == null)
                return null;

            var newAction = newActionMap.FindAction(oldAction.name);
            if (newAction == null)
                return null;

            return InputActionReference.Create(newAction);
        }

        /// <summary>
        ///  The <see cref="InputActionAsset"/> that contains the necessary UI actions used by the UI module.
        /// </summary>
        public InputActionAsset actionsAsset
        {
            get => m_ActionsAsset;
            set
            {
                if (value != m_ActionsAsset)
                {
                    UnhookActions();

                    m_ActionsAsset = value;

                    point = UpdateReferenceForNewAsset(point);
                    move = UpdateReferenceForNewAsset(move);
                    leftClick = UpdateReferenceForNewAsset(leftClick);
                    rightClick = UpdateReferenceForNewAsset(rightClick);
                    middleClick = UpdateReferenceForNewAsset(middleClick);
                    scrollWheel = UpdateReferenceForNewAsset(scrollWheel);
                    submit = UpdateReferenceForNewAsset(submit);
                    cancel = UpdateReferenceForNewAsset(cancel);
                    trackedDeviceOrientation = UpdateReferenceForNewAsset(trackedDeviceOrientation);
                    trackedDevicePosition = UpdateReferenceForNewAsset(trackedDevicePosition);

                    HookActions();
                }
            }
        }

        [SerializeField, HideInInspector] private InputActionAsset m_ActionsAsset;
        [SerializeField, HideInInspector] private InputActionReference m_PointAction;
        [SerializeField, HideInInspector] private InputActionReference m_MoveAction;
        [SerializeField, HideInInspector] private InputActionReference m_SubmitAction;
        [SerializeField, HideInInspector] private InputActionReference m_CancelAction;
        [SerializeField, HideInInspector] private InputActionReference m_LeftClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_MiddleClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_RightClickAction;
        [SerializeField, HideInInspector] private InputActionReference m_ScrollWheelAction;
        [SerializeField, HideInInspector] private InputActionReference m_TrackedDevicePositionAction;
        [SerializeField, HideInInspector] private InputActionReference m_TrackedDeviceOrientationAction;

        [SerializeField] private bool m_DeselectOnBackgroundClick = true;
        [SerializeField] private UIPointerBehavior m_PointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
        [SerializeField, HideInInspector] internal CursorLockBehavior m_CursorLockBehavior = CursorLockBehavior.OutsideScreen;

        // See ISXB-766 for a history of where the 6.0f value comes from
        // (we used to have 120 per tick on Windows and divided it by 20.)
        [SerializeField] private float m_ScrollDeltaPerTick = 6.0f;

        private static Dictionary<InputAction, InputActionReferenceState> s_InputActionReferenceCounts = new Dictionary<InputAction, InputActionReferenceState>();

        private struct InputActionReferenceState
        {
            public int refCount;
            public bool enabledByInputModule;
        }

        [NonSerialized] private bool m_ActionsHooked;
        [NonSerialized] private bool m_NeedToPurgeStalePointers;

        private Action<InputAction.CallbackContext> m_OnPointDelegate;
        private Action<InputAction.CallbackContext> m_OnMoveDelegate;
        private Action<InputAction.CallbackContext> m_OnSubmitCancelDelegate;
        private Action<InputAction.CallbackContext> m_OnLeftClickDelegate;
        private Action<InputAction.CallbackContext> m_OnRightClickDelegate;
        private Action<InputAction.CallbackContext> m_OnMiddleClickDelegate;
        private Action<InputAction.CallbackContext> m_OnScrollWheelDelegate;
        private Action<InputAction.CallbackContext> m_OnTrackedDevicePositionDelegate;
        private Action<InputAction.CallbackContext> m_OnTrackedDeviceOrientationDelegate;
        private Action<object> m_OnControlsChangedDelegate;

        // Pointer-type input (also tracking-type).
        [NonSerialized] private int m_CurrentPointerId = -1; // Keeping track of the current pointer avoids searches in most cases.
        [NonSerialized] private int m_CurrentPointerIndex = -1;
        [NonSerialized] internal UIPointerType m_CurrentPointerType = UIPointerType.None;
        internal InlinedArray<int> m_PointerIds; // Index in this array maps to index in m_PointerStates. Separated out to make searching more efficient (we do a linear search).
        internal InlinedArray<PointerModel> m_PointerStates;

        // Navigation-type input.
        private NavigationModel m_NavigationState;
        private SubmitCancelModel m_SubmitCancelState;

        [NonSerialized] private GameObject m_LocalMultiPlayerRoot;

#if UNITY_INPUT_SYSTEM_SENDPOINTERHOVERTOPARENT
        // Needed for testing.
        internal new bool sendPointerHoverToParent
        {
            get => base.sendPointerHoverToParent;
            set => base.sendPointerHoverToParent = value;
        }
#else
        private bool sendPointerHoverToParent => true;
#endif

        /// <summary>
        /// Controls the origin point of raycasts when the cursor is locked.
        /// </summary>
        public enum CursorLockBehavior
        {
            /// <summary>
            /// The internal pointer position will be set to -1, -1. This short-circuits the raycasting
            /// logic so no objects will be intersected. This is the default setting.
            /// </summary>
            OutsideScreen,

            /// <summary>
            /// Raycasts will originate from the center of the screen. This mode can be useful for
            /// example to check in pointer-driven FPS games if the player is looking at some world-space
            /// object that implements the <see cref="IPointerEnterHandler"/> and <see cref="IPointerExitHandler"/>
            /// interfaces.
            /// </summary>
            ScreenCenter
        }
    }
}
#endif
