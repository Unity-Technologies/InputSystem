using System;

namespace UnityEngine.InputSystem
{
    internal sealed class DeferBindingResolutionContext : IDisposable
    {
        public int deferredCount => m_DeferredCount;

        public void Acquire()
        {
            ++m_DeferredCount;
        }

        public void Release()
        {
            if (m_DeferredCount > 0 && --m_DeferredCount == 0)
                ExecuteDeferredResolutionOfBindings();
        }

        /// <summary>
        /// Allows usage within using() blocks, i.e. we need a "Release" method to match "Acquire", but we also want
        /// to implement IDisposable so instance are automatically cleaned up when exiting a using() block.
        /// </summary>
        public void Dispose()
        {
            Release();
        }

        private void ExecuteDeferredResolutionOfBindings()
        {
            ++m_DeferredCount;
            try
            {
                if (bindingsNeedResolving)
                {
                    ref var globalList = ref InputActionState.s_GlobalState.globalList;

                    for (var i = 0; i < globalList.length; ++i)
                    {
                        var handle = globalList[i];

                        var state = handle.IsAllocated ? (InputActionState)handle.Target : null;
                        if (state == null)
                        {
                            // Stale entry in the list. State has already been reclaimed by GC. Remove it.
                            if (handle.IsAllocated)
                                globalList[i].Free();
                            globalList.RemoveAtWithCapacity(i);
                            --i;
                            continue;
                        }

                        for (var n = 0; n < state.totalMapCount; ++n)
                            state.maps[n].ResolveBindingsIfNecessary();
                    }
                    bindingsNeedResolving = false;
                }
            }
            finally
            {
                --m_DeferredCount;
            }
        }

        private int m_DeferredCount;
        public bool bindingsNeedResolving;
    }
}
