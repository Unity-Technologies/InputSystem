using System.Runtime.CompilerServices;

namespace UnityEngine.InputSystem.Experimental
{
    // Note that we utilize the struct itself as keeper of state since owned by node
    public struct MyStatefulOperation
    {
        private bool m_Previous;
        
        //[InputOperation, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Changed<TContext>(TContext ctx, [InputPort] bool value)
            where TContext : IForwardOnNext<bool>
        {
            if (m_Previous == value)
                return;
            m_Previous = value;
            ctx.ForwardOnNext(value);
        }
    }
}