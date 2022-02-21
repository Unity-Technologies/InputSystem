using System;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.LowLevel
{
    /// <summary>
    /// Wraps around mechanisms for listening in on the <see cref="InputEvent"/> stream made
    /// available through <see cref="InputSystem.onEvent"/>.
    /// </summary>
    /// <remarks>
    /// This struct can be used to add (<see cref="op_Addition"/>) or remove (<see cref="op_Subtraction"/>)
    /// callbacks directly to/from the event pipeline.
    ///
    /// Alternatively, it can be used as an <c>IObservable</c> to <see cref="Subscribe"/> observers to
    /// the event stream. See <see cref="Observable"/> for extension methods to set up various observer
    /// mechanisms.
    ///
    /// <example>
    /// <code>
    /// InputSystem.onEvent
    ///     .ForDevice(Mouse.current)
    ///     .Call(evt =>
    ///         {
    ///             foreach (var control in evt.EnumerateChangedControls())
    ///                 Debug.Log($"Control {control} on mouse has changed value");
    ///         });
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="InputSystem.onEvent"/>
    public struct InputEventListener : IObservable<InputEventPtr>
    {
        internal static ObserverState s_ObserverState;

        /// <summary>
        /// Add a delegate to be called for each <see cref="InputEvent"/> that is processed by the Input System.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="callback">A callback to call for each event.</param>
        /// <returns>The same listener instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent +=
        ///     (eventPtr, device) =>
        ///     {
        ///         Debug.Log($"Event for {device}");
        ///     };
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputSystem.onEvent"/>
        public static InputEventListener operator+(InputEventListener _, Action<InputEventPtr, InputDevice> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            lock (InputSystem.s_Manager)
                InputSystem.s_Manager.onEvent += callback;
            return default;
        }

        /// <summary>
        /// Remove a delegate from <see cref="InputEvent"/>.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="callback">A callback that was previously installed on <see cref="InputSystem.onEvent"/>.</param>
        /// <returns>The same listener instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <c>null</c>.</exception>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent -= myDelegate;
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputSystem.onEvent"/>
        public static InputEventListener operator-(InputEventListener _, Action<InputEventPtr, InputDevice> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            lock (InputSystem.s_Manager)
                InputSystem.s_Manager.onEvent -= callback;
            return default;
        }

        /// <summary>
        /// Subscribe an observer to the event pump.
        /// </summary>
        /// <param name="observer">Observer to be notified for each event.</param>
        /// <returns>A handle to dispose of the subscription.</returns>
        /// <remarks>
        /// The easiest way to subscribe is via the extension methods in <see cref="Observable"/>.
        /// <example>
        /// <code>
        /// // Subscribe.
        /// var subscription = InputSystem.onEvent.Call(e => Debug.Log("Event"));
        ///
        /// // Unsubscribe.
        /// subscription.Dispose();
        /// </code>
        /// </example>
        /// </remarks>
        public IDisposable Subscribe(IObserver<InputEventPtr> observer)
        {
            if (s_ObserverState == null)
                s_ObserverState = new ObserverState();

            if (s_ObserverState.observers.length == 0)
                InputSystem.s_Manager.onEvent += s_ObserverState.onEventDelegate;

            s_ObserverState.observers.AppendWithCapacity(observer);
            return new DisposableObserver { observer = observer };
        }

        internal class ObserverState
        {
            public InlinedArray<IObserver<InputEventPtr>> observers;
            public Action<InputEventPtr, InputDevice> onEventDelegate;

            public ObserverState()
            {
                onEventDelegate =
                    (eventPtr, device) =>
                {
                    for (var i = observers.length - 1; i >= 0; --i)
                        observers[i].OnNext(eventPtr);
                };
            }
        }

        private class DisposableObserver : IDisposable
        {
            public IObserver<InputEventPtr> observer;

            public void Dispose()
            {
                var index = s_ObserverState.observers.IndexOfReference(observer);
                if (index >= 0)
                    s_ObserverState.observers.RemoveAtWithCapacity(index);
                if (s_ObserverState.observers.length == 0)
                    InputSystem.s_Manager.onEvent -= s_ObserverState.onEventDelegate;
            }
        }
    }
}
