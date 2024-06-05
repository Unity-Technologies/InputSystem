using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO RX interface only allows single observer per type, this is a problem
    // TODO Should setup observerlist so that we unsubscribe on underlying properly
    public class Press : IObserver<bool>, IInputBindingSource<InputEvent>
    {
        public InputBindingSource<InputEvent> pressed { get; set; }
        private bool m_PreviousValue; // TODO Not properly initialized
        private ObserverList<InputEvent> m_ObserverList;
        private IDisposable m_Subscription;
        private InputBindingSource<bool> m_Trigger;
        private Context m_Context;

        public Press(InputBindingSource<bool> button)
            : this(button, Context.instance)
        {}

        public Press(InputBindingSource<bool> button, Context context = null)
        {
            m_Trigger = button;
            m_Context = context;
            m_ObserverList = new ObserverList<InputEvent>(() =>
            {
                if (m_Subscription == null)
                    return;
                m_Subscription.Dispose();
                m_Subscription = null;
            });
        }

        public void Process(bool value)
        {
            if (value && !m_PreviousValue)
                Emit();
            m_PreviousValue = value;
        }

        private void Emit()
        {
            m_ObserverList.OnNext(new InputEvent());
        }

        private void UpdateSubscription(Context context)
        {
            if (m_Subscription != null)
                return;
            m_Subscription = m_Trigger.Subscribe(context, this);
        }

        public IDisposable Subscribe(IObserver<InputEvent> observer)
        {
            UpdateSubscription(Context.instance); // TODO FIX, it should belong to
            return m_ObserverList.Add(observer);
        }

        public IDisposable Subscribe(Context ctx, IObserver<InputEvent> observer)
        {
            UpdateSubscription(ctx); // TODO Fix, it should belong to
            return m_ObserverList.Add(observer);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(bool value)
        {
            Process(value);
        }
    }

    public static class PressInteractionExtensions
    {
        public static IInputBindingSource<InputEvent> Pressed(this InputBindingSource<bool> source, Context context)
        {
            return new Press(source, context);
        }

        public static DerivedInputBindingSource<Button, InputEvent> Pressed(this InputBindingSource<Button> source, Context context)
        {
            // TODO We need to allocate a derived observer
            return new DerivedInputBindingSource<Button, InputEvent>(source, (x) => new InputEvent());

            // TODO Here we basically want to return a source that would derive our desired data from data
            //var observer = new PressInputBindingSource();
            //data.Subscribe(context, observer);
            //return new InputBindingSource<Event>(); // TODO Here we could create a source that provides an operation on data
            //for (var i=0; i < data.)
            // TODO We want to apply press interaction here to provide a derived stream or observable
            //data.Subscribe();
        }

        // TODO Should not have 2
        public static DerivedInputBindingSource<Button, InputEvent> Pressed(this InputBindingSource<Button> data)
        {
            return Pressed(data, Context.instance);
        }

        public static IInputBindingSource<InputEvent> Pressed(this InputBindingSource<bool> source)
        {
            return Pressed(source, Context.instance);
            //return new DerivedInputBindingSource<bool, InputEvent>(source, (x) => new InputEvent());
        }
    }
}
