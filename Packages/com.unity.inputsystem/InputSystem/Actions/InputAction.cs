using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

////TODO: add way to retrieve the binding correspond to a control

////TODO: add way to retrieve the currently ongoing interaction and also add way to know how long it's been going on

////FIXME: control goes back to invalid when the action ends, so there's no guarantee you can get to the control through the polling API

////FIXME: Whether a control from a binding that's part of a composite appears on an action is currently not consistently enforced.
////       If it mentions the action, it appears on the action. Otherwise it doesn't. The controls should consistently appear on the
////       action based on what action the *composite* references.

////REVIEW: Should we bring the checkboxes for actions back? We tried to "simplify" things by collapsing everything into a InputActionTypes
////        and making the various behavior toggles implicit in that. However, my impression is that this has largely backfired by making
////        it opaque what the choices actually entail and by giving no way out if the choices for one reason or another don't work out
////        perfectly.
////
////        My impression is that at least two the following two checkboxes would make sense:
////        1) Initial State Check? Whether the action should immediately sync to the current state of controls when enabled.
////        2) Resolve Conflicting Inputs? Whether the action should try to resolve conflicts between multiple concurrent inputs.
////
////        I'm fine hiding this under an "Advanced" foldout or something. But IMO, control over this should be available to the user.
////
////        In the same vein, we probably also should expose control over how an action behaves on focus loss (https://forum.unity.com/threads/actions-canceled-when-game-loses-focus.855217/).

////REVIEW: I think the action system as it is today offers too many ways to shoot yourself in the foot. It has
////        flexibility but at the same time has abundant opportunity for ending up with dysfunction. Common setups
////        have to come preconfigured and work robustly for the user without requiring much understanding of how
////        the system fits together.

////REVIEW: add "lastControl" property? (and maybe a lastDevice at the InputActionMap/Asset level?)

////REVIEW: have single delegate instead of separate performed/started/canceled callbacks?

////REVIEW: Do we need to have separate display names for actions?

////REVIEW: what about having the concept of "consumed" on the callback context?

////REVIEW: have "Always Enabled" toggle on actions?

////TODO: allow temporarily disabling individual bindings (flag on binding) such that no re-resolve is needed
////      (SilenceBinding? DisableBinding)

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A named input signal that can flexibly decide which input data to tap.
    /// </summary>
    /// <remarks>
    /// An input action is an abstraction over the source of input(s) it receives. They are
    /// most useful for representing input as "logical" concepts (e.g. "jump") rather than
    /// as "physical" inputs (e.g. "space bar on keyboard pressed").
    ///
    /// In its most basic form, an action is simply an object along with a collection of
    /// bindings that trigger the action.
    ///
    /// <example>
    /// <code>
    /// // A simple action can be created directly using `new`. If desired, a binding
    /// // can be specified directly as part of construction.
    /// var action = new InputAction(binding: "&lt;Gamepad&gt;/buttonSouth");
    ///
    /// // Additional bindings can be added using `AddBinding`.
    /// action.AddBinding("&lt;Mouse&gt;/leftButton");
    /// </code>
    /// </example>
    ///
    /// Bindings use control path expressions to reference controls. See <see cref="InputBinding"/>
    /// for more details. There may be arbitrary many bindings targeting a single action. The
    /// list of bindings targeting an action can be obtained through <see cref="bindings"/>.
    ///
    /// By itself an action does not do anything until it is enabled:
    ///
    /// <example>
    /// <code>
    /// action.Enable();
    /// </code>
    /// </example>
    ///
    /// Once enabled, the action will actively monitor all controls on devices present
    /// in the system (see <see cref="InputSystem.devices"/>) that match any of the binding paths
    /// associated with the action. If you want to restrict the set of bindings used at runtime
    /// or restrict the set of devices which controls are chosen from, you can do so using
    /// <see cref="bindingMask"/> or, if the action is part of an <see cref="InputActionMap"/>,
    /// by setting the <see cref="InputActionMap.devices"/> property of the action map. The
    /// controls that an action uses can be queried using the <see cref="controls"/> property.
    ///
    /// When input is received on controls bound to an action, the action will trigger callbacks
    /// in response. These callbacks are <see cref="started"/>, <see cref="performed"/>, and
    /// <see cref="canceled"/>. The callbacks are triggered as part of input system updates
    /// (see <see cref="InputSystem.Update"/>), i.e. they happen before the respective
    /// <c>MonoBehaviour.Update</c> or <c>MonoBehaviour.FixedUpdate</c> methods
    /// get executed (depending on which <see cref="InputSettings.updateMode"/> the system is
    /// set to).
    ///
    /// In what order and how those callbacks get triggered depends on both the <see cref="type"/>
    /// of the action as well as on the interactions (see <see cref="IInputInteraction"/>) present
    /// on the bindings of the action. The default behavior is that when a control is actuated
    /// (that is, moving away from its resting position), <see cref="started"/> is called and then
    /// <see cref="performed"/>. Subsequently, whenever the a control further changes value to
    /// anything other than its default value, <see cref="performed"/> will be called again.
    /// Finally, when the control moves back to its default value (i.e. resting position),
    /// <see cref="canceled"/> is called.
    ///
    /// To hook into the callbacks, there are several options available to you. The most obvious
    /// one is to hook directly into <see cref="started"/>, <see cref="performed"/>, and/or
    /// <see cref="canceled"/>. In these callbacks, you will receive a <see cref="CallbackContext"/>
    /// with information about how the action got triggered. For example, you can use <see
    /// cref="CallbackContext.ReadValue{TValue}"/> to read the value from the binding that triggered
    /// or use <see cref="CallbackContext.interaction"/> to find the interaction that is in progress.
    ///
    /// <example>
    /// <code>
    /// action.started += context => Debug.Log($"{context.action} started");
    /// action.performed += context => Debug.Log($"{context.action} performed");
    /// action.canceled += context => Debug.Log($"{context.action} canceled");
    /// </code>
    /// </example>
    ///
    /// Alternatively, you can use the <see cref="InputActionMap.actionTriggered"/> callback for
    /// actions that are part of an action map or the global <see cref="InputSystem.onActionChange"/>
    /// callback to globally listen for action activity. To simply record action activity instead
    /// of responding to it directly, you can use <see cref="InputActionTrace"/>.
    ///
    /// If you prefer to poll an action directly as part of your <c>MonoBehaviour.Update</c>
    /// or <c>MonoBehaviour.FixedUpdate</c> logic, you can do so using the <see cref="triggered"/>
    /// and <see cref="ReadValue{TValue}"/> methods.
    ///
    /// <example>
    /// <code>
    /// protected void Update()
    /// {
    ///     // For a button type action.
    ///     if (action.triggered)
    ///         /* ... */;
    ///
    ///     // For a value type action.
    ///     // (Vector2 is just an example; pick the value type that is the right
    ///     // one according to the bindings you have)
    ///     var v = action.ReadValue&lt;Vector2&gt;();
    /// }
    /// </code>
    /// </example>
    ///
    /// Note that actions are not generally frame-based. What this means is that an action
    /// will observe any value change on its connected controls, even if the control changes
    /// value multiple times in the same frame. In practice, this means that, for example,
    /// no button press will get missed.
    ///
    /// Actions can be grouped into maps (see <see cref="InputActionMap"/>) which can in turn
    /// be grouped into assets (see <see cref="InputActionAsset"/>).
    ///
    /// Please note that actions are a player-only feature. They are not supported in
    /// edit mode.
    ///
    /// For more in-depth reading on actions, see the <a href="../manual/Actions.html">manual</a>.
    /// </remarks>
    /// <seealso cref="InputActionMap"/>
    /// <seealso cref="InputActionAsset"/>
    /// <seealso cref="InputBinding"/>
    [Serializable]
    public sealed class InputAction : ICloneable, IDisposable
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
        /// <value>Plain-text name of the action.</value>
        /// <remarks>
        /// Can be null for anonymous actions created in code.
        ///
        /// If the action is part of an <see cref="InputActionMap"/>, it will have a name and the name
        /// will be unique in the map. The name is just the name of the action alone, not a "mapName/actionName"
        /// combination.
        ///
        /// The name should not contain slashes or dots but can contain spaces and other punctuation.
        ///
        /// An action can be renamed after creation using <see cref="InputActionSetupExtensions.Rename"/>..
        /// </remarks>
        /// <seealso cref="InputActionMap.FindAction(string,bool)"/>
        public string name => m_Name;

        /// <summary>
        /// Behavior type of the action.
        /// </summary>
        /// <value>General behavior type of the action.</value>
        /// <remarks>
        /// Determines how the action gets triggered in response to control value changes.
        ///
        /// For details about how the action type affects an action, see <see cref="InputActionType"/>.
        /// </remarks>
        public InputActionType type => m_Type;

        /// <summary>
        /// A stable, unique identifier for the action.
        /// </summary>
        /// <value>Unique ID of the action.</value>
        /// <remarks>
        /// This can be used instead of the name to refer to the action. Doing so allows referring to the
        /// action such that renaming the action does not break references.
        /// </remarks>
        public Guid id
        {
            get
            {
                MakeSureIdIsInPlace();
                return new Guid(m_Id);
            }
        }

        internal Guid idDontGenerate
        {
            get
            {
                if (string.IsNullOrEmpty(m_Id))
                    return default;
                return new Guid(m_Id);
            }
        }

        /// <summary>
        /// Name of control layout expected for controls bound to this action.
        /// </summary>
        /// <remarks>
        /// This is optional and is null by default.
        ///
        /// Constraining an action to a particular control layout allows determine the value
        /// type and expected input behavior of an action without being reliant on any particular
        /// binding.
        /// </remarks>
        public string expectedControlType
        {
            get => m_ExpectedControlType;
            set => m_ExpectedControlType = value;
        }

        /// <summary>
        /// Processors applied to every binding on the action.
        /// </summary>
        /// <value>Processors added to all bindings on the action.</value>
        /// <remarks>
        /// This property is equivalent to appending the same string to the
        /// <see cref="InputBinding.processors"/> field of every binding that targets
        /// the action. It is thus simply a means of avoiding the need configure the
        /// same processor the same way on every binding in case it uniformly applies
        /// to all of them.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction(processors: "scaleVector2(x=2, y=2)");
        ///
        /// // Both of the following bindings will implicitly have a
        /// // ScaleVector2Processor applied to them.
        /// action.AddBinding("&lt;Gamepad&gt;/leftStick");
        /// action.AddBinding("&lt;Joystick&gt;/stick");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.processors"/>
        /// <seealso cref="InputProcessor"/>
        /// <seealso cref="InputSystem.RegisterProcessor{T}"/>
        public string processors => m_Processors;

        /// <summary>
        /// Interactions applied to every binding on the action.
        /// </summary>
        /// <value>Interactions added to all bindings on the action.</value>
        /// <remarks>
        /// This property is equivalent to appending the same string to the
        /// <see cref="InputBinding.interactions"/> field of every binding that targets
        /// the action. It is thus simply a means of avoiding the need configure the
        /// same interaction the same way on every binding in case it uniformly applies
        /// to all of them.
        ///
        /// <example>
        /// <code>
        /// var action = new InputAction(interactions: "press");
        ///
        /// // Both of the following bindings will implicitly have a
        /// // Press interaction applied to them.
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// action.AddBinding("&lt;Joystick&gt;/trigger");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.interactions"/>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="InputSystem.RegisterInteraction{T}"/>
        public string interactions => m_Interactions;

        /// <summary>
        /// The map the action belongs to.
        /// </summary>
        /// <value><see cref="InputActionMap"/> that the action belongs to or null.</value>
        /// <remarks>
        /// If the action is a loose action created in code, this will be <c>null</c>.
        ///
        /// <example>
        /// <code>
        /// var action1 = new InputAction(); // action1.actionMap will be null
        ///
        /// var actionMap = new InputActionMap();
        /// var action2 = actionMap.AddAction("action"); // action2.actionMap will point to actionMap
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionSetupExtensions.AddAction"/>
        public InputActionMap actionMap => isSingletonAction ? null : m_ActionMap;

        /// <summary>
        /// An optional mask that determines which bindings of the action to enable and
        /// which to ignore.
        /// </summary>
        /// <value>Optional mask that determines which bindings on the action to enable.</value>
        /// <remarks>
        /// Binding masks can be applied at three different levels: for an entire asset through
        /// <see cref="InputActionAsset.bindingMask"/>, for a specific map through <see
        /// cref="InputActionMap.bindingMask"/>, and for single actions through this property.
        /// By default, none of the masks will be set (i.e. they will be <c>null</c>).
        ///
        /// When an action is enabled, all the binding masks that apply to it are taken into
        /// account. Specifically, this means that any given binding on the action will be
        /// enabled only if it matches the mask applied to the asset, the mask applied
        /// to the map that contains the action, and the mask applied to the action itself.
        /// All the masks are individually optional.
        ///
        /// Masks are matched against bindings using <see cref="InputBinding.Matches"/>.
        ///
        /// Note that if you modify the masks applicable to an action while it is
        /// enabled, the action's <see cref="controls"/> will get updated immediately to
        /// respect the mask. To avoid repeated binding resolution, it is most efficient
        /// to apply binding masks before enabling actions.
        ///
        /// Binding masks are non-destructive. All the bindings on the action are left
        /// in place. Setting a mask will not affect the value of the <see cref="bindings"/>
        /// property.
        ///
        /// <example>
        /// <code>
        /// // Create a free-standing action with two bindings, one in the
        /// // "Keyboard" group and one in the "Gamepad" group.
        /// var action = new InputAction();
        /// action.AddBinding("&lt;Gamepad&gt;/buttonSouth", groups: "Gamepad");
        /// action.AddBinding("&lt;Keyboard&gt;/space", groups: "Keyboard");
        ///
        /// // By default, all bindings will be enabled. This means if both
        /// // a keyboard and gamepad (or several of them) is present, the action
        /// // will respond to input from all of them.
        /// action.Enable();
        ///
        /// // With a binding mask we can restrict the action to just specific
        /// // bindings. For example, to only enable the gamepad binding:
        /// action.bindingMask = InputBinding.MaskByGroup("Gamepad");
        ///
        /// // Note that we can mask by more than just by group. Masking by path
        /// // or by action as well as a combination of these is also possible.
        /// // We could, for example, mask for just a specific binding path:
        /// action.bindingMask = new InputBinding()
        /// {
        ///     // Select the keyboard binding based on its specific path.
        ///     path = "&lt;Keyboard&gt;/space"
        /// };
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputBinding.MaskByGroup"/>
        /// <seealso cref="InputActionMap.bindingMask"/>
        /// <seealso cref="InputActionAsset.bindingMask"/>
        public InputBinding? bindingMask
        {
            get => m_BindingMask;
            set
            {
                if (value == m_BindingMask)
                    return;

                if (value != null)
                {
                    var v = value.Value;
                    v.action = name;
                    value = v;
                }

                m_BindingMask = value;

                var map = GetOrCreateActionMap();
                if (map.m_State != null)
                    map.LazyResolveBindings(fullResolve: true);
            }
        }

        /// <summary>
        /// The list of bindings associated with the action.
        /// </summary>
        /// <value>List of bindings for the action.</value>
        /// <remarks>
        /// This list contains all bindings from <see cref="InputActionMap.bindings"/> of the action's
        /// <see cref="actionMap"/> that reference the action through their <see cref="InputBinding.action"/>
        /// property.
        ///
        /// Note that on the first call, the list may have to be extracted from the action map first which
        /// may require allocating GC memory. However, once initialized, no further GC allocation hits should occur.
        /// If the binding setup on the map is changed, re-initialization may be required.
        /// </remarks>
        /// <seealso cref="InputActionMap.bindings"/>
        public ReadOnlyArray<InputBinding> bindings => GetOrCreateActionMap().GetBindingsForSingleAction(this);

        /// <summary>
        /// The set of controls to which the action's <see cref="bindings"/> resolve.
        /// </summary>
        /// <value>Controls resolved from the action's <see cref="bindings"/>.</value>
        /// <remarks>
        /// This property can be queried whether the action is enabled or not and will return the
        /// set of controls that match the action's bindings according to the current setup of
        /// binding masks (<see cref="bindingMask"/>) and device restrictions (<see
        /// cref="InputActionMap.devices"/>).
        ///
        /// Note that internally, controls are not stored on a per-action basis. This means
        /// that on the first read of this property, the list of controls for just the action
        /// may have to be extracted which in turn may allocate GC memory. After the first read,
        /// no further GC allocations should occur except if the set of controls is changed (e.g.
        /// by changing the binding mask or by adding/removing devices to/from the system).
        ///
        /// If the property is queried when the action has not been enabled yet, the system
        /// will first resolve controls on the action (and for all actions in the map and/or
        /// the asset). See <a href="../manual/ActionBindings.html#binding-resolution">Binding Resolution</a>
        /// in the manual for details.
        ///
        /// To map a control in this array to an index into <see cref="bindings"/>, use
        /// <see cref="InputActionRebindingExtensions.GetBindingIndexForControl"/>.
        ///
        /// <example>
        /// <code>
        /// // Map control list to binding indices.
        /// var bindingIndices = myAction.controls.Select(c => myAction.GetBindingIndexForControl(c));
        /// </code>
        /// </example>
        ///
        /// Note that this array will not contain the same control multiple times even if more than
        /// one binding on an action references the same control.
        ///
        /// <example>
        /// <code>
        /// var action1 = new InputAction();
        /// action1.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// action1.AddBinding("&lt;Gamepad&gt;/buttonSouth"); // This binding will be ignored.
        ///
        /// // Contains only one instance of buttonSouth which is associated
        /// // with the first binding (at index #0).
        /// var action1Controls = action1.controls;
        ///
        /// var action2 = new InputAction();
        /// action2.AddBinding("&lt;Gamepad&gt;/buttonSouth");
        /// // Add a binding that implicitly matches the first binding, too. When binding resolution
        /// // happens, this binding will only receive buttonNorth, buttonWest, and buttonEast, but not
        /// // buttonSouth as the first binding already received that control.
        /// action2.AddBinding("&lt;Gamepad&gt;/button*");
        ///
        /// // Contains only all four face buttons (buttonSouth, buttonNorth, buttonEast, buttonWest)
        /// // but buttonSouth is associated with the first button and only buttonNorth, buttonEast,
        /// // and buttonWest are associated with the second binding.
        /// var action2Controls = action2.controls;
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputActionRebindingExtensions.GetBindingIndexForControl"/>
        /// <seealso cref="bindings"/>
        public ReadOnlyArray<InputControl> controls
        {
            get
            {
                var map = GetOrCreateActionMap();
                map.ResolveBindingsIfNecessary();
                return map.GetControlsForSingleAction(this);
            }
        }

        /// <summary>
        /// The current phase of the action.
        /// </summary>
        /// <remarks>
        /// When listening for control input and when responding to control value changes,
        /// actions will go through several possible phases.
        ///
        /// In general, when an action starts receiving input, it will go to <see cref="InputActionPhase.Started"/>
        /// and when it stops receiving input, it will go to <see cref="InputActionPhase.Canceled"/>.
        /// When <see cref="InputActionPhase.Performed"/> is used depends primarily on the type
        /// of action. <see cref="InputActionType.Value"/> will trigger <see cref="InputActionPhase.Performed"/>
        /// whenever the value of the control changes (including the first time; i.e. it will first
        /// trigger <see cref="InputActionPhase.Started"/> and then <see cref="InputActionPhase.Performed"/>
        /// right after) whereas <see cref="InputActionType.Button"/> will trigger <see cref="InputActionPhase.Performed"/>
        /// as soon as the button press threshold (<see cref="InputSettings.defaultButtonPressPoint"/>)
        /// has been crossed.
        ///
        /// Note that both interactions and the action <see cref="type"/> can affect the phases
        /// that an action goes through. <see cref="InputActionType.PassThrough"/> actions will
        /// only ever use <see cref="InputActionPhase.Performed"/> and not go to <see
        /// cref="InputActionPhase.Started"/> or <see cref="InputActionPhase.Canceled"/> (as
        /// pass-through actions do not follow the start-performed-canceled model in general).
        ///
        /// While an action is disabled, its phase is <see cref="InputActionPhase.Disabled"/>.
        /// </remarks>
        public InputActionPhase phase => currentState.phase;

        /// <summary>
        /// True if the action is currently in <see cref="InputActionPhase.Started"/> or <see cref="InputActionPhase.Performed"/>
        /// phase. False in all other cases.
        /// </summary>
        /// <see cref="phase"/>
        public bool inProgress => phase.IsInProgress();

        /// <summary>
        /// Whether the action is currently enabled, i.e. responds to input, or not.
        /// </summary>
        /// <value>True if the action is currently enabled.</value>
        /// <remarks>
        /// An action is enabled by either calling <see cref="Enable"/> on it directly or by calling
        /// <see cref="InputActionMap.Enable"/> on the <see cref="InputActionMap"/> containing the action.
        /// When enabled, an action will listen for changes on the controls it is bound to and trigger
        /// callbacks such as <see cref="started"/>, <see cref="performed"/>, and <see cref="canceled"/>
        /// in response.
        /// </remarks>
        /// <seealso cref="Enable"/>
        /// <seealso cref="Disable"/>
        /// <seealso cref="InputActionMap.Enable"/>
        /// <seealso cref="InputActionMap.Disable"/>
        /// <seealso cref="InputSystem.ListEnabledActions()"/>
        public bool enabled => phase != InputActionPhase.Disabled;

        /// <summary>
        /// Event that is triggered when the action has been started.
        /// </summary>
        /// <remarks>
        /// See <see cref="phase"/> for details of how an action progresses through phases
        /// and triggers this callback.
        /// </remarks>
        /// <see cref="InputActionPhase.Started"/>
        public event Action<CallbackContext> started
        {
            add => m_OnStarted.AddCallback(value);
            remove => m_OnStarted.RemoveCallback(value);
        }

        /// <summary>
        /// Event that is triggered when the action has been <see cref="started"/>
        /// but then canceled before being fully <see cref="performed"/>.
        /// </summary>
        /// <remarks>
        /// See <see cref="phase"/> for details of how an action progresses through phases
        /// and triggers this callback.
        /// </remarks>
        /// <see cref="InputActionPhase.Canceled"/>
        public event Action<CallbackContext> canceled
        {
            add => m_OnCanceled.AddCallback(value);
            remove => m_OnCanceled.RemoveCallback(value);
        }

        /// <summary>
        /// Event that is triggered when the action has been fully performed.
        /// </summary>
        /// <remarks>
        /// See <see cref="phase"/> for details of how an action progresses through phases
        /// and triggers this callback.
        /// </remarks>
        /// <see cref="InputActionPhase.Performed"/>
        public event Action<CallbackContext> performed
        {
            add => m_OnPerformed.AddCallback(value);
            remove => m_OnPerformed.RemoveCallback(value);
        }

        ////TODO: Obsolete and drop this when we can break API
        /// <summary>
        /// Equivalent to <see cref="WasPerformedThisFrame"/>.
        /// </summary>
        /// <seealso cref="WasPerformedThisFrame"/>
        public bool triggered => WasPerformedThisFrame();

        /// <summary>
        /// The currently active control that is driving the action. <see langword="null"/> while the action
        /// is in waiting (<see cref="InputActionPhase.Waiting"/>) or canceled (<see cref="InputActionPhase.Canceled"/>)
        /// state. Otherwise the control that last had activity on it which wasn't ignored.
        /// </summary>
        /// <remarks>
        /// Note that the control's value does not necessarily correspond to the value of the
        /// action (<see cref="ReadValue{TValue}"/>) as the control may be part of a composite.
        /// </remarks>
        /// <seealso cref="CallbackContext.control"/>
        public unsafe InputControl activeControl
        {
            get
            {
                var state = GetOrCreateActionMap().m_State;
                if (state != null)
                {
                    var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                    var controlIndex = actionStatePtr->controlIndex;
                    if (controlIndex != InputActionState.kInvalidIndex)
                        return state.controls[controlIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// Type of value returned by <see cref="ReadValueAsObject"/> and currently expected
        /// by <see cref="ReadValue{TValue}"/>. <see langword="null"/> while the action
        /// is in waiting (<see cref="InputActionPhase.Waiting"/>) or canceled (<see cref="InputActionPhase.Canceled"/>)
        /// state as this is based on the currently active control that is driving the action.
        /// </summary>
        /// <value>Type of object returned when reading a value.</value>
        /// <remarks>
        /// The type of value returned by an action is usually determined by the
        /// <see cref="InputControl"/> that triggered the action, i.e. by the
        /// control referenced from <see cref="activeControl"/>.
        ///
        /// However, if the binding that triggered is a composite, then the composite
        /// will determine values and not the individual control that triggered (that
        /// one just feeds values into the composite).
        ///
        /// The active value type may change depending on which controls are actuated if there are multiple
        /// bindings with different control types. This property can be used to ensure you are calling the
        /// <see cref="ReadValue{TValue}"/> method with the expected type parameter if your action is
        /// configured to allow multiple control types as otherwise that method will throw an <see cref="InvalidOperationException"/>
        /// if the type of the control that triggered the action does not match the type parameter.
        /// </remarks>
        /// <seealso cref="InputControl.valueType"/>
        /// <seealso cref="InputBindingComposite.valueType"/>
        /// <seealso cref="activeControl"/>
        public unsafe Type activeValueType
        {
            get
            {
                var state = GetOrCreateActionMap().m_State;
                if (state != null)
                {
                    var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                    var controlIndex = actionStatePtr->controlIndex;
                    if (controlIndex != InputActionState.kInvalidIndex)
                        return state.GetValueType(actionStatePtr->bindingIndex, controlIndex);
                }

                return null;
            }
        }

        /// <summary>
        /// Whether the action wants a state check on its bound controls as soon as it is enabled. This is always
        /// true for <see cref="InputActionType.Value"/> actions but can optionally be enabled for <see cref="InputActionType.Button"/>
        /// or <see cref="InputActionType.PassThrough"/> actions.
        /// </summary>
        /// <remarks>
        /// Usually, when an action is <see cref="enabled"/> (e.g. via <see cref="Enable"/>), it will start listening for input
        /// and then trigger once the first input arrives. However, <see cref="controls"/> bound to an action may already be
        /// actuated when an action is enabled. For example, if a "jump" action is bound to <see cref="Keyboard.spaceKey"/>,
        /// the space bar may already be pressed when the jump action is enabled.
        ///
        /// <see cref="InputActionType.Value"/> actions handle this differently by immediately performing an "initial state check"
        /// in the next input update (see <see cref="InputSystem.Update"/>) after being enabled. If any of the bound controls
        /// is already actuated, the action will trigger right away -- even with no change in state on the controls.
        ///
        /// This same behavior can be enabled explicitly for <see cref="InputActionType.Button"/> and <see cref="InputActionType.PassThrough"/>
        /// actions using this property.
        /// </remarks>
        /// <seealso cref="Enable"/>
        /// <seealso cref="InputActionType.Value"/>
        public bool wantsInitialStateCheck
        {
            get => type == InputActionType.Value || (m_Flags & ActionFlags.WantsInitialStateCheck) != 0;
            set
            {
                if (value)
                    m_Flags |= ActionFlags.WantsInitialStateCheck;
                else
                    m_Flags &= ~ActionFlags.WantsInitialStateCheck;
            }
        }

        /// <summary>
        /// ProfilerMarker for measuring the enabling/disabling of InputActions.
        /// </summary>
        static readonly ProfilerMarker k_InputActionEnableProfilerMarker = new ProfilerMarker("InputAction.Enable");
        static readonly ProfilerMarker k_InputActionDisableProfilerMarker = new ProfilerMarker("InputAction.Disable");

        /// <summary>
        /// Construct an unnamed, free-standing action that is not part of any map or asset
        /// and has no bindings. Bindings can be added with <see
        /// cref="InputActionSetupExtensions.AddBinding(InputAction,string,string,string,string)"/>.
        /// The action type defaults to <see cref="InputActionType.Value"/>.
        /// </summary>
        /// <remarks>
        /// The action will not have an associated <see cref="InputActionMap"/> and <see cref="actionMap"/>
        /// will thus be <c>null</c>. Use <see cref="InputActionSetupExtensions.AddAction"/> instead if
        /// you want to add a new action to an action map.
        ///
        /// The action will remain disabled after construction and thus not listen/react to input yet.
        /// Use <see cref="Enable"/> to enable the action.
        ///
        /// <example>
        /// <code>
        /// // Create an action with two bindings.
        /// var action = new InputAction();
        /// action.AddBinding("&lt;Gamepad&gt;/leftStick");
        /// action.AddBinding("&lt;Mouse&gt;/delta");
        ///
        /// action.performed += ctx => Debug.Log("Value: " + ctx.ReadValue&lt;Vector2&gt;());
        ///
        /// action.Enable();
        /// </code>
        /// </example>
        /// </remarks>
        public InputAction()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Construct a free-standing action that is not part of an <see cref="InputActionMap"/>.
        /// </summary>
        /// <param name="name">Name of the action. If null or empty, the action will be unnamed.</param>
        /// <param name="type">Type of action to create. Defaults to <see cref="InputActionType.Value"/>, i.e.
        /// an action that provides continuous values.</param>
        /// <param name="binding">If not null or empty, a binding with the given path will be added to the action
        /// right away. The format of the string is the as for <see cref="InputBinding.path"/>.</param>
        /// <param name="interactions">If <paramref name="binding"/> is not null or empty, this parameter represents
        /// the interaction to apply to the newly created binding (i.e. <see cref="InputBinding.interactions"/>). If
        /// <paramref name="binding"/> is not supplied, this parameter represents the interactions to apply to the action
        /// (i.e. the value of <see cref="interactions"/>).</param>
        /// <param name="processors">If <paramref name="binding"/> is not null or empty, this parameter represents
        /// the processors to apply to the newly created binding (i.e. <see cref="InputBinding.processors"/>). If
        /// <paramref name="binding"/> is not supplied, this parameter represents the processors to apply to the
        /// action (i.e. the value of <see cref="processors"/>).</param>
        /// <param name="expectedControlType">The optional expected control type for the action (i.e. <see
        /// cref="expectedControlType"/>).</param>
        /// <remarks>
        /// The action will not have an associated <see cref="InputActionMap"/> and <see cref="actionMap"/>
        /// will thus be <c>null</c>. Use <see cref="InputActionSetupExtensions.AddAction"/> instead if
        /// you want to add a new action to an action map.
        ///
        /// The action will remain disabled after construction and thus not listen/react to input yet.
        /// Use <see cref="Enable"/> to enable the action.
        ///
        /// Additional bindings can be added with <see
        /// cref="InputActionSetupExtensions.AddBinding(InputAction,string,string,string,string)"/>.
        ///
        /// <example>
        /// <code>
        /// // Create a button action responding to the gamepad A button.
        /// var action = new InputAction(type: InputActionType.Button, binding: "&lt;Gamepad&gt;/buttonSouth");
        /// action.performed += ctx => Debug.Log("Pressed");
        /// action.Enable();
        /// </code>
        /// </example>
        /// </remarks>
        public InputAction(string name = null, InputActionType type = default, string binding = null,
                           string interactions = null, string processors = null, string expectedControlType = null)
        {
            m_Name = name;
            m_Type = type;

            if (!string.IsNullOrEmpty(binding))
            {
                m_SingletonActionBindings = new[]
                {
                    new InputBinding
                    {
                        path = binding,
                        interactions = interactions,
                        processors = processors,
                        action = m_Name,
                        id = Guid.NewGuid(),
                    },
                };
                m_BindingsStartIndex = 0;
                m_BindingsCount = 1;
            }
            else
            {
                m_Interactions = interactions;
                m_Processors = processors;
            }

            m_ExpectedControlType = expectedControlType;
            m_Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Release internal state held on to by the action.
        /// </summary>
        /// <remarks>
        /// Once enabled, actions will allocate a block of state internally that they will hold on to
        /// until disposed of. For free-standing actions, that state is private to just the action.
        /// For actions that are part of <see cref="InputActionMap"/>s, the state is shared by all
        /// actions in the map and, if the map itself is part of an <see cref="InputActionAsset"/>,
        /// also by all the maps that are part of the asset.
        ///
        /// Note that the internal state holds on to GC heap memory as well as memory from the
        /// unmanaged, C++ heap.
        /// </remarks>
        public void Dispose()
        {
            m_ActionMap?.m_State?.Dispose();
        }

        /// <summary>
        /// Return a string version of the action. Mainly useful for debugging.
        /// </summary>
        /// <returns>A string version of the action.</returns>
        public override string ToString()
        {
            string str;
            if (m_Name == null)
                str = "<Unnamed>";
            else if (m_ActionMap != null && !isSingletonAction && !string.IsNullOrEmpty(m_ActionMap.name))
                str = $"{m_ActionMap.name}/{m_Name}";
            else
                str = m_Name;

            var controls = this.controls;
            if (controls.Count > 0)
            {
                str += "[";
                var isFirst = true;
                foreach (var control in controls)
                {
                    if (!isFirst)
                        str += ",";
                    str += control.path;
                    isFirst = false;
                }
                str += "]";
            }

            return str;
        }

        /// <summary>
        /// Enable the action such that it actively listens for input and runs callbacks
        /// in response.
        /// </summary>
        /// <remarks>
        /// If the action is already enabled, this method does nothing.
        ///
        /// By default, actions start out disabled, i.e. with <see cref="enabled"/> being false.
        /// When enabled, two things happen.
        ///
        /// First, if it hasn't already happened, an action will resolve all of its bindings
        /// to <see cref="InputControl"/>s. This also happens if, since the action was last enabled,
        /// the setup of devices in the system has changed such that it may impact the action.
        ///
        /// Second, for all the <see cref="controls"/> bound to an action, change monitors (see
        /// <see cref="IInputStateChangeMonitor"/>) will be added to the system. If any of the
        /// controls changes state in the future, the action will get notified and respond.
        ///
        /// <see cref="InputActionType.Value"/> type actions will also perform an initial state
        /// check in the input system update following the call to Enable. This means that if
        /// any of the bound controls are already actuated and produce a non-<c>default</c> value,
        /// the action will immediately trigger in response.
        ///
        /// Note that this method only enables a single action. This is also allowed for action
        /// that are part of an <see cref="InputActionMap"/>. To enable all actions in a map,
        /// call <see cref="InputActionMap.Enable"/>.
        ///
        /// The <see cref="InputActionMap"/> associated with an action (if any), will immediately
        /// toggle to being enabled (see <see cref="InputActionMap.enabled"/>) as soon as the first
        /// action in the map is enabled and for as long as any action in the map is still enabled.
        ///
        /// The first time an action is enabled, it will allocate a block of state internally that it
        /// will hold on to until disposed of. For free-standing actions, that state is private to
        /// just the action. For actions that are part of <see cref="InputActionMap"/>s, the state
        /// is shared by all actions in the map and, if the map itself is part of an <see
        /// cref="InputActionAsset"/>, also by all the maps that are part of the asset.
        ///
        /// To dispose of the state, call <see cref="Dispose"/>.
        ///
        /// <example>
        /// <code>
        /// var gamepad = InputSystem.AddDevice&lt;Gamepad&gt;();
        ///
        /// var action = new InputAction(type: InputActionType.Value, binding: "&lt;Gamepad&gt;/leftTrigger");
        /// action.performed = ctx => Debug.Log("Action triggered!");
        ///
        /// // Perform some fake input on the gamepad. Note that the action
        /// // will *NOT* get triggered as it is not enabled.
        /// // NOTE: We use Update() here only for demonstration purposes. In most cases,
        /// //       it's not a good method to call directly as it basically injects artificial
        /// //       input frames into the player loop. Usually a recipe for breakage.
        /// InputSystem.QueueStateEvent(gamepad, new GamepadState { leftTrigger = 0.5f });
        /// InputSystem.Update();
        ///
        /// action.Enable();
        ///
        /// // Now, with the left trigger already being down and the action enabled, it will
        /// // trigger in the next frame.
        /// InputSystem.Update();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Disable"/>
        /// <seealso cref="enabled"/>
        public void Enable()
        {
            using (k_InputActionEnableProfilerMarker.Auto())
            {
                if (enabled)
                    return;

                // For singleton actions, we create an internal-only InputActionMap
                // private to the action.
                var map = GetOrCreateActionMap();

                // First time we're enabled, find all controls.
                map.ResolveBindingsIfNecessary();

                // Go live.
                map.m_State.EnableSingleAction(this);
            }
        }

        /// <summary>
        /// Disable the action such that is stop listening/responding to input.
        /// </summary>
        /// <remarks>
        /// If the action is already disabled, this method does nothing.
        ///
        /// If the action is currently in progress, i.e. if <see cref="phase"/> is
        /// <see cref="InputActionPhase.Started"/>, the action will be canceled as
        /// part of being disabled. This means that you will see a call on <see cref="canceled"/>
        /// from within the call to <c>Disable()</c>.
        /// </remarks>
        /// <seealso cref="enabled"/>
        /// <seealso cref="Enable"/>
        public void Disable()
        {
            using (k_InputActionDisableProfilerMarker.Auto())
            {
                if (!enabled)
                    return;

                m_ActionMap.m_State.DisableSingleAction(this);
            }
        }

        ////REVIEW: is *not* cloning IDs here really the right thing to do?
        /// <summary>
        /// Return an identical instance of the action.
        /// </summary>
        /// <returns>An identical clone of the action</returns>
        /// <remarks>
        /// Note that if you clone an action that is part of an <see cref="InputActionMap"/>,
        /// you will not get a new action that is part of the same map. Instead, you will
        /// get a free-standing action not associated with any action map.
        ///
        /// Also, note that the <see cref="id"/> of the action is not cloned. Instead, the
        /// clone will receive a new unique ID. Also, callbacks install on events such
        /// as <see cref="started"/> will not be copied over to the clone.
        /// </remarks>
        public InputAction Clone()
        {
            var clone = new InputAction(name: m_Name, type: m_Type)
            {
                m_SingletonActionBindings = bindings.ToArray(),
                m_BindingsCount = m_BindingsCount,
                m_ExpectedControlType = m_ExpectedControlType,
                m_Interactions = m_Interactions,
                m_Processors = m_Processors,
                m_Flags = m_Flags,
            };
            return clone;
        }

        /// <summary>
        /// Return an boxed instance of the action.
        /// </summary>
        /// <returns>An boxed clone of the action</returns>
        /// <seealso cref="Clone"/>
        object ICloneable.Clone()
        {
            return Clone();
        }

        ////TODO: ReadValue(void*, int)

        /// <summary>
        /// Read the current value of the control that is driving this action. If no bound control is actuated, returns
        /// default(TValue), but note that binding processors are always applied.
        /// </summary>
        /// <typeparam name="TValue">Value type to read. Must match the value type of the binding/control that triggered.</typeparam>
        /// <returns>The current value of the control/binding that is driving this action with all binding processors applied.</returns>
        /// <remarks>
        /// This method can be used as an alternative to hooking into <see cref="started"/>, <see cref="performed"/>,
        /// and/or <see cref="canceled"/> and reading out the value using <see cref="CallbackContext.ReadValue{TValue}"/>
        /// there. Instead, this API acts more like a polling API that can be called, for example, as part of
        /// <c>MonoBehaviour.Update</c>.
        ///
        /// <example>
        /// <code>
        /// // Let's say you have a MyControls.inputactions file with "Generate C# Class" enabled
        /// // and it has an action map called "gameplay" with a "move" action of type Vector2.
        /// public class MyBehavior : MonoBehaviour
        /// {
        ///     public MyControls controls;
        ///     public float moveSpeed = 4;
        ///
        ///     protected void Awake()
        ///     {
        ///         controls = new MyControls();
        ///     }
        ///
        ///     protected void OnEnable()
        ///     {
        ///         controls.gameplay.Enable();
        ///     }
        ///
        ///     protected void OnDisable()
        ///     {
        ///         controls.gameplay.Disable();
        ///     }
        ///
        ///     protected void Update()
        ///     {
        ///         var moveVector = controls.gameplay.move.ReadValue&lt;Vector2&gt;() * (moveSpeed * Time.deltaTime);
        ///         //...
        ///     }
        /// }
        /// </code>
        /// </example>
        ///
        /// If the action has button-like behavior, then <see cref="triggered"/> is usually a better alternative to
        /// reading out a float and checking if it is above the button press point.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The given <typeparamref name="TValue"/> type does not match
        /// the value type of the control or composite currently driving the action.</exception>
        /// <seealso cref="triggered"/>
        /// <seealso cref="ReadValueAsObject"/>
        /// <seealso cref="CallbackContext.ReadValue{TValue}"/>
        public unsafe TValue ReadValue<TValue>()
            where TValue : struct
        {
            var state = GetOrCreateActionMap().m_State;
            if (state == null)
                return default(TValue);

            var actionStatePtr = &state.actionStates[m_ActionIndexInState];
            return actionStatePtr->phase.IsInProgress()
                ? state.ReadValue<TValue>(actionStatePtr->bindingIndex, actionStatePtr->controlIndex)
                : state.ApplyProcessors(actionStatePtr->bindingIndex, default(TValue));
        }

        /// <summary>
        /// Same as <see cref="ReadValue{TValue}"/> but read the value without having to know the value type
        /// of the action.
        /// </summary>
        /// <returns>The current value of the action or <c>null</c> if the action is not currently in <see cref="InputActionPhase.Started"/>
        /// or <see cref="InputActionPhase.Performed"/> phase.</returns>
        /// <remarks>
        /// This method allocates GC memory and is thus not a good choice for getting called as part of gameplay
        /// logic.
        /// </remarks>
        /// <seealso cref="ReadValue{TValue}"/>
        /// <seealso cref="InputAction.CallbackContext.ReadValueAsObject"/>
        public unsafe object ReadValueAsObject()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state == null)
                return null;

            var actionStatePtr = &state.actionStates[m_ActionIndexInState];
            if (actionStatePtr->phase.IsInProgress())
            {
                var controlIndex = actionStatePtr->controlIndex;
                if (controlIndex != InputActionState.kInvalidIndex)
                    return state.ReadValueAsObject(actionStatePtr->bindingIndex, controlIndex);
            }

            return null;
        }

        /// <summary>
        /// Read the current amount of actuation of the control that is driving this action.
        /// </summary>
        /// <returns>Returns the current level of control actuation (usually [0..1]) or -1 if
        /// the control is actuated but does not support computing magnitudes.</returns>
        /// <remarks>
        /// <para>
        /// Magnitudes do not make sense for all types of controls. Controls that have no meaningful magnitude
        /// will return -1 when calling this method. Any negative magnitude value should be considered an invalid value.
        /// </para>
        /// <para>
        /// The magnitude returned by an action is usually determined by the
        /// <see cref="InputControl"/> that triggered the action, i.e. by the
        /// control referenced from <see cref="activeControl"/>. See <see cref="InputControl.EvaluateMagnitude()"/> and
        /// <see cref="InputBindingComposite.EvaluateMagnitude"/> for additional information.
        /// </para>
        /// <para>
        /// However, if the binding that triggered is a composite, then the composite
        /// will determine the magnitude and not the individual control that triggered.
        /// Instead, the value of the control that triggered the action will be fed into the composite magnitude calculation.
        /// </para>
        /// </remarks>
        public unsafe float GetControlMagnitude()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                if (actionStatePtr->haveMagnitude)
                    return actionStatePtr->magnitude;
            }

            return 0f;
        }

        /// <summary>
        /// Reset the action state to default.
        /// </summary>
        /// <remarks>
        /// This method can be used to forcibly cancel an action even while it is in progress. Note that unlike
        /// disabling an action, for example, this also effects APIs such as <see cref="WasPressedThisFrame"/>.
        ///
        /// Note that invoking this method will not modify enabled state.
        /// </remarks>
        /// <seealso cref="inProgress"/>
        /// <seealso cref="phase"/>
        /// <seealso cref="Enable"/>
        /// <seealso cref="Disable"/>
        public void Reset()
        {
            var state = GetOrCreateActionMap().m_State;
            state?.ResetActionState(m_ActionIndexInState, toPhase: enabled ? InputActionPhase.Waiting : InputActionPhase.Disabled, hardReset: true);
        }

        /// <summary>
        /// Check whether the current actuation of the action has crossed the button press threshold (see
        /// <see cref="InputSettings.defaultButtonPressPoint"/>) and has not yet fallen back below the
        /// release threshold (see <see cref="InputSettings.buttonReleaseThreshold"/>).
        /// </summary>
        /// <returns>True if the action is considered to be in "pressed" state, false otherwise.</returns>
        /// <remarks>
        /// This method is different from simply reading the action's current <c>float</c> value and comparing
        /// it to the press threshold and is also different from comparing the current actuation of
        /// <see cref="activeControl"/> to it. This is because the current level of actuation might have already
        /// fallen below the press threshold but might not yet have reached the release threshold.
        ///
        /// This method works with any <see cref="type"/> of action, not just buttons.
        ///
        /// Also note that because this operates on the results of <see cref="InputControl.EvaluateMagnitude()"/>,
        /// it works with many kind of controls, not just buttons. For example, if an action is bound
        /// to a <see cref="StickControl"/>, the control will be considered "pressed" once the magnitude
        /// of the Vector2 of the control has crossed the press threshold.
        ///
        /// Finally, note that custom button press points of controls (see <see cref="ButtonControl.pressPoint"/>)
        /// are respected and will take precedence over <see cref="InputSettings.defaultButtonPressPoint"/>.
        ///
        /// <example>
        /// <code>
        /// var up = playerInput.actions["up"];
        /// if (up.IsPressed())
        ///    transform.Translate(0, 10 * Time.deltaTime, 0);
        /// </code>
        /// </example>
        ///
        /// Disabled actions will always return false from this method, even if a control bound to the action
        /// is currently pressed. Also, re-enabling an action will not restore the state to when the action
        /// was disabled even if the control is still actuated.
        /// </remarks>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        /// <seealso cref="ButtonControl.pressPoint"/>
        /// <seealso cref="CallbackContext.ReadValueAsButton"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="WasReleasedThisFrame"/>
        public unsafe bool IsPressed()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->isPressed;
            }
            return false;
        }

        /// <summary>
        /// Whether the action has been <see cref="InputActionPhase.Started"/> or <see cref="InputActionPhase.Performed"/>.
        /// </summary>
        /// <returns>True if the action is currently triggering.</returns>
        /// <seealso cref="phase"/>
        public unsafe bool IsInProgress()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->phase.IsInProgress();
            }
            return false;
        }

        private int ExpectedFrame()
        {
            // Used by the Was<XXX>ThisFrame() methods below.
            // When processing events manually the event processing will happen one frame later.
            //
            int frameOffset = InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsManually ? 1 : 0;
            int expectedFrame = Time.frameCount - frameOffset;
            return expectedFrame;
        }

        /// <summary>
        /// Returns true if the action's value crossed the press threshold (see <see cref="InputSettings.defaultButtonPressPoint"/>)
        /// at any point in the frame.
        /// </summary>
        /// <returns>True if the action was pressed this frame.</returns>
        /// <remarks>
        /// This method is different from <see cref="WasPerformedThisFrame"/> in that it is not bound
        /// to <see cref="phase"/>. Instead, if the action's level of actuation (that is, the level of
        /// magnitude -- see <see cref="InputControl.EvaluateMagnitude()"/> -- of the control(s) bound
        /// to the action) crossed the press threshold (see <see cref="InputSettings.defaultButtonPressPoint"/>)
        /// at any point in the frame, this method will return true. It will do so even if there is an
        /// interaction on the action that has not yet performed the action in response to the press.
        ///
        /// This method works with any <see cref="type"/> of action, not just buttons.
        ///
        /// Also note that because this operates on the results of <see cref="InputControl.EvaluateMagnitude()"/>,
        /// it works with many kind of controls, not just buttons. For example, if an action is bound
        /// to a <see cref="StickControl"/>, the control will be considered "pressed" once the magnitude
        /// of the Vector2 of the control has crossed the press threshold.
        ///
        /// Finally, note that custom button press points of controls (see <see cref="ButtonControl.pressPoint"/>)
        /// are respected and will take precedence over <see cref="InputSettings.defaultButtonPressPoint"/>.
        ///
        /// <example>
        /// <code>
        /// var fire = playerInput.actions["fire"];
        /// if (fire.WasPressedThisFrame() &amp;&amp; fire.IsPressed())
        ///     StartFiring();
        /// else if (fire.WasReleasedThisFrame())
        ///     StopFiring();
        /// </code>
        /// </example>
        ///
        /// This method will disregard whether the action is currently enabled or disabled. It will keep returning
        /// true for the duration of the frame even if the action was subsequently disabled in the frame.
        ///
        /// NOTE: If the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/> and InputSystem.Update() is not called in
        /// the dynamic Update, use <see cref="WasPressedThisDynamicUpdate"/> during dynamic Update instead.
        /// </remarks>
        /// <seealso cref="IsPressed"/>
        /// <seealso cref="WasPressedThisDynamicUpdate"/>
        /// <seealso cref="WasReleasedThisFrame"/>
        /// <seealso cref="CallbackContext.ReadValueAsButton"/>
        /// <seealso cref="WasPerformedThisFrame"/>
        public unsafe bool WasPressedThisFrame()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                var currentUpdateStep = InputUpdate.s_UpdateStepCount;
                return actionStatePtr->pressedInUpdate == currentUpdateStep && currentUpdateStep != default;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the action's value crossed the press threshold (see <see cref="InputSettings.defaultButtonPressPoint"/>)
        /// in the MonoBehaviour Update cycle (rendering frame).
        /// </summary>
        /// <returns>True if the action was pressed in the MonoBehaviour Update cycle (rendering frame).</returns>
        /// <remarks>
        /// Unlike <see cref="WasPressedThisFrame"/>, this method will return true only if the InputSystem was updated and the action was pressed in the current dynamic Update cycle (in between the previous and the current frame).
        /// This can be used in dynamic update if the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/>.
        /// If the update mode is set to <see cref="InputSettings.UpdateMode.ProcessEventsInDynamicUpdate"/>, this method will behave exactly like <see cref="WasPressedThisFrame"/>.
        ///
        /// When processing input events manually, updating the InputSystem in the dynamic Update cycle will lead to a delay of one frame for WasPressedThisDynamicUpdate,
        /// you may want to use WasPressedThisFrame to avoid this, or set the input update mode to InputSettings.UpdateMode.ProcessEventsInDynamicUpdate.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = playerInput.actions["fire"];
        /// if (fire.WasPressedThisDynamicUpdate() &amp;&amp; fire.IsPressed())
        ///     StartFiring();
        /// else if (fire.WasReleasedThisDynamicUpdate())
        ///     StopFiring();
        /// </code>
        /// </example>
        /// <seealso cref="IsPressed"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="WasReleasedThisFrame"/>
        /// <seealso cref="WasPerformedThisFrame"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public unsafe bool WasPressedThisDynamicUpdate()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->framePressed == ExpectedFrame();
            }

            return false;
        }

        /// <summary>
        /// Returns true if the action's value crossed the release threshold (see <see cref="InputSettings.buttonReleaseThreshold"/>)
        /// at any point in the frame after being in pressed state.
        /// </summary>
        /// <returns>True if the action was released this frame.</returns>
        /// <remarks>
        /// This method works with any <see cref="type"/> of action, not just buttons.
        ///
        /// Also note that because this operates on the results of <see cref="InputControl.EvaluateMagnitude()"/>,
        /// it works with many kind of controls, not just buttons. For example, if an action is bound
        /// to a <see cref="StickControl"/>, the control will be considered "pressed" once the magnitude
        /// of the Vector2 of the control has crossed the press threshold.
        ///
        /// Finally, note that custom button press points of controls (see <see cref="ButtonControl.pressPoint"/>)
        /// are respected and will take precedence over <see cref="InputSettings.defaultButtonPressPoint"/>.
        ///
        /// <example>
        /// <code>
        /// var fire = playerInput.actions["fire"];
        /// if (fire.WasPressedThisFrame() &amp;&amp; fire.IsPressed())
        ///     StartFiring();
        /// else if (fire.WasReleasedThisFrame())
        ///     StopFiring();
        /// </code>
        /// </example>
        ///
        /// This method will disregard whether the action is currently enabled or disabled. It will keep returning
        /// true for the duration of the frame even if the action was subsequently disabled in the frame.
        ///
        /// NOTE: If the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/> and InputSystem.Update() is not called in
        /// the dynamic Update, use <see cref="WasReleasedThisDynamicUpdate"/> during dynamic Update instead.
        /// </remarks>
        /// <seealso cref="IsPressed"/>
        /// <seealso cref="WasReleasedThisDynamicUpdate"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="CallbackContext.ReadValueAsButton"/>
        /// <seealso cref="WasCompletedThisFrame"/>
        public unsafe bool WasReleasedThisFrame()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                var currentUpdateStep = InputUpdate.s_UpdateStepCount;
                return actionStatePtr->releasedInUpdate == currentUpdateStep && currentUpdateStep != default;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the action's value crossed the release threshold (see <see cref="InputSettings.buttonReleaseThreshold"/>)
        /// at any point in the MonoBehaviour Update cycle (rendering frame).
        /// </summary>
        /// <returns>True if the action was released in the MonoBehaviour Update cycle (rendering frame).</returns>
        /// <remarks>
        /// Unlike <see cref="WasReleasedThisFrame"/>, this method will return true only if the InputSystem was updated and the action was released in the current dynamic Update cycle (in between the previous and the current frame).
        /// This can be used in dynamic update if the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/>.
        /// If the update mode is set to <see cref="InputSettings.UpdateMode.ProcessEventsInDynamicUpdate"/>, this method will behave exactly like <see cref="WasReleasedThisFrame"/>.
        ///
        /// When processing input events manually, updating the InputSystem in the dynamic Update cycle will lead to a delay of one frame for WasReleasedThisDynamicUpdate,
        /// you may want to use WasReleasedThisFrame to avoid this, or set the input update mode to InputSettings.UpdateMode.ProcessEventsInDynamicUpdate.
        /// </remarks>
        /// <example>
        /// <code>
        /// var fire = playerInput.actions["fire"];
        /// if (fire.WasPressedThisDynamicUpdate() &amp;&amp; fire.IsPressed())
        ///     StartFiring();
        /// else if (fire.WasReleasedThisDynamicUpdate())
        ///     StopFiring();
        /// </code>
        /// </example>
        /// <seealso cref="IsPressed"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="WasReleasedThisFrame"/>
        /// <seealso cref="CallbackContext.ReadValueAsButton"/>
        /// <seealso cref="WasCompletedThisFrame"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public unsafe bool WasReleasedThisDynamicUpdate()
        {
            var state = GetOrCreateActionMap().m_State;
            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->frameReleased == ExpectedFrame();
            }

            return false;
        }

        ////REVIEW: Should we also have WasStartedThisFrame()? (and WasCanceledThisFrame()?)

        /// <summary>
        /// Check whether <see cref="phase"/> was <see cref="InputActionPhase.Performed"/> at any point
        /// in the current frame.
        /// </summary>
        /// <returns>True if the action performed this frame.</returns>
        /// <remarks>
        /// This method is different from <see cref="WasPressedThisFrame"/> in that it depends directly on the
        /// interaction(s) driving the action (including the default interaction if no specific interaction
        /// has been added to the action or binding).
        ///
        /// For example, let's say the action is bound to the space bar and that the binding has a
        /// <see cref="Interactions.HoldInteraction"/> assigned to it. In the frame where the space bar
        /// is pressed, <see cref="WasPressedThisFrame"/> will be true (because the button/key is now pressed)
        /// but <c>WasPerformedThisFrame</c> will still be false (because the hold has not been performed yet).
        /// Only after the hold time has expired will <c>WasPerformedThisFrame</c> be true and only in the frame
        /// where the hold performed.
        ///
        /// This is different from checking <see cref="phase"/> directly as the action might have already progressed
        /// to a different phase after performing. In other words, even if an action performed in a frame, <see cref="phase"/>
        /// might no longer be <see cref="InputActionPhase.Performed"/>, whereas <c>WasPerformedThisFrame</c> will remain
        /// true for the entirety of the frame regardless of what else the action does.
        ///
        /// Unlike <see cref="ReadValue{TValue}"/>, which will reset when the action goes back to waiting
        /// state, this property will stay true for the duration of the current frame (that is, until the next
        /// <see cref="InputSystem.Update"/> runs) as long as the action was triggered at least once.
        ///
        /// <example>
        /// <code>
        /// var warp = playerInput.actions["Warp"];
        /// if (warp.WasPerformedThisFrame())
        ///     InitiateWarp();
        /// </code>
        /// </example>
        ///
        /// This method will disregard whether the action is currently enabled or disabled. It will keep returning
        /// true for the duration of the frame even if the action was subsequently disabled in the frame.
        ///
        /// NOTE: If the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/> and InputSystem.Update() is not called in
        /// the dynamic Update, use <see cref="WasPerformedThisDynamicUpdate"/> when trying to access in dynamic Update instead.
        /// </remarks>
        /// <seealso cref="WasPerformedThisDynamicUpdate"/>
        /// <seealso cref="WasCompletedThisFrame"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="phase"/>
        public unsafe bool WasPerformedThisFrame()
        {
            var state = GetOrCreateActionMap().m_State;

            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                var currentUpdateStep = InputUpdate.s_UpdateStepCount;
                return actionStatePtr->lastPerformedInUpdate == currentUpdateStep && currentUpdateStep != default;
            }

            return false;
        }

        /// <summary>
        /// Check whether <see cref="phase"/> was <see cref="InputActionPhase.Performed"/> at any point
        /// in the MonoBehaviour Update cycle (rendering frame).
        /// </summary>
        /// <returns>True if the action performed in the MonoBehaviour Update cycle (rendering frame).</returns>
        /// <remarks>
        /// Unlike <see cref="WasPerformedThisFrame"/>, this method will return true only if the InputSystem was updated and the action was performed in the current dynamic Update cycle (in between the previous and the current frame).
        /// This can be used in dynamic update if the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/>.
        /// If the update mode is set to <see cref="InputSettings.UpdateMode.ProcessEventsInDynamicUpdate"/>, this method will behave exactly like <see cref="WasPerformedThisFrame"/>.
        ///
        /// When processing input events manually, updating the InputSystem in the dynamic Update cycle will lead to a delay of one frame for WasPerformedThisDynamicUpdate,
        /// you may want to use WasPerformedThisFrame to avoid this, or set the input update mode to InputSettings.UpdateMode.ProcessEventsInDynamicUpdate.
        /// </remarks>
        /// <example>
        /// <code>
        /// var warp = playerInput.actions["Warp"];
        /// if (warp.WasPerformedThisDynamicUpdate())
        ///     InitiateWarp();
        /// </code>
        /// </example>
        /// <seealso cref="WasPerformedThisFrame"/>
        /// <seealso cref="WasCompletedThisFrame"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="phase"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public unsafe bool WasPerformedThisDynamicUpdate()
        {
            var state = GetOrCreateActionMap().m_State;

            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->framePerformed == ExpectedFrame();
            }

            return false;
        }

        /// <summary>
        /// Check whether <see cref="phase"/> transitioned from <see cref="InputActionPhase.Performed"/> to any other phase
        /// value at least once in the current frame.
        /// </summary>
        /// <returns>True if the action completed this frame.</returns>
        /// <remarks>
        /// <para>
        /// Although <see cref="InputActionPhase.Disabled"/> is technically a phase, this method does not consider disabling
        /// the action while the action is in <see cref="InputActionPhase.Performed"/> to be "completed".
        /// </para>
        /// <para>
        /// This method is different from <see cref="WasReleasedThisFrame"/> in that it depends directly on the
        /// interaction(s) driving the action (including the default interaction if no specific interaction
        /// has been added to the action or binding).
        /// </para>
        /// <para>
        /// For example, let's say the action is bound to the space bar and that the binding has a
        /// <see cref="Interactions.HoldInteraction"/> assigned to it. In the frame where the space bar
        /// is pressed, <see cref="WasPressedThisFrame"/> will be true (because the button/key is now pressed)
        /// but <see cref="WasPerformedThisFrame"/> will still be false (because the hold has not been performed yet).
        /// If at that time the space bar is released, <see cref="WasReleasedThisFrame"/> will be true (because the
        /// button/key is now released) but <c>WasCompletedThisFrame</c> will still be false (because the hold
        /// had not been performed yet). If instead the space bar is held down for long enough for the hold interaction,
        /// the phase will change to and stay <see cref="InputActionPhase.Performed"/> and <see cref="WasPerformedThisFrame"/>
        /// will be true for one frame as it meets the duration threshold. Once released, <c>WasCompletedThisFrame</c> will be true
        /// (because the action is no longer performed) and only in the frame where the hold transitioned away from Performed.
        /// </para>
        /// <para>
        /// For another example where the action could be considered pressed but also completed, let's say the action
        /// is bound to the thumbstick and that the binding has a Sector interaction from the XR Interaction Toolkit assigned
        /// to it such that it only performs in the forward sector area past a button press threshold. In the frame where the
        /// thumbstick is pushed forward, both <see cref="WasPressedThisFrame"/> will be true (because the thumbstick actuation is
        /// now considered pressed) and <see cref="WasPerformedThisFrame"/> will be true (because the thumbstick is in
        /// the forward sector). If the thumbstick is then moved to the left in a sweeping motion, <see cref="IsPressed"/>
        /// will still be true. However, <c>WasCompletedThisFrame</c> will also be true (because the thumbstick is
        /// no longer in the forward sector while still crossed the button press threshold) and only in the frame where
        /// the thumbstick was no longer within the forward sector. For more details about the Sector interaction, see
        /// <a href="https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/api/UnityEngine.XR.Interaction.Toolkit.Inputs.Interactions.SectorInteraction.html"><c>SectorInteraction</c></a>
        /// in the XR Interaction Toolkit Scripting API documentation.
        /// </para>
        /// <para>
        /// Unlike <see cref="ReadValue{TValue}"/>, which will reset when the action goes back to waiting
        /// state, this property will stay true for the duration of the current frame (that is, until the next
        /// <see cref="InputSystem.Update"/> runs) as long as the action was completed at least once.
        /// </para>
        /// <para>
        /// This method will disregard whether the action is currently enabled or disabled. It will keep returning
        /// true for the duration of the frame even if the action was subsequently disabled in the frame.
        /// </para>
        /// <para>
        /// NOTE: If the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/> and InputSystem.Update() is not called in
        /// the dynamic Update, use <see cref="WasCompletedThisDynamicUpdate"/> to access this during dynamic Update instead.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var teleport = playerInput.actions["Teleport"];
        /// if (teleport.WasPerformedThisFrame())
        ///     InitiateTeleport();
        /// else if (teleport.WasCompletedThisFrame())
        ///     StopTeleport();
        /// </code>
        /// </example>
        /// <seealso cref="WasCompletedThisDynamicUpdate"/>
        /// <seealso cref="WasPerformedThisFrame"/>
        /// <seealso cref="WasReleasedThisFrame"/>
        /// <seealso cref="phase"/>
        public unsafe bool WasCompletedThisFrame()
        {
            var state = GetOrCreateActionMap().m_State;

            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                var currentUpdateStep = InputUpdate.s_UpdateStepCount;
                return actionStatePtr->lastCompletedInUpdate == currentUpdateStep && currentUpdateStep != default;
            }

            return false;
        }

        /// <summary>
        /// Check whether <see cref="phase"/> transitioned from <see cref="InputActionPhase.Performed"/> to any other phase
        /// value at least once in the MonoBehaviour Update cycle (rendering frame).
        /// </summary>
        /// <returns>True if the action completed in this MonoBehaviour Update cycle (rendering frame).</returns>
        /// <remarks>
        /// Unlike <see cref="WasCompletedThisFrame"/>, this method will return true only if the InputSystem was updated and the action was completed in the current dynamic Update cycle (in between the previous and the current frame).
        /// This can be used in dynamic update if the <see cref="InputSettings.updateMode"/> is set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/> or <see cref="InputSettings.UpdateMode.ProcessEventsManually"/>.
        /// If the update mode is set to <see cref="InputSettings.UpdateMode.ProcessEventsInDynamicUpdate"/>, this method will behave exactly like <see cref="WasCompletedThisFrame"/>.
        ///
        /// When processing input events manually, updating the InputSystem in the dynamic Update cycle will lead to a delay of one frame for WasCompletedThisDynamicUpdate,
        /// you may want to use WasCompletedThisFrame to avoid this, or set the input update mode to InputSettings.UpdateMode.ProcessEventsInDynamicUpdate.
        /// </remarks>
        /// <example>
        /// <code>
        /// var teleport = playerInput.actions["Teleport"];
        /// if (teleport.WasPerformedThisDynamicUpdate())
        ///     InitiateTeleport();
        /// else if (teleport.WasCompletedThisDynamicUpdate())
        ///     StopTeleport();
        /// </code>
        /// </example>
        /// <seealso cref="WasPerformedThisFrame"/>
        /// <seealso cref="WasCompletedThisFrame"/>
        /// <seealso cref="WasPressedThisFrame"/>
        /// <seealso cref="phase"/>
        /// <seealso cref="InputSettings.updateMode"/>
        public unsafe bool WasCompletedThisDynamicUpdate()
        {
            var state = GetOrCreateActionMap().m_State;

            if (state != null)
            {
                var actionStatePtr = &state.actionStates[m_ActionIndexInState];
                return actionStatePtr->frameCompleted == ExpectedFrame();
            }

            return false;
        }

        /// <summary>
        /// Return the completion percentage of the timeout (if any) running on the current interaction.
        /// </summary>
        /// <returns>A value &gt;= 0 (no progress) and &lt;= 1 (finished) indicating the level of completion
        /// of the currently running timeout.</returns>
        /// <remarks>
        /// This method is useful, for example, when providing UI feedback for an ongoing action. If, say,
        /// you have a <see cref="Interactions.HoldInteraction"/> on a binding, you might want to show a
        /// progress indicator in the UI and need to know how far into the hold the action
        /// current is. Once the hold has been started, this method will return how far into the hold
        /// the action currently is.
        ///
        /// Note that if an interaction performs and stays performed (see <see cref="InputInteractionContext.PerformedAndStayPerformed"/>),
        /// the completion percentage will remain at 1 until the interaction is canceled.
        ///
        /// Also note that completion is based on the progression of time and not dependent on input
        /// updates. This means that if, for example, the timeout for a <see cref="Interactions.HoldInteraction"/>
        /// has expired according the current time but the expiration has not yet been processed by
        /// an input update (thus causing the hold to perform), the returned completion percentage
        /// will still be 1. In other words, there isn't always a correlation between the current
        /// completion percentage and <see cref="phase"/>.
        ///
        /// The meaning of the timeout is dependent on the interaction in play. For a <see cref="Interactions.HoldInteraction"/>,
        /// "completion" represents the duration timeout (that is, the time until a "hold" is considered to be performed), whereas
        /// for a <see cref="Interactions.TapInteraction"/> "completion" represents "time to failure" (that is, the remaining time window
        /// that the interaction can be completed within).
        ///
        /// Note that an interaction might run multiple timeouts in succession. One such example is <see cref="Interactions.MultiTapInteraction"/>.
        /// In this case, progression towards a single timeout does not necessarily mean progression towards completion
        /// of the whole interaction. An interaction can call <see cref="InputInteractionContext.SetTotalTimeoutCompletionTime"/>
        /// to inform the Input System of the total length of timeouts to run. If this is done, the result of the
        /// <c>GetTimeoutCompletionPercentage</c> method will return a value reflecting the progression with respect
        /// to total time.
        ///
        /// <example>
        /// <code>
        /// // Scale a UI element in response to the completion of a hold on the gamepad's A button.
        ///
        /// Transform uiObjectToScale;
        ///
        /// InputAction holdAction;
        ///
        /// void OnEnable()
        /// {
        ///     if (holdAction == null)
        ///     {
        ///         // Create hold action with a 2 second timeout.
        ///         // NOTE: Here we create the action in code. You can, of course, grab the action from an .inputactions
        ///         //       asset created in the editor instead.
        ///         holdAction = new InputAction(type: InputActionType.Button, interactions: "hold(duration=2)");
        ///
        ///         // Show the UI object when the hold starts and hide it when it ends.
        ///         holdAction.started += _ =&gt; uiObjectToScale.SetActive(true);
        ///         holdAction.canceled += _ =&gt; uiObjectToScale.SetActive(false);
        ///
        ///         // If you want to play a visual effect when the action performs, you can initiate from
        ///         // the performed callback.
        ///         holdAction.performed += _ =&gt; /* InitiateVisualEffectWhenHoldIsComplete() */;
        ///     }
        ///
        ///     holdAction.Enable();
        ///
        ///     // Hide the UI object until the action is started.
        ///     uiObjectToScale.gameObject.SetActive(false);
        /// }
        ///
        /// void OnDisable()
        /// {
        ///     holdAction.Disable();
        /// }
        ///
        /// void Update()
        /// {
        ///     var completion = holdAction.GetTimeoutCompletionPercentage();
        ///     uiObjectToScale.localScale = new Vector3(1, completion, 1);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="IInputInteraction"/>
        /// <seealso cref="InputInteractionContext.SetTimeout"/>
        /// <seealso cref="InputInteractionContext.SetTotalTimeoutCompletionTime"/>
        public unsafe float GetTimeoutCompletionPercentage()
        {
            var actionMap = GetOrCreateActionMap();
            var state = actionMap.m_State;

            // If there's no state, there can't be activity on the action so our completion
            // percentage must be zero.
            if (state == null)
                return 0;

            ref var actionState = ref state.actionStates[m_ActionIndexInState];
            var interactionIndex = actionState.interactionIndex;
            if (interactionIndex == -1)
            {
                ////REVIEW: should this use WasPerformedThisFrame()?
                // There's no interactions on the action or on the currently active binding, so go
                // entirely by the current phase. Performed is 100%, everything else is 0%.
                return actionState.phase == InputActionPhase.Performed ? 1 : 0;
            }

            ref var interactionState = ref state.interactionStates[interactionIndex];
            switch (interactionState.phase)
            {
                case InputActionPhase.Started:
                    // If the interaction was started and there is a timer running, the completion level
                    // is determined by far we are between the interaction start time and timer expiration.
                    var timerCompletion = 0f;
                    if (interactionState.isTimerRunning)
                    {
                        var duration = interactionState.timerDuration;
                        var startTime = interactionState.timerStartTime;
                        var endTime = startTime + duration;
                        var remainingTime = endTime - InputState.currentTime;
                        if (remainingTime <= 0)
                            timerCompletion = 1;
                        else
                            timerCompletion = (float)((duration - remainingTime) / duration);
                    }

                    if (interactionState.totalTimeoutCompletionTimeRemaining > 0)
                    {
                        return (interactionState.totalTimeoutCompletionDone + timerCompletion * interactionState.timerDuration)  /
                            (interactionState.totalTimeoutCompletionDone + interactionState.totalTimeoutCompletionTimeRemaining);
                    }
                    else
                    {
                        return timerCompletion;
                    }

                case InputActionPhase.Performed:
                    return 1;
            }

            return 0;
        }

        ////REVIEW: it would be best if these were InternedStrings; however, for serialization, it has to be strings
        [Tooltip("Human readable name of the action. Must be unique within its action map (case is ignored). Can be changed "
            + "without breaking references to the action.")]
        [SerializeField] internal string m_Name;
        [Tooltip("Determines how the action triggers.\n"
            + "\n"
            + "A Value action will start and perform when a control moves from its default value and then "
            + "perform on every value change. It will cancel when controls go back to default value. Also, when enabled, a Value "
            + "action will respond right away to a control's current value.\n"
            + "\n"
            + "A Button action will start when a button is pressed and perform when the press threshold (see 'Default Button Press Point' in settings) "
            + "is reached. It will cancel when the button is going below the release threshold (see 'Button Release Threshold' in settings). Also, "
            + "if a button is already pressed when the action is enabled, the button has to be released first.\n"
            + "\n"
            + "A Pass-Through action will not explicitly start and will never cancel. Instead, for every value change on any bound control, "
            + "the action will perform.")]
        [SerializeField] internal InputActionType m_Type;
        [FormerlySerializedAs("m_ExpectedControlLayout")]
        [Tooltip("The type of control expected by the action (e.g. \"Button\" or \"Stick\"). This will limit the controls shown "
            + "when setting up bindings in the UI and will also limit which controls can be bound interactively to the action.")]
        [SerializeField] internal string m_ExpectedControlType;
        [Tooltip("Unique ID of the action (GUID). Used to reference the action from bindings such that actions can be renamed "
            + "without breaking references.")]
        [SerializeField] internal string m_Id; // Can't serialize System.Guid and Unity's GUID is editor only.
        [SerializeField] internal string m_Processors;
        [SerializeField] internal string m_Interactions;

        // For singleton actions, we serialize the bindings directly as part of the action.
        // For any other type of action, this is null.
        [SerializeField] internal InputBinding[] m_SingletonActionBindings;
        [SerializeField] internal ActionFlags m_Flags;

        [NonSerialized] internal InputBinding? m_BindingMask;
        [NonSerialized] internal int m_BindingsStartIndex;
        [NonSerialized] internal int m_BindingsCount;
        [NonSerialized] internal int m_ControlStartIndex;
        [NonSerialized] internal int m_ControlCount;

        /// <summary>
        /// Index of the action in the <see cref="InputActionState"/> associated with the
        /// action's <see cref="InputActionMap"/>.
        /// </summary>
        /// <remarks>
        /// This is not necessarily the same as the index of the action in its map.
        /// </remarks>
        /// <seealso cref="actionMap"/>
        [NonSerialized] internal int m_ActionIndexInState = InputActionState.kInvalidIndex;

        /// <summary>
        /// The action map that owns the action.
        /// </summary>
        /// <remarks>
        /// This is not serialized. The action map will restore this back references after deserialization.
        /// </remarks>
        [NonSerialized] internal InputActionMap m_ActionMap;

        // Listeners. No array allocations if only a single listener.
        [NonSerialized] internal CallbackArray<Action<CallbackContext>> m_OnStarted;
        [NonSerialized] internal CallbackArray<Action<CallbackContext>> m_OnCanceled;
        [NonSerialized] internal CallbackArray<Action<CallbackContext>> m_OnPerformed;

        /// <summary>
        /// Whether the action is a loose action created in code (e.g. as a property on a component).
        /// </summary>
        /// <remarks>
        /// Singleton actions are not contained in maps visible to the user. Internally, we do create
        /// a map for them that contains just the singleton action. To the action system, there are no
        /// actions without action maps.
        /// </remarks>
        internal bool isSingletonAction => m_ActionMap == null || ReferenceEquals(m_ActionMap.m_SingletonAction, this);

        [Flags]
        internal enum ActionFlags
        {
            WantsInitialStateCheck = 1 << 0,
        }

        private InputActionState.TriggerState currentState
        {
            get
            {
                if (m_ActionIndexInState == InputActionState.kInvalidIndex)
                    return new InputActionState.TriggerState();
                Debug.Assert(m_ActionMap != null, "Action must have associated action map");
                Debug.Assert(m_ActionMap.m_State != null, "Action map must have state at this point");
                return m_ActionMap.m_State.FetchActionState(this);
            }
        }

        internal string MakeSureIdIsInPlace()
        {
            if (string.IsNullOrEmpty(m_Id))
                GenerateId();
            return m_Id;
        }

        internal void GenerateId()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        internal InputActionMap GetOrCreateActionMap()
        {
            if (m_ActionMap == null)
                CreateInternalActionMapForSingletonAction();
            return m_ActionMap;
        }

        private void CreateInternalActionMapForSingletonAction()
        {
            m_ActionMap = new InputActionMap
            {
                m_Actions = new[] { this },
                m_SingletonAction = this,
                m_Bindings = m_SingletonActionBindings
            };
        }

        internal void RequestInitialStateCheckOnEnabledAction()
        {
            Debug.Assert(enabled, "This should only be called on actions that are enabled");

            var map = GetOrCreateActionMap();
            var state = map.m_State;
            state.SetInitialStateCheckPending(m_ActionIndexInState);
        }

        // NOTE: This does *NOT* check whether the control is valid according to the binding it
        //       resolved from and/or the current binding mask. If, for example, the binding is
        //       "<Keyboard>/#(ä)" and the keyboard switches from a DE layout to a US layout, the
        //       key would still be considered valid even if the path in the binding would actually
        //       no longer resolve to it.
        internal bool ActiveControlIsValid(InputControl control)
        {
            if (control == null)
                return false;

            // Device must still be added.
            var device = control.device;
            if (!device.added)
                return false;

            // If we have a device list in the map or asset, device
            // must be in list.
            var map = GetOrCreateActionMap();
            var deviceList = map.devices;
            if (deviceList != null && !deviceList.Value.ContainsReference(device))
                return false;

            return true;
        }

        internal InputBinding? FindEffectiveBindingMask()
        {
            if (m_BindingMask.HasValue)
                return m_BindingMask;

            if (m_ActionMap?.m_BindingMask != null)
                return m_ActionMap.m_BindingMask;

            return m_ActionMap?.m_Asset?.m_BindingMask;
        }

        internal int BindingIndexOnActionToBindingIndexOnMap(int indexOfBindingOnAction)
        {
            // We don't want to hit InputAction.bindings here as this requires setting up per-action
            // binding info which we then nuke as part of the override process. Calling ApplyBindingOverride
            // repeatedly with an index would thus cause the same data to be computed and thrown away
            // over and over.
            // Instead we manually search through the map's bindings to find the right binding index
            // in the map.

            var actionMap = GetOrCreateActionMap();
            var bindingsInMap = actionMap.m_Bindings;
            var bindingCountInMap = bindingsInMap.LengthSafe();
            var actionName = name;

            var currentBindingIndexOnAction = -1;
            for (var i = 0; i < bindingCountInMap; ++i)
            {
                ref var binding = ref bindingsInMap[i];

                if (!binding.TriggersAction(this))
                    continue;

                ++currentBindingIndexOnAction;
                if (currentBindingIndexOnAction == indexOfBindingOnAction)
                    return i;
            }

            throw new ArgumentOutOfRangeException(nameof(indexOfBindingOnAction),
                $"Binding index {indexOfBindingOnAction} is out of range for action '{this}' with {currentBindingIndexOnAction + 1} bindings");
        }

        internal int BindingIndexOnMapToBindingIndexOnAction(int indexOfBindingOnMap)
        {
            var actionMap = GetOrCreateActionMap();
            var bindingsInMap = actionMap.m_Bindings;
            var actionName = name;

            var bindingIndexOnAction = 0;
            for (var i = indexOfBindingOnMap - 1; i >= 0; --i)
            {
                ref var binding = ref bindingsInMap[i];

                if (string.Compare(binding.action, actionName, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                    binding.action == m_Id)
                    ++bindingIndexOnAction;
            }

            return bindingIndexOnAction;
        }

        ////TODO: make current event available in some form
        ////TODO: make source binding info available (binding index? binding instance?)

        /// <summary>
        /// Information provided to action callbacks about what triggered an action.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The callback context represents the current state of an <see cref="action"/> associated with the callback
        /// and provides information associated with the bound <see cref="control"/>, its value, and its
        /// <see cref="phase"/>.
        /// </para>
        /// <para>
        /// The callback context provides you with a way to consume events (push-based input) as part of an update when using
        /// input action callback notifications. For example, <see cref="InputAction.started"/>,
        /// <see cref="InputAction.performed"/>, <see cref="InputAction.canceled"/> rather than relying on
        /// pull-based reading. Also see <a href="https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/RespondingToActions.html">
        /// Responding To Actions</a> for additional information on differences between callbacks and polling.
        /// </para>
        /// <para>
        /// Use this struct to read the current input value through any of the read-method overloads:
        /// <see cref="ReadValue{T}()"/>, <see cref="ReadValueAsButton"/>,
        /// <see cref="ReadValueAsObject()"/> or <see cref="ReadValue"/> (unsafe). If you don't know the expected value type,
        /// you might need to check <see cref="valueType"/> before reading the value.
        /// </para>
        /// <para>
        /// Use the <see cref="phase"/> property to get the current phase of the associated action or
        /// evaluate it directly using any of the convenience methods <see cref="started"/>, <see cref="performed"/>,
        /// <see cref="canceled"/>.
        /// </para>
        /// <para>
        /// To obtain information about the current timestamp of the associated event, or to check when the event
        /// started, use <see cref="time"/> or <see cref="startTime"/> respectively.
        /// </para>
        /// <para>
        /// You should not use or keep this struct outside of the callback.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using UnityEngine;
        /// using UnityEngine.InputSystem;
        /// using UnityEngine.InputSystem.Interactions;
        ///
        /// public class MyController : MonoBehaviour
        ///  {
        ///      [SerializeField] InputActionReference move;
        ///      [SerializeField] InputActionReference fire;
        ///
        ///      void Awake()
        ///      {
        ///          /// Receive notifications when move or fire actions are performed
        ///          move.action.performed += MovePerformed;
        ///          fire.action.performed += FirePerformed;
        ///      }
        ///
        ///      void OnEnable()
        ///      {
        ///          /// Enable actions as part of enabling this behavior.
        ///          move.action.Enable();
        ///          fire.action.Enable();
        ///      }
        ///
        ///      void OnDisable()
        ///      {
        ///          /// Disable actions as part of disabling this behavior.
        ///          move.action.Disable();
        ///          fire.action.Disable();
        ///      }
        ///
        ///      void MovePerformed(InputAction.CallbackContext context)
        ///      {
        ///          /// Read the current 2D vector value reported by the associated input action.
        ///          var direction = context.ReadValue&lt;Vector2&gt;();
        ///          Debug.Log("Move: " + direction * Time.deltaTime);
        ///      }
        ///
        ///      void FirePerformed(InputAction.CallbackContext context)
        ///      {
        ///          /// If underlying interaction is a slow-tap fire charged projectile, otherwise fire regular
        ///          /// projectile.
        ///          if (context.interaction is SlowTapInteraction)
        ///              Debug.Log("Fire charged projectile");
        ///          else
        ///              Debug.Log("Fire projectile");
        ///      }
        ///  }
        /// </code>
        /// </example>
        /// <seealso cref="performed"/>
        /// <seealso cref="started"/>
        /// <seealso cref="canceled"/>
        /// <seealso cref="InputActionMap.actionTriggered"/>
        public struct CallbackContext // Ideally would be a ref struct but couldn't use it in lambdas then.
        {
            internal InputActionState m_State;
            internal int m_ActionIndex;

            ////REVIEW: there should probably be a mechanism for the user to be able to correlate
            ////        the callback to a specific binding on the action

            private int actionIndex => m_ActionIndex;
            private unsafe int bindingIndex => m_State.actionStates[actionIndex].bindingIndex;
            private unsafe int controlIndex => m_State.actionStates[actionIndex].controlIndex;
            private unsafe int interactionIndex => m_State.actionStates[actionIndex].interactionIndex;

            /// <summary>
            /// Current phase of the action. Equivalent to accessing <see cref="InputAction.phase"/>
            /// on <see cref="action"/>.
            /// </summary>
            /// <value>Current phase of the action.</value>
            /// <seealso cref="started"/>
            /// <seealso cref="performed"/>
            /// <seealso cref="canceled"/>
            /// <seealso cref="InputAction.phase"/>
            public unsafe InputActionPhase phase
            {
                get
                {
                    if (m_State == null)
                        return InputActionPhase.Disabled;
                    return m_State.actionStates[actionIndex].phase;
                }
            }

            /// <summary>
            /// Whether the <see cref="action"/> has just been started.
            /// </summary>
            /// <remarks>If true, the action was just started.</remarks>
            /// <seealso cref="InputAction.started"/>
            public bool started => phase == InputActionPhase.Started;

            /// <summary>
            /// Whether the <see cref="action"/> has just been performed.
            /// </summary>
            /// <remarks>If true, the action was just performed.</remarks>
            /// <seealso cref="InputAction.performed"/>
            public bool performed => phase == InputActionPhase.Performed;

            /// <summary>
            /// Whether the <see cref="action"/> has just been canceled.
            /// </summary>
            /// <remarks>If true, the action was just canceled.</remarks>
            /// <seealso cref="InputAction.canceled"/>
            public bool canceled => phase == InputActionPhase.Canceled;

            /// <summary>
            /// The associated action that triggered the callback.
            /// </summary>
            public InputAction action => m_State?.GetActionOrNull(bindingIndex);

            /// <summary>
            /// The control that triggered the action.
            /// </summary>
            /// <remarks>
            /// In case of a composite binding, this is the control of the composite that activated the
            /// composite as a whole. For example, in case of a WASD-style binding, it could be the W key.
            ///
            /// Note that an action may also change its <see cref="phase"/> in response to a timeout.
            /// For example, a <see cref="Interactions.TapInteraction"/> will cancel itself if the
            /// button control is not released within a certain time. When this happens, the <c>control</c>
            /// property will be the control that last fed input into the action.
            /// </remarks>
            /// <seealso cref="InputAction.controls"/>
            /// <seealso cref="InputBinding.path"/>
            public InputControl control => m_State?.controls[controlIndex];

            /// <summary>
            /// The interaction that triggered the action or <c>null</c> if the binding that triggered does not
            /// have any particular interaction set on it.
            /// </summary>
            /// <remarks>
            /// <example>
            /// <code>
            /// using UnityEngine;
            /// using UnityEngine.InputSystem;
            /// using UnityEngine.InputSystem.Interactions;
            ///
            /// class Example : MonoBehaviour
            /// {
            ///     public InputActionReference fire;
            ///
            ///     public void Awake()
            ///     {
            ///         fire.action.performed += FirePerformed;
            ///     }
            ///
            ///     void OnEnable() => fire.action.Enable();
            ///     void OnDisable() => fire.action.Disable();
            ///
            ///     void FirePerformed(InputAction.CallbackContext context)
            ///     {
            ///          /// If SlowTap interaction was performed, perform a charged
            ///          /// firing. Otherwise, fire normally.
            ///          if (context.interaction is SlowTapInteraction)
            ///              Debug.Log("Fire charged projectile");
            ///          else
            ///              Debug.Log("Fire projectile");
            ///     }
            /// }
            /// </code>
            /// </example>
            /// </remarks>
            /// <seealso cref="InputBinding.interactions"/>
            /// <seealso cref="InputAction.interactions"/>
            public IInputInteraction interaction
            {
                get
                {
                    if (m_State == null)
                        return null;
                    var index = interactionIndex;
                    if (index == InputActionState.kInvalidIndex)
                        return null;
                    return m_State.interactions[index];
                }
            }

            /// <summary>
            /// The time at which the action got triggered.
            /// </summary>
            /// <remarks>
            /// Time is relative to <see cref="UnityEngine.Time.realtimeSinceStartup"/> at which the action got
            /// triggered.
            ///
            /// This is usually determined by the timestamp of the input event that activated a control
            /// bound to the action. What this means is that this is normally <em>not</em> the
            /// value of <c>Time.realtimeSinceStartup</c> when the input system calls the
            /// callback but rather the time at which the input was generated that triggered
            /// the action.
            /// </remarks>
            /// <seealso cref="InputEvent.time"/>
            public unsafe double time
            {
                get
                {
                    if (m_State == null)
                        return 0;
                    return m_State.actionStates[actionIndex].time;
                }
            }

            /// <summary>
            /// Time at which the action was <see cref="started"/> with relation to <c>Time.realtimeSinceStartup</c>.
            /// </summary>
            /// <remarks>
            /// This is only relevant for actions that go through distinct a <see cref="InputActionPhase.Started"/>
            /// cycle as driven by <see cref="IInputInteraction">interactions</see>.
            ///
            /// The value of this property is that of <see cref="time"/> when <see
            /// cref="InputAction.started"/> was called. See the <see cref="time"/>
            /// property for how the timestamp works.
            /// </remarks>
            public unsafe double startTime
            {
                get
                {
                    if (m_State == null)
                        return 0;
                    return m_State.actionStates[actionIndex].startTime;
                }
            }

            /// <summary>
            /// Time difference between <see cref="time"/> and <see cref="startTime"/>.
            /// </summary>
            /// <remarks>
            /// This property can be used, for example, to determine how long a button
            /// was held down.
            /// </remarks>
            /// <example>
            /// <code>
            /// // Let's create a button action bound to the A button
            /// // on the gamepad.
            /// var action = new InputAction(
            ///     type: InputActionType.Button,
            ///     binding: "&lt;Gamepad&gt;/buttonSouth");
            ///
            /// // When the action is performed (which will happen when the
            /// // button is pressed and then released) we take the duration
            /// // of the press to determine how many projectiles to spawn.
            /// action.performed +=
            ///     context =>
            ///     {
            ///         const float kSpawnRate = 3; // 3 projectiles per second
            ///         var projectileCount = kSpawnRate * context.duration;
            ///         for (var i = 0; i &lt; projectileCount; ++i)
            ///         {
            ///             var projectile = UnityEngine.Object.Instantiate(projectile);
            ///             // Apply other changes to the projectile...
            ///         }
            ///     };
            /// </code>
            /// </example>
            public double duration => time - startTime;

            /// <summary>
            /// Type of value returned by <see cref="ReadValueAsObject"/> and expected
            /// by <see cref="ReadValue{TValue}"/>.
            /// </summary>
            /// <remarks>
            /// The type of value returned by an action is usually determined by the
            /// <see cref="InputControl"/> that triggered the action, i.e. by the
            /// control referenced from <see cref="control"/>. However, if the binding that
            /// triggered is a composite, then the composite will determine values and
            /// not the individual control that triggered (that one just feeds values into the composite).
            /// </remarks>
            /// <seealso cref="InputControl.valueType"/>
            /// <seealso cref="InputBindingComposite.valueType"/>
            /// <seealso cref="activeValueType"/>
            public Type valueType => m_State?.GetValueType(bindingIndex, controlIndex);

            /// <summary>
            /// Size of values returned by <see cref="ReadValue(void*,int)"/> in bytes.
            /// </summary>
            /// <remarks>
            /// <para>
            /// All input values passed around by the system are required to be "blittable",
            /// i.e. they cannot contain references, cannot be heap objects themselves, and
            /// must be trivially mem-copyable. This means that any value can be read out
            /// and retained in a raw byte buffer.
            /// </para>
            /// <para>
            /// The value of this property determines how many bytes will be written
            /// by <see cref="ReadValue(void*,int)"/>.
            /// </para>
            /// <para>
            /// This property indirectly maps to the value of <see cref="InputControl.valueSizeInBytes"/> or
            /// <see cref="InputBindingComposite{TValue}.valueSizeInBytes"/>.
            /// </para>
            /// </remarks>
            public int valueSizeInBytes
            {
                get
                {
                    if (m_State == null)
                        return 0;

                    return m_State.GetValueSizeInBytes(bindingIndex, controlIndex);
                }
            }

            /// <summary>
            /// Read the value of the action as a raw byte buffer.
            /// </summary>
            /// <param name="buffer">Memory buffer to read the value into.</param>
            /// <param name="bufferSize">Size of buffer allocated at <paramref name="buffer"/>. Must be
            /// at least <see cref="valueSizeInBytes"/>.</param>
            /// <remarks>
            /// This allows reading values without having to know value types but also,
            /// unlike <see cref="ReadValueAsObject"/>, without allocating GC heap memory.
            /// </remarks>
            /// <example>
            /// <code>
            /// // Read a Vector2 using the raw memory ReadValue API.
            /// // Here we just read into a local variable which we could
            /// // just as well (and more easily) do using ReadValue&lt;Vector2&gt;.
            /// // Still, it serves as a demonstration for how the API
            /// // operates in general.
            /// unsafe
            /// {
            ///     var value = default(Vector2);
            ///     var valuePtr = UnsafeUtility.AddressOf(ref value);
            ///     context.ReadValue(buffer, UnsafeUtility.SizeOf&lt;Vector2&gt;());
            /// }
            /// </code>
            /// </example>
            /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <c>null</c>.</exception>
            /// <exception cref="ArgumentException"><paramref name="bufferSize"/> is too small.</exception>
            /// <seealso cref="InputControlExtensions.ReadValueIntoBuffer"/>
            /// <seealso cref="InputAction.ReadValue{TValue}"/>
            /// <seealso cref="ReadValue{TValue}"/>
            public unsafe void ReadValue(void* buffer, int bufferSize)
            {
                if (buffer == null)
                    throw new ArgumentNullException(nameof(buffer));

                if (m_State != null && phase.IsInProgress())
                {
                    m_State.ReadValue(bindingIndex, controlIndex, buffer, bufferSize);
                }
                else
                {
                    var valueSize = valueSizeInBytes;
                    if (bufferSize < valueSize)
                        throw new ArgumentException(
                            $"Expected buffer of at least {valueSize} bytes but got buffer of only {bufferSize} bytes", nameof(bufferSize));
                    UnsafeUtility.MemClear(buffer, valueSizeInBytes);
                }
            }

            /// <summary>
            /// Read the current value of the associated action.
            /// </summary>
            /// <typeparam name="TValue">Type of value to read. This must correspond to the
            /// expected by either <see cref="control"/> or, if it is a composite, by the
            /// <see cref="InputBindingComposite"/> in use.</typeparam>
            /// <returns>The current value of type <typeparamref name="TValue"/> associated with the action.</returns>
            /// <exception cref="InvalidOperationException">The given type <typeparamref name="TValue"/>
            /// does not match the value type expected by the control or binding composite.</exception>
            /// <seealso cref="InputAction.ReadValue{TValue}"/>
            /// <seealso cref="ReadValue"/>
            /// <seealso cref="ReadValueAsObject"/>
            /// <remarks>
            /// The following example shows how to read the current value of a specific type:
            /// </remarks>
            /// <example>
            /// <code>
            /// using UnityEngine;
            /// using UnityEngine.InputSystem;
            ///
            /// public class Example : MonoBehaviour
            /// {
            ///     public InputActionReference move;
            ///
            ///     void Awake()
            ///     {
            ///         if (move.action != null)
            ///         {
            ///             move.action.performed += context =>
            ///             {
            ///                 // Note: Assumes the underlying value type is Vector2.
            ///                 Debug.Log($"Value is: {context.ReadValue&lt;Vector2&gt;()}");
            ///             };
            ///         }
            ///     }
            ///
            ///     void OnEnable()
            ///     {
            ///         move.action.Enable();
            ///     }
            ///
            ///     void OnDisable()
            ///     {
            ///         move.action.Disable();
            ///     }
            /// }
            /// </code>
            /// </example>
            public TValue ReadValue<TValue>()
                where TValue : struct
            {
                var value = default(TValue);
                if (m_State != null)
                {
                    value = phase.IsInProgress() ?
                        m_State.ReadValue<TValue>(bindingIndex, controlIndex) :
                        m_State.ApplyProcessors(bindingIndex, value);
                }

                return value;
            }

            /// <summary>
            /// Read the current value of the action as a <c>float</c> and return true if it is equal to
            /// or greater than the button press threshold.
            /// </summary>
            /// <returns>True if the action is considered in "pressed" state, false otherwise.</returns>
            /// <remarks>
            /// If the currently active control is a <see cref="ButtonControl"/>, the <see cref="ButtonControl.pressPoint"/>
            /// of the button will be taken into account (if set). If there is no custom button press point, the
            /// global <see cref="InputSettings.defaultButtonPressPoint"/> will be used.
            /// </remarks>
            /// <example>
            /// <code>
            /// using UnityEngine;
            /// using UnityEngine.InputSystem;
            ///
            /// public class Example : MonoBehaviour
            /// {
            ///     public InputActionReference fire;
            ///
            ///     void Awake()
            ///     {
            ///         if (fire.action != null)
            ///         {
            ///             fire.action.performed += context =>
            ///             {
            ///                 // ReadValueAsButton attempts to interpret the value as a button.
            ///                 Debug.Log($"Is firing: {context.ReadValueAsButton()}");
            ///             };
            ///         }
            ///     }
            ///
            ///     void OnEnable()
            ///     {
            ///         fire.action.Enable();
            ///     }
            ///
            ///     void OnDisable()
            ///     {
            ///         fire.action.Disable();
            ///     }
            /// }
            /// </code>
            /// </example>
            /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
            /// <seealso cref="ButtonControl.pressPoint"/>
            public bool ReadValueAsButton()
            {
                var value = false;
                if (m_State != null && phase.IsInProgress())
                    value = m_State.ReadValueAsButton(bindingIndex, controlIndex);
                return value;
            }

            /// <summary>
            /// Same as <see cref="ReadValue{TValue}"/> except that it is not necessary to know the type of the value
            /// at compile time.
            /// </summary>
            /// <returns>The current value from the binding that triggered the action or <c>null</c> if the action
            /// is not currently in progress.</returns>
            /// <remarks>
            /// This method allocates GC heap memory due to boxing. Using it during normal gameplay will lead
            /// to frame-rate instabilities.
            /// </remarks>
            /// <example>
            /// <code>
            /// using UnityEngine;
            /// using UnityEngine.InputSystem;
            ///
            /// public class Example : MonoBehaviour
            /// {
            ///     public InputActionReference move;
            ///
            ///     void Awake()
            ///     {
            ///         if (move.action != null)
            ///         {
            ///             move.action.performed += context =>
            ///             {
            ///                 // ReadValueAsObject allows reading the associated value as a boxed reference type.
            ///                 object obj = context.ReadValueAsObject();
            ///                 if (obj is Vector2)
            ///                     Debug.Log($"Current value is Vector2 type: {obj}");
            ///                 else
            ///                     Debug.Log($"Current value is of another type: {context.valueType}");
            ///             };
            ///         }
            ///     }
            ///
            ///     void OnEnable()
            ///     {
            ///         move.action.Enable();
            ///     }
            ///
            ///     void OnDisable()
            ///     {
            ///         move.action.Disable();
            ///     }
            /// }
            /// </code>
            /// </example>
            /// <seealso cref="ReadValue{TValue}"/>
            /// <seealso cref="InputAction.ReadValueAsObject"/>
            public object ReadValueAsObject()
            {
                if (m_State != null && phase.IsInProgress())
                    return m_State.ReadValueAsObject(bindingIndex, controlIndex);
                return null;
            }

            /// <summary>
            /// Return a string representation of the context useful for debugging.
            /// </summary>
            /// <returns>String representation of the context.</returns>
            /// <remarks>
            /// The following example illustrates how to log callback context to console when a callback is received
            /// for debugging purposes:
            /// </remarks>
            /// <example>
            /// <code>
            /// using UnityEngine;
            /// using UnityEngine.InputSystem;
            ///
            /// public class Example : MonoBehaviour
            /// {
            ///     public InputActionReference move;
            ///
            ///     void Awake()
            ///     {
            ///         if (move.action != null)
            ///         {
            ///             move.action.performed += context =>
            ///             {
            ///                 // Outputs the associated callback context in its textual representation which may
            ///                 // be useful for debugging purposes.
            ///                 Debug.Log(context.ToString());
            ///             };
            ///         }
            ///     }
            ///
            ///     void OnEnable() => move.action.Enable();
            ///     void OnDisable() => move.action.Disable();
            /// }
            /// </code>
            /// </example>
            public override string ToString()
            {
                return $"{{ action={action} phase={phase} time={time} control={control} value={ReadValueAsObject()} interaction={interaction} }}";
            }
        }
    }
}
