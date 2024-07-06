using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental
{
    public interface IReader<T> : IEnumerable<T>
    {
        
    }
    
    // Should be able to 
    public class Reader<T, TSource> : IEnumerable<T>
        where TSource : IEnumerable<T>
    {
        private readonly TSource m_Source;
        
        public Reader(TSource source)
        {
            m_Source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            // How do we do this, basically we drive observables by emitting form source
            // If relying on same we would dig up underlying and drive them, but this is problematic.
            // It might be that this is difficult....
            return m_Source.GetEnumerator(); // TODO How can this not be virtual?!
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}