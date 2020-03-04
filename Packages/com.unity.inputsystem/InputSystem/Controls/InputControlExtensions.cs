using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;

////REVIEW: some of the stuff here is really low-level; should we move it into a separate static class inside of .LowLevel?

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Various extension methods for <see cref="InputControl"/>. Mostly low-level routines.
    /// </summary>
    public static class InputControlExtensions
    {
        /// <summary>
        /// Find a control of the given type in control hierarchy of <paramref name="control"/>.
        /// </summary>
        /// <param name="control">Control whose parents to inspect.</param>
        /// <typeparam name="TControl">Type of control to look for. Actual control type can be
        /// subtype of this.</typeparam>
        /// <remarks>The found control of type <typeparamref name="TControl"/> which may be either
        /// <paramref name="control"/> itself or one of its parents. If no such control was found,
        /// returns <c>null</c>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        public static TControl FindInParentChain<TControl>(this InputControl control)
            where TControl : InputControl
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            for (var parent = control; parent != null; parent = parent.parent)
                if (parent is TControl parentOfType)
                    return parentOfType;

            return null;
        }

        /// <summary>
        /// Check whether the given control is considered pressed according to the button press threshold.
        /// </summary>
        /// <param name="control">Control to check.</param>
        /// <param name="buttonPressPoint">Optional custom button press point. If not supplied, <see cref="InputSettings.defaultButtonPressPoint"/>
        /// is used.</param>
        /// <returns>True if the actuation of the given control is high enough for it to be considered pressed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method checks the actuation level of the control as <see cref="IsActuated"/> does. For <see cref="Controls.ButtonControl"/>s
        /// and other <c>float</c> value controls, this will effectively check whether the float value of the control exceeds the button
        /// point threshold. Note that if the control is an axis that can be both positive and negative, the press threshold works in
        /// both directions, i.e. it can be crossed both in the positive direction and in the negative direction.
        /// </remarks>
        /// <seealso cref="IsActuated"/>
        /// <seealso cref="InputSettings.defaultButtonPressPoint"/>
        /// <seealso cref="InputSystem.settings"/>
        public static bool IsPressed(this InputControl control, float buttonPressPoint = 0)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (Mathf.Approximately(0, buttonPressPoint))
            {
                if (control is ButtonControl button)
                    buttonPressPoint = button.pressPointOrDefault;
                else
                    buttonPressPoint = ButtonControl.s_GlobalDefaultButtonPressPoint;
            }
            return control.IsActuated(buttonPressPoint);
        }

        /// <summary>
        /// Return true if the given control is actuated.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="threshold">Magnitude threshold that the control must match or exceed to be considered actuated.
        /// An exception to this is the default value of zero. If threshold is zero, the control must have a magnitude
        /// greater than zero.</param>
        /// <returns></returns>
        /// <remarks>
        /// Actuation is defined as a control having a magnitude (<see cref="InputControl.EvaluateMagnitude()"/>
        /// greater than zero or, if the control does not support magnitudes, has been moved from its default
        /// state.
        ///
        /// In practice, this means that when actuated, a control will produce a value other than its default
        /// value.
        /// </remarks>
        public static bool IsActuated(this InputControl control, float threshold = 0)
        {
            // First perform cheap memory check. If we're in default state, we don't
            // need to invoke virtuals on the control.
            if (control.CheckStateIsAtDefault())
                return false;

            // Check magnitude of actuation.
            var magnitude = control.EvaluateMagnitude();
            if (magnitude < 0)
            {
                ////REVIEW: we probably want to do a value comparison on this path to compare it to the default value
                return true;
            }

            if (Mathf.Approximately(threshold, 0))
                return magnitude > 0;

            return magnitude >= threshold;
        }

        /// <summary>
        /// Read the current value of the control and return it as an object.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This method allocates GC memory and thus may cause garbage collection when used during gameplay.
        ///
        /// Use <seealso cref="ReadValueIntoBuffer"/> to read values generically without having to know the
        /// specific value type of a control.
        /// </remarks>
        /// <seealso cref="ReadValueIntoBuffer"/>
        /// <seealso cref="InputControl{TValue}.ReadValue"/>
        public static unsafe object ReadValueAsObject(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            return control.ReadValueFromStateAsObject(control.currentStatePtr);
        }

        /// <summary>
        /// Read the current, processed value of the control and store it into the given memory buffer.
        /// </summary>
        /// <param name="buffer">Buffer to store value in. Note that the value is not stored with the offset
        /// found in <see cref="InputStateBlock.byteOffset"/> of the control's <see cref="InputControl.stateBlock"/>. It will
        /// be stored directly at the given address.</param>
        /// <param name="bufferSize">Size of the memory available at <paramref name="buffer"/> in bytes. Has to be
        /// at least <see cref="InputControl.valueSizeInBytes"/>. If the size is smaller, nothing will be written to the buffer.</param>
        /// <seealso cref="InputControl.valueSizeInBytes"/>
        /// <seealso cref="InputControl.valueType"/>
        /// <seealso cref="InputControl.ReadValueFromStateIntoBuffer"/>
        public static unsafe void ReadValueIntoBuffer(this InputControl control, void* buffer, int bufferSize)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            control.ReadValueFromStateIntoBuffer(control.currentStatePtr, buffer, bufferSize);
        }

        /// <summary>
        /// Read the control's default value and return it as an object.
        /// </summary>
        /// <param name="control">Control to read default value from.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        /// <remarks>
        /// This method allocates GC memory and should thus not be used during normal gameplay.
        /// </remarks>
        /// <seealso cref="InputControl.hasDefaultState"/>
        /// <seealso cref="InputControl.defaultStatePtr"/>
        public static unsafe object ReadDefaultValueAsObject(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            return control.ReadValueFromStateAsObject(control.defaultStatePtr);
        }

        /// <summary>
        /// Read the value for the given control from the given event.
        /// </summary>
        /// <param name="control">Control to read value for.</param>
        /// <param name="inputEvent">Event to read value from. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <typeparam name="TValue">Type of value to read.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="inputEvent"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</exception>
        /// <returns>The value for the given control as read out from the given event or <c>default(TValue)</c> if the given
        /// event does not contain a value for the control (e.g. if it is a <see cref="DeltaStateEvent"/> not containing the relevant
        /// portion of the device's state memory).</returns>
        public static TValue ReadValueFromEvent<TValue>(this InputControl<TValue> control, InputEventPtr inputEvent)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!ReadValueFromEvent(control, inputEvent, out var value))
                return default;
            return value;
        }

        /// <summary>
        /// Check if the given event contains a value for the given control and if so, read the value.
        /// </summary>
        /// <param name="control">Control to read value for.</param>
        /// <param name="inputEvent">Input event. This must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.
        /// Note that in the case of a <see cref="DeltaStateEvent"/>, the control may not actually be part of the event. In this
        /// case, the method returns false and stores <c>default(TValue)</c> in <paramref name="value"/>.</param>
        /// <param name="value">Variable that receives the control value.</param>
        /// <typeparam name="TValue">Type of value to read.</typeparam>
        /// <returns>True if the value has been successfully read from the event, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="inputEvent"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</exception>
        /// <seealso cref="ReadUnprocessedValueFromEvent{TValue}(InputControl{TValue},InputEventPtr)"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static unsafe bool ReadValueFromEvent<TValue>(this InputControl<TValue> control, InputEventPtr inputEvent, out TValue value)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var statePtr = control.GetStatePtrFromStateEvent(inputEvent);
            if (statePtr == null)
            {
                value = control.ReadDefaultValue();
                return false;
            }

            value = control.ReadValueFromState(statePtr);
            return true;
        }

        public static TValue ReadUnprocessedValueFromEvent<TValue>(this InputControl<TValue> control, InputEventPtr eventPtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var result = default(TValue);
            control.ReadUnprocessedValueFromEvent(eventPtr, out result);
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public static unsafe bool ReadUnprocessedValueFromEvent<TValue>(this InputControl<TValue> control, InputEventPtr inputEvent, out TValue value)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var statePtr = control.GetStatePtrFromStateEvent(inputEvent);
            if (statePtr == null)
            {
                value = control.ReadDefaultValue();
                return false;
            }

            value = control.ReadUnprocessedValueFromState(statePtr);
            return true;
        }

        public static unsafe void WriteValueFromObjectIntoEvent(this InputControl control, InputEventPtr eventPtr, object value)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == null)
                return;

            control.WriteValueFromObjectIntoState(value, statePtr);
        }

        /// <summary>
        /// Write the control's current value into <paramref name="statePtr"/>.
        /// </summary>
        /// <param name="control">Control to read the current value from and to store state for in <paramref name="statePtr"/>.</param>
        /// <param name="statePtr">State to receive the control's value in its respective <see cref="InputControl.stateBlock"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null or <paramref name="statePtr"/> is null.</exception>
        /// <remarks>
        /// This method is equivalent to <see cref="InputControl{TValue}.WriteValueIntoState"/> except that one does
        /// not have to know the value type of the given control.
        /// </remarks>
        /// <exception cref="NotSupportedException">The control does not support writing. This is the case, for
        /// example, that compute values (such as the magnitude of a vector).</exception>
        /// <seealso cref="InputControl{TValue}.WriteValueIntoState"/>
        public static unsafe void WriteValueIntoState(this InputControl control, void* statePtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            var valueSize = control.valueSizeInBytes;
            var valuePtr = UnsafeUtility.Malloc(valueSize, 8, Allocator.Temp);
            try
            {
                control.ReadValueFromStateIntoBuffer(control.currentStatePtr, valuePtr, valueSize);
                control.WriteValueFromBufferIntoState(valuePtr, valueSize, statePtr);
            }
            finally
            {
                UnsafeUtility.Free(valuePtr, Allocator.Temp);
            }
        }

        public static unsafe void WriteValueIntoState<TValue>(this InputControl control, TValue value, void* statePtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!(control is InputControl<TValue> controlOfType))
                throw new ArgumentException(
                    $"Expecting control of type '{typeof(TValue).Name}' but got '{control.GetType().Name}'");

            controlOfType.WriteValueIntoState(value, statePtr);
        }

        public static unsafe void WriteValueIntoState<TValue>(this InputControl<TValue> control, TValue value, void* statePtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            var valuePtr = UnsafeUtility.AddressOf(ref value);
            var valueSize = UnsafeUtility.SizeOf<TValue>();

            control.WriteValueFromBufferIntoState(valuePtr, valueSize, statePtr);
        }

        public static unsafe void WriteValueIntoState<TValue>(this InputControl<TValue> control, void* statePtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            control.WriteValueIntoState(control.ReadValue(), statePtr);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="state"></param>
        /// <param name="value">Value for <paramref name="control"/> to write into <paramref name="state"/>.</param>
        /// <typeparam name="TState"></typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        /// <exception cref="ArgumentException">Control's value does not fit within the memory of <paramref name="state"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="control"/> does not support writing.</exception>
        public static unsafe void WriteValueIntoState<TValue, TState>(this InputControl<TValue> control, TValue value, ref TState state)
            where TValue : struct
            where TState : struct, IInputStateTypeInfo
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            // Make sure the control's state actually fits within the given state.
            var sizeOfState = UnsafeUtility.SizeOf<TState>();
            if (control.stateOffsetRelativeToDeviceRoot + control.m_StateBlock.alignedSizeInBytes >= sizeOfState)
                throw new ArgumentException(
                    $"Control {control.path} with offset {control.stateOffsetRelativeToDeviceRoot} and size of {control.m_StateBlock.sizeInBits} bits is out of bounds for state of type {typeof(TState).Name} with size {sizeOfState}",
                    nameof(state));

            // Write value.
            var statePtr = (byte*)UnsafeUtility.AddressOf(ref state);
            control.WriteValueIntoState(value, statePtr);
        }

        public static void WriteValueIntoEvent<TValue>(this InputControl control, TValue value, InputEventPtr eventPtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            if (!(control is InputControl<TValue> controlOfType))
                throw new ArgumentException(
                    $"Expecting control of type '{typeof(TValue).Name}' but got '{control.GetType().Name}'");

            controlOfType.WriteValueIntoEvent(value, eventPtr);
        }

        public static unsafe void WriteValueIntoEvent<TValue>(this InputControl<TValue> control, TValue value, InputEventPtr eventPtr)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == null)
                return;

            control.WriteValueIntoState(value, statePtr);
        }

        /// <summary>
        /// Copy the state of the device to the given memory buffer.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <param name="buffer">Buffer to copy the state of the device to.</param>
        /// <param name="bufferSizeInBytes">Size of <paramref name="buffer"/> in bytes.</param>
        /// <exception cref="ArgumentException"><paramref name="bufferSizeInBytes"/> is less than or equal to 0.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The method will copy however much fits into the given buffer. This means that if the state of the device
        /// is larger than what fits into the buffer, not all of the device's state is copied.
        /// </remarks>
        /// <seealso cref="InputControl.stateBlock"/>
        public static unsafe void CopyState(this InputDevice device, void* buffer, int bufferSizeInBytes)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (bufferSizeInBytes <= 0)
                throw new ArgumentException("bufferSizeInBytes must be positive", nameof(bufferSizeInBytes));

            var stateBlock = device.m_StateBlock;
            var sizeToCopy = Math.Min(bufferSizeInBytes, stateBlock.alignedSizeInBytes);

            UnsafeUtility.MemCpy(buffer, (byte*)device.currentStatePtr + stateBlock.byteOffset, sizeToCopy);
        }

        /// <summary>
        /// Copy the state of the device to the given struct.
        /// </summary>
        /// <param name="device">An input device.</param>
        /// <param name="state">Struct to copy the state of the device into.</param>
        /// <typeparam name="TState">A state struct type such as <see cref="MouseState"/>.</typeparam>
        /// <exception cref="ArgumentException">The state format of <typeparamref name="TState"/> does not match
        /// the state form of <paramref name="device"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method will copy memory verbatim into the memory of the given struct. It will copy whatever
        /// memory of the device fits into the given struct.
        /// </remarks>
        /// <seealso cref="InputControl.stateBlock"/>
        public static unsafe void CopyState<TState>(this InputDevice device, out TState state)
            where TState : struct, IInputStateTypeInfo
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            state = default;
            if (device.stateBlock.format != state.format)
                throw new ArgumentException(
                    $"Struct '{typeof(TState).Name}' has state format '{state.format}' which doesn't match device '{device}' with state format '{device.stateBlock.format}'",
                    nameof(TState));

            var stateSize = UnsafeUtility.SizeOf<TState>();
            var statePtr = UnsafeUtility.AddressOf(ref state);

            device.CopyState(statePtr, stateSize);
        }

        /// <summary>
        /// Check whether the memory of the given control is in its default state.
        /// </summary>
        /// <param name="control">An input control on a device that's been added to the system (see <see cref="InputDevice.added"/>).</param>
        /// <returns>True if the state memory of the given control corresponds to the control's default.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method is a cheaper check than actually reading out the value from the control and checking whether it
        /// is the same value as the default value. The method bypasses all value reading and simply performs a trivial
        /// memory comparison of the control's current state memory to the default state memory stored for the control.
        ///
        /// Note that the default state for the memory of a control does not necessary need to be all zeroes. For example,
        /// a stick axis may be stored as an unsigned 8-bit value with the memory state corresponding to a 0 value being 127.
        /// </remarks>
        /// <seealso cref="InputControl.stateBlock"/>
        public static unsafe bool CheckStateIsAtDefault(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            return CheckStateIsAtDefault(control, control.currentStatePtr);
        }

        /// <summary>
        /// Check if the given state corresponds to the default state of the control.
        /// </summary>
        /// <param name="control">Control to check the state for in <paramref name="statePtr"/>.</param>
        /// <param name="statePtr">Pointer to a state buffer containing the <see cref="InputControl.stateBlock"/> for <paramref name="control"/>.</param>
        /// <param name="maskPtr">If not null, only bits set to <c>false</c> (!) in the buffer will be taken into account. This can be used
        /// to mask out noise, i.e. every bit that is set in the mask is considered to represent noise.</param>
        /// <returns>True if the control/device is in its default state.</returns>
        /// <remarks>
        /// Note that default does not equate all zeroes. Stick axes, for example, that are stored as unsigned byte
        /// values will have their resting position at 127 and not at 0. This is why we explicitly store default
        /// state in a memory buffer instead of assuming zeroes.
        /// </remarks>
        /// <seealso cref="InputStateBuffers.defaultStateBuffer"/>
        public static unsafe bool CheckStateIsAtDefault(this InputControl control, void* statePtr, void* maskPtr = null)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            return control.CompareState(statePtr, control.defaultStatePtr, maskPtr);
        }

        public static unsafe bool CheckStateIsAtDefaultIgnoringNoise(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            return control.CheckStateIsAtDefaultIgnoringNoise(control.currentStatePtr);
        }

        public static unsafe bool CheckStateIsAtDefaultIgnoringNoise(this InputControl control, void* statePtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            return control.CheckStateIsAtDefault(statePtr, InputStateBuffers.s_NoiseMaskBuffer);
        }

        /// <summary>
        /// Compare the control's current state to the state stored in <paramref name="statePtr"/>.
        /// </summary>
        /// <param name="statePtr">State memory containing the control's <see cref="stateBlock"/>.</param>
        /// <returns>True if </returns>
        /// <seealso cref="currentStatePtr"/>
        /// <remarks>
        /// This method ignores noise
        ///
        /// This method will not actually read values but will instead compare state directly as it is stored
        /// in memory. <see cref="InputControl{TValue}.ReadValue"/> is not invoked and thus processors will
        /// not be run.
        /// </remarks>
        public static unsafe bool CompareStateIgnoringNoise(this InputControl control, void* statePtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            return control.CompareState(control.currentStatePtr, statePtr, control.noiseMaskPtr);
        }

        /// <summary>
        /// Compare the control's stored state in <paramref name="firstStatePtr"/> to <paramref name="secondStatePtr"/>.
        /// </summary>
        /// <param name="firstStatePtr">Memory containing the control's <see cref="InputControl.stateBlock"/>.</param>
        /// <param name="secondStatePtr">Memory containing the control's <see cref="InputControl.stateBlock"/></param>
        /// <param name="maskPtr">Optional mask. If supplied, it will be used to mask the comparison between
        /// <paramref name="firstStatePtr"/> and <paramref name="secondStatePtr"/> such that any bit not set in the
        /// mask will be ignored even if different between the two states. This can be used, for example, to ignore
        /// noise in the state (<see cref="InputControl.noiseMaskPtr"/>).</param>
        /// <returns>True if the state is equivalent in both memory buffers.</returns>
        /// <remarks>
        /// Unlike <see cref="InputControl.CompareValue"/>, this method only compares raw memory state. If used on a stick, for example,
        /// it may mean that this method returns false for two stick values that would compare equal using <see cref="CompareValue"/>
        /// (e.g. if both stick values fall below the deadzone).
        /// </remarks>
        /// <seealso cref="InputControl.CompareValue"/>
        public static unsafe bool CompareState(this InputControl control, void* firstStatePtr, void* secondStatePtr, void* maskPtr = null)
        {
            ////REVIEW: for compound controls, do we want to go check leaves so as to not pick up on non-control noise in the state?
            ////        e.g. from HID input reports; or should we just leave that to maskPtr?

            var firstPtr = (byte*)firstStatePtr + (int)control.m_StateBlock.byteOffset;
            var secondPtr = (byte*)secondStatePtr + (int)control.m_StateBlock.byteOffset;
            var mask = maskPtr != null ? (byte*)maskPtr + (int)control.m_StateBlock.byteOffset : null;

            if (control.m_StateBlock.sizeInBits == 1)
            {
                // If we have a mask and the bit is set in the mask, the control is to be ignored
                // and thus we consider it at default value.
                if (mask != null && MemoryHelpers.ReadSingleBit(mask, control.m_StateBlock.bitOffset))
                    return true;

                return MemoryHelpers.ReadSingleBit(secondPtr, control.m_StateBlock.bitOffset) ==
                    MemoryHelpers.ReadSingleBit(firstPtr, control.m_StateBlock.bitOffset);
            }

            return MemoryHelpers.MemCmpBitRegion(firstPtr, secondPtr,
                control.m_StateBlock.bitOffset, control.m_StateBlock.sizeInBits, mask);
        }

        public static unsafe bool CompareState(this InputControl control, void* statePtr, void* maskPtr = null)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            return control.CompareState(control.currentStatePtr, statePtr, maskPtr);
        }

        /// <summary>
        /// Return true if the current value of <paramref name="control"/> is different to the one found
        /// in <paramref name="statePtr"/>.
        /// </summary>
        /// <param name="control">Control whose state to compare to what is stored in <paramref name="statePtr"/>.</param>
        /// <param name="statePtr">A block of input state memory containing the <see cref="InputControl.stateBlock"/>
        /// of <paramref name="control."/></param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c> or <paramref name="statePtr"/>
        /// is <c>null</c>.</exception>
        /// <returns>True if the value of <paramref name="control"/> stored in <paramref name="statePtr"/> is different
        /// compared to what <see cref="InputControl{T}.ReadValue"/> of the control returns.</returns>
        public static unsafe bool HasValueChangeInState(this InputControl control, void* statePtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (statePtr == null)
                throw new ArgumentNullException(nameof(statePtr));

            return control.CompareValue(control.currentStatePtr, statePtr);
        }

        public static unsafe bool HasValueChangeInEvent(this InputControl control, InputEventPtr eventPtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            return control.CompareValue(control.currentStatePtr, control.GetStatePtrFromStateEvent(eventPtr));
        }

        /// <summary>
        /// Given a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>, return the raw memory pointer that can
        /// be used, for example, with <see cref="InputControl{T}.ReadValueFromState"/> to read out the value of <paramref name="control"/>
        /// contained in the event.
        /// </summary>
        /// <param name="control">Control to access state for in the given state event.</param>
        /// <param name="eventPtr">A <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/> containing input state.</param>
        /// <returns>A pointer that can be used with methods such as <see cref="InputControl{TValue}.ReadValueFromState"/> or <c>null</c>
        /// if <paramref name="eventPtr"/> does not contain state for the given <paramref name="control"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c> -or- <paramref name="eventPtr"/> is invalid.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</exception>
        /// <remarks>
        /// Note that the given state event must have the same state format (see <see cref="InputStateBlock.format"/>) as the device
        /// to which <paramref name="control"/> belongs. If this is not the case, the method will invariably return <c>null</c>.
        ///
        /// In practice, this means that the method cannot be used with touch events or, in general, with events sent to devices
        /// that implement <see cref="IInputStateCallbackReceiver"/> (which <see cref="Touchscreen"/> does) except if the event
        /// is in the same state format as the device. Touch events will generally be sent as state events containing only the
        /// state of a single <see cref="Controls.TouchControl"/>, not the state of the entire <see cref="Touchscreen"/>.
        /// </remarks>
        public static unsafe void* GetStatePtrFromStateEvent(this InputControl control, InputEventPtr eventPtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            uint stateOffset;
            FourCC stateFormat;
            uint stateSizeInBytes;
            void* statePtr;
            if (eventPtr.IsA<DeltaStateEvent>())
            {
                var deltaEvent = DeltaStateEvent.From(eventPtr);

                // If it's a delta event, we need to subtract the delta state offset if it's not set to the root of the device
                stateOffset = deltaEvent->stateOffset;
                stateFormat = deltaEvent->stateFormat;
                stateSizeInBytes = deltaEvent->deltaStateSizeInBytes;
                statePtr = deltaEvent->deltaState;
            }
            else if (eventPtr.IsA<StateEvent>())
            {
                var stateEvent = StateEvent.From(eventPtr);

                stateOffset = 0;
                stateFormat = stateEvent->stateFormat;
                stateSizeInBytes = stateEvent->stateSizeInBytes;
                statePtr = stateEvent->state;
            }
            else
            {
                throw new ArgumentException("Event must be a state or delta state event", nameof(eventPtr));
            }

            // Make sure we have a state event compatible with our device. The event doesn't
            // have to be specifically for our device (we don't require device IDs to match) but
            // the formats have to match and the size must be within range of what we're trying
            // to read.
            var device = control.device;
            if (stateFormat != device.m_StateBlock.format)
            {
                // If the device is an IInputStateCallbackReceiver, there's a chance it actually recognizes
                // the state format in the event and can correlate it to the state as found on the device.
                if (!device.hasStateCallbacks ||
                    !((IInputStateCallbackReceiver)device).GetStateOffsetForEvent(control, eventPtr, ref stateOffset))
                    return null;
            }

            // Once a device has been added, global state buffer offsets are baked into control hierarchies.
            // We need to unsubtract those offsets here.
            // NOTE: If the given device has not actually been added to the system, the offset is simply 0
            //       and this is a harmless NOP.
            stateOffset += device.m_StateBlock.byteOffset;

            // Return null if state is out of range.
            var controlOffset = (int)control.m_StateBlock.byteOffset - stateOffset;
            if (controlOffset < 0 || controlOffset + control.m_StateBlock.alignedSizeInBytes > stateSizeInBytes)
                return null;

            return (byte*)statePtr - (int)stateOffset;
        }

        /// <summary>
        /// Queue a value change on the given <paramref name="control"/> which will be processed and take effect
        /// in the next input update.
        /// </summary>
        /// <param name="control">Control to change the value of.</param>
        /// <param name="value">New value for the control.</param>
        /// <param name="time">Optional time at which the value change should take effect. If set, this will become
        /// the <see cref="InputEvent.time"/> of the queued event. If the time is in the future, the event will not
        /// be processed until it falls within the time of an input update slice (except if <see cref="InputSettings.timesliceEvents"/>
        /// is false, in which case the event will invariably be consumed in the next update).</param>
        /// <typeparam name="TValue">Type of value.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        public static void QueueValueChange<TValue>(this InputControl<TValue> control, TValue value, double time = -1)
            where TValue : struct
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            ////TODO: if it's not a bit-addressing control, send a delta state change only
            using (StateEvent.From(control.device, out var eventPtr))
            {
                if (time >= 0)
                    eventPtr.time = time;
                control.WriteValueIntoEvent(value, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
        }

        /// <summary>
        /// Modify <paramref name="newState"/> to write an accumulated value of the control
        /// rather than the value currently found in the event.
        /// </summary>
        /// <param name="control">Control to perform the accumulation on.</param>
        /// <param name="currentStatePtr">Memory containing the control's current state. See <see
        /// cref="InputControl.currentStatePtr"/>.</param>
        /// <param name="newState">Event containing the new state about to be written to the device.</param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method reads the current, unprocessed value of the control from <see cref="currentStatePtr"/>
        /// and then adds it to the value of the control found in <paramref name="newState"/>.
        ///
        /// Note that the method does nothing if a value for the control is not contained in <paramref name="newState"/>.
        /// This can be the case, for example, for <see cref="DeltaStateEvent"/>s.
        /// </remarks>
        /// <seealso cref="Pointer.delta"/>
        public static unsafe void AccumulateValueInEvent(this InputControl<float> control, void* currentStatePtr, InputEventPtr newState)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!control.ReadUnprocessedValueFromEvent(newState, out var newDelta))
                return; // Value for the control not contained in the given event.

            var oldDelta = control.ReadUnprocessedValueFromState(currentStatePtr);
            control.WriteValueIntoEvent(oldDelta + newDelta, newState);
        }

        public static void FindControlsRecursive<TControl>(this InputControl parent, IList<TControl> controls, Func<TControl, bool> predicate)
            where TControl : InputControl
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (controls == null)
                throw new ArgumentNullException(nameof(controls));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            if (parent is TControl parentAsTControl && predicate(parentAsTControl))
                controls.Add(parentAsTControl);

            var children = parent.children;
            var childCount = children.Count;
            for (var i = 0; i < childCount; ++i)
            {
                var child = parent.children[i];
                FindControlsRecursive(child, controls, predicate);
            }
        }

        internal static string BuildPath(this InputControl control, string deviceLayout, StringBuilder builder = null)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (string.IsNullOrEmpty(deviceLayout))
                throw new ArgumentNullException(nameof(deviceLayout));

            if (builder == null)
                builder = new StringBuilder();

            var device = control.device;

            builder.Append('<');
            builder.Append(deviceLayout);
            builder.Append('>');

            // Add usages of device, if any.
            var deviceUsages = device.usages;
            for (var i = 0; i < deviceUsages.Count; ++i)
            {
                builder.Append('{');
                builder.Append(deviceUsages[i]);
                builder.Append('}');
            }

            builder.Append('/');

            var devicePath = device.path;
            var controlPath = control.path;
            builder.Append(controlPath, devicePath.Length + 1, controlPath.Length - devicePath.Length - 1);

            return builder.ToString();
        }
    }
}
