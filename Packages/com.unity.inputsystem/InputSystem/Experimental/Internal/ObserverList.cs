using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider backing ObserverList by a memory pool to reduce allocations
    // TODO Consider pooling both list and subscriptions. Subscription could be simplified if mapping subscription to subscription keys. A subscription key may allow tracking both the source and a derivative from the source.
    // TODO Consider if this should be a class.
    // TODO Consider if instead of have onUnsubscribed, this could instead be passed sources to subscribe to and handle unsubscribe for us?
    
    // Subscription:
    // - An object that represents a specific callback subscription and supports disposal in which case the
    //   subscription is removed and all underlying subscriptions are removed.

   internal class ReferenceTypeObjectPool<T> : IDisposable 
       where T : class
    {
        private struct Interval
        {
            public readonly int LowerBoundInclusive;
            public readonly int UpperBoundExclusive;
        }
        
        private T[] m_Data;
        private Interval[] m_FreeList;

        public ReferenceTypeObjectPool(int initialCapacity)
        {
            m_Data = new T[initialCapacity];
        }

        public PooledRange Allocate(int size)
        {
            return new PooledRange(this, 0, 0);
        }

        public void Dispose()
        {
#if DEVELOPMENT_BUILD
            if (m_Data != null)
            {
                for (var i = 0; i < m_Data.Length; ++i)
                    m_Data[i] = null;
            }
#endif
            m_Data = null;
        }

        internal readonly struct PooledRange : IDisposable
        {
            private readonly ReferenceTypeObjectPool<T> m_Pool;
            private readonly int m_LowerBoundInclusive;
            private readonly int m_UpperBoundExclusive;

            internal PooledRange(ReferenceTypeObjectPool<T> pool, int lowerBoundInclusive, int upperBoundExclusive)
            {
                m_Pool = pool;
                m_LowerBoundInclusive = lowerBoundInclusive;
                m_UpperBoundExclusive = upperBoundExclusive;
            }
            
            public int count => m_UpperBoundExclusive - m_LowerBoundInclusive;

            public T this[int index]
            {
                get => m_Pool.m_Data[m_LowerBoundInclusive + index];
                set => m_Pool.m_Data[m_LowerBoundInclusive + index] = value;
            }

            public void Dispose()
            {
                // Clear slots in data array
                for (var i = m_LowerBoundInclusive; i < m_UpperBoundExclusive; ++i)
                    m_Pool.m_Data[i] = null;
            }
        }
    }

   // This implementation is driving towards sub-goal of making a processing step free of GC allocation.
   //
   // What do we need?
   // - A place to store a list of disposables to be cleared once list has zero subscribers
   // - A way to return IDisposable objects (Subscriptions) that when disposed remove 1 specific observer from list.
   // - A reference to consecutive list fo observables to callback and its count
   //
   // Note that disposables not necessarily need to be consecutive, a basic object pool might be good enough.
   // Note that when adding subscribers we might need to reallocate underlying array ob observers so subscription
   // cannot keep a reference to it unless subscriptions are known by list and updated when necessary.
   // Note that if list is in turn stored in a pool we may associate an id with it to solve subscription problem.
   //
   // What is an observer list? It is basically just the offset to the observer, so we do not need an object
   /*internal struct ObserverList3<T> : IObserver<T>, IDisposable
   {
       private static Dictionary<int, object> _observerListLookup = new Dictionary<int, object>();
       private static int _observerListId;
       
       private ArrayPool<IObserver<T>> observerPool;
       private IObserver<T>[] m_Observers;
       private int m_ObserverCount;
       
       private ArrayPool<IDisposable> disposablePool;
       private IDisposable[] disposables;
       private int m_DisposableCount;

       private ObserverList3(ArrayPool<IObserver<T>> observerPool, ArrayPool<IDisposable> disposablePool, 
           int disposableLength)
       {
           this.observerPool = observerPool;
           this.disposablePool = disposablePool;
           this.disposables = disposablePool.Rent(disposableLength);

           // We would like to register list, but we cannot
           var id = _observerListId++;
           while (_observerListLookup.ContainsKey(id))
               id = _observerListId++;
           _observerListLookup.Add(id, null);
           
           m_Observers = null;
           m_ObserverCount = 0;
           m_DisposableCount = 0;
       }
       
       public ObserverList3(ArrayPool<IObserver<T>> observerPool, ArrayPool<IDisposable> disposablePool, 
           IDisposable disposable)
        : this(observerPool, disposablePool, 1)
       { 
           disposables[0] = disposable;
       }
       
       public ObserverList3(ArrayPool<IObserver<T>> observerPool, ArrayPool<IDisposable> disposablePool, 
           IDisposable disposable0, IDisposable disposable1)
       : this(observerPool, disposablePool, 2)
       {
           disposables[0] = disposable0;
           disposables[1] = disposable1;
       }

       public ObserverList3(ArrayPool<IObserver<T>> observerPool, ArrayPool<IDisposable> disposablePool, 
           IDisposable disposable0, IDisposable disposable1, IDisposable disposable2)
           : this(observerPool, disposablePool, 3)
       {
           disposables[0] = disposable0;
           disposables[1] = disposable1;
           disposables[2] = disposable2;
       }       
       
       public ObserverList3(ArrayPool<IObserver<T>> observerPool, ArrayPool<IDisposable> disposablePool, 
           IDisposable disposable0, IDisposable disposable1, IDisposable disposable2, IDisposable disposable3)
           : this(observerPool, disposablePool, 4)
       {
           disposables[0] = disposable0;
           disposables[1] = disposable1;
           disposables[2] = disposable2;
           disposables[3] = disposable3;
       }

       public ObserverList3(ArrayPool<IObserver<T>> observablePool, ArrayPool<IDisposable> disposablePool,
           IDisposable[] disposables)
        : this(observablePool, disposablePool, disposables.Length)
       {
           for (var i = 0; i < this.disposables.Length; ++i)
               this.disposables[i] = disposables[i];
       }
       
       public void OnCompleted()
       {
           for (var i = 0; i < m_ObserverCount; ++i)
               m_Observers[i].OnCompleted();
       }

       public void OnError(Exception e)
       {
           for (var i = 0; i < m_ObserverCount; ++i)
               m_Observers[i].OnError(e);
       }

       public void OnNext(T value)
       {
           for (var i = 0; i < m_ObserverCount; ++i)
               m_Observers[i].OnNext(value);
       }

       internal struct Subscription : IDisposable
       {
           // TODO This wouldn't work we need to remove from observable list, which basically is not any different from context if single array. However, with array pool it is different.
           
           private readonly Context m_Context;
           private readonly int m_ObserverOffset; // TODO This would get outdated if re-allocated, so we need a generated handle
           
           public Subscription(object observer)
           {
               //m_Observer = observer;
               // TODO Store reference to context and an identifier (index or ID?) of the relevant observer.
           }

           public void Dispose()
           {
               // TODO Release subscription registered
           }
       }
       
       public IDisposable Subscribe(Context context, IObserver<T> observer)
       {
           // TODO We definately want observers in a consecutive array, hence we likely want ArrayPool or SlicePool for that. This solves problem of registration and disposal. 
           // TODO Subscription needs to be associated with observer/observerArray combo.
           
           if (m_Observers == null)
               m_Observers = observerPool.Rent(1);
           else if (m_ObserverCount == m_Observers.Length)
           {
               var old = m_Observers;
               var n = m_ObserverCount;
               m_Observers = observerPool.Rent(m_ObserverCount + 1);
               Array.Copy(old, m_Observers, n);
           }
           m_Observers[m_ObserverCount++] = observer;
           return new Subscription(this, observer);
       }
       
       public void Dispose()
       {
           if (disposables != null)
           {
               disposablePool.Return(disposables);    
           }
       }
   }*/
   
   // STRUCT-BASED VARIANT OF OBSERVER LIST 4
   // TODO Consider using a pool: https://medium.com/@epeshk/the-big-performance-difference-between-arraypools-in-net-b25c9fc5e31d
   // TODO Compare to a class based subscription with object pool for subscriptions and pooled observer list of class type
   // TODO Problem with Subscription as a struct is that would be boxed if generalized. Right now its type based, which might be fine though.
   internal struct ObserverList4<T> : IObserver<T>
   {
       private const int kInitialObserverRegisterCapacity = 100;
       private static int _handle = 0;
       private static readonly Dictionary<int, ObserverList4<T>> HandleToArray = new Dictionary<int, ObserverList4<T>>(kInitialObserverRegisterCapacity);
       
       // Note that this will likely not be boxed if used in a using statement, see:
       // https://stackoverflow.com/questions/2412981/if-my-struct-implements-idisposable-will-it-be-boxed-when-used-in-a-using-statem
       internal struct Subscription : IDisposable
       {
           private readonly int m_OwnerHandle;
           private IObserver<T> m_Observer;

           public Subscription(int ownerHandle, IObserver<T> observer)
           {
               m_OwnerHandle = ownerHandle;
               m_Observer = observer;
           }

           public void Dispose()
           {
               // If already disposed
               if (m_Observer == null) // If disposed
                   return;
               
               // Retrieve copy of list, modify it and store it back in the register
               if (!HandleToArray.TryGetValue(m_OwnerHandle, out var owner))
                   return;
               owner.Remove(m_Observer);
               HandleToArray[m_OwnerHandle] = owner;

               // Mark disposed
               m_Observer = null;
           }
       }
       
       private IObserver<T>[] m_Observers; // TODO If this was a range in a common array we could have used indices, that would give one array per type
       private int m_Length;
       private readonly int m_Handle;

       public ObserverList4(IObserver<T> observer) 
           : this()
       {
           m_Observers = Allocate(1);
           m_Observers[0] = observer;
           m_Length = 1;
           m_Handle = ++_handle;
           HandleToArray[m_Handle] = this; // copy
       }

       public ReadOnlySpan<IObserver<T>> observers => 
           new ReadOnlySpan<IObserver<T>>(m_Observers, 0, m_Length);

       private IObserver<T>[] Allocate(int size)
       {
           return ArrayPool<IObserver<T>>.Shared.Rent(size);
       }

       private void Deallocate(IObserver<T>[] array)
       {
           ArrayPool<IObserver<T>>.Shared.Return(array, clearArray: true);
       }
       
       public Subscription Add(IObserver<T> observer)
       {
           var reallocated = false;
           if (m_Length >= m_Observers.Length)
           {
               var array = Allocate(m_Length + 1);
               Array.Copy(m_Observers, 0, array, 0, m_Length);
               Deallocate(m_Observers);
               m_Observers = array;
               reallocated = true;
           }
           m_Observers[m_Length++] = observer;
           if (reallocated)
               HandleToArray[m_Handle] = this;

           return new Subscription(m_Handle, observer);
       }

       public void Remove(IObserver<T> observer)
       {
           var index = Array.IndexOf(m_Observers, observer, m_Length);
           if (index < 0)
               return; // TODO Since subscription is struct we might have disposed an accidental copy, we should probably generate a warning
           Array.Copy(m_Observers, index + 1, m_Observers, index, m_Length - index - 1);
           --m_Length;
           HandleToArray[m_Handle] = this;
       }
       
       public void OnCompleted()
       {
           for (var i = 0; i < m_Length; ++i)
               m_Observers[i].OnCompleted();
       }

       public void OnError(Exception error)
       {
           for (var i = 0; i < m_Length; ++i)
               m_Observers[i].OnError(error);
       }

       public void OnNext(T value)
       {
           for (var i = 0; i < m_Length; ++i)
               m_Observers[i].OnNext(value);
       }
   }   
   
   // TODO Consider standard or custom ArrayPools: https://medium.com/@epeshk/the-big-performance-difference-between-arraypools-in-net-b25c9fc5e31d

   // TODO Potential future improvement could be having a single inline observer but that should be guided by measurements
   // TODO Problematic to have this as a class but necessary to refer back to it from subscription.
   internal class ObserverList3<T> : IObserver<T>
   {
       internal sealed class Subscription : IDisposable
       {
           private ObserverList3<T> m_Owner;   // TODO This could be sub array or pooled array or simply context ref
           private IObserver<T> m_Observer;    // TODO This could be id since this is anyway O(N)

           public Subscription(ObserverList3<T> owner, IObserver<T> observer)
           {
               m_Owner = owner;
               m_Observer = observer;
           }

           public void Dispose()
           {
               if (m_Owner == null)
                   return;
                
               m_Owner.Remove(m_Observer);
                
               m_Owner = null;
               m_Observer = null;
           }
       }
       
       private IObserver<T> m_Observer;
       private IObserver<T>[] m_Observers; // TODO If this was a range in a common array we could have used indices, that would give one array per type
       private int m_Length;

       public ObserverList3(IObserver<T> observer)
       {
           m_Observer = observer;
           m_Observers = null;
           m_Length = 0;
       }

       public ReadOnlySpan<IObserver<T>> observers => 
           new ReadOnlySpan<IObserver<T>>(m_Observers, 0, m_Length);

       public Subscription Add(IObserver<T> observer)
       {
           if (m_Observers == null)
           {
               m_Observers = new IObserver<T>[2];
               m_Observers[0] = observer;
               m_Length = 1;
           }
           else if (m_Length < m_Observers.Length)
           {
               m_Observers[m_Length++] = observer;
           }
           else
           {
               var a = new IObserver<T>[m_Observers.Length * 2];
               Array.Copy(m_Observers, 0, a, 0, m_Length);
           }
           m_Observers[m_Length++] = observer;

           return null;
           //return new Subscription()
       }

       public void Remove(IObserver<T> observer)
       {
           if (ReferenceEquals(m_Observer, observer))
           {
               var index = m_Length - 1;
               m_Observer = m_Observers[index];
               m_Observers[index] = null;
               --m_Length;
           }
           else
           {
               var index = Array.IndexOf(m_Observers, observer, m_Length);
               if (index < 0)
                   throw new Exception();
               Array.Copy(m_Observers, index + 1, m_Observers, index, m_Length - index - 1);
           }
       }
       
       public void OnCompleted()
       {
           m_Observer.OnCompleted();
           for (var i = 0; i < m_Length; ++i)
           {
               m_Observers[i].OnCompleted();
           }
       }

       public void OnError(Exception error)
       {
           m_Observer.OnError(error);
           for (var i = 0; i < m_Length; ++i)
           {
               m_Observers[i].OnError(error);
           }
       }

       public void OnNext(T value)
       {
           m_Observer.OnNext(value);
           for (var i = 0; i < m_Length; ++i)
           {
               m_Observers[i].OnNext(value);
           }
       }
   }
   
    internal class ObserverList2<T> : IObserver<T>
    {
        private List<IObserver<T>> m_Observers;
        private IDisposable[] m_Disposables;

        public ObserverList2()
        {
            m_Observers = null;
        }
        public void Initialize(IDisposable[] disposables)
        {
            m_Disposables = disposables;
        }

        protected void Initialize(IDisposable disposable1, IDisposable disposable2)
        {
            m_Disposables = new [] { disposable1, disposable2 };
        }
        
        public ObserverList2(IDisposable disposable)
            : this(new [] { disposable })
        { }
        
        public ObserverList2(IDisposable disposable1, IDisposable disposable2)
            : this(new [] { disposable1, disposable2 })
        { }

        public ObserverList2(IDisposable disposable1, IDisposable disposable2, IDisposable disposable3, IDisposable disposable4)
            : this(new [] { disposable1, disposable2, disposable3, disposable4 })
        { }
        
        public ObserverList2(IDisposable[] disposables)
        {
            m_Observers = null;
            m_Disposables = disposables;
        }

        private void Remove(IObserver<T> observer)
        {
            if (!m_Observers.Remove(observer))
                throw new Exception("Unexpected error");
            if (m_Observers.Count == 0 && m_Disposables != null)
            {
                for (var i=0; i < m_Disposables.Length; ++i)
                    m_Disposables[i].Dispose();
            }
        }

        public bool empty => m_Observers == null || m_Observers.Count == 0;

        public int count => m_Observers?.Count ?? 0;

        #region IObserver<T>
        
        public void OnCompleted()
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnCompleted();
        }

        public void OnError(Exception e)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnError(e);
        }

        public void OnNext(T value)
        {
            for (var i = 0; i < m_Observers.Count; ++i)
                m_Observers[i].OnNext(value);
        }
        
        #endregion

        public Subscription Subscribe(Context context, IObserver<T> observer)
        {
            m_Observers ??= new List<IObserver<T>>(1); // TODO Opportunity to cache observers in context-specific array
            m_Observers.Add(observer);
            return new Subscription(this, observer);
        }
        
        // TODO Allow linking subscriptions, this way when doing bulk subscriptions, we may link them and only return first subscription of chain. This can avoid some array allocations?
        // TODO Consider caching subscriptions globally.
        // TODO Additionally, if always returning this subscription type we might as well avoid IDisposable.
        // TODO Could basically be index to observer list and index to observer within that list, or only global observer. Note that if we apply defragmentation we might need to to store subscriptions as well which might be desirable from debugging perspective.
        internal sealed class Subscription : IDisposable
        {
            private ObserverList2<T> m_Owner;   // TODO This could be sub array or pooled array or simply context ref
            private IObserver<T> m_Observer;    // TODO This could be id since this is anyway O(N)

            public Subscription(ObserverList2<T> owner, IObserver<T> observer)
            {
                m_Owner = owner;
                m_Observer = observer;
            }

            public void Dispose()
            {
                if (m_Owner == null)
                    return; // Already disposed
                
                m_Owner.Remove(m_Observer);
                
                m_Owner = null;
                m_Observer = null;
            }
        }
    }
}
