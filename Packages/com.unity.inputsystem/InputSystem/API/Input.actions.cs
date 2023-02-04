using System;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace UnityEngine.InputSystem.HighLevel
{
    public static partial class Input
    {
	    public static IInputActionCollection2 globalActions => s_GlobalActions;

        /// <summary>
        /// True if the specified action is currently pressed.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsControlPressed(string actionName, string actionMapName = "")
        {
            Debug.Assert(s_GlobalActions != null, "Global actions have not been correctly initialized");
	        
            var action = s_GlobalActions?.FindAction(string.IsNullOrEmpty(actionMapName) ? actionName : $"{actionMapName}/{actionName}");
            return action != null && action.IsPressed();
        }

        /// <summary>
        /// True in the frame that the action started.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsControlDown(string actionName, string actionMapName = "")
        {
			Debug.Assert(s_GlobalActions != null, "Global actions have not been correctly initialized");

			var action = s_GlobalActions?.FindAction(string.IsNullOrEmpty(actionMapName) ? actionName : $"{actionMapName}/{actionName}");
			return action != null && action.WasPressedThisFrame();
		}

        /// <summary>
        /// True in the frame that the action ended.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsControlUp(string actionName, string actionMapName = "")
        {
			Debug.Assert(s_GlobalActions != null, "Global actions have not been correctly initialized");

			var action = s_GlobalActions?.FindAction(string.IsNullOrEmpty(actionMapName) ? actionName : $"{actionMapName}/{actionName}");
			return action != null && action.WasReleasedThisFrame();
		}

        public static bool IsControlPressed<TActionType>(Input<TActionType> input) where TActionType : struct
        {
	        throw new NotImplementedException();
        }

        public static bool IsControlDown<TActionType>(Input<TActionType> input) where TActionType : struct
        {
	        throw new NotImplementedException();
        }

        public static bool IsControlUp<TActionType>(Input<TActionType> input) where TActionType : struct
        {
	        throw new NotImplementedException();
        }

        internal static void InitializeGlobalActions(string defaultAssetPath, string assetPath)
        {
#if UNITY_EDITOR
	        if (!EditorApplication.isPlayingOrWillChangePlaymode)
		        return;

	        s_GlobalActions = GlobalActionsAsset.GetOrCreateGlobalActionsAsset(assetPath, defaultAssetPath);
#else
			// at build time, a pre-compiled class will be created for the global asset. Be careful that the name doesn't
			// conflict with any user types!
			// TODO
            throw new NotImplementedException();
#endif
	        if (s_GlobalActions == null)
	        {
		        Debug.LogError($"Couldn't initialize global input actions");
		        return;
	        }

            // TODO: Once the source generator is running, only the actions that have actually been used should be enabled
	        s_GlobalActions.Enable();
        }

        internal static void ShutdownGlobalActions()
        {
	        if (s_GlobalActions == null)
		        return;

	        s_GlobalActions.Disable();
	        s_GlobalActions = null;
        }

		private static InputActionAsset s_GlobalActions;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <typeparam name="TActionType"></typeparam>
    public class Input<TActionType> where TActionType : struct
    {
        public InputAction action => m_Action;
        public bool isPressed => m_Action.IsPressed();
        public bool wasPressedThisFrame => m_Action.WasPressedThisFrame();
        public bool wasReleasedThisFrame => m_Action.WasReleasedThisFrame();
        public TActionType value => m_Action.ReadValue<TActionType>();

        /// <summary>
        /// Get the index of the binding within this input action that caused it to fire. -1 if the action is not
        /// currently performing.
        /// </summary>
        /// <remarks>
        /// This property can be useful when an input action exists that has multiple bindings and game logic needs
        /// to know which of the bindings was the one that actually actuated. For example, in an RTS game where
        /// game logic often operates on user assigned groups, there might exist an input action called SelectGroup
        /// that can trigger when any of the number keys on the keyboard are pressed. One way to deal with this would
        /// be to create ten individual input actions SelectGroupOne to SelectGroupTen, but that is cumbersome to
        /// deal with. With this API, it is possible to create one input action with ten bindings, one for each
        /// number key, and then check at runtime which of the bindings is active.
        /// </remarks>
        public int activeBindingIndex
        {
	        get
	        {
                throw new NotImplementedException();
	        }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="action"></param>
        /// <remarks>
        /// </remarks>
        public Input(InputAction action)
        {
            m_Action = action;
        }

        /// <summary>
        /// Check if a specific interaction was performed this frame.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <returns>true in the frame that the interaction performed in</returns>
        /// <remarks>
        /// </remarks>
        public bool WasPerformedThisFrame<TInteraction>() where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
        }

        public bool WasStartedThisFrame<TInteraction>() where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
        }

        public bool WasCancelledThisFrame<TInteraction>() where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
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
        public bool SetInteractionParameter<TInteraction, TParameter>(Expression<Func<TInteraction, TParameter>> expr, TParameter value)
            where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the value of the indicated parameter from the first interaction of type TInteraction that
        /// exists on the bindings of this action.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="expr"></param>
        /// <returns></returns>
        public bool TryGetInteractionParameter<TInteraction, TParameter>(Expression<Func<TInteraction, TParameter>> expr, out TParameter value)
            where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add an interaction to all bindings on this action. For more control over what bindings the interaction gets
        /// added to, drop down to using the ApplyBindingOverride method directly.
        /// </summary>
        /// <param name="interaction"></param>
        /// <remarks>
        /// </remarks>
        public bool AddInteraction(IInputInteraction interaction)
        {
            throw new NotImplementedException();
        }

        public TInteraction AddInteraction<TInteraction>() where TInteraction : IInputInteraction, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove all interactions of type TInteraction from all bindings attached to this action.
        /// </summary>
        /// <typeparam name="TInteraction"></typeparam>
        public bool RemoveInteraction<TInteraction>() where TInteraction : IInputInteraction
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Find the binding that has the specific interaction instance and remove it.
        /// </summary>
        /// <param name="interaction"></param>
        public bool RemoveInteraction(IInputInteraction interaction)
        {
            throw new NotImplementedException();
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

        public static implicit operator InputAction(Input<TActionType> input)
        {
            return input.action;
        }

        private InputAction m_Action;
    }

    public class InputInteraction<TInteraction, TActionType> : IDisposable
        where TInteraction : IInputInteraction
        where TActionType : struct
    {
        public bool wasPerformedThisFrame => m_Input.WasPerformedThisFrame<TInteraction>();
        public bool wasStartedThisFrame => m_Input.WasStartedThisFrame<TInteraction>();
        public bool wasCancelledThisFrame => m_Input.WasCancelledThisFrame<TInteraction>();

        public InputInteraction(Input<TActionType> input)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void OnPerformed(InputAction.CallbackContext ctx)
        {
            throw new NotImplementedException();
        }

        private void OnStarted(InputAction.CallbackContext ctx)
        {
            throw new NotImplementedException();
        }

        private void OnCancelled(InputAction.CallbackContext ctx)
        {
            throw new NotImplementedException();
        }

        private Input<TActionType> m_Input;
    }
}
