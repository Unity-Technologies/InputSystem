using System;
using UnityEngine.InputSystem.Experimental.Devices;

namespace UnityEngine.InputSystem.Experimental
{
    internal class KeyControlNode : IObservableInput<bool>, IObserver<KeyboardState>
    {
        private readonly Key m_Key;
        private readonly ObserverList2<bool> m_Observers;

        public KeyControlNode(Context context, IObservableInputNode<KeyboardState> source, Key key)
        {
            m_Observers = new ObserverList2<bool>(source.Subscribe(context, this));
            m_Key = key;
        }
        
        public IDisposable Subscribe(IObserver<bool> observer) => Subscribe(Context.instance, observer);

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) 
            where TObserver : IObserver<bool>
        {
            return m_Observers.Subscribe(context, observer);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(KeyboardState value) // TODO An obvious in/ref
        {
            var next = value.GetKey(m_Key);
            m_Observers.OnNext(next);
        }
    }

    // TODO It would be desirable to avoid this at all and move decoder prior to this 
    [Serializable]
    public struct KeyControl : IObservableInputNode<bool>, IUnsafeObservable<bool> 
    {
        [SerializeField] private ObservableInput<KeyboardState> source;
        [SerializeField] private Key key;

        public KeyControl(ObservableInput<KeyboardState> source, Key key)
        {
            this.source = source;
            this.key = key;
        }

        public IDisposable Subscribe(IObserver<bool> observer) =>
            Subscribe(Context.instance, observer);

        public bool Equals(IDependencyGraphNode other) => other is KeyControl node && Equals(node);
        public bool Equals(KeyControl other) => source.Equals(other.source);

        public string displayName => "KeyControl";
        public int childCount => 1;
        public IDependencyGraphNode GetChild(int index)
        {
            switch (index)
            {
                case 0: return source;
                default: throw new Exception();
            }
        }

        public IDisposable Subscribe<TObserver>(Context context, TObserver observer) where TObserver : IObserver<bool>
        {
            if (source.usage == Usage.Invalid)
                throw new Exception($"Invalid source usage");
            if (key == Key.None)
                throw new Exception($"Invalid key for subscription");
            
            return new KeyControlNode(context, source, key).Subscribe(observer);
        }

        public UnsafeSubscription Subscribe(Context context, UnsafeDelegate<bool> observer)
        {
            throw new NotImplementedException();
        }
    }

    public static class KeyControlExtensions
    {
        public static KeyControl Key(this ObservableInput<KeyboardState> source, Key key)
        {
            return new KeyControl(source, key);
        }
    }
}