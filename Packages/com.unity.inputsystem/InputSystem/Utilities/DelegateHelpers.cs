using System;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class DelegateHelpers
    {
        // InvokeCallbacksSafe protects both against the callback getting removed while being called
        // and against exceptions being thrown by the callback.

        public static void InvokeCallbacksSafe(ref InlinedArray<Action> callbacks, string callbackName, object context = null)
        {
            Profiler.BeginSample(callbackName);
            for (var i = 0; i < callbacks.length; ++i)
            {
                var lengthBefore = callbacks.length;

                try
                {
                    callbacks[i]();
                }
                catch (Exception exception)
                {
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }

                ////REVIEW: is this enough?
                if (callbacks.length == lengthBefore - 1)
                    --i;
            }
            Profiler.EndSample();
        }

        public static void InvokeCallbacksSafe<TValue>(ref InlinedArray<Action<TValue>> callbacks, TValue argument, string callbackName, object context = null)
        {
            Profiler.BeginSample(callbackName);
            for (var i = 0; i < callbacks.length; ++i)
            {
                var lengthBefore = callbacks.length;

                try
                {
                    callbacks[i](argument);
                }
                catch (Exception exception)
                {
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }

                ////REVIEW: is this enough?
                if (callbacks.length == lengthBefore - 1)
                    --i;
            }
            Profiler.EndSample();
        }

        public static void InvokeCallbacksSafe<TValue1, TValue2>(ref InlinedArray<Action<TValue1, TValue2>> callbacks, TValue1 argument1, TValue2 argument2, string callbackName, object context = null)
        {
            Profiler.BeginSample(callbackName);
            for (var i = 0; i < callbacks.length; ++i)
            {
                var lengthBefore = callbacks.length;

                try
                {
                    callbacks[i](argument1, argument2);
                }
                catch (Exception exception)
                {
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }

                ////REVIEW: is this enough?
                if (callbacks.length == lengthBefore - 1)
                    --i;
            }
            Profiler.EndSample();
        }
    }
}
