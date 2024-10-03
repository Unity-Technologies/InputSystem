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
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
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
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
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
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
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
                    {
                        callbacks.UnlockForChanges();
                        Profiler.EndSample();
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                }
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
            return false;
        }

        /// <summary>
        /// Invokes the given callbacks and also invokes any callback returned from the result of the first.
        /// </summary>
        /// <seealso cref="System.Action"/>
        /// <remarks>
        /// Allows an chaining up an additional, optional block of code to the original callback
        /// and allow the external code make the decision about whether this code should be executed.
        /// </remarks>
        public static void InvokeCallbacksSafe_AndInvokeReturnedActions<TValue>(
            ref CallbackArray<Func<TValue, Action>> callbacks, TValue argument,
            string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return;

            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
                try
                {
                    callbacks[i](argument)?.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                }
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
        }

        /// <summary>
        /// Invokes the given callbacks and returns true if any of them returned a non-null result.
        /// </summary>
        /// <remarks>
        /// Returns false if every callback invocation returned null.
        /// </remarks>
        public static bool InvokeCallbacksSafe_AnyCallbackReturnsObject<TValue, TReturn>(
            ref CallbackArray<Func<TValue, TReturn>> callbacks, TValue argument,
            string callbackName, object context = null)
        {
            if (callbacks.length == 0)
                return false;

            Profiler.BeginSample(callbackName);
            callbacks.LockForChanges();
            for (var i = 0; i < callbacks.length; ++i)
            {
                try
                {
                    var ret = callbacks[i](argument);
                    if (ret != null)
                    {
                        callbacks.UnlockForChanges();
                        Profiler.EndSample();
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    if (context != null)
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks of '{context}'");
                    else
                        Debug.LogError($"{exception.GetType().Name} while executing '{callbackName}' callbacks");
                }
            }
            callbacks.UnlockForChanges();
            Profiler.EndSample();
            return false;
        }
    }
}
