using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    public struct MyStatelessOperation
    {
        // Simplest case: Single input stateless node
        [InputOperation]
        internal static void Forward<TContext>(TContext ctx, [InputPort] bool value)
            where TContext : IForwardOnNext<bool>
        {
            ctx.ForwardOnNext(value);
        }
        
        // TODO: Eliminate InputPort?! it doesn't add value
        // Binary function: Two inputs stateless node.
        //[InputOperation]
        public static void Or<TContext>(TContext ctx, [InputPort] bool a, [InputPort] bool b)
            where TContext : IForwardOnNext<bool>
        {
            ctx.ForwardOnNext(a || b);
        }
    }

    // TODO Likely slightly better since it guides via interface and provides consistency between stateless and stateful operations. Expecting impact to be be small.
    // public interface IOperation<T>
    // {
    //     public void Forward<TContext>(TContext context, T value)
    //         where TContext : IForwardOnNext<T>;
    // }
    //
    // public struct Invert : IOperation<bool>
    // {
    //     [InputOperation]
    //     public void Forward<TContext>(TContext ctx, [InputPort] bool value)
    //         where TContext : IForwardOnNext<bool>
    //     {
    //         ctx.ForwardOnNext(value);
    //     }
    // }
}