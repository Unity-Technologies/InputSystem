using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A floating-point 3D vector control composed of three <see cref="AxisControl">AxisControls</see>.
    /// </summary>
    public class Vector3Control : InputControl<Vector3>
    {
        /// <summary>The X component of this Vector3 control.</summary>
        [InputControl(offset = 0, displayName = "X")]
        public AxisControl x { get; set; }
        /// <summary>The Y component of this Vector3 control.</summary>
        [InputControl(offset = 4, displayName = "Y")]
        public AxisControl y { get; set; }
        /// <summary>The Z component of this Vector3 control.</summary>
        [InputControl(offset = 8, displayName = "Z")]
        public AxisControl z { get; set; }

        /// <summary>Initializes a new Vector3Control.</summary>
        public Vector3Control()
        {
            m_StateBlock.format = InputStateBlock.FormatVector3;
        }

        /// <summary>Resolves child axis controls after the control hierarchy is built.</summary>
        protected override void FinishSetup()
        {
            x = GetChildControl<AxisControl>("x");
            y = GetChildControl<AxisControl>("y");
            z = GetChildControl<AxisControl>("z");

            base.FinishSetup();
        }

        /// <summary>Reads the raw, unprocessed Vector3 value from the given state buffer.</summary>
        public override unsafe Vector3 ReadUnprocessedValueFromState(void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatVector3:
                    return *(Vector3*)((byte*)statePtr + (int)m_StateBlock.byteOffset);
                default:
                    return new Vector3(
                        x.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        y.ReadUnprocessedValueFromStateWithCaching(statePtr),
                        z.ReadUnprocessedValueFromStateWithCaching(statePtr));
            }
        }

        /// <summary>Writes the given Vector3 value into the given state buffer.</summary>
        public override unsafe void WriteValueIntoState(Vector3 value, void* statePtr)
        {
            switch (m_OptimizedControlDataType)
            {
                case InputStateBlock.kFormatVector3:
                    *(Vector3*)((byte*)statePtr + (int)m_StateBlock.byteOffset) = value;
                    break;
                default:
                    x.WriteValueIntoState(value.x, statePtr);
                    y.WriteValueIntoState(value.y, statePtr);
                    z.WriteValueIntoState(value.z, statePtr);
                    break;
            }
        }

        /// <summary>Returns the magnitude of the Vector3 value in the given state buffer.</summary>
        public override unsafe float EvaluateMagnitude(void* statePtr)
        {
            ////REVIEW: this can go beyond 1; that okay?
            return ReadValueFromStateWithCaching(statePtr).magnitude;
        }

        /// <summary>Returns the FourCC type code for the optimized state format of this control.</summary>
        protected override FourCC CalculateOptimizedControlDataType()
        {
            if (
                m_StateBlock.sizeInBits == sizeof(float) * 3 * 8 &&
                m_StateBlock.bitOffset == 0 &&
                x.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.optimizedControlDataType == InputStateBlock.FormatFloat &&
                z.optimizedControlDataType == InputStateBlock.FormatFloat &&
                y.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 4 &&
                z.m_StateBlock.byteOffset == x.m_StateBlock.byteOffset + 8
            )
                return InputStateBlock.FormatVector3;

            return InputStateBlock.FormatInvalid;
        }
    }
}
