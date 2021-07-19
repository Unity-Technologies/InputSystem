using System;
using UnityEngine.Profiling;

namespace UnityEngine.InputSystem.Utilities
{
    internal static class DelegateHelpers
    {
        // InvokeCallbacksSafe protects both against the callback getting removed while being called
        // and against exceptions being thrown by the callback.

        public static void InvokeCallbacksSafe(ref CallbackArray<Action> callbacks, string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return;
            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
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
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
        }

        public static void InvokeCallbacksSafe<TValue>(ref CallbackArray<Action<TValue>> callbacks, TValue argument, string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return;
            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
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
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
        }

        public static void InvokeCallbacksSafe<TValue1, TValue2>(ref CallbackArray<Action<TValue1, TValue2>> callbacks, TValue1 argument1, TValue2 argument2, string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return;
            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
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
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
        }

        public static bool InvokeCallbacksSafe_AnyCallbackReturnsTrue<TValue1, TValue2>(ref CallbackArray<Func<TValue1, TValue2, bool>> callbacks,
            TValue1 argument1, TValue2 argument2, string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return true;
            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
                try
                {
                    if (callbacks[i](argument1, argument2))
                        return true;
                }
                catch (Exception exception)
                {
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                    Debug.LogException(exception);
                }
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
            return false;
        }
    }
}
