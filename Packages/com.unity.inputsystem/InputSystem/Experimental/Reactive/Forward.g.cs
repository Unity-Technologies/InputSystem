using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Internal;
namespace UnityEngine.InputSystem.Experimental
{


[InputSourceAttribute]
    public partial struct Forward<TSource> : IObservableInputNode<bool>
        where TSource : struct, IObservableInputNode<bool>
    {
        private TSource source;
        public Forward(TSource source)
        {
            this.source = source;
        }
        public IDisposable Subscribe(IObserver<bool> observer)
        {
            return Subscribe(Context.instance, observer);
        }
        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<System.Boolean>
        {
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
    internal sealed class ForwardNode : ObserverBase<bool>, IUnsubscribe<bool>, IObserver<bool>
    {
        private IDisposable m_sourceSubscription;
        public void Initialize(IDisposable sourceSubscription)
        {
            this.m_sourceSubscription = sourceSubscription;
        }
        public void Unsubscribe(IObserver<bool> observer)
        {
            if (!RemoveObserver(observer)) return;
            this.m_sourceSubscription.Dispose();
            this.m_sourceSubscription = null;
        }
        public void OnNext(bool value)
        {
            UnityEngine.InputSystem.Experimental.MyStatelessOperation.Forward(this, value);
        }
    }
    public static partial class ForwardExtensions
    {
    }
}