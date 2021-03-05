using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.DmytroRnD;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

class DmytroTests
{
    [Test]
    [Category("Dmytro")]
    public unsafe void Dmytro_ProgrammableDemuxer_Performance()
    {
        const int statesCount = 1000000;
        const int freq = 1000;

        var managedStates = new NativeMouseState[statesCount];
        for (var i = 0; i < managedStates.Length; ++i)
        {
            managedStates[i].Position = new Vector2(Random.Range(0.0f, 500.0f), Random.Range(0.0f, 500.0f));
            managedStates[i].Delta = new Vector2(Random.Range(-50.0f, 50.0f), Random.Range(-50.0f, 50.0f));

            if (i % (freq / 2) == 0) // 2 times a second we do a scroll
                managedStates[i].Scroll = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));

            if (i % (freq / 10) == 0) // 10 times a second we do press a random button
                managedStates[i].Buttons = (ushort) (1 << Random.Range(0, 5));
        }

        var rawStates = new NativeArray<StaticMouseDemuxer.NativeMouseState2>(statesCount, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory);
        for (var i = 0; i < statesCount; ++i)
            Marshal.StructureToPtr(managedStates[i],
                (IntPtr) ((StaticMouseDemuxer.NativeMouseState2*) rawStates.GetUnsafePtr() + i), false);

        /*
        var dynamicDemuxer = new DynamicDemuxer(sizeof(NativeMouseState), new []
        {
            new DynamicDemuxerField
            {
                BitOffset = 0,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 0,
                OftenChangingFieldHint = true
            },
            new DynamicDemuxerField
            {
                BitOffset = 32,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 1,
                OftenChangingFieldHint = true
            },
            new DynamicDemuxerField
            {
                BitOffset = 64,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 2,
                OftenChangingFieldHint = true
            },
            new DynamicDemuxerField
            {
                BitOffset = 96,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 3,
                OftenChangingFieldHint = true
            },
            new DynamicDemuxerField // scroll X
            {
                BitOffset = 128,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 4,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField // scroll Y
            {
                BitOffset = 160,
                BitSize = 32,
                SourceType = DynamicDemuxerSourceType.Float32,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 5,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField
            {
                BitOffset = 24 * 8 + 0,
                BitSize = 1,
                SourceType = DynamicDemuxerSourceType.UnsignedBits,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 6,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField
            {
                BitOffset = 24 * 8 + 1,
                BitSize = 1,
                SourceType = DynamicDemuxerSourceType.UnsignedBits,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 7,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField
            {
                BitOffset = 24 * 8 + 2,
                BitSize = 1,
                SourceType = DynamicDemuxerSourceType.UnsignedBits,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 8,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField
            {
                BitOffset = 24 * 8 + 3,
                BitSize = 1,
                SourceType = DynamicDemuxerSourceType.UnsignedBits,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 9,
                OftenChangingFieldHint = false
            },
            new DynamicDemuxerField
            {
                BitOffset = 24 * 8 + 4,
                BitSize = 1,
                SourceType = DynamicDemuxerSourceType.UnsignedBits,
                DestinationType = DynamicDemuxerDestinationType.Float,
                DestinationSlot = 10,
                OftenChangingFieldHint = false
            }
        });

        Profiler.BeginSample("DynamicDemuxing");

        for (var i = 0; i < statesCount; ++i)
            dynamicDemuxer.Run( (NativeMouseState*)rawPtr + i, sizeof(NativeMouseState));

        Profiler.EndSample();

        dynamicDemuxer.Dispose();
        */

        var staticDemuxer = new StaticMouseDemuxer(statesCount);

        for (var i = 0; i < 5; i++)
        {
            Profiler.BeginSample($"StaticDemuxing {i}");

            //for (var i = 0; i < statesCount; ++i)
            staticDemuxer.Run(rawStates, statesCount);

            Profiler.EndSample();
        }

        Assert.That(Math.Abs(staticDemuxer.soa.posX.Value[10] - managedStates[10].Position.x) <= float.Epsilon);
        
        staticDemuxer.Dispose();

        rawStates.Dispose();
    }
}