using System;
using System.Linq.Expressions;

namespace UnityEngine.InputSystem.HighLevel
{
    public static partial class Input
    {
        static Input()
        {
        }

        public static InputActionAsset globalAsset => s_GlobalAsset;

        /// <summary>
        /// True if the specified action is currently pressed.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        /// <remarks>
        /// ActionMapName comes second and is optional because A) the system is smart enough to look for
        /// a uniquely named action if it exists, and B) new users won't know what an action map is.
        /// </remarks>
        public static bool IsActionPressed(string actionName, string actionMapName = "")
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the action started.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsActionDown(string actionName, string actionMapName = "")
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// True in the frame that the action ended.
        /// </summary>
        /// <param name="actionMapName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static bool IsActionUp(string actionName, string actionMapName = "")
        {
            throw new NotImplementedException();
        }

        private static InputActionAsset s_GlobalAsset;
    }

    /// <summary>
    /// Strongly typed access around an Input Action. At lower levels, it is up to the user to
    /// provide the correct type in the call to ReadValue, resulting in exceptions being thrown
    /// if the wrong type is provided. This should never be possible when using this type.
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
