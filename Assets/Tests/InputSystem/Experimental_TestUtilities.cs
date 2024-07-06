using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    internal class ListObserver<T> : IObserver<T>
    {
        public readonly List<Exception> Error = new();
        public readonly List<T> Next = new();

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            Error.Add(error);
        }

        public void OnNext(T value)
        {
            Next.Add(value);
        }
    }

    /// <summary>
    /// An observer that prints observed values to the console.
    /// </summary>
    /// <typeparam name="T">The observed value type.</typeparam>
    public class DebugObserver<T> : IObserver<T> where T : struct
    {
        public void OnCompleted() => Debug.Log("OnCompleted");
        public void OnError(Exception error) => Debug.Log("OnError: " + error);
        public void OnNext(T value) => Debug.Log("OnNext: " + value);
    }
    
    // Button stub to improve readability of test code
    internal readonly struct ButtonStub
    {
        private readonly Stream<bool> m_Stream;

        public ButtonStub(Stream<bool> stream)
        {
            m_Stream = stream;
        }

        public void Press()
        {
            m_Stream.OfferByValue(true);
        }

        public void Release()
        {
            m_Stream.OfferByValue(false);
        }
    }

    internal readonly struct Stub<T> where T : struct
    {
        private readonly Stream<T> m_Stream;

        public Stub(Stream<T> stream)
        {
            m_Stream = stream;
        }

        public void Change(T value)
        {
            m_Stream.OfferByValue(value);
        }
        
        public void Change(ref T value)
        {
            m_Stream.OfferByRef(ref value);
        }
    }

    // Allows constructing a stub from an observable input
    internal static class StubExtensions
    {
        public static Stub<T> Stub<T>(this ObservableInput<T> source, Context context, T initialValue = default)
            where T : struct
        {
            return new Stub<T>(context.CreateStream(key: source.Usage, initialValue: initialValue));
        }
        
        public static ButtonStub Stub(this ObservableInput<bool> source, Context context, bool initialValue = false)
        {
            return new ButtonStub(context.CreateStream(key: source.Usage, initialValue: initialValue));
        }
    }
}
