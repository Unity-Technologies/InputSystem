using System;

namespace UnityEngine.InputSystem.Experimental
{
    public struct UnsafeSubscription : IDisposable
    {
        // TODO Instead consider storing an unsafe delegate + callback or id associated with that callback, main reason being that we want to unregister underlying if reaching 0
        // TODO This would yield 16 + 16 bytes subscription size on 64-bit or 8+8 bytes on 32-bit.
        // TODO Note that empty C# class object is 16 bytes, see https://stackoverflow.com/questions/3694423/size-of-a-class-object-in-net
        
        // private delegate*<void*, void> m_Unsubscribe;
        
        //private IntPtr m_EventHandler; // TODO Consider if this should be a pointer to event handler to modify state?!
        private UnsafeDelegate<UnsafeCallback> m_Unsubscribe;    // 1 pointer
        private readonly UnsafeCallback m_Callback;              // 2 pointers
          
        internal UnsafeSubscription(UnsafeDelegate<UnsafeCallback> unsubscribe, UnsafeCallback callback) 
        {
            m_Unsubscribe = unsubscribe;
            m_Callback = callback;
        }
        
        /*internal UnsafeSubscription(IntPtr eventHandler, UnsafeDelegateHelper.Callback callback) 
        {
            m_EventHandler = eventHandler;
            m_Callback = callback;
        }*/
            
        public void Dispose()
        {
            // TODO Remove callback, and if removing the callback reDesults in empty delegate unsubscribe from underlying via destroy
            m_Unsubscribe.Invoke(m_Callback);
            //UnsafeDelegateHelper.Remove(ref m_EventHandler, m_Callback);
        }
    }
}