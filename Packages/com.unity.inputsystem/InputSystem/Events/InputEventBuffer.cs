using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.LowLevel
{
    public struct InputEventBuffer : IEnumerable<InputEventPtr>
    {
        public int count
        {
            get { throw new NotImplementedException(); }
        }

        public InputEventPtr eventPtr
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator<InputEventPtr> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
