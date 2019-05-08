using System;
using System.Linq;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class DelegateHelpers
    {
        // InvokeCallbacksSafe protects both against the callback getting removed while being called
        // and against exceptions being thrown by the callback.

        public static void InvokeCallbacksSafe<TValue>(ref InlinedArray<Action<TValue>> callbacks, TValue argument, string callbackName)
        {
            for (var i = 0; i < callbacks.length; ++i)
            {
                var lengthBefore = callbacks.length;

                try
                {
                    callbacks[i](argument);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }

                ////REVIEW: is this enough?
                if (callbacks.length == lengthBefore - 1)
                    --i;
            }
        }

        public static void InvokeCallbacksSafe<TValue1, TValue2>(ref InlinedArray<Action<TValue1, TValue2>> callbacks, TValue1 argument1, TValue2 argument2, string callbackName)
        {
            for (var i = 0; i < callbacks.length; ++i)
            {
                var lengthBefore = callbacks.length;

                try
                {
                    callbacks[i](argument1, argument2);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }

                ////REVIEW: is this enough?
                if (callbacks.length == lengthBefore - 1)
                    --i;
            }
        }
    }
}
