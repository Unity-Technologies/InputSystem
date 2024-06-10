using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Note that we can evaluate buttons jointly
    
    /*public static void Press(bool a, bool b)
    {
            
    }

    public static unsafe void Press<T>(T initialState, NativeSlice<T> slice) 
        where T : struct, IComparable<T>
    {
            
    }*/
    
    // // TODO RX interface only allows single observer per type, this is a problem
    // // TODO Should setup observerlist so that we unsubscribe on underlying properly
    // public class Press<TSource> : IObserver<bool>, IInputBindingSource<InputEvent>
    //     where TSource : IInputBindingSource<bool>
    // {
    //     //public InputBindingSource<InputEvent> pressed { get; set; }
    //     private bool m_PreviousValue; // TODO Not properly initialized
    //     private ObserverList<InputEvent> m_ObserverList;
    //     private IDisposable m_Subscription;
    //     private TSource m_Trigger;
    //     private Context m_Context;
    //
    //     public Press(TSource button)
    //         : this(button, Context.instance)
    //     {}
    //
    //     public Press(TSource button, Context context = null)
    //     {
    //         m_Trigger = button;
    //         m_Context = context;
    //         m_ObserverList = new ObserverList<InputEvent>(() =>
    //         {
    //             if (m_Subscription == null)
    //                 return;
    //             m_Subscription.Dispose();
    //             m_Subscription = null;
    //         });
    //     }
    //
    //     private void Emit()
    //     {
    //         m_ObserverList.OnNext(new InputEvent());
    //     }
    //
    //     private void UpdateSubscription(Context context)
    //     {
    //         if (m_Subscription != null)
    //             return;
    //         m_Subscription = m_Trigger.Subscribe(context, this);
    //     }
    //
    //     public IDisposable Subscribe(IObserver<InputEvent> observer)
    //     {
    //         UpdateSubscription(Context.instance); // TODO FIX, it should belong to
    //         return m_ObserverList.Add(observer);
    //     }
    //
    //     public IDisposable Subscribe(Context ctx, IObserver<InputEvent> observer)
    //     {
    //         UpdateSubscription(ctx); // TODO Fix, it should belong to
    //         return m_ObserverList.Add(observer);
    //     }
    //
    //     public void OnCompleted()
    //     {
    //         throw new NotImplementedException();
    //     }
    //
    //     public void OnError(Exception error)
    //     {
    //         Debug.Log(error);
    //     }
    //
    //     public void OnNext(bool value)
    //     {
    //         if (value && !m_PreviousValue)
    //             Emit();
    //         m_PreviousValue = value;
    //     }
    // }
    
    // TODO RX interface only allows single observer per type, this is a problem
    // TODO Should setup observerlist so that we unsubscribe on underlying properly
    public class Press<TSource> : IObserver<bool>, IObservableInput<InputEvent>
        where TSource : IObservableInput<bool>
    {
        private bool m_PreviousValue; // TODO Initialize
        private ObserverList<InputEvent> m_ObserverList;
        private IDisposable m_Subscription;
        private TSource m_Trigger;
        private Context m_Context;

        public Press(TSource button)
            : this(button, Context.instance)
        { }

        public Press(TSource button, Context context = null)
        {
            m_Trigger = button;
            m_Context = context;
        }

        private void UpdateSubscription(Context context)
        {
            if (m_Subscription != null)
                return;
            m_Subscription = m_Trigger.Subscribe(context, this);
            m_ObserverList = new ObserverList<InputEvent>(() =>
            {
                if (m_Subscription == null)
                    return;
                m_Subscription.Dispose();
                m_Subscription = null;
            });
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            UpdateSubscription(Context.instance); // TODO FIX, it should belong to
            return m_ObserverList.Add(observer);
        }

        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
        {
            UpdateSubscription(context); // TODO Fix, it should belong to
            return m_ObserverList.Add(observer);
        }

        public void OnCompleted()
        {
            m_ObserverList.Clear();
        }

        public void OnError(Exception error)
        {
            Debug.Log(error);
            m_ObserverList.Clear();
        }

        public void OnNext(bool value)
        {
            if (value && !m_PreviousValue)
                m_ObserverList.OnNext(new InputEvent());
            m_PreviousValue = value;
        }
    }    

    // This is a proxy concept to avoid up-front construction or allocations.
    // TODO Review whether there is an issue with relying on a shared press, yes there is, we need a centralized registry, however m_Press would be copied with struct copies. Might gain very little compared to creating class instance. 
    // TODO We might generalize this into a Proxy object instead taking an input and a operation type based on deferred initialization?
    internal struct PressObservableInput<TSource> : IObservableInput<InputEvent>
        where TSource : IObservableInput<bool>
    {
        private readonly TSource m_Source;
        private Press<TSource> m_Press;
        
        public PressObservableInput(TSource source)
        {
            m_Source = source;
            m_Press = null;
        }
        
        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            return Subscribe(Context.instance, observer);
        }

        public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
        {
            m_Press ??= new Press<TSource>(m_Source);
            return m_Press.Subscribe(observer);
        }
        
        public IDisposable Subscribe<TObserver>(TObserver observer)
            where TObserver : IObserver<InputEvent>
        {
            return Subscribe(Context.instance, observer);
        }
        
        public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
            where TObserver : IObserver<InputEvent>
        {
            m_Press ??= new Press<TSource>(m_Source);
            return m_Press.Subscribe(observer);
        }
    }

    /// <summary>
    /// Allows applying press interaction evaluation as part of fluent API design.
    /// </summary>
    // TODO There is little point in having the struct if we return interface here since we introduce boxing.
    public static class PressExtensionMethods
    {
        public static IObservableInput<InputEvent> Pressed<TSource>(this TSource source, Context context)
            where TSource : IObservableInput<bool>
        {
            return new Press<TSource>(source, context);
        }

        public static IObservableInput<InputEvent> Pressed<TSource>(this TSource source)
            where TSource : IObservableInput<bool>
        {
            return Pressed(source, Context.instance);
        }
    }
}
