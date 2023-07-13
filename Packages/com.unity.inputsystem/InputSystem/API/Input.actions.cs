#if UNITY_INPUT_SYSTEM_ENABLE_GLOBAL_ACTIONS_API

using System;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using System.Reflection;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem.HighLevel.Editor;
#endif

namespace UnityEngine.InputSystem.HighLevel
{
    public static partial class Input
    {
        internal const string kGlobalActionsAssetName = "GlobalInputActions";
        internal const string kGlobalActionsAssetConfigKey = "com.unity.inputsystem.globalactionsasset";

        /// <summary>
        /// True if the specified action is currently pressed.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsPressed(string actionName, string actionMapName = "")
        {
            Debug.Assert(InputSystem.actions != null, "Global actions have not been correctly initialized");

            var action = InputSystem.actions?.FindAction(string.IsNullOrEmpty(actionMapName)
                ? actionName
                : $"{actionMapName}/{actionName}");
            return action != null && action.IsPressed();
        }

        /// <summary>
        /// True in the frame that the action started.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool WasPressedThisFrame(string actionName, string actionMapName = "")
        {
            Debug.Assert(InputSystem.actions != null, "Global actions have not been correctly initialized");

            var action = InputSystem.actions?.FindAction(string.IsNullOrEmpty(actionMapName)
                ? actionName
                : $"{actionMapName}/{actionName}");
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>
        /// True in the frame that the action ended.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool WasReleasedThisFrame(string actionName, string actionMapName = "")
        {
            Debug.Assert(InputSystem.actions != null, "Global actions have not been correctly initialized");

            var action = InputSystem.actions?.FindAction(string.IsNullOrEmpty(actionMapName)
                ? actionName
                : $"{actionMapName}/{actionName}");
            return action != null && action.WasReleasedThisFrame();
        }

        public static bool IsPressed<TActionType>(Input<TActionType> input) where TActionType : struct
        {
            return input.isPressed;
        }

        public static bool WasPressedThisFrame<TActionType>(Input<TActionType> input) where TActionType : struct
        {
            return input.wasPressedThisFrame;
        }

        public static bool WasReleasedThisFrame<TActionType>(Input<TActionType> input) where TActionType : struct
        {
            return input.wasReleasedThisFrame;
        }

        internal static void InitializeGlobalActions(string defaultAssetPath = null, string assetPath = null)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            InputSystem.actions = GlobalActionsAsset.GetOrCreateGlobalActionsAsset(assetPath, defaultAssetPath);
#else
            // find the source generated global actions runtime file in the users assembly and use it to load the
            // runtime actions
            var runtimeAssetLoaderType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetExportedTypes())
                .FirstOrDefault(t => t.IsAssignableFrom(typeof(IGlobalActionsRuntimeAsset)));
            if (runtimeAssetLoaderType == null)
            {
                Debug.LogError($"Couldn't load global actions runtime asset.");
            }
            else
            {
                var runtimeAssetLoader = (IGlobalActionsRuntimeAsset)Activator.CreateInstance(runtimeAssetLoaderType);
                InputSystem.actions = runtimeAssetLoader.Load();
            }
#endif
            if (InputSystem.actions == null)
            {
                Debug.LogError($"Couldn't initialize global input actions");
                return;
            }

            // TODO: Once the source generator is running, only the actions that have actually been used should be enabled
            InputSystem.actions.Enable();
        }

        internal static void ShutdownGlobalActions()
        {
            if (InputSystem.actions == null)
                return;

            InputSystem.actions.Disable();
            InputSystem.actions = null;
        }
    }

    public interface IGlobalActionsRuntimeAsset
    {
        InputActionAsset Load();
    }

    /// <summary>
    /// A strongly-typed wrapper for an Input Action.
    /// </summary>
    /// <typeparam name="TActionType">The type that will be used in calls to <see cref="InputAction.ReadValue{T}"/></typeparam>
    public class Input<TActionType> where TActionType : struct
    {
        /// <summary>
        /// Enables access to the underlying Input Action for advanced functionality such as rebinding.
        /// </summary>
        public InputAction action => m_Action;


        /// <see cref="InputAction.IsPressed"/>
        public bool isPressed => m_Action.IsPressed();
        /// <see cref="InputAction.WasPressedThisFrame"/>
        public bool wasPressedThisFrame => m_Action.WasPressedThisFrame();
        /// <see cref="InputAction.WasReleasedThisFrame"/>
        public bool wasReleasedThisFrame => m_Action.WasReleasedThisFrame();

        /// <summary>
        /// Returns the current value of the Input Action.
        /// </summary>
        public TActionType value
        {
            get
            {
                try
                {
                    // it should be unusual for ReadValue to throw because in most cases instances of this class
                    // will be created through the source generator, and that can catch mismatched control types
                    // at compile time and throw compiler errors, but it will always be possible to dynamically
                    // add incompatible bindings, and the best we can do then is to catch the exceptions
                    // thrown when we try to read from those controls.
                    return m_Action.ReadValue<TActionType>();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogWarning(ex.Message);
                }

                return m_Action.ReadDefaultValue<TActionType>(
                    new BindingIndex(m_Action, m_Action.activeBindingIndex, BindingIndex.IndexType.IncludeCompositeParts)
                        .ToIndexWithoutCompositeParts().value);
            }
        }

        /// <summary>
        /// A high-level collection of Input Bindings that by default skips over the composite part bindings
        /// for purposes of indexing.
        /// </summary>
        /// <remarks>
        ///
        /// </remarks>
        public InputBindingsCollection bindings =>
            new InputBindingsCollection(m_Action, InputBindingsCollection.EnumerationBehaviour.SkipCompositeParts);

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <remarks>
        /// </remarks>
        public Input(InputAction action)
        {
            Debug.Assert(action != null);

            m_Action = action ?? throw new ArgumentNullException(nameof(action));
            m_Action.Enable();
        }

        public bool WasPerformedThisFrame<TInteraction>()
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(BindingIndex.None, state => state.lastPerformedInUpdate);
        }

        public bool WasStartedThisFrame<TInteraction>()
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(BindingIndex.None, state => state.lastStartedInUpdate);
        }

        public bool WasCanceledThisFrame<TInteraction>()
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(BindingIndex.None, state => state.lastCanceledInUpdate);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        /// <remarks>The same type of interaction can exist on multiple bindings within an action. When this is the case,
        /// this method can be made to consider the interaction from a specific binding by passing the 'bindingIndex'
        /// argument. This index is relative to the bindings of just the Input Action. Most lower-level APIs are relative
        /// to the Input Action Map in which the binding is contained.</remarks>
        public bool WasPerformedThisFrame<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(bindingIndex, state => state.lastPerformedInUpdate);
        }

        public bool WasStartedThisFrame<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(bindingIndex, state => state.lastStartedInUpdate);
        }

        public bool WasCanceledThisFrame<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            return WasPhaseChangedThisFrame<TInteraction>(bindingIndex, state => state.lastCanceledInUpdate);
        }

        /// <summary>
        /// Adds an interaction of type TInteraction to every binding on this action.
        /// </summary>
        /// <typeparam name="TInteraction">The interaction type. Must implement IInputInteraction and be a built-in interaction
        /// or for a custom type, be registered with the Input System using <see cref="InputSystem.RegisterInteraction{T}"/>.</typeparam>
        public void AddInteraction<TInteraction>() where TInteraction : IInputInteraction
        {
            Debug.Assert(m_Action != null);

            foreach (var binding in bindings)
            {
                m_Action.ChangeBinding(binding.index.ToIndexIncludingCompositeParts().value).WithInteraction<TInteraction>();
            }
        }

        /// <summary>
        /// Add an interaction to the specified binding.
        /// </summary>
        public Interaction<TInteraction, TActionType> AddInteraction<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            Debug.Assert(m_Action != null);

            m_Action.ChangeBinding(bindingIndex.ToIndexIncludingCompositeParts().value).WithInteraction<TInteraction>();

            return new Interaction<TInteraction, TActionType>(this, bindingIndex);
        }

        /// <summary>
        /// Get the first interaction of type TInteraction on the binding at bindingIndex.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <param name="bindingIndex"></param>
        /// <returns></returns>
        public Interaction<TInteraction, TActionType> GetInteraction<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            Debug.Assert(m_Action != null);

            return new Interaction<TInteraction, TActionType>(this, bindingIndex);
        }

        /// <summary>
        /// Remove an interaction of type TInteraction from the binding at 'bindingIndex'.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        public void RemoveInteraction<TInteraction>(BindingIndex bindingIndex)
            where TInteraction : IInputInteraction
        {
            Debug.Assert(m_Action != null);

            m_Action
                .ChangeBinding(bindingIndex.ToIndexIncludingCompositeParts().value)
                .RemoveInteraction<TInteraction>();
        }

        /// <summary>
        /// Returns the value of the indicated parameter from the first interaction of type TInteraction that
        /// exists on the bindings of this action.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public TParameter GetInteractionParameter<TInteraction, TParameter>(BindingIndex bindingIndex,
            Expression<Func<TInteraction, TParameter>> expr)
            where TInteraction : IInputInteraction
            where TParameter : struct
        {
            Debug.Assert(m_Action != null);

            var result = m_Action.GetParameterValue(expr, m_Action.m_ActionMap.bindings[bindingIndex.ToMapIndex()]);
            return result ?? default(TParameter);
        }

        /// <summary>
        /// Find all interactions of type TInteraction and sets the specified parameter on them.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="expr"></param>
        /// <param name="value"></param>
        /// <remarks>
        /// </remarks>
        public void SetInteractionParameter<TInteraction, TParameter>(BindingIndex bindingIndex,
            Expression<Func<TInteraction, TParameter>> expr, TParameter value)
            where TInteraction : IInputInteraction
            where TParameter : struct
        {
            Debug.Assert(m_Action != null);

            m_Action.ApplyParameterOverride(expr, value, m_Action.m_ActionMap.bindings[bindingIndex.ToMapIndex()]);
        }

        /// <summary>
        /// True when the action is pressed.
        /// </summary>
        /// <param name="input"></param>
        /// <remarks>
        /// </remarks>
        public static implicit operator bool(Input<TActionType> input)
        {
            return input.m_Action.IsPressed();
        }

        /// <summary>
        /// Implicitly returns the InputAction inside an Input&lt;TActionType&gt; instance.
        /// </summary>
        /// <param name="input"></param>
        public static implicit operator InputAction(Input<TActionType> input)
        {
            return input.action;
        }

        private unsafe bool WasPhaseChangedThisFrame<TInteraction>(BindingIndex bindingIndex,
            Func<InputActionState.InteractionState, uint> lastChangedInUpdateGetter)
            where TInteraction : IInputInteraction
        {
            var map = m_Action.GetOrCreateActionMap();

            var bindings = m_Action.bindings;
            var bindingsCount = bindings.Count;
            var bindingStartIndexInMap = m_Action.m_BindingsStartIndex;

            if (bindingIndex != BindingIndex.None)
            {
                bindingStartIndexInMap = bindingIndex.ToMapIndex();
                bindingsCount = 1;
            }

            for (var i = 0; i < bindingsCount; i++)
            {
                var bindingIndexInState = map.m_State.GetBindingIndexInState(map.m_MapIndexInState, bindingStartIndexInMap + i);
                ref var bindingState = ref map.m_State.bindingStates[bindingIndexInState];

                for (var j = 0; j < bindingState.interactionCount; j++)
                {
                    var interaction = map.m_State.interactions[bindingState.interactionStartIndex + j];
                    if (interaction.GetType() != typeof(TInteraction))
                        continue;

                    ref var interactionState = ref map.m_State.interactionStates[bindingState.interactionStartIndex + j];
                    return InputUpdate.s_UpdateStepCount == lastChangedInUpdateGetter(interactionState) &&
                        InputUpdate.s_UpdateStepCount != default;
                }
            }

            return false;
        }

        private InputAction m_Action;
    }

    /// <summary>
    /// A wrapper struct for accessing interactions on an Input Action.
    /// </summary>
    /// <typeparam name="TInteraction"></typeparam>
    /// <typeparam name="TActionType"></typeparam>
    /// <remarks>
    /// The <see cref="Interaction{TInteraction,TActionType}"/> type is a convenience wrapper around an interaction
    /// that exists on either a specific binding of an Input Action, or any binding of an Input Action. Instances of
    /// this type can only be retrieved by calling <see cref="Input{TActionType}.GetInteraction{TInteraction}(BindingIndex)"/>.
    /// </remarks>
    /// <seealso cref="Input{TActionType}"/>
    /// <seealso cref="Input{TActionType}.GetInteraction{TInteraction}"/>
    public struct Interaction<TInteraction, TActionType>
        where TInteraction : IInputInteraction
        where TActionType : struct
    {
        /// <see cref="Input{TActionType}.WasPerformedThisFrame{TInteraction}()"/>
        public bool wasPerformedThisFrame => m_Input.WasPerformedThisFrame<TInteraction>(m_BindingIndex);

        /// <see cref="Input{TActionType}.WasStartedThisFrame{TInteraction}()"/>
        public bool wasStartedThisFrame => m_Input.WasStartedThisFrame<TInteraction>(m_BindingIndex);

        /// <see cref="Input{TActionType}.WasCanceledThisFrame{TInteraction}()"/>
        public bool wasCanceledThisFrame => m_Input.WasCanceledThisFrame<TInteraction>(m_BindingIndex);

        /// <summary>
        /// An Interaction instance is valid if the specified binding has an interaction
        /// of type TInteraction, or if the binding index BindingIndex.None, if any of the bindings in
        /// the action have an interaction of type TInteraction.
        /// </summary>
        public unsafe bool isValid
        {
            get
            {
                Debug.Assert(m_Input != null);

                var map = m_Input?.action?.GetOrCreateActionMap();
                if (map == null)
                    return false;

                // if the binding index is specified, only check that specific binding for an interaction of the correct type.
                if (m_BindingIndex != BindingIndex.None)
                {
                    var bindingIndexOnState = map.m_State.GetBindingIndexInState(map.m_MapIndexInState, m_BindingIndex.ToMapIndex());
                    ref var bindingState = ref map.m_State.bindingStates[bindingIndexOnState];
                    for (var j = 0; j < bindingState.interactionCount; j++)
                    {
                        var interaction = map.m_State.interactions[bindingState.interactionStartIndex + j];
                        if (interaction.GetType() == typeof(TInteraction))
                            return true;
                    }

                    return false;
                }

                // otherwise, check all bindings on the action
                var bindingCount = m_Input.action.bindings.Count;
                for (var i = 0; i < bindingCount; i++)
                {
                    int bindingIndexOnMap;
                    if (map.bindingsAreContiguous)
                        bindingIndexOnMap = m_Input.action.m_BindingsStartIndex + i;
                    else
                        bindingIndexOnMap = m_Input.action.BindingIndexOnActionToBindingIndexOnMap(i);

                    var bindingIndexOnState = map.m_State.GetBindingIndexInState(map.m_MapIndexInState, bindingIndexOnMap);
                    ref var bindingState = ref map.m_State.bindingStates[bindingIndexOnState];

                    for (var j = 0; j < bindingState.interactionCount; j++)
                    {
                        var interaction = map.m_State.interactions[bindingState.interactionStartIndex + j];
                        if (interaction.GetType() == typeof(TInteraction))
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Create a new interaction wrapper.
        /// </summary>
        /// <param name="input">The <see cref="Input{TActionType}"/> instance that this interaction will find interactions on.</param>
        /// <param name="bindingIndex">The binding index in the Input Action that this interaction is defined on.</param>
        /// <remarks>
        /// Since it's possible to have the same type of interaction on multiple bindings in an Input Action, the
        /// <paramref name="bindingIndex"/> argument enables specifying a particular binding.
        /// </remarks>
        internal Interaction(Input<TActionType> input, BindingIndex bindingIndex)
        {
            Debug.Assert(input != null);

            m_Input = input;
            m_BindingIndex = bindingIndex;
        }

        /// <see cref="Input{TActionType}.GetInteractionParameter{TInteraction,TParameter}(BindingIndex,Expression{Func{TInteraction,TParameter}})"/>
        public TParameter GetInteractionParameter<TInteraction, TParameter>(
            Expression<Func<TInteraction, TParameter>> expr)
            where TInteraction : IInputInteraction
            where TParameter : struct
        {
            Debug.Assert(m_Input != null);

            return m_Input.GetInteractionParameter(m_BindingIndex, expr);
        }

        /// <see cref="Input{TActionType}.SetInteractionParameter{TInteraction,TParameter}(BindingIndex,Expression{Func{TInteraction,TParameter}},TParameter)"/>
        public void SetInteractionParameter<TInteraction, TParameter>(
            Expression<Func<TInteraction, TParameter>> expr, TParameter value)
            where TInteraction : IInputInteraction
            where TParameter : struct
        {
            Debug.Assert(m_Input != null);

            m_Input.SetInteractionParameter(m_BindingIndex, expr, value);
        }

        /// <summary>
        /// Implicitly cast an interaction instance to a bool by returning the value of
        /// <see cref="wasPerformedThisFrame"/>.
        /// </summary>
        /// <param name="input">An <see cref="Interaction{TInteraction,TActionType}"/> instance.</param>
        public static implicit operator bool(Interaction<TInteraction, TActionType> input)
        {
            return input.wasPerformedThisFrame;
        }

        private readonly Input<TActionType> m_Input;
        private readonly BindingIndex m_BindingIndex;
    }
}
#endif
