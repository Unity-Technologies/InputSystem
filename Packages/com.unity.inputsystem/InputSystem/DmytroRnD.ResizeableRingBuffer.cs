using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;

namespace UnityEngine.InputSystem.DmytroRnD
{
    // a prototype of on-the-fly-resizeable ringbuffer
    internal struct ResizeableRingBuffer<T> where T : struct
    {
        // NativeArray is a bit meh structure here
        // replace with something a bit faster, backed by a pool, because we need fast reallocs
        public NativeArray<T> Buffer;
        public uint Tail; // points to last element
        public uint Head; // points to an empty slot
        // MaxCapacity = Length - 1 

        public void Setup()
        {
            Head = 0;
            Tail = 0;
        }

        public void Clear()
        {
            if (Buffer.IsCreated)
                Buffer.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T value)
        {
            if (Full())
            {
                // TODO it's probably fine as-is, but maybe there is a better way than to just copy to a new buffer?
                var newBuffer = new NativeArray<T>(
                    Mathf.Max(8, Buffer.Length * 2), // TODO better size hints
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory /*NativeArrayOptions.UninitializedMemory */);
                
                //Debug.Log($"resize to new size of {newBuffer.Length}");

                for (uint i = 0; i < Count(); ++i)
                    newBuffer[(int)((Tail + i) % newBuffer.Length)] = Get(Tail + i );

                if (Buffer.IsCreated)
                    Buffer.Dispose();
                Buffer = newBuffer;
                
            }

            Buffer[(int)(Head % Buffer.Length)] = value;
            Head++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopN(int count)
        {
            if ((uint) count > Head - Tail)
                Tail = Head;
            else
                Tail += (uint)count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Empty()
        {
            return Buffer.Length == 0 || ((Head % Buffer.Length) == (Tail % Buffer.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Full()
        {
            return Buffer.Length == 0 || (Head + 1) % Buffer.Length == Tail % Buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Count()
        {
            return Head - Tail;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(uint i)
        {
            // TODO if we know that length is power of two, we can replace it with 'i & (Buffer.Length - 1)' 
            return Buffer[(int)(i % Buffer.Length)];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"t{Tail}->h{Head}(");
            for (var i = 0; i < Buffer.Length; ++i)
            {
                if (i != 0)
                    sb.Append(",");
                sb.Append(Buffer[i]);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}