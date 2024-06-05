using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider if this is an anti-pattern due to the facts that it creates a binding with an
    // operation on it that prevents seeing observables as a single type if though implementing interfacees.
    // Instead consider an object defining inputs and outputs which decouples this and removes the restriction
    // of single input/output. It basically needs to implement filter


    // TODO This needs to be hidden somehow, otherwise we are forced to use an interface and
    //      proxies become less useful since they require heap memory allocation.
    //      It might be difficult to avoid this unless we derive a custom ObservableBindingSource.

    // TODO Multiple IObservable<T> inputs may be solved by internal instances or via custom interface providing source tracking

    /// <summary>
    /// Represents a binding source providing output of type <typeparamref name="T"/> which is associated with
    /// a specific end-point but not with a context.
    /// </summary>
    /// <typeparam name="T">The output type.</typeparam>
    public interface IInputBindingSource<out T> : IObservable<T> where T : struct
    {
        /// <summary>
        /// Subscribes to the given source within context <paramref name="context"/>
        /// </summary>
        /// <param name="context">The associated context.</param>
        /// <param name="observer">The observer to receive data when available.</param>
        /// <returns>Disposable subscription object.</returns>
        public IDisposable Subscribe([NotNull] Context context, IObserver<T> observer); // TODO If we allow the subscription type to implement IDisposable but be a specific type we could avoid indirection
    }

    // TODO If we cannot obtain the flexibility we need with this struct consider making it a class and store with device
    // TODO This makes sense when usage uniquely identifies a stream.
    //      This makes less sense for a derived binding source unless cached.
    //      However, cached behavior is enforced if done via DOTS so maybe
    //      we should compute into memory by default and avoid complexity?
    //      Should likely compare the two variants in a benchmark.
    //      Stream memory could easily be allocated from a native memory pool.

    // TODO Should really have key and not usage
    // A binding source associated with a specific usage but not with a specific context.
    public readonly struct InputBindingSource<T> : IInputBindingSource<T>
        where T : struct
    {
        public readonly Usage Usage;

        public InputBindingSource(Usage usage)
            : this(usage, Context.instance)
        {}

        // TODO Remove context constructor if not needed/relevant
        public InputBindingSource(Usage usage, [NotNull] Context context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context) + " is required.");
            if (usage == Usage.Invalid)
                throw new ArgumentException(nameof(usage) + " is not a valid usage.");

            Usage = usage;
        }

        public IDisposable Subscribe([NotNull] Context ctx, IObserver<T> observer)
        {
            // Subscribe to source by subscribing to a stream context.
            // Note that this is the approach we should use for any cached or underlying data.
            return ctx.GetOrCreateStreamContext<T>(Usage).Subscribe(observer);
        }

        public IDisposable Subscribe([NotNull] IObserver<T> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        /*public static IInputBindingSource operator |(InputBindingSource<T> a, InputBindingSource<T> b)
            => Combine(a, b);*/

        // TODO Delay evaluation
    }

    /*public static class InputBindingSourceExtensions
    {
        static MultiplexedBindingSource<T> Combine<T>(IInputBindingSource<T> a, IInputBindingSource<T> b)
            where T : struct, IComparable<T>
        {
            return new MultiplexedBindingSource<T>(a, b);
        }
    }*/

    internal interface ISourceObserver<in T> where T : struct
    {
        void OnCompleted(int source);
        void OnError(int source, Exception error);
        void OnNext(int source, T value);
    }

    internal readonly struct ForwardingObserver<T, TTarget> : IObserver<T>
        where T : struct
        where TTarget : struct, ISourceObserver<T>
    {
        private readonly TTarget m_Target; // Note: expecting devirtualization through constraints
        private readonly int m_Source;

        public ForwardingObserver(int source, ref TTarget target)
        {
            m_Source = source;
            m_Target = target;
        }

        public void OnCompleted()
        {
            //m_Target.OnCompleted(m_Source);
        }

        public void OnError(Exception error)
        {
            //m_Target.OnError(m_Source, error);
        }

        public void OnNext(T value)
        {
            //m_Target.OnNext(m_Source, value);
        }
    }

    /*internal class ObserverList2<T> : List<IObservable<T>> where T : struct
    {
        public void Subscribe()
    }*/

    // TODO This need to be further constrained
    // E.g. combining two press interactions make little sense, can't define AND/OR of impulses
    // E.g. combining two buttons makes more sense since we can define mathematical definition
    // E.g. combining two phases makes more sense since we can define mathematical definition and see it as a value
    //
    // Use case: combining two numerical values may define OR=a || b, AND=a && b
    public struct MultiplexedBindingSource<T> : IInputBindingSource<T>
        where T : struct, IComparable<T>
    {
        private readonly InputBindingSource<T> m_First;
        private readonly InputBindingSource<T> m_Second;
        private Multiplexer<T> m_Multiplexer;

        public MultiplexedBindingSource(InputBindingSource<T> first, InputBindingSource<T> second)
        {
            m_First = first;
            m_Second = second;
            m_Multiplexer = null; // Avoid allocating GC memory unless really used
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe(Context context, IObserver<T> observer)
        {
            m_Multiplexer ??= new Multiplexer<T>(m_First, m_Second);
            return m_Multiplexer.Subscribe(context, observer);
        }
    }

    // TODO Consider BinaryBindingSource
}
