using System;
using System.Globalization;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Input
{
    // - Input IDs are bit offsets at the same time
    // - There's no synthetic controls or any other sharing of state; every control must have its own state
    // - Input values have very specific semantics.


    public struct InputDescriptor
    {
        public string Path;
        public uint Id;
        public uint Format;
    }

    //have some kind of identifier that reveals the source of input? (i.e. where the memory initially came from?) if so, what do combiners do?

    public struct PS4ControllerHidInput
    {
        public const int Format = 0x11111111;

        public enum Id : uint
        {
            LeftStickX = 1 * 8,
            LeftStickY = 2 * 8,
            RightStickX = 3 * 8,
            RightStickY = 4 * 8,
            Square = 5 * 8 + 4,
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

        public static InputDescriptor[] Descriptors = new[]
        {
            new InputDescriptor { Path = "square", Id = (uint)Id.Square, /* ... */ }
        };
    }

    public struct PS4ControllerInput
    {
    }

    public struct PSControllerInput
    {
    }

    public struct GamepadInput
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

        public AxisInput LeftStickX;
        public AxisInput LeftStickY;

        public AxisInput RightStickX;
        public AxisInput RightStickY;

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

        public ButtonInput ButtonSouth;
        public ButtonInput ButtonNorth;
        public ButtonInput ButtonWest;
        public ButtonInput ButtonEeat;
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
            return Mathf.Approximately(Value, other.Value);
        }

        public bool Equals(float other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return Mathf.Approximately(Value, other);
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
            return Mathf.Approximately(Value, other.Value);
        }

        public bool Equals(float other)
        {
            ////TODO: do this with Unity.Mathematics... how?
            return Mathf.Approximately(Value, other);
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
    }

    public struct Float3Input
    {
        public float3 Value;
    }

    //this would be auto-generated from an .inputactions file
    public struct GameplayInput
    {
        public enum Id : uint
        {
            Look = 0 * 8,
            Move = 8 * 8,
            Fire = 16 * 8,
        }

        public Float2Input Look;
        public Float2Input Move;
        public ButtonInput Fire;
    }

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

        //use pointers or references?

        // PS4ControllerHidInput -> GamepadInput
        public static void ConvertPs4ControllerHidInputsToGamepadInputs(ref PS4ControllerHidInput inputs, ref GamepadInput outputs)
        {
        }

        // GamepadInput -> GameplayInput
        public static void ConvertGamepadInputsToGameplayInputs(ref GamepadInput input, ref GameplayInput output)
        {
        }

        public static void ConvertGamepadInputsToGameplayInputs(ref GamepadInput input, ref GameplayInput output, ref GameplayInputProcessing processing, NativeArray<int> mappings)
        {
            // let's say we need to map the left stick from `input` to `move` on `output` and apply deadzone processing; let's ignore mappings for now
            // (in reality, deadzone processing would be applied inside GamepadInput)
        }

        public enum Operation
        {
            None,
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
            BitsToButton,
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

        //separate this into arrays?
        //what if we need several chained operations? in that case, what previously was the output, now becomes the input
        public struct InputMapping
        {
            public uint Operation; // (8bit:operation)|(24bits:type)
            public uint InputId1;
            public uint InputId2;
            public uint InputId3;
            public uint InputId4;
            public uint OutputId;
        }

        public static uint ToOperation(Conversion conversion)
        {
            return ((uint)Operation.Convert << 24) | (uint)conversion;
        }

        public static uint ToOperation(Processor processor)
        {
            return ((uint)Operation.Process << 24) | (uint)processor;
        }

        public static uint ToOperation(Combination combination)
        {
            return ((uint)Operation.Combine << 24) | (uint)combination;
        }

        private static unsafe void* Ptr(void* ptr, uint bitOffset)
        {
            return (byte*)ptr + (bitOffset >> 3);
        }

        //instead of per-combination generated function, one function plus a mappings list for each combination
        //structs are still generated

        public static unsafe void Map(void* input, void* output, int mappingsCount, InputMapping* mappings)
        {
            for (var i = 0; i < mappingsCount; ++i)
            {
                ref var mapping = ref mappings[i];

                ////REVIEW: how do we make this thing here extensible?

                var operation = (Operation)(mapping.Operation >> 24);
                switch (operation)
                {
                    case Operation.Convert:
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
                        }
                        break;

                    case Operation.Process:
                        break;

                    case Operation.Combine:
                        break;
                }
            }
        }
    }
}
