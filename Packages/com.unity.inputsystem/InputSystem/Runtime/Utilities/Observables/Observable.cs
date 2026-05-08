using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Utilities
{
    /// <summary>
    /// Extension methods for working with <a ref="https://docs.microsoft.com/en-us/dotnet/api/system.iobservable-1">IObservable</a>
    /// in the context of the Input System.
    /// </summary>
    public static class Observable
    {
        /// <summary>
        /// Filter a stream of observable values by a predicate.
        /// </summary>
        /// <param name="source">The stream of observable values.</param>
        /// <param name="predicate">Filter to apply to the stream. Only values for which the predicate returns true
        /// are passed on to <c>OnNext</c> of the observer.</param>
        /// <typeparam name="TValue">Value type for the observable stream.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c> -or- <paramref name="predicate"/> is <c>null</c>.</exception>
        /// <returns>A new observable that is filtered by the given predicate.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .Where(e => e.HasButtonPress())
        ///     .Call(e => Debug.Log("Press"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        public static IObservable<TValue> Where<TValue>(this IObservable<TValue> source, Func<TValue, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return new WhereObservable<TValue>(source, predicate);
        }

        /// <summary>
        /// Transform each value in an observable stream of values into a value of a different type.
        /// </summary>
        /// <param name="source">The stream of observable values.</param>
        /// <param name="filter">Function to transform values in the stream.</param>
        /// <typeparam name="TSource">Type of source values to transform from.</typeparam>
        /// <typeparam name="TResult">Type of target values to transform to.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c> -or- <paramref name="filter"/> is <c>null</c>.</exception>
        /// <returns>A new observable of values of the new result type.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .Select(eventPtr => eventPtr.GetFirstButtonPressOrNull())
        ///     .Call(ctrl =>
        ///     {
        ///         if (ctrl != null)
        ///             Debug.Log(ctrl);
        ///     });
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> filter)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            return new SelectObservable<TSource, TResult>(source, filter);
        }

        /// <summary>
        /// Transform each value in an observable stream of values such that one value is translated to zero or more values
        /// of a new type.
        /// </summary>
        /// <param name="source">The stream of observable values.</param>
        /// <param name="filter">Function to transform each value in the stream into zero or more new values.</param>
        /// <typeparam name="TSource">Type of source values to transform from.</typeparam>
        /// <typeparam name="TResult">Type of target values to transform to.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c> -or- <paramref name="filter"/> is <c>null</c>.</exception>
        /// <returns>A new observable of values of the new result type.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .SelectMany(eventPtr => eventPtr.GetAllButtonPresses())
        ///     .Call(ctrl =>
        ///         Debug.Log($"Button {ctrl} pressed"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> filter)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            return new SelectManyObservable<TSource, TResult>(source, filter);
        }

        /// <summary>
        /// Take up to the first N values from the given observable stream of values.
        /// </summary>
        /// <param name="source">An observable source of values.</param>
        /// <param name="count">The maximum number of values to take from the source.</param>
        /// <typeparam name="TValue">Types of values to read from the stream.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <returns>A stream of up to <paramref name="count"/> values.</returns>
        public static IObservable<TValue> Take<TValue>(this IObservable<TValue> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            return new TakeNObservable<TValue>(source, count);
        }

        /// <summary>
        /// From an observable stream of events, take only those that are for the given <paramref name="device"/>.
        /// </summary>
        /// <param name="source">An observable stream of events.</param>
        /// <param name="device">Device to filter events for.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <returns>An observable stream of events for the given device.</returns>
        /// <remarks>
        /// Each event has an <see cref="InputEvent.deviceId"/> associated with it. This is used to match
        /// against the <see cref="InputDevice.deviceId"/> of <paramref name="device"/>.
        ///
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .ForDevice(Mouse.current)
        ///     .Call(e => Debug.Log($"Mouse event: {e}");
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEvent.deviceId"/>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        public static IObservable<InputEventPtr> ForDevice(this IObservable<InputEventPtr> source, InputDevice device)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new ForDeviceEventObservable(source, null, device);
        }

        /// <summary>
        /// From an observable stream of events, take only those that are for a device of the given type.
        /// </summary>
        /// <param name="source">An observable stream of events.</param>
        /// <typeparam name="TDevice">Type of device (such as <see cref="Gamepad"/>) to filter for.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        /// <returns>An observable stream of events for devices of type <typeparamref name="TDevice"/>.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .ForDevice&lt;Gamepad&gt;()
        ///     .Where(e => e.HasButtonPress())
        ///     .CallOnce(e => PlayerInput.Instantiate(myPrefab,
        ///         pairWithDevice: InputSystem.GetDeviceById(e.deviceId)));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        public static IObservable<InputEventPtr> ForDevice<TDevice>(this IObservable<InputEventPtr> source)
            where TDevice : InputDevice
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new ForDeviceEventObservable(source, typeof(TDevice), null);
        }

        /// <summary>
        /// Call an action for the first value in the given stream of values and then automatically dispose
        /// the observer.
        /// </summary>
        /// <param name="source">An observable source of values.</param>
        /// <param name="action">Action to call for the first value that arrives from the source.</param>
        /// <typeparam name="TValue">Type of values delivered by the source.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c> -or- <paramref name="action"/> is <c>null</c>.</exception>
        /// <returns>A handle to the subscription. Call <c>Dispose</c> to unsubscribe at any time.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .Where(e => e.type == DeviceConfigurationEvent.typeStatic)
        ///     .CallOnce(_ => Debug.Log("Device configuration changed"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        /// <seealso cref="Call{TValue}"/>
        public static IDisposable CallOnce<TValue>(this IObservable<TValue> source, Action<TValue> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            IDisposable subscription = null;
            subscription = source.Take(1).Subscribe(new Observer<TValue>(action, () => subscription?.Dispose()));
            return subscription;
        }

        /// <summary>
        /// Call the given callback for every value generated by the given observable stream of values.
        /// </summary>
        /// <param name="source">An observable stream of values.</param>
        /// <param name="action">A callback to invoke for each value.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c> -or- <paramref name="action"/> is <c>null</c>.</exception>
        /// <returns>A handle to the subscription. Call <c>Dispose</c> to unsubscribe at any time.</returns>
        /// <remarks>
        /// <example>
        /// <code>
        /// InputSystem.onEvent
        ///     .Where(e => e.type == DeviceConfigurationEvent.typeStatic)
        ///     .Call(_ => Debug.Log("Device configuration changed"));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputEventListener"/>
        /// <seealso cref="InputSystem.onEvent"/>
        /// <seealso cref="CallOnce{TValue}"/>
        public static IDisposable Call<TValue>(this IObservable<TValue> source, Action<TValue> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return source.Subscribe(new Observer<TValue>(action));
        }
    }
}
