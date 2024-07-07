using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem
{
    // Extensions that simplify creating a stub from a given source.
    internal static class StubExtensions
    {
        public static Stub<T> Stub<T>(this ObservableInput<T> source, Context context, T initialValue = default)
            where T : struct
        {
            return new Stub<T>(context.CreateStream(key: source.Usage, initialValue: initialValue));
        }
        
        public static ButtonStub Stub(this ObservableInput<bool> source, Context context, bool initialValue = default)
        {
            return new ButtonStub(context.CreateStream(key: source.Usage, initialValue: initialValue));
        }
    }
}