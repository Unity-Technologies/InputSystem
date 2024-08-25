using UnityEngine.InputSystem.Experimental;

// TODO Consider if stubs are really needed if anyway have to support writers.

namespace Tests.InputSystem
{
    // Extensions that simplify creating a stub from a given source.
    internal static class StubExtensions
    {
        public static Stub<T> Stub<T>(this ObservableInputNode<T> source, Context context, T initialValue = default)
            where T : struct
        {
            return new Stub<T>(context.CreateStream(key: source.usage, initialValue: initialValue));
        }
        
        public static ButtonStub Stub(this ObservableInputNode<bool> source, Context context, bool initialValue = default)
        {
            return new ButtonStub(context.CreateStream(key: source.usage, initialValue: initialValue));
        }
    }
}