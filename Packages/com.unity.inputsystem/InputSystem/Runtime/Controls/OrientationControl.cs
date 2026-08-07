using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Controls
{
    /// <summary>
    /// A control reading a <see cref="DeviceOrientation"/> value.
    /// </summary>
    /// <remarks>
    /// This is used by <see cref="OrientationSensor"/> to report the physical orientation of the device
    /// (see <see cref="OrientationSensor.orientation"/>). It provides feature parity with the legacy
    /// <c>UnityEngine.Input.deviceOrientation</c> property.
    /// </remarks>
    /// <seealso cref="OrientationSensor"/>
    [InputControlLayout(hideInUI = true)]
    public class OrientationControl : InputControl<DeviceOrientation>
    {
        /// <summary>
        /// Default-initialize the control.
        /// </summary>
        /// <remarks>
        /// Format of the control is <see cref="InputStateBlock.FormatInt"/>
        /// by default.
        /// </remarks>
        public OrientationControl()
        {
            m_StateBlock.format = InputStateBlock.FormatInt;
        }

        /// <inheritdoc />
        public override unsafe DeviceOrientation ReadUnprocessedValueFromState(void* statePtr)
        {
            var intValue = stateBlock.ReadInt(statePtr);
            return (DeviceOrientation)intValue;
        }

        /// <inheritdoc />
        public override unsafe void WriteValueIntoState(DeviceOrientation value, void* statePtr)
        {
            var valuePtr = (byte*)statePtr + (int)m_StateBlock.byteOffset;
            *(int*)valuePtr = (int)value;
        }
    }
}
