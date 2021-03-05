using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.DmytroRnD
{
    public struct PreDemuxer
    {
        public NativeArray<ulong> BackBuffer;
        public NativeArray<ulong> FrontBuffer;
        public NativeArray<ulong> EnabledBits;
        public NativeArray<ulong> ChangedBits;

        public int Length;
        public bool GotFirstEvent;
        public bool Flip;

        public PreDemuxer(int stateSizeInBytes)
        {
            Length = ((stateSizeInBytes + 7) / 8);
            GotFirstEvent = false;
            Flip = false;

            // allocate length + 1, so we can read two ulongs when iterator is at last position
            BackBuffer = new NativeArray<ulong>(Length + 1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            FrontBuffer = new NativeArray<ulong>(Length + 1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            EnabledBits = new NativeArray<ulong>(Length + 1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            ChangedBits = new NativeArray<ulong>(Length + 1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            // enable all bits
            unsafe
            {
                UnsafeUtility.MemSet(EnabledBits.GetUnsafePtr(), 0xff, Length * sizeof(ulong));
            }

            // set latest item to 0 
            BackBuffer[Length] = 0;
            FrontBuffer[Length] = 0;
            EnabledBits[Length] = 0;
            ChangedBits[Length] = 0;
        }

        public unsafe void Execute(void* rawState, int rawStateSize, ref ulong* front, ref ulong* changed)
        {
            var length = Length;

            front = (ulong*) (Flip ? BackBuffer.GetUnsafePtr() : FrontBuffer.GetUnsafePtr());
            var back = (ulong*) (Flip ? FrontBuffer.GetUnsafePtr() : BackBuffer.GetUnsafePtr());
            var enabled = (ulong*) EnabledBits.GetUnsafePtr();
            changed = (ulong*) ChangedBits.GetUnsafePtr();
            Flip = !Flip;

            // just copy the whole struct so we get padded data
            UnsafeUtility.MemCpy(front, rawState, Math.Min(rawStateSize, length * sizeof(ulong)));

            // invert all bits so all of them are in changed mask first time
            if (!GotFirstEvent)
            {
                for (var i = 0; i < length; ++i)
                    back[i] = ~front[i];
                GotFirstEvent = true;
            }

            // calculate change mask
            for (var i = 0; i < length; ++i)
                changed[i] = (back[i] ^ front[i]) & enabled[i];
        }

        public void Dispose()
        {
            BackBuffer.Dispose();
            FrontBuffer.Dispose();
            EnabledBits.Dispose();
            ChangedBits.Dispose();
        }
    }

    // describes what type of binary data we're reading
    public enum DynamicDemuxerSourceType
    {
        UnsignedBits, // read as unsigned bits, take magnitude as-is

        //ExcessK, // read as excess-K bits, apply precalculated offset
        Float32, // IEEE 754 float32

        //NormalizedUnsignedBits,
        // SignedBits
        // NormalizedSignedBits
    }

    // describes what destination data type we want, for example:
    // - reading 0xff as UnsignedBits -> Float will give us 255.0f
    // - reading 0x3f800000 as Float32 -> UInt will give us 1
    // - reading 0x3f800000 as UnsignedBits -> UInt will give us 1065353216
    public enum DynamicDemuxerDestinationType
    {
        Float,
        UInt
    }

    public struct DynamicDemuxerField
    {
        public uint BitOffset;
        public byte BitSize;

        public DynamicDemuxerSourceType SourceType;
        public DynamicDemuxerDestinationType DestinationType;
        public uint DestinationSlot;

        // if true, please assume that in nearly every state change this field's data will be changing
        // suitable for mouse position/delta for example
        public bool OftenChangingFieldHint;
    }

    //[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct DynamicDemuxer : IJob, IDisposable
    {
        private enum DynamicDemuxerCommand
        {
            GoToCommand,
            Parse,
            Break
        }

        private PreDemuxer m_PreDemuxer;

        private NativeArray<uint> m_Index;
        private NativeArray<ulong> m_MaskA;
        private NativeArray<ulong> m_MaskB;
        private NativeArray<DynamicDemuxerCommand> m_CommandIfChanged;
        private NativeArray<DynamicDemuxerCommand> m_CommandIfNotChanged;
        private NativeArray<int> m_GoToCommand;
        private NativeArray<byte> m_ShiftA;
        private NativeArray<byte> m_ShiftB;
        private NativeArray<DynamicDemuxerSourceType> m_SourceType;
        private NativeArray<DynamicDemuxerDestinationType> m_DestinationType;
        private NativeArray<uint> m_DestinationSlot;

        private unsafe void* m_RawState;
        private int m_RawStateSize;

        public NativeArray<uint> floatResultsDestinationSlotArray;
        public NativeArray<float> floatResultsValuesArray;
        public NativeArray<uint> uintResultsDestinationSlotArray;
        public NativeArray<uint> uintResultsValuesArray;
        public int floatResultsCount;
        public int uintResultsCount;

        public DynamicDemuxer(int stateSizeInBytes, DynamicDemuxerField[] fields)
        {
            m_PreDemuxer = new PreDemuxer(stateSizeInBytes);

            // naive algorithm

            // TODO better algorithm were we do cascading checks:
            // for example if we have 64 bit packaged of buttons
            // first check that whole 64 bit value is != 0
            // then check if 32 bit value is != 0
            // then .. 16 bit
            // then .. 8 bit
            // then .. 4 bit
            // then go value by value?
            // so we get amortized O(logN)

            var length = fields.Length + 1;

            m_Index = new NativeArray<uint>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_MaskA = new NativeArray<ulong>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_MaskB = new NativeArray<ulong>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_CommandIfChanged = new NativeArray<DynamicDemuxerCommand>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_CommandIfNotChanged = new NativeArray<DynamicDemuxerCommand>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_GoToCommand =
                new NativeArray<int>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ShiftA =
                new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ShiftB =
                new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_SourceType =
                new NativeArray<DynamicDemuxerSourceType>(length, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            m_DestinationType = new NativeArray<DynamicDemuxerDestinationType>(length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_DestinationSlot =
                new NativeArray<uint>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_CommandIfChanged[fields.Length] = DynamicDemuxerCommand.Break;
            m_CommandIfNotChanged[fields.Length] = DynamicDemuxerCommand.Break;

            for (var i = 0; i < fields.Length; ++i)
            {
                // bytes in memory:
                // 0123456789abcdef
                // .......XXXX.....
                // AAAAAAAABBBBBBBB
                // bitOffset = 56
                // shiftA = 56
                // shiftB = 8
                // maskA = 0xFF  


                var f = fields[i];
                var ulongOffset = f.BitOffset / 64;
                var bitOffset = f.BitOffset - ulongOffset * 64;
                var maskValue = (~0UL) >> (64 - f.BitSize);

                m_Index[i] = ulongOffset;
                if (bitOffset == 0)
                {
                    m_ShiftA[i] = 0;
                    m_ShiftB[i] = 0;
                    m_MaskA[i] = maskValue;
                    m_MaskB[i] = 0; // special case, because we can only shift up to 63
                }
                else
                {
                    m_ShiftA[i] = (byte) bitOffset;
                    m_ShiftB[i] = (byte) (64 - bitOffset);
                    m_MaskA[i] = maskValue << m_ShiftA[i];
                    m_MaskB[i] = maskValue >> m_ShiftB[i];
                }

                m_CommandIfChanged[i] = DynamicDemuxerCommand.Parse;
                m_CommandIfNotChanged[i] = DynamicDemuxerCommand.GoToCommand;
                m_GoToCommand[i] = i + 1;

                m_SourceType[i] = f.SourceType;
                m_DestinationType[i] = f.DestinationType;
                m_DestinationSlot[i] = f.DestinationSlot;

                Debug.Log(
                    $"field {i:d2} index={m_Index[i]:d2} shiftA={m_ShiftA[i]:d2} shiftB={m_ShiftB[i]:d2} maskA={m_MaskA[i]:x16} maskB={m_MaskB[i]:x16}");
            }

            unsafe
            {
                m_RawState = null;
            }

            m_RawStateSize = 0;

            floatResultsDestinationSlotArray = new NativeArray<uint>(fields.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            floatResultsValuesArray =
                new NativeArray<float>(fields.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            uintResultsDestinationSlotArray = new NativeArray<uint>(fields.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            uintResultsValuesArray = new NativeArray<uint>(fields.Length, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            floatResultsCount = 0;
            uintResultsCount = 0;
        }

        public unsafe void Run(void* rawState, int rawStateSize)
        {
            m_RawState = rawState;
            m_RawStateSize = rawStateSize;
            Execute();
        }

        public unsafe void Execute()
        {
            floatResultsCount = 0;
            uintResultsCount = 0;

            ulong* state = null;
            ulong* changed = null;
            m_PreDemuxer.Execute(m_RawState, m_RawStateSize, ref state, ref changed);

            var cmdIndex = 0;
            var run = true;

            while (run)
            {
                var index = m_Index[cmdIndex];
                var maskA = m_MaskA[cmdIndex];
                var maskB = m_MaskB[cmdIndex];
                var cmdIfChanged = m_CommandIfChanged[cmdIndex];
                var cmdIfNotChanged = m_CommandIfNotChanged[cmdIndex];

                var isChanged = ((changed[index] & maskA) | (changed[index + 1] & maskB)) != 0;

                switch (isChanged ? cmdIfChanged : cmdIfNotChanged)
                {
                    case DynamicDemuxerCommand.GoToCommand:
                    {
                        var goToCommand = m_GoToCommand[cmdIndex];
                        cmdIndex = goToCommand;
                        break;
                    }
                    case DynamicDemuxerCommand.Parse:
                    {
                        var goToCommand = m_GoToCommand[cmdIndex];
                        var shiftA = m_ShiftA[cmdIndex];
                        var shiftB = m_ShiftB[cmdIndex];
                        var srcType = m_SourceType[cmdIndex];
                        var dstType = m_DestinationType[cmdIndex];
                        var dstSlot = m_DestinationSlot[cmdIndex];

                        var rawData = ((state[index] & maskA) >> shiftA) + ((state[index + 1] & maskB) << shiftB);

                        switch (srcType)
                        {
                            case DynamicDemuxerSourceType.UnsignedBits:
                            {
                                var data = rawData;
                                switch (dstType)
                                {
                                    case DynamicDemuxerDestinationType.Float:
                                        floatResultsDestinationSlotArray[floatResultsCount] = dstSlot;
                                        floatResultsValuesArray[floatResultsCount++] = (float) data;
                                        break;
                                    case DynamicDemuxerDestinationType.UInt:
                                        uintResultsDestinationSlotArray[uintResultsCount] = dstSlot;
                                        uintResultsValuesArray[uintResultsCount++] = (uint) data;
                                        break;
                                }

                                break;
                            }
                            case DynamicDemuxerSourceType.Float32:
                            {
                                var data = *(float*) &rawData;
                                switch (dstType)
                                {
                                    case DynamicDemuxerDestinationType.Float:
                                        floatResultsDestinationSlotArray[floatResultsCount] = dstSlot;
                                        floatResultsValuesArray[floatResultsCount++] = (float) data;
                                        break;
                                    case DynamicDemuxerDestinationType.UInt:
                                        uintResultsDestinationSlotArray[uintResultsCount] = dstSlot;
                                        uintResultsValuesArray[uintResultsCount++] = (uint) data;
                                        break;
                                }

                                break;
                            }
                        }

                        cmdIndex = goToCommand;

                        break;
                    }
                    case DynamicDemuxerCommand.Break:
                        run = false;
                        break;
                }
            }
        }

        public void Dispose()
        {
            m_PreDemuxer.Dispose();
            m_Index.Dispose();
            m_MaskA.Dispose();
            m_MaskB.Dispose();
            m_CommandIfChanged.Dispose();
            m_CommandIfNotChanged.Dispose();
            m_GoToCommand.Dispose();
            m_ShiftA.Dispose();
            m_ShiftB.Dispose();
            m_SourceType.Dispose();
            m_DestinationType.Dispose();
            m_DestinationSlot.Dispose();
            floatResultsDestinationSlotArray.Dispose();
            floatResultsValuesArray.Dispose();
            uintResultsDestinationSlotArray.Dispose();
            uintResultsValuesArray.Dispose();
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct StaticMouseDemuxer : IJob, IDisposable
    {
        [ReadOnly]
        private NativeArray<NativeMouseState2> m_States;
        private int m_StatesCount;

        public struct ValueTimeline : IDisposable
        {
            public NativeArray<float> Value;
            public NativeArray<ulong> Timestamp;
            public int Count;

            public ValueTimeline(int size, Allocator allocator)
            {
                Value = new NativeArray<float>(size, allocator, NativeArrayOptions.UninitializedMemory);
                Timestamp = new NativeArray<ulong>(size, allocator, NativeArrayOptions.UninitializedMemory);
                Count = 0;
            }

            public void Dispose()
            {
                Value.Dispose();
                Timestamp.Dispose();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Write(float value, ulong timestamp)
            {
                Value[Count] = value;
                Timestamp[Count] = timestamp;
                Count++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                Count = 0;
            }
        }

        public struct SoA : IDisposable
        {
            public ValueTimeline posX;
            public ValueTimeline posY;
            public ValueTimeline dltX;
            public ValueTimeline dltY;
            public ValueTimeline scrlX;
            public ValueTimeline scrlY;
            public ValueTimeline button0;
            public ValueTimeline button1;
            public ValueTimeline button2;
            public ValueTimeline button3;
            public ValueTimeline button4;

            public SoA(int size, Allocator allocator)
            {
                posX =    new ValueTimeline(size, allocator);
                posY =    new ValueTimeline(size, allocator);
                dltX =    new ValueTimeline(size, allocator);
                dltY =    new ValueTimeline(size, allocator);
                scrlX =   new ValueTimeline(size, allocator);
                scrlY =   new ValueTimeline(size, allocator);
                button0 = new ValueTimeline(size, allocator);
                button1 = new ValueTimeline(size, allocator);
                button2 = new ValueTimeline(size, allocator);
                button3 = new ValueTimeline(size, allocator);
                button4 = new ValueTimeline(size, allocator);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                posX.Reset();
                posY.Reset();
                dltX.Reset();
                dltY.Reset();
                scrlX.Reset();
                scrlY.Reset();
                button0.Reset();
                button1.Reset();
                button2.Reset();
                button3.Reset();
                button4.Reset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                posX.Dispose();
                posY.Dispose();
                dltX.Dispose();
                dltY.Dispose();
                scrlX.Dispose();
                scrlY.Dispose();
                button0.Dispose();
                button1.Dispose();
                button2.Dispose();
                button3.Dispose();
                button4.Dispose();
            }
        }

        public SoA soa;

        public StaticMouseDemuxer(int maxStates)
        {
            m_States = default;
            m_StatesCount = 0;

            soa = new SoA(maxStates, Allocator.Persistent);
        }

        public unsafe void Run(NativeArray<NativeMouseState2> states, int count)
        {
            m_States = states;
            m_StatesCount = count;
            Execute();
        }

        public unsafe void Execute()
        {
            var directSoa = new SoA(m_StatesCount, Allocator.Temp);

            var rawPtr = (NativeMouseState2*)(m_States.GetUnsafeReadOnlyPtr());
            var dposX = (float*)directSoa.posX.Value.GetUnsafePtr();
            var dposY = (float*)directSoa.posY.Value.GetUnsafePtr();
            var ddltX = (float*)directSoa.dltX.Value.GetUnsafePtr();
            var ddltY = (float*)directSoa.dltY.Value.GetUnsafePtr();
            var dscrlX = (float*)directSoa.scrlX.Value.GetUnsafePtr();
            var dscrlY = (float*)directSoa.scrlY.Value.GetUnsafePtr();
            var dbutton0 = (uint*)directSoa.button0.Value.GetUnsafePtr();
            var dbutton1 = (uint*)directSoa.button1.Value.GetUnsafePtr();
            var dbutton2 = (uint*)directSoa.button2.Value.GetUnsafePtr();
            var dbutton3 = (uint*)directSoa.button3.Value.GetUnsafePtr();
            var dbutton4 = (uint*)directSoa.button4.Value.GetUnsafePtr();

            for (var i = 0; i < m_StatesCount; ++i)
            {
                var s = rawPtr[i];
                dposX[i] = s.Position.x;
                dposY[i] = s.Position.y;
                ddltX[i] = s.Delta.x;
                ddltY[i] = s.Delta.y;
                dscrlX[i] = s.Scroll.x;
                dscrlY[i] = s.Scroll.y;
                dbutton0[i] = (uint)((s.Buttons >> 0) & 0x1);
                dbutton1[i] = (uint)((s.Buttons >> 1) & 0x1);
                dbutton2[i] = (uint)((s.Buttons >> 2) & 0x1);
                dbutton3[i] = (uint)((s.Buttons >> 3) & 0x1);
                dbutton4[i] = (uint)((s.Buttons >> 4) & 0x1);
            }

            soa.Reset();

            var uposX = (uint*)directSoa.posX.Value.GetUnsafePtr();
            var uposY = (uint*)directSoa.posY.Value.GetUnsafePtr();
            var udltX = (uint*)directSoa.dltX.Value.GetUnsafePtr();
            var udltY = (uint*)directSoa.dltY.Value.GetUnsafePtr();
            var uscrlX = (uint*)directSoa.scrlX.Value.GetUnsafePtr();
            var uscrlY = (uint*)directSoa.scrlY.Value.GetUnsafePtr();
            var ubutton0 = (uint*)directSoa.button0.Value.GetUnsafePtr();
            var ubutton1 = (uint*)directSoa.button1.Value.GetUnsafePtr();
            var ubutton2 = (uint*)directSoa.button2.Value.GetUnsafePtr();
            var ubutton3 = (uint*)directSoa.button3.Value.GetUnsafePtr();
            var ubutton4 = (uint*)directSoa.button4.Value.GetUnsafePtr();
            
            for (var i = 0; i < m_StatesCount; ++i)
                if(i == 0 || (uposX[i] != uposX[i - 1]))
                    soa.posX.Write(dposX[i], 0);
            for (var i = 0; i < m_StatesCount; ++i)
                if(i == 0 || (uposY[i] != uposY[i - 1]))
                    soa.posY.Write(dposY[i], 0);
            for (var i = 0; i < m_StatesCount; ++i)
                if(i == 0 || (udltX[i] != udltX[i - 1]))
                    soa.dltX.Write(ddltX[i], 0);
            for (var i = 0; i < m_StatesCount; ++i)
                if(i == 0 || (udltY[i] != udltY[i - 1]))
                    soa.dltY.Write(ddltY[i], 0);

            directSoa.Dispose();
        }


        public void Dispose()
        {
            soa.Dispose();
        }

        // allow padding in the end, so the size is != 30
        [StructLayout(LayoutKind.Explicit)]
        public struct NativeMouseState2
        {
            [FieldOffset(0)] public Vector2 Position;
            [FieldOffset(8)] public Vector2 Delta;
            [FieldOffset(16)] public Vector2 Scroll;
            [FieldOffset(24)] public ushort Buttons;
            [FieldOffset(26)] private ushort _displayIndex;
            [FieldOffset(28)] public ushort ClickCount;
        }
    }
}