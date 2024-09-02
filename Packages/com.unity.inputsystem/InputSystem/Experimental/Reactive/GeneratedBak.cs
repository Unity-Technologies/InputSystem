// // WARNING: This is an auto-generated file. Any manual edits will be lost.
// using System;
// using System.Buffers;
// using System.Collections.Concurrent;
// using UnityEngine.InputSystem.Utilities;
//
// namespace UnityEngine.InputSystem.Experimental
// {
//     /// <summary>
//     /// TemplateSummaryDoc
//     /// </summary>
//     [InputSource]
//     public readonly partial struct Process<TSource> : IObservableInputNode<InputEvent>
//         where TSource : IObservableInputNode<bool>
//     {
//         private readonly TSource m_Source;
//
//         public Process([InputPort] TSource source)
//         {
//             m_Source = source;
//         }
//     } 
//
//     public readonly partial struct Process<TSource> : IObservableInputNode<InputEvent>
//         where TSource : IObservableInputNode<bool>
//     {
//         #region IObservable<InputEvent>
//
//         public IDisposable Subscribe(IObserver<InputEvent> observer) 
//         {
//             // Forward subscription  request to context-specific subscription method using the default context.
//             return Subscribe(Context.instance, observer);
//         }
//
//         #endregion
//
//         #region IObservableInput<InputEvent>
//
//         public IDisposable Subscribe<TObserver>(Context context, TObserver observer)
//             where TObserver : IObserver<InputEvent>
//         {
//             var impl = ProcessObserver<TSource>.Rent();
//             impl.Initialize(m_Source);
//             // TODO This is confusion between sharing a node and object pool, clarify
//             // Reuse node if an equivalent instance has already been registered. 
//             // If no such instance exists within the given context, construct a new instance.
//             /*var impl = context.GetNodeImpl<ProcessObserver<TSource>>(this);
//             if (impl == null)
//             {
//                 impl = new ProcessObserver<TSource>();
//                 impl.Initialize(m_Source);
//                 context.RegisterNodeImpl(this, impl); // TODO Unable to unregister impl with this design
//             }*/
//             
//             // Note that subscription is done on the actual implementation node where we register observer
//             // to be invoked when impl has a new value.
//             return impl.Subscribe(context, observer);
//         }
//
//         #endregion
//
//         #region IDependencyGraphNode
//
//         public bool Equals(IDependencyGraphNode other) => other is Process<TSource> node && Equals(node);
//         public bool Equals(Process<TSource> other) => m_Source.Equals(other.m_Source);    
//         public string displayName => "Process"; 
//         public int childCount => 1; 
//         public IDependencyGraphNode GetChild(int index) 
//         {
//             switch (index)
//             {
//                 case 0:  return m_Source;
//                 default: throw new ArgumentOutOfRangeException(nameof(index));
//             }
//         }
//
//         #endregion
//     }
//
//     /// <summary>
//     /// Extension methods for <see cref="UnityEngine.InputSystem.Experimental.Process<TSource>"/> providing
//     /// type invariant decoration of dependency chain nodes.
//     /// </summary>
//     public static class ProcessExtensions
//     {
//
//     }
//
//
//     
//     // Safe implementation of "Process" behavior.
//     internal sealed class ProcessObserver<TSource> : IObserver<bool>, IDisposable, IUnsubscribe<InputEvent>
//         where TSource : IObservableInputNode<bool>
//     {
//         // TODO Pool could be outside this class
//         private static readonly ConcurrentQueue<ProcessObserver<TSource>> NodePool = new ();
//
//         private bool m_PreviousValue;                   // TODO This is state    
//         private IObserver<InputEvent>[] m_Observers;    // TODO Use a pool
//         private int m_ObserverCount;
//         private TSource m_Source;
//         private IDisposable m_SourceSubscription;
//
//         public static ProcessObserver<TSource> Rent()
//         {
//             return NodePool.TryDequeue(out var instance) ? instance : new ProcessObserver<TSource>();
//         }
//
//         public static void Return(ProcessObserver<TSource> instance)
//         {
//             NodePool.Enqueue(instance);
//         }
//         
//         public void Initialize(TSource source)
//         {
//             m_Source = source;
//         }
//
//         public void Dispose()
//         {
//             // Unsubscribe to dependencies
//             m_SourceSubscription.Dispose();
//             m_SourceSubscription = null;
//             
//             // Return list of observers to pool
//             ArrayPool<IObserver<InputEvent>>.Shared.Return(m_Observers);
//             m_Observers = null;
//             m_ObserverCount = 0;
//         }
//
//         public void Unsubscribe(IObserver<InputEvent> observer)
//         {
//             var index = Array.IndexOf(m_Observers, observer);
//             if (index == -1)
//                 throw new Exception("Internal error: Observer not found, this should never happen. Please report this as a bug.");
//             Array.Copy(m_Observers, index + 1, m_Observers, index, m_ObserverCount - index - 1);
//             if (--m_ObserverCount == 0)
//                 Dispose();
//         }
//         
//         public IDisposable Subscribe(Context context, IObserver<InputEvent> observer)
//         {
//             if (m_Observers == null)
//             {
//                 // This is the first subscription to this node
//                 m_Observers = ArrayPool<IObserver<InputEvent>>.Shared.Rent(1);
//                 m_Observers[0] = observer;
//                 m_ObserverCount = 1;
//
//                 // Subscribe to underlying dependencies
//                 m_Source.Subscribe(context, this);
//                 
//                 // TODO Synchronize state
//             }
//             else if (m_ObserverCount == m_Observers.Length)
//             {
//                 // Adding the new observer exceeds buffer capacity so reallocate
//                 var array = ArrayPool<IObserver<InputEvent>>.Shared.Rent( m_ObserverCount + 1);
//                 Array.Copy(m_Observers, 0, array, 0, m_ObserverCount);
//                 array[m_ObserverCount++] = observer;
//                 ArrayPool<IObserver<InputEvent>>.Shared.Return(m_Observers);
//                 m_Observers = array;
//             }
//             else
//             {
//                 m_Observers[m_ObserverCount++] = observer;
//             }
//             
//             return new Subscription<InputEvent>(this, observer); // TODO Fix
//         }
//
//         public void OnCompleted()
//         {
//             for (var i = 0; i < m_ObserverCount; ++i)
//                 m_Observers[i].OnCompleted();
//         }
//         public void OnError(Exception error)
//         {
//             for (var i = 0; i < m_ObserverCount; ++i)
//                 m_Observers[i].OnError(error);
//         }
//
//         private void ForwardOnNext(InputEvent value)
//         {
//             for (var i = 0; i < m_ObserverCount; ++i)
//                 m_Observers[i].OnNext(new InputEvent());
//         }
//
//         private static void Process(bool value, ref bool previous, ref Observers<T> observers)
//         {
//             if (previous == value)
//                 return;
//             if (value)
//                 ForwardOnNext(observers, value);
//             previous = value;
//         }
//         
//         public void OnNext(bool value)
//         {
//             if (m_PreviousValue == value) 
//                 return;
//             if (value) // TODO Let class be converted to Step and take a IComparable<T> type, then we can use for both Press and Relase
//                 ForwardOnNext(new InputEvent());
//             m_PreviousValue = value;
//         }        
//     }
// }