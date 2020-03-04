using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Scripting;

// [GESTURES]
// Idea for v2 of the input system:
//     Separate interaction *recognition* from interaction *representation*
//     This will likely also "solve" gestures
//
//     ATM, an interaction is a prebuilt thing that rolls recognition and representation of an interaction into
//     one single thing. That limits how powerful this can be. There's only ever one interaction coming from each interaction
//     added to a setup.
//
//     A much more powerful way would be to have the interactions configured on actions and bindings add *recognizers*
//     which then *generate* interactions. This way, a single recognizer could spawn arbitrary many interactions. What the
//     recognizer is attached to (the bindings) would simply act as triggers. Beyond that, the recognizer would have
//     plenty freedom to start, perform, and stop interactions happening in response to input.
//
//     It'll likely be a breaking change as far as user-implemented interactions go but at least the data as it looks today
//     should work with this just fine.

////TODO: allow interactions to be constrained to a specific InputActionType

////TODO: add way for parameters on interactions and processors to be driven from global value source that is NOT InputSettings
////      (ATM it's very hard to e.g. have a scale value on gamepad stick bindings which is determined dynamically from player
////      settings in the game)

////REVIEW: what about putting an instance of one of these on every resolved control instead of sharing it between all controls resolved from a binding?

////REVIEW: can we have multiple interactions work together on the same binding? E.g. a 'Press' causing a start and a 'Release' interaction causing a performed

////REVIEW: have a default interaction so that there *always* is an interaction object when processing triggers?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Interface for interaction patterns that drive actions.
    /// </summary>
    /// <remarks>
    /// Actions have a built-in interaction pattern that to some extent depends on their type (<see
    /// cref="InputActionType"/>, <see cref="InputAction.type"/>). What this means is that when controls
    /// bound to an action are actuated, the action will initiate an interaction that in turn determines
    /// when <see cref="InputAction.started"/>, <see cref="InputAction.performed"/>, and <see cref="InputAction.canceled"/>
    /// are called.
    ///
    /// The default interaction (i.e. when no interaction has been added to a binding or the
    /// action that the binding targets) will generally start and perform an action as soon as a control
    /// is actuated, then perform the action whenever the value of the control changes except if the value
    /// changes back to the default in which case the action is cancelled.
    ///
    /// By writing custom interactions, it is possible to implement different interactions. For example,
    /// <see cref="Interactions.HoldInteraction"/> will only start when a control is being actuated but
    /// will only perform the action if the control is held for a minimum amount of time.
    ///
    /// Interactions can be stateful and mutate state over time. In fact, interactions will usually
    /// represent miniature state machines driven directly by input.
    ///
    /// Multiple interactions can be applied to the same binding. The interactions will be processed in
    /// sequence. However, the first interaction that starts the action will get to drive the state of
    /// the action. If it performs the action, all interactions are reset. If it cancels, the first
    /// interaction in the list that is in started state will get to take over and drive the action.
    ///
    /// This makes it possible to have several interaction patterns on the same action. For example,
    /// to have a "fire" action that allows for charging, one can have a "Hold" and a "Press" interaction
    /// in sequence on the action.
    ///
    /// <example>
    /// <code>
    /// // Create a fire action with two interactions:
    /// // 1. Hold. Triggers charged firing. Has to come first as otherwise "Press" will immediately perform the action.
    /// // 2. Press. Triggers instant firing.
    /// // NOTE: An alternative is to use "Tap;Hold", i.e. a "Tap" first and then a "Hold". The difference
    /// //       is relatively minor. In this setup, the "Tap" turns into a "Hold" if the button is held for
    /// //       longer than the tap time whereas in the setup below, the "Hold" turns into a "Press" if the
    /// //       button is released before the hold time has been reached.
    /// var fireAction = new InputAction(type: InputActionType.Button, interactions: "Hold;Press");
    /// fireAction.AddBinding("&lt;Gamepad&gt;/buttonSouth");
    /// </code>
    /// </example>
    ///
    /// Custom interactions can be registered using <see cref="InputSystem.RegisterInteraction"/>. This can be
    /// done at any point during or after startup but has to be done before actions that reference the interaction
    /// are enabled or have their controls queried. A good point is usually to do it during loading like so:
    ///
    /// <example>
    /// <code>
    /// #if UNITY_EDITOR
    /// [InitializeOnLoad]
    /// #endif
    /// public class MyInteraction : IInputInteraction
    /// {
    ///     public void Process(ref InputInteractionContext context)
    ///     {
    ///         // ...
    ///     }
    ///
    ///     public void Reset()
    ///     {
    ///     }
    ///
    ///     static MyInteraction()
    ///     {
    ///         InputSystem.RegisterInteraction&lt;MyInteraction&gt;();
    ///     }
    ///
    ///     [RuntimeInitializeOnLoad]
    ///     private static void Initialize()
    ///     {
    ///         // Will execute the static constructor as a side effect.
    ///     }
    /// }
    /// </code>
    /// </example>
    ///
    /// If your interaction will only work with a specific type of value (e.g. <c>float</c>), it is better
    /// to base the implementation on <see cref="IInputInteraction{TValue}"/> instead. While the interface is the
    /// same, the type parameter communicates to the input system that only controls that have compatible value
    /// types should be used with your interaction.
    ///
    /// Interactions, like processors (<see cref="InputProcessor"/>) and binding composites (<see cref="InputBindingComposite"/>)
    /// may define their own parameters which can then be configured through the editor UI or set programmatically in
    /// code. To define a parameter, add a public field to your class that has either a <c>bool</c>, an <c>int</c>,
    /// a <c>float</c>, or an <c>enum</c> type. To set defaults for the parameters, assign default values
    /// to the fields.
    ///
    /// <example>
    /// <code>
    /// public class MyInteraction : IInputInteraction
    /// {
    ///     public bool boolParameter;
    ///     public int intParameter;
    ///     public float floatParameter;
    ///     public MyEnum enumParameter = MyEnum.C; // Custom default.
    ///
    ///     public enum MyEnum
    ///     {
    ///         A,
    ///         B,
    ///         C
    ///     }
    ///
    ///     public void Process(ref InputInteractionContext context)
    ///     {
    ///         // ...
    ///     }
    ///
    ///     public void Reset()
    ///     {
    ///     }
    /// }
    ///
    /// // The parameters can be configured graphically in the editor or set programmatically in code.
    /// // NOTE: Enum parameters are represented by their integer values. However, when setting enum parameters
    /// //       graphically in the UI, they will be presented as a dropdown using the available enum values.
    /// var action = new InputAction(interactions: "MyInteraction(boolParameter=true,intParameter=1,floatParameter=1.2,enumParameter=1);
    /// </code>
    /// </example>
    ///
    /// A default UI will be presented in the editor UI to configure the parameters of your interaction.
    /// You can customize this by replacing the default UI with a custom implementation using <see cref="Editor.InputParameterEditor"/>.
    /// This mechanism is the same as for processors and binding composites.
    ///
    /// <example>
    /// <code>
    /// #if UNITY_EDITOR
    /// public class MyCustomInteractionEditor : InputParameterEditor&lt;MyCustomInteraction&gt;
    /// {
    ///     protected override void OnEnable()
    ///     {
    ///         // Do any setup work you need.
    ///     }
    ///
    ///     protected override void OnGUI()
    ///     {
    ///         // Use standard Unity UI calls do create your own parameter editor UI.
    ///     }
    /// }
    /// #endif
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputSystem.RegisterInteraction"/>
    /// <seealso cref="InputBinding.interactions"/>
    /// <seealso cref="InputAction.interactions"/>
    /// <seealso cref="Editor.InputParameterEditor"/>
    [Preserve]
    public interface IInputInteraction
    {
        /// <summary>
        /// Perform processing of the interaction in response to input.
        /// </summary>
        /// <param name="context"></param>
        /// <remarks>
        /// This method is called whenever a control referenced in the binding that the interaction sits on
        /// changes value. The interaction is expected to process the value change and, if applicable, call
        /// <see cref="InputInteractionContext.Started"/> and/or its related methods to initiate a state change.
        ///
        /// Note that if "control disambiguation" (i.e. the process where if multiple controls are bound to
        /// the same action, the system decides which control gets to drive the action at any one point) is
        /// in effect -- i.e. when either <see cref="InputActionType.Button"/> or <see cref="InputActionType.Value"/>
        /// are used but not if <see cref="InputActionType.PassThrough"/> is used -- inputs that the disambiguation
        /// chooses to ignore will cause this method to not be called.
        ///
        /// Note that this method is called on the interaction even when there are multiple interactions
        /// and the interaction is not the one currently in control of the action (because another interaction
        /// that comes before it in the list had already started the action). Each interaction will get
        /// processed independently and the action will decide when to use which interaction to drive the
        /// action as a whole.
        ///
        /// <example>
        /// <code>
        ///     // Processing for an interaction that will perform the action only if a control
        ///     // is held at least at 3/4 actuation for at least 1 second.
        ///     public void Process(ref InputInteractionContext context)
        ///     {
        ///         var control = context.control;
        ///
        ///         // See if we're currently tracking a control.
        ///         if (m_Control != null)
        ///         {
        ///             // Ignore any input on a control we're not currently tracking.
        ///             if (m_Control != control)
        ///                 return;
        ///
        ///             // Check if the control is currently actuated past our 3/4 threshold.
        ///             var isStillActuated = context.ControlIsActuated(0.75f);
        ///
        ///             // See for how long the control has been held.
        ///             var actuationTime = context.time - context.startTime;
        ///
        ///             if (!isStillActuated)
        ///             {
        ///                 // Control is no longer actuated above 3/4 threshold. If it was held
        ///                 // for at least a second, perform the action. Otherwise cancel it.
        ///
        ///                 if (actuationTime >= 1)
        ///                     context.Performed();
        ///                 else
        ///                     context.Cancelled();
        ///             }
        ///
        ///             // Control changed value somewhere above 3/4 of its actuation. Doesn't
        ///             // matter to us so no change.
        ///         }
        ///         else
        ///         {
        ///             // We're not already tracking a control. See if the control that just triggered
        ///             // is actuated at least 3/4th of its way. If so, start tracking it.
        ///
        ///             var isActuated = context.ControlIsActuated(0.75f);
        ///             if (isActuated)
        ///             {
        ///                 m_Control = context.control;
        ///                 context.Started();
        ///             }
        ///         }
        ///     }
        ///
        ///     InputControl m_Control;
        ///
        ///     public void Reset()
        ///     {
        ///         m_Control = null;
        ///     }
        /// </code>
        /// </example>
        /// </remarks>
        void Process(ref InputInteractionContext context);

        /// <summary>
        /// Reset state that the interaction may hold. This should put the interaction back in its original
        /// state equivalent to no input yet having been received.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Identical to <see cref="IInputInteraction"/> except that it allows an interaction to explicitly
    /// advertise the value it expects.
    /// </summary>
    /// <typeparam name="TValue">Type of values expected by the interaction</typeparam>
    /// <remarks>
    /// Advertising the value type will an interaction type to be filtered out in the UI if the value type
    /// it has is not compatible with the value type expected by the action.
    ///
    /// In all other ways, this interface is identical to <see cref="IInputInteraction"/>.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "This interface is used to mark implementing classes to advertise the value it expects. This seems more elegant then the suggestion to use an attribute.")]
    [Preserve]
    public interface IInputInteraction<TValue> : IInputInteraction
        where TValue : struct
    {
    }

    internal static class InputInteraction
    {
        public static TypeTable s_Interactions;

        public static Type GetValueType(Type interactionType)
        {
            if (interactionType == null)
                throw new ArgumentNullException(nameof(interactionType));

            return TypeHelpers.GetGenericTypeArgumentFromHierarchy(interactionType, typeof(IInputInteraction<>), 0);
        }

        public static string GetDisplayName(string interaction)
        {
            if (string.IsNullOrEmpty(interaction))
                throw new ArgumentNullException(nameof(interaction));

            var interactionType = s_Interactions.LookupTypeRegistration(interaction);
            if (interactionType == null)
                return interaction;

            return GetDisplayName(interactionType);
        }

        public static string GetDisplayName(Type interactionType)
        {
            if (interactionType == null)
                throw new ArgumentNullException(nameof(interactionType));

            var displayNameAttribute = interactionType.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute == null)
            {
                if (interactionType.Name.EndsWith("Interaction"))
                    return interactionType.Name.Substring(0, interactionType.Name.Length - "Interaction".Length);
                return interactionType.Name;
            }

            return displayNameAttribute.DisplayName;
        }
    }
}
