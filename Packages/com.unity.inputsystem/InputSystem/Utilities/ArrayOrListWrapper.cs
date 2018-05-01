using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Utilities
{
    /// <summary>
    /// A helper to allow a single code path to add elements to both
    /// a list or an array without requiring an object wrapper to be allocated.
    /// </summary>
    internal struct ArrayOrListWrapper<TValue>
    {
        public List<TValue> list;
        public TValue[] array;
        public int count;

        public bool isNull
        {
            get { return array == null && list == null; }
        }

        public ArrayOrListWrapper(List<TValue> list)
        {
            this.list = list;
            array = null;
            if (list != null)
                count = list.Count;
            else
                count = 0;
        }

        public ArrayOrListWrapper(TValue[] array, int count)
        {
            list = null;
            this.array = array;
            this.count = count;
        }

        public void Add(TValue value)
        {
            if (list != null)
            {
                list.Add(value);
                ++count;
            }
            else
                ArrayHelpers.AppendWithCapacity(ref array, ref count, value);
        }
    }
}
