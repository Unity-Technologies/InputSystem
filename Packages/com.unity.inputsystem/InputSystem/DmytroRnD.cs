using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
//using UnityEditorInternal;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngineInternal.Input;
using UnityEngine.InputSystem.DataPipeline;
using UnityEngine.InputSystem.DataPipeline.Collections;
using UnityEngine.InputSystem.DataPipeline.Demux;
using UnityEngine.InputSystem.DataPipeline.Demux.Dynamic;
using UnityEngine.InputSystem.DataPipeline.Merger;
using UnityEngine.InputSystem.DataPipeline.Processor;
using UnityEngine.InputSystem.DataPipeline.SlidingWindow;
using UnityEngine.InputSystem.DataPipeline.TypeConversion;
using UnityEngine.InputSystem.XInput.LowLevel;

namespace UnityEngine.InputSystem.DmytroRnD
{
    public static class Core
    {
        internal static IngressPipeline s_IngressPipeline;
        internal static bool s_IsInitialized;
        internal static float TestOutputVar;
        internal static string[] s_ValueIndexToName;

        public static Action<Vector2> s_ValueCallback;

        internal static void NativeSetup()
        {
            if (s_IsInitialized)
                return;
            s_IngressPipeline = CreateIngressPipeline();
            s_IsInitialized = true;
        }

        internal static void NativeClear()
        {
            if (!s_IsInitialized)
                return;
            s_IngressPipeline.Dispose();
            s_IsInitialized = false;
        }

        internal static void NativeBeforeUpdate(NativeInputUpdateType updateType)
        {
        }

        internal static unsafe void NativeUpdate(NativeInputUpdateType updateType, NativeInputEventBuffer* buffer)
        {
            // limit blast radius during development 
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                return;

            if (!s_IsInitialized)
                NativeSetup();

            Profiler.BeginSample("DmytroRnD.NativeUpdate");

            s_IngressPipeline.eventBuffer = buffer;

            if (EditorApplication.isPlaying)
                s_IngressPipeline.Run();
            else
                s_IngressPipeline.Execute();

            // var slice = s_IngressPipeline.demuxer.results.ToNativeSlice();
            // foreach (var data in slice)
            // //if (slice.Length > 0)
            // {
            //     //var data = slice[0];
            //     Debug.Log($"{data.valueStepfunctionIndex} / {data.timestamp} / {data.value}");
            //     TestOutputVar = data.value;
            // }

            //PrintStepFunctionValues(36);
            //PrintStepFunctionValues(37);

            //var allValues = s_IngressPipeline.dataset.values.ToNativeSlice();
            //TestOutputVar = allValues.Length > 0 ? allValues[0] : 0;

            Profiler.BeginSample("Callbacks");

            if (s_ValueCallback != null)
            {
                var values = s_IngressPipeline.dataset.values.ToNativeSlice();

                var offset1 = s_IngressPipeline.dataset.valueAxisIndexToOffset[37];
                var offset2 = s_IngressPipeline.dataset.valueAxisIndexToOffset[38];
                var length = s_IngressPipeline.dataset.timestampAxisIndexToLength[37];

                for (var i = 0; i < length; ++i)
                    s_ValueCallback(new Vector2(values[offset1 + i], values[offset2 + i]));
            }

            Profiler.EndSample();

            Profiler.EndSample();

            DebuggerWindow.RefreshCurrent();
        }

        // private static void PrintStepFunctionValues(int valuesAxisIndex)
        // {
        //     var timestampsAxisIndex =
        //         s_IngressPipeline.dataset.valueAxisIndexToTimestampIndex[valuesAxisIndex];
        //
        //     var offset = s_IngressPipeline.dataset.valueAxisIndexToOffset[valuesAxisIndex];
        //     var length = s_IngressPipeline.dataset.timestampAxisIndexToLength[timestampsAxisIndex];
        //
        //     var values = s_IngressPipeline.dataset.values.ToNativeSlice().Slice(offset, length);
        //     if (values.Length == 0)
        //         return;
        //     var valuesStr = string.Join(", ", values);
        //     Debug.Log($"{valuesAxisIndex} => {valuesStr}");
        // }

        private static IngressPipeline CreateIngressPipeline()
        {
            const int mouseStepFunctionsOffset = 0;
            const int gamepadStepFunctionsOffset = 11;

            // All of this should be baked by magic code that takes current layouts and figure out how to compute them
            var demuxer = new Demuxer
            {
                deviceDescriptions = new NativeArray<DeviceDescription>(new DeviceDescription[]
                {
                    // new DeviceDescription
                    // {
                    //     deviceId = 1,
                    //     stateEventFourCC = KeyboardState.Format,
                    //     dataFieldsOffset = 0,
                    //     dataFieldsCount = 110,
                    //     stateOffsetInULongs = 0,
                    //     stateSizeInULongs = 2,
                    //     validStateIndex = 0,
                    //     timestampStepFunctionOffset = 0,
                    //     valueStepFunctionOffset = 0
                    // },
                    new DeviceDescription
                    {
                        deviceId = 2,
                        stateEventFourCC = MouseState.Format,
                        demuxerType = DemuxerType.StaticMouse,
                        dataFieldsOffset = 0,
                        dataFieldsCount = 0, // 11,
                        stateOffsetInULongs = 2,
                        stateSizeInULongs = 5, // TODO should be 4, fix DataField.From
                        validStateIndex = 1,
                        valueStepFunctionOffset = mouseStepFunctionsOffset
                    },
                    new DeviceDescription
                    {
                        deviceId = 21,
                        stateEventFourCC = new XInputControllerWindowsState().format,
                        demuxerType = DemuxerType.Dynamic,
                        dataFieldsOffset = 0, //11,
                        dataFieldsCount = 20, //20,
                        stateOffsetInULongs = 7,
                        stateSizeInULongs = 3, // TODO should be 2, fix DataField.From
                        validStateIndex = 2,
                        valueStepFunctionOffset = gamepadStepFunctionsOffset
                    }
                }, Allocator.Persistent),
                dataFields = new NativeArray<DemuxerDataField>(new DemuxerDataField[]
                {
                    // @formatter:off

// mouse
// DataField.From(0, 0 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // posX
// DataField.From(1, 4 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // posY
// DataField.From(2, 8 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // deltaX
// DataField.From(3, 12 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // deltaY
// DataField.From(4, 16 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // scrollX
// DataField.From(5, 20 * 8, 4 * 8, 4, SourceDataType.Float32, DestinationDataType.Float32), // scrollY
// DataField.From(6, 24 * 8 + 0, 1, 4, SourceDataType.UnsignedBits, DestinationDataType.Float32), // left
// DataField.From(7, 24 * 8 + 1, 1, 4, SourceDataType.UnsignedBits, DestinationDataType.Float32), // right
// DataField.From(8, 24 * 8 + 2, 1, 4, SourceDataType.UnsignedBits, DestinationDataType.Float32), // middle
// DataField.From(9, 24 * 8 + 3, 1, 4, SourceDataType.UnsignedBits, DestinationDataType.Float32), // forward
// DataField.From(10, 24 * 8 + 4, 1, 4, SourceDataType.UnsignedBits, DestinationDataType.Float32), // back

// xinput windows
DemuxerDataField.From(0, 0, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // DPadUp
DemuxerDataField.From(1, 1, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // DPadDown
DemuxerDataField.From(2, 2, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // DPadLeft
DemuxerDataField.From(3, 3, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // DPadRight
DemuxerDataField.From(4, 4, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // Start
DemuxerDataField.From(5, 5, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // Select
DemuxerDataField.From(6, 6, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // LeftThumbstickPress
DemuxerDataField.From(7, 7, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // RightThumbstickPress
DemuxerDataField.From(8, 8, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // LeftShoulder
DemuxerDataField.From(9, 9, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // RightShoulder
DemuxerDataField.From(10, 12, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // A
DemuxerDataField.From(11, 13, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // B
DemuxerDataField.From(12, 14, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // X
DemuxerDataField.From(13, 15, 1, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // Y
DemuxerDataField.From(14, 2 * 8, 8, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // leftTrigger
DemuxerDataField.From(15, 3 * 8, 8, 2, SourceDataType.UnsignedBits, DestinationDataType.Float32), // rightTrigger
DemuxerDataField.From(16, 4 * 8, 16, 2, SourceDataType.TwosComplementSignedBits, DestinationDataType.Float32), // leftStickX
DemuxerDataField.From(17, 6 * 8, 16, 2, SourceDataType.TwosComplementSignedBits, DestinationDataType.Float32), // leftStickY
DemuxerDataField.From(18, 8 * 8, 16, 2, SourceDataType.TwosComplementSignedBits, DestinationDataType.Float32), // rightStickX
DemuxerDataField.From(19, 10 * 8, 16, 2, SourceDataType.TwosComplementSignedBits, DestinationDataType.Float32), // rightStickY
                    
                    // @formatter:on
                }, Allocator.Persistent),
                deviceStates =
                    new NativeArray<ulong>(10, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                deviceHasValidState = new NativeArray<bool>(3, Allocator.Persistent),
                changedBits = new NativeArray<ulong>(5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                results = new ResizableNativeArray<DemuxedData>(16 * 1024)
            };

            const int valueStepFunctionsCount = 31 + 10;
            const int opaqueValueStepFunctionsCount = 1;

            const int timestampCount = valueStepFunctionsCount + opaqueValueStepFunctionsCount;

            var dataset = new Dataset
            {
                timestamps = new ResizableNativeArray<ulong>(16 * 1024),
                timestampAxisIndexToLength = new NativeArray<int>(timestampCount, Allocator.Persistent),
                timestampAxisIndexToMaxLength = new NativeArray<int>(timestampCount, Allocator.Persistent),
                timestampAxisIndexToOffset = new NativeArray<int>(timestampCount, Allocator.Persistent),
                timestampAxisIndexToPreviousRunValue = new NativeArray<ulong>(timestampCount, Allocator.Persistent),

                values = new ResizableNativeArray<float>(16 * 1024),
                valueAxisIndexToOffset = new NativeArray<int>(valueStepFunctionsCount, Allocator.Persistent),
                valueAxisIndexToTimestampIndex = new NativeArray<int>(valueStepFunctionsCount, Allocator.Persistent),
                valueAxisIndexToPreviousRunValue =
                    new NativeArray<float>(valueStepFunctionsCount, Allocator.Persistent),

                opaqueValues = new ResizableNativeArray<byte>(1),
                opaqueValueAxisIndexToOffset =
                    new NativeArray<int>(opaqueValueStepFunctionsCount, Allocator.Persistent),
                opaqueValueIndexToTimestampIndex =
                    new NativeArray<int>(opaqueValueStepFunctionsCount, Allocator.Persistent)
            };

            // TODO this should really be somewhere in the pipeline
            for (var i = 0; i < dataset.valueAxisIndexToOffset.Length; ++i)
                dataset.valueAxisIndexToPreviousRunValue[i] = float.NaN;

            // ingress
            for (var i = 0; i < 31; ++i)
                dataset.valueAxisIndexToTimestampIndex[i] = i;

            dataset.valueAxisIndexToTimestampIndex[31] = 31;
            dataset.valueAxisIndexToTimestampIndex[32] = 32;
            dataset.valueAxisIndexToTimestampIndex[33] = 33;
            dataset.valueAxisIndexToTimestampIndex[34] = 33;
            dataset.valueAxisIndexToTimestampIndex[35] = 35;
            dataset.valueAxisIndexToTimestampIndex[36] = 35;
            dataset.valueAxisIndexToTimestampIndex[37] = 37;
            dataset.valueAxisIndexToTimestampIndex[38] = 37;
            dataset.valueAxisIndexToTimestampIndex[39] = 39;
            dataset.valueAxisIndexToTimestampIndex[40] = 39;

            s_ValueIndexToName = new string[]
            {
                /* 0 */ "mouse.posX",
                /* 1 */ "mouse.posY",
                /* 2 */ "mouse.deltaX",
                /* 3 */ "mouse.deltaY",
                /* 4 */ "mouse.scrollX",
                /* 5 */ "mouse.scrollY",
                /* 6 */ "mouse.left",
                /* 7 */ "mouse.right",
                /* 8 */ "mouse.middle",
                /* 9 */ "mouse.forward",
                /* 10 */ "mouse.back",
                /* 11 */ "xinput.DPadUp",
                /* 12 */ "xinput.DPadDown",
                /* 13 */ "xinput.DPadLeft",
                /* 14 */ "xinput.DPadRight",
                /* 15 */ "xinput.Start",
                /* 16 */ "xinput.Select",
                /* 17 */ "xinput.LeftThumbstickPress",
                /* 18 */ "xinput.RightThumbstickPress",
                /* 19 */ "xinput.LeftShoulder",
                /* 20 */ "xinput.RightShoulder",
                /* 21 */ "xinput.A",
                /* 22 */ "xinput.B",
                /* 23 */ "xinput.X",
                /* 24 */ "xinput.Y",
                /* 25 */ "xinput.leftTrigger",
                /* 26 */ "xinput.rightTrigger",
                /* 27 */ "xinput.leftStickX",
                /* 28 */ "xinput.leftStickY",
                /* 29 */ "xinput.rightStickX",
                /* 30 */ "xinput.rightStickY",
                /* 31 */ "mouse.deltaXAcc",
                /* 32 */ "mouse.deltaYAcc",
                /* 33 */ "xinput.stickLeftXPaired",
                /* 34 */ "xinput.stickLeftYPaired",
                /* 35 */ "xinput.stickRightXPaired",
                /* 36 */ "xinput.stickRightYPaired",
                /* 37 */ "mouse.posXPaired",
                /* 38 */ "mouse.posYPaired",
                /* 39 */ "xinput.stickX",
                /* 40 */ "xinput.stickY"
            };

            Debug.Assert(s_ValueIndexToName.Length == dataset.valueAxisIndexToOffset.Length);

            var datapipeline = new DataPipeline.DataPipeline
            {
                enumsToFloatsLut = new NativeArray<float>(0, Allocator.Persistent),
                enumsToFloats = new NativeArray<EnumToFloat>(new EnumToFloat[] { }, Allocator.Persistent),
                two1DsTo2Ds = new NativeArray<Two1DsTo2D>(new Two1DsTo2D[]
                {
                    new Two1DsTo2D
                    {
                        srcX = new StepFunction1D
                        {
                            valuesX = 27,
                        },
                        srcY = new StepFunction1D
                        {
                            valuesX = 28,
                        },
                        dst = new StepFunction2D
                        {
                            valuesX = 33,
                            valuesY = 34
                        }
                    },
                    new Two1DsTo2D
                    {
                        srcX = new StepFunction1D
                        {
                            valuesX = 29,
                        },
                        srcY = new StepFunction1D
                        {
                            valuesX = 30,
                        },
                        dst = new StepFunction2D
                        {
                            valuesX = 35,
                            valuesY = 36
                        }
                    },
                    new Two1DsTo2D
                    {
                        srcX = new StepFunction1D
                        {
                            valuesX = 0,
                        },
                        srcY = new StepFunction1D
                        {
                            valuesX = 1,
                        },
                        dst = new StepFunction2D
                        {
                            valuesX = 37,
                            valuesY = 38
                        }
                    }
                }, Allocator.Persistent),
                vec2sToMagnitudes = new NativeArray<Vec2ToMagnitude>(new Vec2ToMagnitude[] { }, Allocator.Persistent),
                process1Ds = new NativeArray<Processor1D>(new Processor1D[] { }, Allocator.Persistent),
                process2Ds = new NativeArray<Processor2D>(new Processor2D[]
                {
                    // new Processor2D
                    // {
                    //     src = new StepFunction2D
                    //     {
                    //         valuesX = 33,
                    //         valuesY = 34
                    //     },
                    //     dst = new StepFunction2D
                    //     {
                    //         valuesX = 35,
                    //         valuesY = 36
                    //     },
                    //     normalize = 1.0f,
                    //     minMagnitude = 0.0f,
                    //     maxMagnitude = 1.0f,
                    //     clamp = 0.0f,
                    //     clampNormalize = 0.0f,
                    //     scale = new Vector2(1.0f, 1.0f),
                    //     offset = new Vector2(0.0f, 0.0f)
                    // }
                }, Allocator.Persistent),
                process3Ds = new NativeArray<Processor3D>(new Processor3D[] { }, Allocator.Persistent),
                accumulate1Ds = new NativeArray<Accumulate1D>(new Accumulate1D[]
                {
                    new Accumulate1D
                    {
                        src = new StepFunction1D
                        {
                            valuesX = 2
                        },
                        dst = new StepFunction1D
                        {
                            valuesX = 31
                        }
                    },
                    new Accumulate1D
                    {
                        src = new StepFunction1D
                        {
                            valuesX = 3
                        },
                        dst = new StepFunction1D
                        {
                            valuesX = 32
                        }
                    },
                }, Allocator.Persistent),
                latest1Ds = new NativeArray<Latest1D>(new Latest1D[] { }, Allocator.Persistent),
                maxValue1Ds = new NativeArray<MaxValue1D>(new MaxValue1D[]
                {
                    // new MaxValue1D
                    // {
                    //     src1 = new StepFunction1D
                    //     {
                    //         valuesX = 0
                    //     },
                    //     src2 = new StepFunction1D
                    //     {
                    //         valuesX = 1
                    //     },
                    //     dst = new StepFunction1D
                    //     {
                    //         valuesX = 39,
                    //     }
                    // }
                }, Allocator.Persistent),
                maxValue2Ds = new NativeArray<MaxValue2D>(new MaxValue2D[]
                {
                    new MaxValue2D
                    {
                        src1 = new StepFunction2D
                        {
                            valuesX = 33,
                            valuesY = 34
                        },
                        src2 = new StepFunction2D
                        {
                            valuesX = 35,
                            valuesY = 36
                        },
                        dst = new StepFunction2D
                        {
                            valuesX = 39,
                            valuesY = 40
                        }
                    }
                }, Allocator.Persistent)
            };

            return new IngressPipeline
            {
                demuxer = demuxer,
                dataset = dataset,
                dataPipeline = datapipeline,
                eventBuffer = null
            };
        }

        internal static unsafe void NativeDeviceDiscovered(int deviceId, string deviceDescriptorJson)
        {
#if UNITY_EDITOR
            SurviveDomainReload.BootstrapWIP();
            //SurviveDomainReload.Preserve(deviceId, deviceDescriptorJson);
#endif
            // TODO
            return;

            //var deviceDescriptor = JsonUtility.FromJson<NativeDeviceDescriptor>(deviceDescriptorJson);
            //Debug.Log($"DRND: device discovered {deviceId} -> {deviceDescriptorJson}");
        }
    }
}

// // keyboard
// DataField.From((int) Key.Space, (int) Key.Space, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Enter, (int) Key.Enter, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Tab, (int) Key.Tab, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Backquote, (int) Key.Backquote, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Quote, (int) Key.Quote, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Semicolon, (int) Key.Semicolon, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Comma, (int) Key.Comma, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Period, (int) Key.Period, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Slash, (int) Key.Slash, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Backslash, (int) Key.Backslash, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftBracket, (int) Key.LeftBracket, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightBracket, (int) Key.RightBracket, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Minus, (int) Key.Minus, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Equals, (int) Key.Equals, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.A, (int) Key.A, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.B, (int) Key.B, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.C, (int) Key.C, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.D, (int) Key.D, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.E, (int) Key.E, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F, (int) Key.F, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.G, (int) Key.G, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.H, (int) Key.H, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.I, (int) Key.I, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.J, (int) Key.J, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.K, (int) Key.K, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.L, (int) Key.L, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.M, (int) Key.M, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.N, (int) Key.N, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.O, (int) Key.O, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.P, (int) Key.P, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Q, (int) Key.Q, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.R, (int) Key.R, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.S, (int) Key.S, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.T, (int) Key.T, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.U, (int) Key.U, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.V, (int) Key.V, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.W, (int) Key.W, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.X, (int) Key.X, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Y, (int) Key.Y, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Z, (int) Key.Z, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit1, (int) Key.Digit1, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit2, (int) Key.Digit2, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit3, (int) Key.Digit3, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit4, (int) Key.Digit4, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit5, (int) Key.Digit5, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit6, (int) Key.Digit6, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit7, (int) Key.Digit7, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit8, (int) Key.Digit8, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit9, (int) Key.Digit9, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Digit0, (int) Key.Digit0, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftShift, (int) Key.LeftShift, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightShift, (int) Key.RightShift, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftAlt, (int) Key.LeftAlt, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightAlt, (int) Key.RightAlt, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftCtrl, (int) Key.LeftCtrl, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightCtrl, (int) Key.RightCtrl, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftMeta, (int) Key.LeftMeta, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightMeta, (int) Key.RightMeta, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.ContextMenu, (int) Key.ContextMenu, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Escape, (int) Key.Escape, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.LeftArrow, (int) Key.LeftArrow, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.RightArrow, (int) Key.RightArrow, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.UpArrow, (int) Key.UpArrow, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.DownArrow, (int) Key.DownArrow, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Backspace, (int) Key.Backspace, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.PageDown, (int) Key.PageDown, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.PageUp, (int) Key.PageUp, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Home, (int) Key.Home, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.End, (int) Key.End, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Insert, (int) Key.Insert, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Delete, (int) Key.Delete, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.CapsLock, (int) Key.CapsLock, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumLock, (int) Key.NumLock, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.PrintScreen, (int) Key.PrintScreen, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.ScrollLock, (int) Key.ScrollLock, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Pause, (int) Key.Pause, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadEnter, (int) Key.NumpadEnter, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadDivide, (int) Key.NumpadDivide, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadMultiply, (int) Key.NumpadMultiply, 1, 2,
//     SourceDataType.UnsignedBits, DestinationDataType.Float32),
// DataField.From((int) Key.NumpadPlus, (int) Key.NumpadPlus, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadMinus, (int) Key.NumpadMinus, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadPeriod, (int) Key.NumpadPeriod, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.NumpadEquals, (int) Key.NumpadEquals, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad0, (int) Key.Numpad0, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad1, (int) Key.Numpad1, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad2, (int) Key.Numpad2, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad3, (int) Key.Numpad3, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad4, (int) Key.Numpad4, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad5, (int) Key.Numpad5, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad6, (int) Key.Numpad6, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad7, (int) Key.Numpad7, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad8, (int) Key.Numpad8, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.Numpad9, (int) Key.Numpad9, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F1, (int) Key.F1, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F2, (int) Key.F2, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F3, (int) Key.F3, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F4, (int) Key.F4, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F5, (int) Key.F5, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F6, (int) Key.F6, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F7, (int) Key.F7, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F8, (int) Key.F8, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F9, (int) Key.F9, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F10, (int) Key.F10, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F11, (int) Key.F11, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.F12, (int) Key.F12, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.OEM1, (int) Key.OEM1, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.OEM2, (int) Key.OEM2, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.OEM3, (int) Key.OEM3, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.OEM4, (int) Key.OEM4, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),
// DataField.From((int) Key.OEM5, (int) Key.OEM5, 1, 2, SourceDataType.UnsignedBits,
//     DestinationDataType.Float32),