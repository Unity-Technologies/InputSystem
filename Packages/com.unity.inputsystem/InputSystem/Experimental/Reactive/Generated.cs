// WARNING: This is an auto-generated file. Any manual edits will be lost.
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Internal; // TODO ArrayPoolExtensions could be incorporated to avoid

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// TemplateSummaryDoc
    /// </summary>
    [InputSource, Serializable]
    public partial struct Compare<TSource> : IObservableInputNode<InputEvent>
        where TSource : IObservableInputNode<bool>
    {
        [SerializeField] private TSource source;

        public Compare([InputPort] TSource source)
        {
            this.source = source;
        }
    } 

    public partial struct Compare<TSource> : IObservableInputNode<InputEvent>
        where TSource : IObservableInputNode<bool>
    {
        // TODO Return Subscription instead of IDispoable when its a struct. Hence why we do not want subscription as a struct since it easily gets boxed.

        #region IObservable<InputEvent>

        public IDisposable Subscribe([NotNull] IObserver<InputEvent> observer) => 
            Subscribe(Context.instance, observer);

        #endregion

        #region IObservableInput<InputEvent>

        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<InputEvent>
        {
            // TODO Implement node sharing (multi-cast)

            // Construct node instance and register underlying subscriptions
            var impl = ObjectPool<CompareObserver>.shared.Rent();
            impl.Initialize( source.Subscribe(context, impl) ); 

            // Register observer with node and return subscription
            impl.AddObserver(observer);
            return new Subscription<InputEvent>(impl, observer);
        }

        #endregion

        #region IDependencyGraphNode

        public bool Equals(IDependencyGraphNode other) => other is Compare<TSource> node && Equals(node);
        public bool Equals(Compare<TSource> other) => source.Equals(other.source);    
        public string displayName => "Compare"; 
        public int childCount => 1; 
        public IDependencyGraphNode GetChild(int index) 
        {
            switch (index)
            {
                case 0:  return source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
    internal sealed class CompareObserver : ObserverBase<InputEvent>, IObserver<bool>, IUnsubscribe<InputEvent>
    {
        private bool m_PreviousValue;
        private IDisposable m_SourceSubscription;

        public void Initialize([NotNull] IDisposable sourceSubscription)
        {
            m_SourceSubscription = sourceSubscription;
        }

        public void Unsubscribe([NotNull] IObserver<InputEvent> observer)
        {
            if (!RemoveObserver(observer)) 
                return;

            m_SourceSubscription.Dispose();
            m_SourceSubscription = null;
        }

        public void OnNext(bool value)
        {
            if (m_PreviousValue == value) 
                return;
            if (value) // TODO Let class be converted to Step and take a IComparable<T> type, then we can use for both Press and Relase
                ForwardOnNext(new InputEvent()); // TODO This needs to be tentative, should indirection between data observer an events or we need another stage, so its either a separate method or parameter
            m_PreviousValue = value;
        }        
    }
   /// <summary>
    /// Fluent API extension methods for <see cref="UnityEngine.InputSystem.Experimental.Compare{TSource}"/>.
    /// </summary>
    public static partial class CompareExtensions
    {
        /// <summary>
        /// Returns a new observable representing the Compare operation applied to <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <typeparam name="TSource">The source observable type.</typeparam>
        /// <returns>A new observable representing the Compare operation applied to <paramref name="source"/>.</returns>
        /// <exception cref="System.ArgumentNullException">if <paramref name="source"/> is <c>null</c>.</exception>
        public static Compare<TSource> Compare<TSource>(this TSource source)
            where TSource : IObservableInputNode<bool>
        {
            return new Compare<TSource>(source); // TODO If we switch to class this should be cached
        }
    }
}
