using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

//is all input state kept on components? (no system internal memory?)

namespace Unity.Input
{
    // - Input IDs are bit offsets at the same time
    // - There's no synthetic controls or any other sharing of state; every control must have its own state
    // - Input values have very specific semantics.
    // - Structs identify memory formats
    //   * The struct names are used as long-form identifiers
    //   * Short-form, numeric identifiers are auto-generated via CRC32
    // - Memory layouts in the DOTS structs are allowed to be different from the memory layouts used in UnityEngine.InputSystem


    public struct InputDescriptor
    {
        public string Path;
        public uint Id;
        public uint Format;
    }

    //have some kind of identifier that reveals the source of input? (i.e. where the memory initially came from?) if so, what do combiners do?

    //most of the structs here will be auto-generated...

    public struct PS4ControllerHidEvent : IInputData
    {
        public enum Id : uint
        {
            LeftStickX = 1 * 8,
            LeftStickY = 2 * 8,
            RightStickX = 3 * 8,
            RightStickY = 4 * 8,

            Square = 5 * 8 + 4,
            Cross = 5 * 8 + 5,
            Circle = 5 * 8 + 6,
            Triangle = 5 * 8 + 7,
        }

        //have this??
        public enum SizeOf : uint
        {
            LeftStickX = 8,
        }

        public byte ReportId;
        public byte LeftStickX;
        public byte LeftStickY;
        public byte RightStickX;
        public byte RightStickY;
        public byte Buttons1;
        public byte Buttons2;
        public byte Buttons3;
        public byte LeftTrigger;
        public byte RightTrigger;

        public static uint Format => CRC32.crc32("PS4ControllerHidEvent");

        public static InputDescriptor[] Descriptors = new[]
        {
            new InputDescriptor { Path = "square", Id = (uint)Id.Square, /* ... */ }
        };

        public DOTSInput.InputPipeline InputPipelineParts
        {
            get
            {
                var structMappings = new NativeArray<DOTSInput.InputStructMapping>(1, Allocator.Persistent)
                {
                    [0] = new DOTSInput.InputStructMapping
                    {
                        InputFormat = CRC32.crc32("PS4ControllerHidEvent"),
                        OutputFormat = CRC32.crc32("GamepadInput"),
                        InputSizeInBytes = (uint)UnsafeUtility.SizeOf<PS4ControllerHidEvent>(),
                        OutputSizeInBytes = (uint)UnsafeUtility.SizeOf<GamepadInput>(),
                        TransformStartIndex = 0,
                        TransformCount = 8,
                    }
                };

                var transforms = new NativeArray<DOTSInput.InputTransform>(8, Allocator.Persistent)
                {
                    [0] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.ByteToAxis),
                        InputId1 = (uint)Id.LeftStickX,
                        OutputId = (uint)GamepadInput.Id.LeftStickX,
                    },
                    [1] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.ByteToAxis),
                        InputId1 = (uint)Id.LeftStickY,
                        OutputId = (uint)GamepadInput.Id.LeftStickY,
                    },
                    [2] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.ByteToAxis),
                        InputId1 = (uint)Id.RightStickX,
                        OutputId = (uint)GamepadInput.Id.RightStickX,
                    },
                    [3] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.ByteToAxis),
                        InputId1 = (uint)Id.RightStickY,
                        OutputId = (uint)GamepadInput.Id.RightStickY,
                    },
                    [4] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.BitToButton),
                        InputId1 = (uint)Id.Square,
                        OutputId = (uint)GamepadInput.Id.ButtonWest,
                    },
                    [5] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.BitToButton),
                        InputId1 = (uint)Id.Cross,
                        OutputId = (uint)GamepadInput.Id.ButtonSouth,
                    },
                    [6] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.BitToButton),
                        InputId1 = (uint)Id.Circle,
                        OutputId = (uint)GamepadInput.Id.ButtonEast,
                    },
                    [7] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.BitToButton),
                        InputId1 = (uint)Id.Triangle,
                        OutputId = (uint)GamepadInput.Id.ButtonNorth,
                    },
                };

                return new DOTSInput.InputPipeline
                {
                    StructMappings = structMappings,
                    Transforms = transforms
                };
            }
        }
    }

    public struct PS4ControllerInput
    {
    }

    public struct PSControllerInput
    {
    }

    public struct GamepadInput : IComponentData, IInputData
    {
        public enum Id
        {
            LeftStick = 0 * 8,
            RightStick = 8 * 8,

            LeftStickX = 16 * 8,
            LeftStickY = 20 * 8,
            RightStickX = 24 * 8,
            RightStickY = 28 * 8,

            LeftStickUp = 32 * 8,
            LeftStickDown = 36 * 8,
            LeftStickLeft = 40 * 8,
            LeftStickRight = 44 * 8,

            RightStickUp = 48 * 8,
            RightStickDown = 52 * 8,
            RightStickLeft = 56 * 8,
            RightStickRight = 60 * 8,

            ButtonLeftStickUp = 64 * 8,
            ButtonLeftStickDown = 65 * 8,
            ButtonLeftStickLeft = 66 * 8,
            ButtonLeftStickRight = 67 * 8,

            ButtonRightStickUp = 68 * 8,
            ButtonRightStickDown = 69 * 8,
            ButtonRightStickLeft = 70 * 8,
            ButtonRightStickRight = 71 * 8,

            ButtonSouth = 72 * 8,
            ButtonNorth = 73 * 8,
            ButtonWest = 74 * 8,
            ButtonEast = 75 * 8,
        }

        public Float2Input LeftStick;
        public Float2Input RightStick;

        public AxisInput LeftStickX; // Primary.
        public AxisInput LeftStickY; // Primary.

        public AxisInput RightStickX; // Primary.
        public AxisInput RightStickY; // Primary.

        public HalfAxisInput LeftStickUp;
        public HalfAxisInput LeftStickDown;
        public HalfAxisInput LeftStickLeft;
        public HalfAxisInput LeftStickRight;

        public HalfAxisInput RightStickUp;
        public HalfAxisInput RightStickDown;
        public HalfAxisInput RightStickLeft;
        public HalfAxisInput RightStickRight;

        public ButtonInput ButtonLeftStickUp;
        public ButtonInput ButtonLeftStickDown;
        public ButtonInput ButtonLeftStickLeft;
        public ButtonInput ButtonLeftStickRight;

        public ButtonInput ButtonRightStickUp;
        public ButtonInput ButtonRightStickDown;
        public ButtonInput ButtonRightStickLeft;
        public ButtonInput ButtonRightStickRight;

        public ButtonInput ButtonSouth; // Primary.
        public ButtonInput ButtonNorth; // Primary.
        public ButtonInput ButtonWest; // Primary.
        public ButtonInput ButtonEast; // Primary.

        public static uint Format => CRC32.crc32("GamepadInput");

        public DOTSInput.InputPipeline InputPipelineParts
        {
            get
            {
                var structMappings = new NativeArray<DOTSInput.InputStructMapping>(1, Allocator.Persistent)
                {
                    // Struct mapping to self which fills out all secondary inputs based on primary ones.
                    [0] = new DOTSInput.InputStructMapping
                    {
                        InputFormat = CRC32.crc32("GamepadInput"),
                        OutputFormat = CRC32.crc32("GamepadInput"),
                        InputSizeInBytes = (uint)UnsafeUtility.SizeOf<GamepadInput>(),
                        OutputSizeInBytes = (uint)UnsafeUtility.SizeOf<GamepadInput>(),
                        TransformStartIndex = 0,
                        TransformCount = 18,
                    }
                };

                var transforms = new NativeArray<DOTSInput.InputTransform>(18, Allocator.Persistent)
                {
                    [0] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Combination.TwoAxesToOneFloat2),
                        InputId1 = (uint)Id.LeftStickX,
                        InputId2 = (uint)Id.LeftStickY,
                        OutputId = (uint)Id.LeftStick,
                    },
                    [1] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Combination.TwoAxesToOneFloat2),
                        InputId1 = (uint)Id.RightStickX,
                        InputId2 = (uint)Id.RightStickY,
                        OutputId = (uint)Id.RightStick,
                    },
                    [2] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.NegativeAxisToHalfAxis),
                        InputId1 = (uint)Id.LeftStickX,
                        OutputId = (uint)Id.LeftStickLeft,
                    },
                    [3] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.LeftStickLeft,
                        OutputId = (uint)Id.ButtonLeftStickLeft,
                    },
                    [4] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.PositiveAxisToHalfAxis),
                        InputId1 = (uint)Id.LeftStickX,
                        OutputId = (uint)Id.LeftStickRight,
                    },
                    [5] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.LeftStickRight,
                        OutputId = (uint)Id.ButtonLeftStickRight,
                    },
                    [6] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.NegativeAxisToHalfAxis),
                        InputId1 = (uint)Id.LeftStickY,
                        OutputId = (uint)Id.LeftStickDown,
                    },
                    [7] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.LeftStickDown,
                        OutputId = (uint)Id.ButtonLeftStickDown,
                    },
                    [8] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.PositiveAxisToHalfAxis),
                        InputId1 = (uint)Id.LeftStickY,
                        OutputId = (uint)Id.LeftStickUp,
                    },
                    [9] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.LeftStickUp,
                        OutputId = (uint)Id.ButtonLeftStickUp,
                    },
                    [10] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.NegativeAxisToHalfAxis),
                        InputId1 = (uint)Id.RightStickX,
                        OutputId = (uint)Id.RightStickLeft,
                    },
                    [11] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.RightStickLeft,
                        OutputId = (uint)Id.ButtonRightStickLeft,
                    },
                    [12] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.PositiveAxisToHalfAxis),
                        InputId1 = (uint)Id.RightStickX,
                        OutputId = (uint)Id.RightStickRight,
                    },
                    [13] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.RightStickRight,
                        OutputId = (uint)Id.ButtonRightStickRight,
                    },
                    [14] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.NegativeAxisToHalfAxis),
                        InputId1 = (uint)Id.RightStickY,
                        OutputId = (uint)Id.RightStickDown,
                    },
                    [15] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.RightStickDown,
                        OutputId = (uint)Id.ButtonRightStickDown,
                    },
                    [16] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.PositiveAxisToHalfAxis),
                        InputId1 = (uint)Id.RightStickY,
                        OutputId = (uint)Id.RightStickUp,
                    },
                    [17] = new DOTSInput.InputTransform
                    {
                        Operation = DOTSInput.ToTransformOperation(DOTSInput.Conversion.HalfAxisToButton),
                        InputId1 = (uint)Id.RightStickUp,
                        OutputId = (uint)Id.ButtonRightStickUp,
                    },
                };

                return new DOTSInput.InputPipeline
                {
                    StructMappings = structMappings,
                    Transforms = transforms
                };
            }
        }
    }

    public struct MouseInput
    {
        public AxisInput x;
        public AxisInput y;
        public AxisInput deltaX;
        public AxisInput deltaY;
        public ButtonInput leftButton;
        public ButtonInput rightButton;
        public ButtonInput middleButton;
    }

    public struct KeyboardInput
    {
    }

    public struct GameplayInputProcessing
    {
    }

    /// <summary>
    /// A [-1..1] floating-point input.
    /// </summary>
    public struct AxisInput : IEquatable<AxisInput>, IEquatable<float>
    {
        public float Value;

        public bool Equals(AxisInput other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return UnityEngine.Mathf.Approximately(Value, other.Value);
        }

        public bool Equals(float other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return UnityEngine.Mathf.Approximately(Value, other);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is AxisInput axis)
                return Equals(axis);
            if (obj is float f)
                return Equals(f);
            return false;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A [0..1] floating-point input.
    /// </summary>
    public struct HalfAxisInput : IEquatable<HalfAxisInput>, IEquatable<float>
    {
        public float Value;

        public bool Equals(HalfAxisInput other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return UnityEngine.Mathf.Approximately(Value, other.Value);
        }

        public bool Equals(float other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return UnityEngine.Mathf.Approximately(Value, other);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is HalfAxisInput axis)
                return Equals(axis);
            if (obj is float f)
                return Equals(f);
            return false;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A 0|1 trigger input.
    /// </summary>
    public struct ButtonInput : IEquatable<ButtonInput>, IEquatable<bool>
    {
        public byte State;

        public bool IsPressed
        {
            get => (State & (byte)Flags.IsPressed) != 0;
            set
            {
                if (value)
                    State |= (byte)Flags.IsPressed;
                else
                    State &= (byte)~Flags.IsPressed;
            }
        }

        public bool WasJustPressed
        {
            get => (State & (byte)Flags.WasJustPressed) != 0;
            set
            {
                if (value)
                    State |= (byte)Flags.WasJustPressed;
                else
                    State &= (byte)~Flags.WasJustPressed;
            }
        }

        public bool WasJustReleased
        {
            get => (State & (byte)Flags.WasJustReleased) != 0;
            set
            {
                if (value)
                    State |= (byte)Flags.WasJustReleased;
                else
                    State &= (byte)~Flags.WasJustReleased;
            }
        }

        public bool Equals(ButtonInput other)
        {
            return other.State == State;
        }

        public bool Equals(bool other)
        {
            return IsPressed == other;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is ButtonInput button)
                return Equals(button);
            if (obj is bool b)
                return Equals(b);
            return false;
        }

        public override string ToString()
        {
            if (IsPressed)
                return "pressed";
            return "released";
        }

        [Flags]
        private enum Flags : byte
        {
            IsPressed = 1 << 0,
            WasJustPressed = 1 << 1,
            WasJustReleased = 1 << 2,
        }
    }

    /// <summary>
    /// A multi-state button. Not floating-point.
    /// </summary>
    public struct SwitchInput
    {
        public int Value;
    }

    /// <summary>
    /// A floating-point value of arbitrary range.
    /// </summary>
    public struct FloatInput
    {
        public float Value;
    }

    public struct Float2Input
    {
        public float2 Value;

        public float X => Value.x;
        public float Y => Value.y;
    }

    public struct Float3Input
    {
        public float3 Value;

        public float X => Value.x;
        public float Y => Value.y;
        public float Z => Value.z;
    }

    //this would be auto-generated from an .inputactions file

    //what I need:
    // - ability to *map* inputs dynamically from source to destination
    // - ability to trace that mapping back from destination to source at runtime
    // - ability for *multiple* inputs to feed into the same output (including *combinations* such as SHIFT+LMB)
    // - ability for certain types of processing to be influenced by dynamic parameters (such as deadzones)
    // - ability for certain types of processing to be stateful

    //invariants:
    // - all operations must be simple, linear transformations going from input to output

    //ideas:
    // - for device inputs to be able to combine, a new combined kind of input has to be created
    // - mappings are simple arrays of (int,int) pairs mapping a source ID to a destination ID
    //   ...hmm, this does not allow for *combinations*/composites
    // - processing parameters and state are packaged into separate structs
    // - pipeline looks *exactly* the same for actions and for device input

    public static partial class DOTSInput
    {
        //each conversion routine takes
        //   - a reference to an input struct
        //   - a reference to an output struct
        //   - a native array of mappings (may be null/empty)
        //   - a reference to a struct with parameters

        public enum Operation
        {
            None,
            Copy,
            Convert,
            Process,
            Combine,
        }

        //does conversion need parameters? e.g. to account for 0..255 values mapping to a [-1..1] axis with 127 being 0?
        public enum Conversion
        {
            Invalid,

            // Axis.
            BitsToAxis,
            ByteToAxis,
            SByteToAxis,
            ShortToAxis,
            UShortToAxis,
            IntToAxis,
            UIntToAxis,
            FloatToAxis,
            DoubleToAxis,

            // Half-Axis.
            // NOTE: Need to go to Axis first. Cannot go directly from memory to half-axis.
            PositiveAxisToHalfAxis,
            NegativeAxisToHalfAxis,

            // Button.
            BitToButton,
            ByteToButton,
            SByteToButton,
            ShortToButton,
            UShortToButton,
            IntToButton,
            UIntToButton,
            FloatToButton,
            DoubleToButton,
            AxisToButton,
            HalfAxisToButton,

            // Switch.
            BitsToSwitch,
            ByteToSwitch,
            SByteToSwitch,
            ShortToSwitch,
            UShortToSwitch,
            IntToSwitch,
            UIntToSwitch,
        }

        public enum Processor
        {
            Invalid,
            ScaleAxis,
            ClampAxis,
            NormalizeAxis,
            DeadzoneAxis,
            DeadzoneFloat2,
        }

        public enum Combination
        {
            Invalid,
            TwoButtonsToOneButton,
            ThreeButtonsToOneButton,
            OneButtonAndOneAxisToOneAxis,
            TwoButtonsAndOneAxisToOneAxis,
            TwoAxesToOneFloat2,
            ThreeAxesToOneFloat3,
        }

        //the operations need to be extensible! (do it by registering entirely new functions?)

        public static unsafe void Convert(void* input, int bitOffset, int bitCount, void* output, int conversionType)
        {
        }

        public static unsafe void Process(void* input, void* output, int processingType, void* processingState)
        {
        }

        public static unsafe void Combine2(void* input1, void* input2, void* output, int combinationType)
        {
        }

        public static unsafe void Combine3(void* input1, void* input2, void* input3, void* output, int combinationType)
        {
        }

        //what if we need several chained operations? in that case, what previously was the output, now becomes the input
        public struct InputTransform
        {
            public uint Operation; // (8bit:operation)|(24bits:type)
            public uint InputId1;
            public uint InputId2;
            public uint InputId3;
            public uint InputId4;
            public uint OutputId;
        }

        public struct InputStructMapping
        {
            public uint InputFormat;
            public uint OutputFormat;
            public uint InputSizeInBytes;
            public uint OutputSizeInBytes;
            public uint TransformStartIndex;
            public uint TransformCount;
        }

        /// <summary>
        /// An input pipeline describes either a sequence of struct transformations or a collection
        /// of such.
        /// </summary>
        public struct InputPipeline : IDisposable
        {
            public NativeArray<InputStructMapping> StructMappings;
            public NativeArray<InputTransform> Transforms;

            public void Dispose()
            {
                if (StructMappings.IsCreated)
                    StructMappings.Dispose();
                if (Transforms.IsCreated)
                    Transforms.Dispose();
            }
        }

        public static uint ToCopyOperation(uint sizeInBits)
        {
            return ((uint)Operation.Copy << 24) | sizeInBits;
        }

        public static uint ToTransformOperation(Conversion conversion)
        {
            return ((uint)Operation.Convert << 24) | (uint)conversion;
        }

        public static uint ToTransformOperation(Processor processor)
        {
            return ((uint)Operation.Process << 24) | (uint)processor;
        }

        public static uint ToTransformOperation(Combination combination)
        {
            return ((uint)Operation.Combine << 24) | (uint)combination;
        }

        private static unsafe void* Ptr(void* ptr, uint bitOffset)
        {
            return (byte*)ptr + (bitOffset >> 3);
        }

        //instead of per-combination generated function, one function plus a mappings list for each combination
        //structs are still generated

        public static unsafe void Transform(void* input, void* output, ref InputPipeline pipeline)
        {
            Transform(input, output, pipeline.StructMappings, pipeline.Transforms, 0, pipeline.StructMappings.Length);
        }

        public static unsafe void Transform(void* input, void* output, NativeArray<InputStructMapping> structMappings,
            NativeArray<InputTransform> transforms, int structMappingIndex, int structMappingCount)
        {
            var structMappingsPtr = (InputStructMapping*)structMappings.GetUnsafeReadOnlyPtr();
            var transformsPtr = (InputTransform*)transforms.GetUnsafeReadOnlyPtr();

            var inputFormat = structMappingsPtr[structMappingIndex].InputFormat;
            var outputFormat = structMappingsPtr[structMappingIndex + structMappingCount - 1].OutputFormat;

            // Figure out how big a temporary buffer we need (if any).
            var tempBufferSize = 0u;
            var currentFormat = inputFormat;
            for (var i = 0; i < (structMappingCount - 1); ++i) // Last struct map output goes to final output so can't be temp.
            {
                if (structMappingsPtr[structMappingIndex + i].OutputFormat != currentFormat)
                {
                    tempBufferSize += structMappingsPtr[i].OutputSizeInBytes;
                    currentFormat = structMappingsPtr[i].OutputFormat;
                }
            }

            // Go through struct mappings one by one.
            using (var tempBuffer = new NativeArray<byte>((int)tempBufferSize, Allocator.Temp))
            {
                var nextTempBufferPtr = (byte*)tempBuffer.GetUnsafePtr();
                var currentTempBufferPtr = nextTempBufferPtr;

                for (var i = 0; i < structMappingCount; ++i)
                {
                    ref var mapping = ref structMappingsPtr[structMappingIndex + i];

                    // Determine where to read from.
                    void* inputPtr;
                    if (mapping.InputFormat == inputFormat)
                        inputPtr = input;
                    else
                        inputPtr = currentTempBufferPtr;

                    // Determine where to write to.
                    void* outputPtr;
                    if (mapping.OutputFormat == mapping.InputFormat)
                        outputPtr = inputPtr; // Identity transform.
                    else if (mapping.OutputFormat == outputFormat)
                        outputPtr = output;
                    else
                    {
                        outputPtr = nextTempBufferPtr;
                        nextTempBufferPtr += mapping.OutputSizeInBytes;
                        currentTempBufferPtr = (byte*)outputPtr;
                    }

                    // Run transforms.
                    Transform(inputPtr, outputPtr, (int)mapping.TransformCount, transformsPtr + mapping.TransformStartIndex);
                }
            }
        }

        public static unsafe void Transform(void* input, void* output, ref InputPipeline pipelines, int structMappingIndex)
        {
            Transform(input, output, (int)pipelines.StructMappings[structMappingIndex].TransformCount,
                (InputTransform*)pipelines.Transforms.GetUnsafeReadOnlyPtr() +
                pipelines.StructMappings[structMappingIndex].TransformStartIndex);
        }

        public static unsafe void Transform(void* input, void* output, int transformCount, InputTransform* transforms)
        {
            for (var i = 0; i < transformCount; ++i)
            {
                ref var mapping = ref transforms[i];

                ////REVIEW: how do we make this thing here extensible?

                var operation = (Operation)(mapping.Operation >> 24);
                switch (operation)
                {
                    case Operation.Convert:
                    {
                        var conversion = (Conversion)(mapping.Operation & 0xffffff);
                        switch (conversion)
                        {
                            case Conversion.ByteToAxis:
                            {
                                var inputPtr = (byte*)Ptr(input, mapping.InputId1);
                                var outputPtr = (AxisInput*)Ptr(output, mapping.OutputId);
                                outputPtr->Value = -1f + *inputPtr / 255f * 2f;
                                break;
                            }

                            case Conversion.NegativeAxisToHalfAxis:
                            {
                                var inputPtr = (AxisInput*)Ptr(input, mapping.InputId1);
                                var outputPtr = (HalfAxisInput*)Ptr(output, mapping.OutputId);
                                outputPtr->Value = math.abs(math.clamp(inputPtr->Value, -1, 0));
                                break;
                            }

                            case Conversion.PositiveAxisToHalfAxis:
                            {
                                var inputPtr = (AxisInput*)Ptr(input, mapping.InputId1);
                                var outputPtr = (HalfAxisInput*)Ptr(output, mapping.OutputId);
                                outputPtr->Value = math.clamp(inputPtr->Value, 0, 1);
                                break;
                            }

                            case Conversion.BitToButton:
                            {
                                var inputPtr = (byte*)Ptr(input, mapping.InputId1);
                                var outputPtr = (ButtonInput*)Ptr(output, mapping.OutputId);
                                var wasPressed = outputPtr->IsPressed;
                                var isPressed = (*inputPtr & (1 << (byte)(mapping.InputId1 % 8))) != 0;
                                outputPtr->IsPressed = isPressed;
                                outputPtr->WasJustPressed = isPressed && !wasPressed;
                                outputPtr->WasJustReleased = !isPressed && wasPressed;
                                break;
                            }

                            case Conversion.HalfAxisToButton:
                            {
                                ////REVIEW: we probably want to keep wasJustReleased and wasJustPressed over an entire frame; not just from transformation to transformation
                                var inputPtr = (HalfAxisInput*)Ptr(input, mapping.InputId1);
                                var outputPtr = (ButtonInput*)Ptr(output, mapping.OutputId);
                                var wasPressed = outputPtr->IsPressed;
                                var isPressed = inputPtr->Value >= 0.5f;///TODO: configurable press points
                                outputPtr->IsPressed = isPressed;
                                outputPtr->WasJustPressed = isPressed && !wasPressed;
                                outputPtr->WasJustReleased = !isPressed && wasPressed;
                                break;
                            }

                            default:
                                throw new NotImplementedException("conversion type");
                        }
                        break;
                    }

                    case Operation.Process:
                        throw new NotImplementedException("process");
                        break;

                    case Operation.Combine:
                    {
                        var combination = (Combination)(mapping.Operation & 0xffffff);
                        switch (combination)
                        {
                            case Combination.TwoAxesToOneFloat2:
                            {
                                var inputPtr1 = (AxisInput*)Ptr(input, mapping.InputId1);
                                var inputPtr2 = (AxisInput*)Ptr(input, mapping.InputId2);
                                var outputPtr = (Float2Input*)Ptr(output, mapping.OutputId);
                                outputPtr->Value = new float2(inputPtr1->Value, inputPtr2->Value);
                                break;
                            }

                            default:
                                throw new NotImplementedException("combination type");
                        }
                        break;
                    }

                    case Operation.Copy:
                    {
                        // Lower 24bits are number of bits to copy from input to output.
                        var sizeInBits = mapping.Operation & 0xffffff;

                        if (sizeInBits % 8 == 0 && mapping.InputId1 % 8 == 0 && mapping.OutputId % 8 == 0)
                        {
                            // Clean byte region. Just use MemCpy.
                            var inputPtr = Ptr(input, mapping.InputId1);
                            var outputPtr = Ptr(output, mapping.OutputId);
                            UnsafeUtility.MemCpy(outputPtr, inputPtr, sizeInBits >> 3);
                        }
                        else
                        {
                            throw new NotImplementedException("copying bit regions");
                        }
                        break;
                    }
                }
            }
        }

        public static unsafe void AddPipelineFragments<TInput>(Dictionary<ulong, InputPipelineFragment> fragments)
            where TInput : struct, IInputData
        {
            using (var pipelineParts = new TInput().InputPipelineParts)
            {
                for (var i = 0; i < pipelineParts.StructMappings.Length; ++i)
                {
                    var structMapping = pipelineParts.StructMappings[i];

                    // Copy transforms.
                    var transforms = new NativeArray<InputTransform>((int)structMapping.TransformCount, Allocator.Persistent);
                    UnsafeUtility.MemCpy(transforms.GetUnsafePtr(),
                        (InputTransform*)pipelineParts.Transforms.GetUnsafeReadOnlyPtr() + structMapping.TransformStartIndex,
                        structMapping.TransformCount * UnsafeUtility.SizeOf<InputTransform>());
                    structMapping.TransformStartIndex = 0;

                    var fragment = new InputPipelineFragment
                    {
                        StructMapping = structMapping,
                        Transforms = transforms,
                    };

                    var key = ToInputOutputKey(structMapping.InputFormat, structMapping.OutputFormat);
                    fragments[key] = fragment;
                }
            }
        }

        private static ulong ToInputOutputKey(uint inputFormat, uint outputFormat)
        {
            return ((ulong)inputFormat << 32) | outputFormat;
        }

        public static void ReleasePipelineFragments(Dictionary<ulong, InputPipelineFragment> fragments)
        {
            foreach (var fragment in fragments)
                fragment.Value.Transforms.Dispose();
        }

        private static unsafe void Grow<T>(ref NativeArray<T> array, int count)
            where T : struct
        {
            if (!array.IsCreated)
            {
                array = new NativeArray<T>(count, Allocator.Persistent);
                return;
            }

            var newArray = new NativeArray<T>(array.Length + count, Allocator.Persistent);
            UnsafeUtility.MemCpy(newArray.GetUnsafePtr(), array.GetUnsafeReadOnlyPtr(), array.Length * UnsafeUtility.SizeOf<T>());
            array.Dispose();
            array = newArray;
        }

        // Tries to find the shortest sequence of transformations going from the given input format to
        // the given output format. If no such transformation exists, returns false.
        public static unsafe bool CreateInputPipeline(Dictionary<ulong, InputPipelineFragment> pipelineFragments, uint inputFormat, uint outputFormat, ref InputPipeline outPipeline)//appends to the given pipeline if already initialized
        {
            var pipelineSteps = new List<InputPipelineFragment>();
            if (!FindPipelineRecursive(pipelineFragments, inputFormat, outputFormat, pipelineSteps))
                return false;

            var structMappingIndex = outPipeline.StructMappings.Length;
            var transformIndex = outPipeline.Transforms.Length;

            var numStructMappingsToAdd = pipelineSteps.Count;
            var numTransformsToAdd = pipelineSteps.Sum(x => x.StructMapping.TransformCount);

            Grow(ref outPipeline.StructMappings, numStructMappingsToAdd);
            Grow(ref outPipeline.Transforms, (int)numTransformsToAdd);

            foreach (var step in pipelineSteps)
            {
                var structMapping = step.StructMapping;

                // Copy transforms.
                UnsafeUtility.MemCpy((InputTransform*)outPipeline.Transforms.GetUnsafePtr() + transformIndex,
                    (InputTransform*)step.Transforms.GetUnsafeReadOnlyPtr() + structMapping.TransformStartIndex,
                    structMapping.TransformCount * UnsafeUtility.SizeOf<InputTransform>());

                // Record struct mapping.
                structMapping.TransformStartIndex = (uint)transformIndex;
                outPipeline.StructMappings[structMappingIndex] = structMapping;

                transformIndex += (int)structMapping.TransformCount;
                ++structMappingIndex;
            }

            return true;
        }

        // Blindly search forward through every possible branch. If there's loops in the pipelines, we
        // will not terminate here.
        private static bool FindPipelineRecursive(Dictionary<ulong, InputPipelineFragment> pipelineFragments, uint inputFormat,
            uint outputFormat, List<InputPipelineFragment> result)
        {
            // If there's an identity transform, add that first.
            if (pipelineFragments.TryGetValue(ToInputOutputKey(inputFormat, inputFormat), out var identityTransform))
                result.Add(identityTransform);

            // If we have a direct way of going from input to output, finish.
            if (pipelineFragments.TryGetValue(ToInputOutputKey(inputFormat, outputFormat), out var matchingFragment))
            {
                result.Add(matchingFragment);

                // Finally, check if there's an identity transform on the *output*. If so,
                // add that as the final step.
                if (pipelineFragments.TryGetValue(ToInputOutputKey(outputFormat, outputFormat), out var finalTransform))
                    result.Add(finalTransform);

                return true;
            }

            //the list stuff in this method is wasteful and ridiculous but good enough for a prototype

            // Do a brute-force search through the solution space finding the shortest path from input to output.
            List<InputPipelineFragment> currentBestPipeline = null;
            foreach (var fragment in pipelineFragments.Values)
            {
                if (fragment.StructMapping.InputFormat != inputFormat)
                    continue;

                // Skip identity transforms.
                if (fragment.StructMapping.InputFormat == fragment.StructMapping.OutputFormat)
                    continue;

                var newList = new List<InputPipelineFragment>(result);
                newList.Add(fragment);

                if (FindPipelineRecursive(pipelineFragments, fragment.StructMapping.OutputFormat, outputFormat, newList) &&
                    (currentBestPipeline == null || GetPipelineLength(newList) < GetPipelineLength(currentBestPipeline)))
                    currentBestPipeline = newList;
            }

            if (currentBestPipeline == null)
                return false;

            result.Clear();
            result.AddRange(currentBestPipeline);

            return true;
        }

        private static int GetPipelineLength(List<InputPipelineFragment> pipeline)
        {
            // Count everything but identity transforms.
            return pipeline.Sum(f => f.StructMapping.InputFormat != f.StructMapping.OutputFormat ? 1 : 0);
        }

        public struct InputPipelineFragment
        {
            public InputStructMapping StructMapping;
            public NativeArray<InputTransform> Transforms;
        }
    }

    public interface IInputData
    {
        DOTSInput.InputPipeline InputPipelineParts { get; }
        //add property to get descriptions
    }

    //need to know whether something works like GamepadInput (no PlayerNumber) or like the generated components
    internal struct CommonGeneratedInputComponentParts
    {
        public int PlayerNumber;

        //device assignments?
    }

    public abstract class InputSystem<TInputComponent> : JobComponentSystem
        where TInputComponent : struct, IComponentData, IInputData
    {
        ////NOTE: Cannot use Entities.ForEach because of generic component type.
        private struct InputJob : IJobForEach<TInputComponent>
        {
            [ReadOnly] public uint OutputFormat;
            [ReadOnly] public NativeArray<byte> EventBuffer;
            [ReadOnly] public NativeArray<InputDevicePairing> DevicePairings;
            [ReadOnly] public NativeArray<DOTSInput.InputStructMapping> StructMappings;
            [ReadOnly] public NativeArray<DOTSInput.InputTransform> Transforms;

            public unsafe void Execute(ref TInputComponent component)
            {
                var output = UnsafeUtility.AddressOf(ref component);
                var deviceCount = DevicePairings.Length;
                var structMappingCount = StructMappings.Length;

                // Go through events.
                for (var eventPtr = new InputEventPtr((InputEvent*)EventBuffer.GetUnsafeReadOnlyPtr());
                     eventPtr.valid;
                     eventPtr = eventPtr.Next())
                {
                    // Only process state events for now.
                    if (!eventPtr.IsA<StateEvent>())
                        continue;

                    ////TODO: need to also check player numbers here
                    // Check if the device is paired to the user.
                    var deviceId = eventPtr.deviceId;
                    var inputFormat = 0u;
                    var isPairedToUser = false;
                    for (var i = 0; i < deviceCount; ++i)
                        if (DevicePairings[i].DeviceId == deviceId)
                        {
                            inputFormat = DevicePairings[i].InputFormat;
                            isPairedToUser = true;
                            break;
                        }
                    if (!isPairedToUser)
                    {
                        // No, so skip event.
                        continue;
                    }

                    // Find the struct mapping pipeline going from the event format to our component format.
                    for (var i = 0; i < structMappingCount; ++i)
                    {
                        if (StructMappings[i].InputFormat != inputFormat)
                            continue;

                        // We've found a possible start to our pipeline. Look for the end.
                        var startIndex = i;
                        for (; i < structMappingCount; ++i)
                        {
                            if (StructMappings[i].OutputFormat == OutputFormat)
                                break;
                        }
                        if (i < structMappingCount)
                        {
                            //how should we handle input that needs to combine? such as keyboard&mouse?

                            // We've found our transform pipeline that will take the given event format and
                            // spit out data in our component format.
                            var input = StateEvent.From(eventPtr)->state;
                            DOTSInput.Transform(input, output, StructMappings, Transforms, startIndex, i - startIndex);
                        }
                    }
                }

                EventBuffer.Dispose();
            }
        }

        private DOTSInput.InputPipeline m_ComponentInputPipelines;

        protected override void OnCreate()
        {
            InputSystemHook.AddRef();

            // Retrieve input pipelines specific to the component.
            m_ComponentInputPipelines = new TInputComponent().InputPipelineParts;
        }

        protected override void OnDestroy()
        {
            InputSystemHook.Release();

            if (m_ComponentInputPipelines.Transforms.IsCreated)
                m_ComponentInputPipelines.Transforms.Dispose();
            if (m_ComponentInputPipelines.StructMappings.IsCreated)
                m_ComponentInputPipelines.StructMappings.Dispose();
        }

        /*
        protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventBuffer = InputSystemHook.Instance.EventBuffer;

            // Event buffers are read-only but are re-filled every frame. So we clone
            // it here for every job we schedule.
            var eventBufferClone = new NativeArray<byte>(eventBuffer.Length, Allocator.TempJob);
            UnsafeUtility.MemCpy(eventBufferClone.GetUnsafePtr(), eventBuffer.GetUnsafeReadOnlyPtr(), eventBuffer.Length);

            var job = new InputJob
            {
                DevicePairings = InputSystemHook.Instance.DevicePairings,
                EventBuffer = eventBufferClone,
            };
            return job.Schedule(this, inputDeps);
        }
        */
    }

    // Establishes a pairing between a player number and a device ID.
    // Multiple player numbers can use the same device.
    internal struct InputDevicePairing
    {
        public int PlayerNumber;
        public int DeviceId;
        public uint InputFormat;
    }

    internal struct InputStructType
    {
        public int SizeInBytes;
        public DOTSInput.InputPipeline Pipelines;
    }

    //ultimately, we want the ability to run arbitrary jobs over input event buffers, not just InputSystem<TComponent> stuff

    // For now, use UnityEngine.InputSystem to simplify a couple things. For one, we don't have access
    // to the NativeInputSystem API whereas UnityEngine.InputSystem has. But also, we only run in play
    // mode and thus will miss all the device discoveries that already happened. Using the current
    // UnityEngine-based InputSystem, we can simplify a few things until we can get rid of the dependency.
    internal class InputSystemHook
    {
        public static InputSystemHook Instance;

        public int ReferenceCount;
        public uint StepCount;
        public NativeArray<InputDevicePairing> DevicePairings;
        public NativeArray<byte> EventBuffer;
        public Dictionary<uint, InputStructType> StructTypeMap;

        private Action<InputEventBuffer> m_EventCallback;

        public static void AddRef()
        {
            if (Instance != null)
            {
                ++Instance.ReferenceCount;
                return;
            }

            // Pair all devices that are already connected and that we have an input
            // pipeline for to player #0. Quick and dirty.
            var devicePairings = new List<InputDevicePairing>();
            foreach (var device in InputSystem.devices)
            {
                uint inputFormat;
                switch (device.layout)
                {
                    case "DualShock4GamepadHID": inputFormat = CRC32.crc32("PS4ControllerHidEvent");
                        break;
                    default:
                        continue;
                }

                devicePairings.Add(new InputDevicePairing
                {
                    PlayerNumber = 0,
                    InputFormat = inputFormat,
                    DeviceId = device.deviceId,
                });
            }

            Instance = new InputSystemHook
            {
                ReferenceCount = 1,
                DevicePairings = new NativeArray<InputDevicePairing>(devicePairings.ToArray(), Allocator.Persistent),
                EventBuffer = new NativeArray<byte>(100 * 1024, Allocator.Persistent),
                StructTypeMap = new Dictionary<uint, InputStructType>()
                {
                    [CRC32.crc32("PS4ControllerHidEvent")] = new InputStructType
                    {
                        SizeInBytes = UnsafeUtility.SizeOf<PS4ControllerHidEvent>(),
                        //Pipelines =
                    }
                },
                m_EventCallback =
                    eventBuffer =>
                {
                    // Copy events.
                    var sizeInBytes = eventBuffer.sizeInBytes;
                    if (Instance.EventBuffer.Length < eventBuffer.sizeInBytes)
                    {
                        Instance.EventBuffer.Dispose();
                        Instance.EventBuffer = new NativeArray<byte>((int)sizeInBytes, Allocator.Persistent);
                    }

                    unsafe
                    {
                        UnsafeUtility.MemCpy(Instance.EventBuffer.GetUnsafePtr(), eventBuffer.data.GetUnsafeReadOnlyPtr(), sizeInBytes);
                    }
                }
            };

            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.s_Manager.onUpdate += Instance.m_EventCallback;

            // For now just pretend all devices belong to player #0.
            // .... could do away with DevicePairings altogether then...
        }

        public static void Release()
        {
            if (Instance == null)
                return;

            --Instance.ReferenceCount;
            if (Instance.ReferenceCount <= 0)
            {
                InputSystem.s_Manager.onUpdate -= Instance.m_EventCallback;
                Instance.DevicePairings.Dispose();
                Instance.EventBuffer.Dispose();
                Instance = null;
            }
        }

        public void Update(uint stepCount)
        {
            if (StepCount == stepCount)
                return;

            InputSystem.Update();
            StepCount = stepCount;
        }
    }

    /*
    private class NativeInputHook
    {
        public NativeArray<byte> EventBuffer;

        public static NativeInputHook Instance;

        public unsafe static void Initialize()
        {
            if (Instance != null)
                return;

            Instance = new NativeInputHook();

            NativeInputSystem.onShouldRunUpdate = _ => false;
            NativeInputSystem.onUpdate =
                (type, events) =>
                {
                    ////TODO: copy buffer
                };

            NativeInputSystem.onDeviceDiscovered =
                (deviceId, deviceDescription) =>
                {

                };
        }
    }
    */
}
