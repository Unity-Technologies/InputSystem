using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Internal;
using UnityEngine;

namespace UnityEngine.InputSystem.Experimental
{
    [Serializable, InputSource]
    public partial struct Forward<TSource> : IObservableInputNode<bool>
        where TSource : struct, IObservableInputNode<bool>
    {
        [SerializeField] private TSource source;

        public Forward(TSource source)
        {
            this.source = source;
        }

        public IDisposable Subscribe( [NotNull]IObserver<bool> observer)
        {
            return Subscribe(Context.instance, observer);
        }
         
        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<System.Boolean>
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            // TODO Implement node sharing (multi-cast)
            var impl = ObjectPool<ForwardNode>.shared.Rent();
            impl.Initialize(
                 source.Subscribe(context, impl)
            );
            impl.AddObserver(observer);
            return new Subscription<bool>(impl, observer);
        }
         
        #region IDependencyGraphNode
        public bool Equals(IDependencyGraphNode other) => other is Forward<TSource> node && Equals(node);
        public bool Equals(Forward<TSource> other) => source.Equals(other.source);
        public string displayName => "Forward";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch(index)
            {
                case 0: return source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        #endregion
    }

    public static partial class ForwardExtensions
    {
        public static Forward<TSource> Forward<TSource>(TSource source)
            where TSource : struct, IObservableInputNode<bool>
        {
            return new Forward<TSource>(source);
        }
    }

    internal sealed class ForwardNode : ObserverBase<bool>, IUnsubscribe<bool>, IObserver<bool>
    {
        private IDisposable m_sourceSubscription;

        public void Initialize( [NotNull]IDisposable sourceSubscription)
        {
            m_sourceSubscription = sourceSubscription;
        }

        public void Unsubscribe(IObserver<bool> observer)
        {
            if (!RemoveObserver(observer)) return;
            m_sourceSubscription.Dispose();
            m_sourceSubscription = null;
        }

        public void OnNext(bool value)
        {
            UnityEngine.InputSystem.Experimental.MyStatelessOperation.Forward(this, value);
        }
    }
}