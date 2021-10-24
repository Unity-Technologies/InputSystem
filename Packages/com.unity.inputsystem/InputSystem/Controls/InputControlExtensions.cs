using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
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

        /// <summary>
        /// Read the value of <paramref name="control"/> from the given <paramref name="inputEvent"/> without having to
        /// know the specific value type of the control.
        /// </summary>
        /// <param name="control">Control to read the value for.</param>
        /// <param name="inputEvent">An <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/> to read the value from.</param>
        /// <returns>The current value for the control or <c>null</c> if the control's value is not included
        /// in the event.</returns>
        /// <seealso cref="InputControl.ReadValueFromStateAsObject"/>
        public static unsafe object ReadValueFromEventAsObject(this InputControl control, InputEventPtr inputEvent)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            var statePtr = control.GetStatePtrFromStateEvent(inputEvent);
            if (statePtr == null)
                return control.ReadDefaultValueAsObject();

            return control.ReadValueFromStateAsObject(statePtr);
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

        ////REVIEW: this has the opposite argument order of WriteValueFromObjectIntoState; fix!
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
        /// <seealso cref="InputControl{TValue}.ReadDefaultValue"/>
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

        /// <summary>
        /// Check if the given state corresponds to the default state of the control or has different state only
        /// for parts marked as noise.
        /// </summary>
        /// <param name="control">Control to check the state for in <paramref name="statePtr"/>.</param>
        /// <param name="statePtr">Pointer to a state buffer containing the <see cref="InputControl.stateBlock"/> for <paramref name="control"/>.</param>
        /// <returns>True if the control/device is in its default state (ignoring any bits marked as noise).</returns>
        /// <remarks>
        /// Note that default does not equate all zeroes. Stick axes, for example, that are stored as unsigned byte
        /// values will have their resting position at 127 and not at 0. This is why we explicitly store default
        /// state in a memory buffer instead of assuming zeroes.
        /// </remarks>
        /// <seealso cref="InputControl{TValue}.ReadDefaultValue"/>
        /// <seealso cref="InputControl.noisy"/>
        /// <seealso cref="InputControl.noiseMaskPtr"/>
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
        /// <param name="statePtr">State memory containing the control's <see cref="InputControl.stateBlock"/>.</param>
        /// <returns>True if </returns>
        /// <seealso cref="InputControl.currentStatePtr"/>
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
        /// it may mean that this method returns false for two stick values that would compare equal using <see cref="InputControl.CompareValue"/>
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

        /// <summary>
        /// Return true if <paramref name="control"/> has a different value (from its current one) in
        /// <paramref name="eventPtr"/>.
        /// </summary>
        /// <param name="control">An input control.</param>
        /// <param name="eventPtr">An input event. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <returns>True if <paramref name="eventPtr"/> contains a value for <paramref name="control"/> that is different
        /// from the control's current value (see <see cref="InputControl{TValue}.ReadValue"/>).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c> -or- <paramref name="eventPtr"/> is a <c>null</c> pointer (see <see cref="InputEventPtr.valid"/>).</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</exception>
        public static unsafe bool HasValueChangeInEvent(this InputControl control, InputEventPtr eventPtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            var statePtr = control.GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == null)
                return false;

            return control.CompareValue(control.currentStatePtr, statePtr);
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

            return GetStatePtrFromStateEventUnchecked(control, eventPtr, eventPtr.type);
        }

        internal static unsafe void* GetStatePtrFromStateEventUnchecked(this InputControl control, InputEventPtr eventPtr, FourCC eventType)
        {
            uint stateOffset;
            FourCC stateFormat;
            uint stateSizeInBytes;
            void* statePtr;

            if (eventType == StateEvent.Type)
            {
                var stateEvent = StateEvent.FromUnchecked(eventPtr);

                stateOffset = 0;
                stateFormat = stateEvent->stateFormat;
                stateSizeInBytes = stateEvent->stateSizeInBytes;
                statePtr = stateEvent->state;
            }
            else if (eventType == DeltaStateEvent.Type)
            {
                var deltaEvent = DeltaStateEvent.FromUnchecked(eventPtr);

                // If it's a delta event, we need to subtract the delta state offset if it's not set to the root of the device
                stateOffset = deltaEvent->stateOffset;
                stateFormat = deltaEvent->stateFormat;
                stateSizeInBytes = deltaEvent->deltaStateSizeInBytes;
                statePtr = deltaEvent->deltaState;
            }
            else
            {
                throw new ArgumentException($"Event must be a StateEvent or DeltaStateEvent but is a {eventType} instead",
                    nameof(eventPtr));
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
            ref var controlStateBlock = ref control.m_StateBlock;
            var controlOffset = (int)controlStateBlock.effectiveByteOffset - stateOffset;
            if (controlOffset < 0 || controlOffset + controlStateBlock.alignedSizeInBytes > stateSizeInBytes)
                return null;

            return (byte*)statePtr - (int)stateOffset;
        }

        /// <summary>
        /// Writes the default state of <paramref name="control"/> into <paramref name="eventPtr"/>.
        /// </summary>
        /// <param name="control">A control whose default state to write.</param>
        /// <param name="eventPtr">A valid pointer to either a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is <c>null</c> -or- <paramref name="eventPtr"/> contains
        /// a null pointer.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</exception>
        /// <returns>True if the default state for <paramref name="control"/> was written to <paramref name="eventPtr"/>, false if the
        /// given state or delta state event did not include memory for the given control.</returns>
        /// <remarks>
        /// Note that the default state of a control does not necessarily need to correspond to zero-initialized memory. For example, if
        /// an axis control yields a range of [-1..1] and is stored as a signed 8-bit value, the default state will be 127, not 0.
        ///
        /// <example>
        /// <code>
        /// // Reset the left gamepad stick to its default state (which results in a
        /// // value of (0,0).
        /// using (StateEvent.From(Gamepad.all[0], out var eventPtr))
        /// {
        ///     Gamepad.all[0].leftStick.ResetToDefaultStateInEvent(eventPtr);
        ///     InputSystem.QueueEvent(eventPtr);
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControl.defaultStatePtr"/>
        public static unsafe bool ResetToDefaultStateInEvent(this InputControl control, InputEventPtr eventPtr)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr));

            var eventType = eventPtr.type;
            if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                throw new ArgumentException("Given event is not a StateEvent or a DeltaStateEvent", nameof(eventPtr));

            var statePtr = (byte*)control.GetStatePtrFromStateEvent(eventPtr);
            if (statePtr == null)
                return false;

            var defaultStatePtr = (byte*)control.defaultStatePtr;
            ref var stateBlock = ref control.m_StateBlock;
            var offset = stateBlock.byteOffset;

            MemoryHelpers.MemCpyBitRegion(statePtr + offset, defaultStatePtr + offset, stateBlock.bitOffset, stateBlock.sizeInBits);
            return true;
        }

        /// <summary>
        /// Queue a value change on the given <paramref name="control"/> which will be processed and take effect
        /// in the next input update.
        /// </summary>
        /// <param name="control">Control to change the value of.</param>
        /// <param name="value">New value for the control.</param>
        /// <param name="time">Optional time at which the value change should take effect. If set, this will become
        /// the <see cref="InputEvent.time"/> of the queued event. If the time is in the future and the update mode is
        /// set to <see cref="InputSettings.UpdateMode.ProcessEventsInFixedUpdate"/>, the event will not be processed until
        /// it falls within the time of an input update slice. Otherwise, the event will invariably be consumed in the
        /// next input update (see <see cref="InputSystem.Update"/>).</param>
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
        /// This method reads the current, unprocessed value of the control from <see cref="InputControl.currentStatePtr"/>
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

            if (!control.ReadUnprocessedValueFromEvent(newState, out var newValue))
                return; // Value for the control not contained in the given event.

            var oldValue = control.ReadUnprocessedValueFromState(currentStatePtr);
            control.WriteValueIntoEvent(oldValue + newValue, newState);
        }

        internal static unsafe void AccumulateValueInEvent(this InputControl<Vector2> control, void* currentStatePtr, InputEventPtr newState)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (!control.ReadUnprocessedValueFromEvent(newState, out var newValue))
                return; // Value for the control not contained in the given event.

            var oldDelta = control.ReadUnprocessedValueFromState(currentStatePtr);
            control.WriteValueIntoEvent(oldDelta + newValue, newState);
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

        /// <summary>
        /// Flags that control which controls are returned by <see cref="InputControlExtensions.EnumerateControls"/>.
        /// </summary>
        [Flags]
        public enum Enumerate
        {
            /// <summary>
            /// Ignore controls whose value is at default (see <see cref="InputControl{TValue}.ReadDefaultValue"/>).
            /// </summary>
            IgnoreControlsInDefaultState = 1 << 0,

            /// <summary>
            /// Ignore controls whose value is the same as their current value (see <see cref="InputControl{TValue}.ReadValue"/>).
            /// </summary>
            IgnoreControlsInCurrentState = 1 << 1,

            /// <summary>
            /// Include controls that are <see cref="InputControl.synthetic"/> and/or use state from other other controls (see
            /// <see cref="Layouts.InputControlLayout.ControlItem.useStateFrom"/>). These are excluded by default.
            /// </summary>
            IncludeSyntheticControls = 1 << 2,

            /// <summary>
            /// Include noisy controls (see <see cref="InputControl.noisy"/>). These are excluded by default.
            /// </summary>
            IncludeNoisyControls = 1 << 3,

            /// <summary>
            /// For any leaf control returned by the enumeration, also return all the parent controls (see <see cref="InputControl.parent"/>)
            /// in turn (but not the root <see cref="InputDevice"/> itself).
            /// </summary>
            IncludeNonLeafControls = 1 << 4,
        }

        /// <summary>
        /// Go through the controls that have values in <paramref name="eventPtr"/>, apply the given filters, and return each
        /// control one by one.
        /// </summary>
        /// <param name="eventPtr">An input event. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <param name="flags">Filter settings that determine which controls to return.</param>
        /// <param name="device">Input device from which to enumerate controls. If this is <c>null</c>, then the <see cref="InputEvent.deviceId"/>
        /// from the given <paramref name="eventPtr"/> will be used to locate the device via  <see cref="InputSystem.GetDeviceById"/>. If the device
        /// cannot be found, an exception will be thrown. Note that type of device must match the state stored in the given event.</param>
        /// <param name="magnitudeThreshold">If not zero, minimum actuation threshold (see <see cref="InputControl.EvaluateMagnitude()"/>) that
        /// a control must reach (as per value in the given <paramref name="eventPtr"/>) in order for it to be included in the enumeration.</param>
        /// <returns>An enumerator for the controls with values in <paramref name="eventPtr"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eventPtr"/> is a <c>null</c> pointer (see <see cref="InputEventPtr.valid"/>).</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> and not a <see cref="DeltaStateEvent"/> -or-
        /// <paramref name="device"/> is <c>null</c> and no device was found with a <see cref="InputDevice.deviceId"/> matching that of <see cref="InputEvent.deviceId"/>
        /// found in <paramref name="eventPtr"/>.</exception>
        /// <remarks>
        /// This method is much more efficient than manually iterating over the controls of <paramref name="device"/> and locating
        /// the ones that have changed in <paramref name="eventPtr"/>. See <see cref="InputEventControlEnumerator"/> for details.
        ///
        /// This method will not allocate GC memory and can safely be used with <c>foreach</c> loops.
        /// </remarks>
        /// <seealso cref="InputEventControlEnumerator"/>
        /// <seealso cref="StateEvent"/>
        /// <seealso cref="DeltaStateEvent"/>
        /// <seealso cref="EnumerateChangedControls"/>
        public static InputEventControlCollection EnumerateControls(this InputEventPtr eventPtr, Enumerate flags, InputDevice device = null, float magnitudeThreshold = 0)
        {
            if (!eventPtr.valid)
                throw new ArgumentNullException(nameof(eventPtr), "Given event pointer must not be null");

            var eventType = eventPtr.type;
            if (eventType != StateEvent.Type && eventType != DeltaStateEvent.Type)
                throw new ArgumentException($"Event must be a StateEvent or DeltaStateEvent but is a {eventType} instead", nameof(eventPtr));

            // Look up device from event, if no device was supplied.
            if (device == null)
            {
                var deviceId = eventPtr.deviceId;
                device = InputSystem.GetDeviceById(deviceId);
                if (device == null)
                    throw new ArgumentException($"Cannot find device with ID {deviceId} referenced by event", nameof(eventPtr));
            }

            return new InputEventControlCollection { m_Device = device, m_EventPtr = eventPtr, m_Flags = flags, m_MagnitudeThreshold = magnitudeThreshold };
        }

        /// <summary>
        /// Go through all controls in the given <paramref name="eventPtr"/> that have changed value.
        /// </summary>
        /// <param name="eventPtr">An input event. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <param name="device">Input device from which to enumerate controls. If this is <c>null</c>, then the <see cref="InputEvent.deviceId"/>
        /// from the given <paramref name="eventPtr"/> will be used to locate the device via  <see cref="InputSystem.GetDeviceById"/>. If the device
        /// cannot be found, an exception will be thrown. Note that type of device must match the state stored in the given event.</param>
        /// <param name="magnitudeThreshold">If not zero, minimum actuation threshold (see <see cref="InputControl.EvaluateMagnitude()"/>) that
        /// a control must reach (as per value in the given <paramref name="eventPtr"/>) in order for it to be included in the enumeration.</param>
        /// <returns>An enumerator for the controls that have changed values in <paramref name="eventPtr"/>.</returns>
        /// <remarks>
        /// This method is a shorthand for calling <see cref="EnumerateControls"/> with <see cref="Enumerate.IgnoreControlsInCurrentState"/>.
        ///
        /// <example>
        /// <code>
        /// // Detect button presses.
        /// InputSystem.onEvent +=
        ///   (eventPtr, device) =>
        ///   {
        ///       // Ignore anything that is not a state event.
        ///       var eventType = eventPtr.type;
        ///       if (eventType != StateEvent.Type &amp;&amp; eventType != DeltaStateEvent.Type)
        ///           return;
        ///
        ///       // Find all changed controls actuated above the button press threshold.
        ///       foreach (var control in eventPtr.EnumerateChangedControls
        ///           (device: device, magnitudeThreshold: InputSystem.settings.defaultButtonPressThreshold))
        ///           if (control is ButtonControl button)
        ///               Debug.Log($"Button {button} was pressed");
        ///   }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputSystem.onEvent"/>
        /// <seealso cref="EnumerateControls"/>
        /// <seealso cref="InputEventControlEnumerator"/>
        public static InputEventControlCollection EnumerateChangedControls(this InputEventPtr eventPtr, InputDevice device = null, float magnitudeThreshold = 0)
        {
            return eventPtr.EnumerateControls
                    (Enumerate.IgnoreControlsInCurrentState, device, magnitudeThreshold);
        }

        /// <summary>
        /// Return true if the given <paramref name="eventPtr"/> has any <see cref="Input"/>
        /// </summary>
        /// <param name="eventPtr"></param>
        /// <param name="magnitude"></param>
        /// <param name="buttonControlsOnly"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="eventPtr"/> is a <c>null</c> pointer.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/> -or-
        /// the <see cref="InputDevice"/> referenced by the <see cref="InputEvent.deviceId"/> in the event cannot be found.</exception>
        /// <seealso cref="EnumerateChangedControls"/>
        /// <seealso cref="ButtonControl.isPressed"/>
        public static bool HasButtonPress(this InputEventPtr eventPtr, float magnitude = -1, bool buttonControlsOnly = true)
        {
            return eventPtr.GetFirstButtonPressOrNull(magnitude, buttonControlsOnly) != null;
        }

        /// <summary>
        /// Get the first pressed button from the given event or null if the event doesn't contain a new button press.
        /// </summary>
        /// <param name="eventPtr"></param>
        /// <param name="magnitude"></param>
        /// <param name="buttonControlsOnly"></param>
        /// <returns>The control that was pressed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eventPtr"/> is a <c>null</c> pointer.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/> -or-
        /// the <see cref="InputDevice"/> referenced by the <see cref="InputEvent.deviceId"/> in the event cannot be found.</exception>
        /// <seealso cref="EnumerateChangedControls"/>
        /// <seealso cref="ButtonControl.isPressed"/>
        /// <remarks>Buttons will be evaluated in the order that they appear in the devices layout i.e. the bit position of each control
        /// in the devices state memory. For example, in the gamepad state, button north (bit position 4) will be evaluated before button
        /// east (bit position 5), so if both buttons were pressed in the given event, button north would be returned.</remarks>
        public static InputControl GetFirstButtonPressOrNull(this InputEventPtr eventPtr, float magnitude = -1, bool buttonControlsOnly = true)
        {
            if (magnitude < 0)
                magnitude = InputSystem.settings.defaultButtonPressPoint;

            foreach (var control in eventPtr.EnumerateControls(Enumerate.IgnoreControlsInDefaultState, magnitudeThreshold: magnitude))
            {
                if (buttonControlsOnly && !control.isButton)
                    continue;
                return control;
            }
            return null;
        }

        /// <summary>
        /// Enumerate all pressed buttons in the given event.
        /// </summary>
        /// <param name="eventPtr">An event. Must be a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.</param>
        /// <param name="magnitude"></param>
        /// <param name="buttonControlsOnly"></param>
        /// <returns>An enumerable collection containing all buttons that were pressed in the given event.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eventPtr"/> is a <c>null</c> pointer.</exception>
        /// <exception cref="ArgumentException"><paramref name="eventPtr"/> is not a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/> -or-
        /// the <see cref="InputDevice"/> referenced by the <see cref="InputEvent.deviceId"/> in the event cannot be found.</exception>
        /// <seealso cref="EnumerateChangedControls"/>
        /// <seealso cref="ButtonControl.isPressed"/>
        public static IEnumerable<InputControl> GetAllButtonPresses(this InputEventPtr eventPtr, float magnitude = -1, bool buttonControlsOnly = true)
        {
            if (magnitude < 0)
                magnitude = InputSystem.settings.defaultButtonPressPoint;

            foreach (var control in eventPtr.EnumerateControls(Enumerate.IgnoreControlsInDefaultState, magnitudeThreshold: magnitude))
            {
                if (buttonControlsOnly && !control.isButton)
                    continue;
                yield return control;
            }
        }

        /// <summary>
        /// Allows iterating over the controls referenced by an <see cref="InputEvent"/> via <see cref="InputEventControlEnumerator"/>.
        /// </summary>
        /// <seealso cref="InputControlExtensions.EnumerateControls"/>
        /// <seealso cref="InputControlExtensions.EnumerateChangedControls"/>
        public struct InputEventControlCollection : IEnumerable<InputControl>
        {
            internal InputDevice m_Device;
            internal InputEventPtr m_EventPtr;
            internal Enumerate m_Flags;
            internal float m_MagnitudeThreshold;

            /// <summary>
            /// The event being iterated over. A <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>.
            /// </summary>
            public InputEventPtr eventPtr => m_EventPtr;

            /// <summary>
            /// Enumerate the controls in the event.
            /// </summary>
            /// <returns>An enumerator.</returns>
            public InputEventControlEnumerator GetEnumerator()
            {
                return new InputEventControlEnumerator(m_EventPtr, m_Device, m_Flags, m_MagnitudeThreshold);
            }

            IEnumerator<InputControl> IEnumerable<InputControl>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Iterates over the controls in a <see cref="StateEvent"/> or <see cref="DeltaStateEvent"/>
        /// while optionally applying certain filtering criteria.
        /// </summary>
        /// <remarks>
        /// One problem with state events (that is, <see cref="StateEvent"/> and <see cref="DeltaStateEvent"/>)
        /// is that they contain raw blocks of memory which may contain state changes for arbitrary many
        /// controls on a device at the same time. Locating individual controls and determining which have
        /// changed state and how can thus be quite inefficient.
        ///
        /// This helper aims to provide an easy and efficient way to iterate over controls relevant to a
        /// given state event. Instead of iterating over the controls of a device looking for the ones
        /// relevant to a given event, enumeration is done the opposite by efficiently searching through
        /// the memory contained in an event and then mapping data found in the event back to controls
        /// on a given device.
        ///
        /// <example>
        /// <code>
        /// // Inefficient:
        /// foreach (var control in device.allControls)
        /// {
        ///     // Skip control if it is noisy, synthetic, or not a leaf control.
        ///     if (control.synthetic || control.noisy || control.children.Count > 0)
        ///         continue;
        ///
        ///     // Locate the control in the event.
        ///     var statePtr = eventPtr.GetStatePtrFromStateEvent(eventPtr);
        ///     if (statePtr == null)
        ///         continue; // Control not included in event.
        ///
        ///     // See if the control is actuated beyond a minimum threshold.
        ///     if (control.EvaluateMagnitude(statePtr) &lt; 0.001f)
        ///         continue;
        ///
        ///     Debug.Log($"Found actuated control {control}");
        /// }
        ///
        /// // Much more efficient:
        /// foreach (var control in eventPtr.EnumerateControls(
        ///     InputControlExtensions.Enumerate.IgnoreControlsInDefaultState,
        ///     device: device,
        ///     magnitudeThreshold: 0.001f))
        /// {
        ///     Debug.Log($"Found actuated control {control}");
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="InputControlExtensions.EnumerateControls"/>
        /// <seealso cref="InputControlExtensions.EnumerateChangedControls"/>
        public unsafe struct InputEventControlEnumerator : IEnumerator<InputControl>
        {
            private Enumerate m_Flags;
            private readonly InputDevice m_Device;
            private readonly uint[] m_StateOffsetToControlIndex;
            private readonly int m_StateOffsetToControlIndexLength;
            private readonly InputControl[] m_AllControls;
            private byte* m_DefaultState; // Already offset by device offset.
            private byte* m_CurrentState;
            private byte* m_NoiseMask; // Already offset by device offset.
            private InputEventPtr m_EventPtr;
            private InputControl m_CurrentControl;
            private int m_CurrentIndexInStateOffsetToControlIndexMap;
            private uint m_CurrentControlStateBitOffset;
            private byte* m_EventState;
            private uint m_CurrentBitOffset;
            private uint m_EndBitOffset;
            private float m_MagnitudeThreshold;

            internal InputEventControlEnumerator(InputEventPtr eventPtr, InputDevice device, Enumerate flags, float magnitudeThreshold = 0)
            {
                Debug.Assert(eventPtr.valid, "eventPtr should be valid at this point");
                Debug.Assert(device != null, "Need to have valid device at this point");

                m_Device = device;
                m_StateOffsetToControlIndex = device.m_StateOffsetToControlMap;
                m_StateOffsetToControlIndexLength = m_StateOffsetToControlIndex.LengthSafe();
                m_AllControls = device.m_ChildrenForEachControl;
                m_EventPtr = eventPtr;
                m_Flags = flags;
                m_CurrentControl = null;
                m_CurrentIndexInStateOffsetToControlIndexMap = default;
                m_CurrentControlStateBitOffset = 0;
                m_EventState = default;
                m_CurrentBitOffset = default;
                m_EndBitOffset = default;
                m_MagnitudeThreshold = magnitudeThreshold;

                if ((flags & Enumerate.IncludeNoisyControls) == 0)
                    m_NoiseMask = (byte*)device.noiseMaskPtr + device.m_StateBlock.byteOffset;
                else
                    m_NoiseMask = default;

                if ((flags & Enumerate.IgnoreControlsInDefaultState) != 0)
                    m_DefaultState = (byte*)device.defaultStatePtr + device.m_StateBlock.byteOffset;
                else
                    m_DefaultState = default;

                if ((flags & Enumerate.IgnoreControlsInCurrentState) != 0)
                    m_CurrentState = (byte*)device.currentStatePtr + device.m_StateBlock.byteOffset;
                else
                    m_CurrentState = default;

                Reset();
            }

            private bool CheckDefault(uint numBits)
            {
                return MemoryHelpers.MemCmpBitRegion(m_EventState, m_DefaultState, m_CurrentBitOffset, numBits, m_NoiseMask);
            }

            private bool CheckCurrent(uint numBits)
            {
                return MemoryHelpers.MemCmpBitRegion(m_EventState, m_CurrentState, m_CurrentBitOffset, numBits, m_NoiseMask);
            }

            public bool MoveNext()
            {
                if (!m_EventPtr.valid)
                    throw new ObjectDisposedException("Enumerator has already been disposed");

                // If we are to include non-leaf controls and we have a current control, walk
                // up the tree until we reach the device.
                if (m_CurrentControl != null && (m_Flags & Enumerate.IncludeNonLeafControls) != 0)
                {
                    var parent = m_CurrentControl.parent;
                    if (parent != m_Device)
                    {
                        m_CurrentControl = parent;
                        return true;
                    }
                }

                var ignoreDefault = m_DefaultState != null;
                var ignoreCurrent = m_CurrentState != null;

                // Search for the next control that matches our filter criteria.
                while (true)
                {
                    m_CurrentControl = null;

                    // If we are ignoring certain state values, try to skip over as much memory as we can.
                    if (ignoreCurrent || ignoreDefault)
                    {
                        // If we are not byte-aligned, search whatever bits are left in the current byte.
                        if ((m_CurrentBitOffset & 0x7) != 0)
                        {
                            var bitsLeftInByte = (m_CurrentBitOffset + 8) & 0x7;
                            if ((ignoreCurrent && CheckCurrent(bitsLeftInByte))
                                || (ignoreDefault && CheckDefault(bitsLeftInByte)))
                                m_CurrentBitOffset += bitsLeftInByte;
                        }

                        // Search byte by byte.
                        while (m_CurrentBitOffset < m_EndBitOffset)
                        {
                            var byteOffset = m_CurrentBitOffset >> 3;
                            var eventByte = m_EventState[byteOffset];
                            var maskByte = m_NoiseMask != null ? m_NoiseMask[byteOffset] : 0xff;

                            if (ignoreCurrent)
                            {
                                var currentByte = m_CurrentState[byteOffset];
                                if ((currentByte & maskByte) == (eventByte & maskByte))
                                {
                                    m_CurrentBitOffset += 8;
                                    continue;
                                }
                            }

                            if (ignoreDefault)
                            {
                                var defaultByte = m_DefaultState[byteOffset];
                                if ((defaultByte & maskByte) == (eventByte & maskByte))
                                {
                                    m_CurrentBitOffset += 8;
                                    continue;
                                }
                            }

                            break;
                        }
                    }

                    // See if we've reached the end.
                    if (m_CurrentBitOffset >= m_EndBitOffset
                        || m_CurrentIndexInStateOffsetToControlIndexMap >= m_StateOffsetToControlIndexLength) // No more controls.
                        return false;

                    // No, so find the control at the current bit offset.
                    for (;
                         m_CurrentIndexInStateOffsetToControlIndexMap < m_StateOffsetToControlIndexLength;
                         ++m_CurrentIndexInStateOffsetToControlIndexMap)
                    {
                        InputDevice.DecodeStateOffsetToControlMapEntry(
                            m_StateOffsetToControlIndex[m_CurrentIndexInStateOffsetToControlIndexMap],
                            out var controlIndex,
                            out var controlBitOffset,
                            out var controlBitSize);

                        // If the control's bit region lies *before* the memory we're looking at,
                        // skip it.
                        if (controlBitOffset < m_CurrentControlStateBitOffset ||
                            m_CurrentBitOffset >= (controlBitOffset + controlBitSize - m_CurrentControlStateBitOffset))
                            continue;

                        // If the bit region we're looking at lies *before* the current control,
                        // keep searching through memory.
                        if ((controlBitOffset - m_CurrentControlStateBitOffset) >= m_CurrentBitOffset + 8)
                        {
                            // Jump to location of control.
                            m_CurrentBitOffset = controlBitOffset - m_CurrentControlStateBitOffset;
                            break;
                        }

                        // If the control's bit region runs past of what we actually have (may be the case both
                        // with delta events and normal state events), skip it.
                        if (controlBitOffset + controlBitSize - m_CurrentControlStateBitOffset > m_EndBitOffset)
                            continue;

                        // If the control is byte-aligned both in its start offset and its length,
                        // we have what we're looking for.
                        if ((controlBitOffset & 0x7) == 0 && (controlBitSize & 0x7) == 0)
                        {
                            m_CurrentControl = m_AllControls[controlIndex];
                        }
                        else
                        {
                            // Otherwise, we may need to check the bit region specifically for the control.
                            if ((ignoreCurrent && MemoryHelpers.MemCmpBitRegion(m_EventState, m_CurrentState, controlBitOffset - m_CurrentControlStateBitOffset, controlBitSize, m_NoiseMask))
                                || (ignoreDefault && MemoryHelpers.MemCmpBitRegion(m_EventState, m_DefaultState, controlBitOffset - m_CurrentControlStateBitOffset, controlBitSize, m_NoiseMask)))
                                continue;

                            m_CurrentControl = m_AllControls[controlIndex];
                        }

                        if ((m_Flags & Enumerate.IncludeNoisyControls) == 0 && m_CurrentControl.noisy)
                        {
                            m_CurrentControl = null;
                            continue;
                        }

                        if ((m_Flags & Enumerate.IncludeSyntheticControls) == 0)
                        {
                            var controlHasSharedState = (m_CurrentControl.m_ControlFlags &
                                (InputControl.ControlFlags.UsesStateFromOtherControl |
                                    InputControl.ControlFlags.IsSynthetic)) != 0;

                            // Filter out synthetic and useStateFrom controls.
                            if (controlHasSharedState)
                            {
                                m_CurrentControl = null;
                                continue;
                            }
                        }

                        ++m_CurrentIndexInStateOffsetToControlIndexMap;
                        break;
                    }

                    if (m_CurrentControl != null)
                    {
                        // If we are the filter by magnitude, last check is to go let the control evaluate
                        // its magnitude based on the data in the event and if it's too low, keep searching.
                        if (m_MagnitudeThreshold != 0)
                        {
                            var statePtr = m_EventState - (m_CurrentControlStateBitOffset >> 3) - m_Device.m_StateBlock.byteOffset;
                            var magnitude = m_CurrentControl.EvaluateMagnitude(statePtr);
                            if (magnitude >= 0 && magnitude < m_MagnitudeThreshold)
                                continue;
                        }

                        return true;
                    }
                }
            }

            public void Reset()
            {
                if (!m_EventPtr.valid)
                    throw new ObjectDisposedException("Enumerator has already been disposed");

                var eventType = m_EventPtr.type;
                FourCC stateFormat;
                if (eventType == StateEvent.Type)
                {
                    var stateEvent = StateEvent.FromUnchecked(m_EventPtr);
                    m_EventState = (byte*)stateEvent->state;
                    m_EndBitOffset = stateEvent->stateSizeInBytes * 8;
                    m_CurrentBitOffset = 0;
                    stateFormat = stateEvent->stateFormat;
                }
                else if (eventType == DeltaStateEvent.Type)
                {
                    var deltaEvent = DeltaStateEvent.FromUnchecked(m_EventPtr);
                    m_EventState = (byte*)deltaEvent->deltaState - deltaEvent->stateOffset; // We access m_EventState as if it contains a full state event.
                    m_CurrentBitOffset = deltaEvent->stateOffset * 8;
                    m_EndBitOffset = m_CurrentBitOffset + deltaEvent->deltaStateSizeInBytes * 8;
                    stateFormat = deltaEvent->stateFormat;
                }
                else
                {
                    throw new NotSupportedException($"Cannot iterate over controls in event of type '{eventType}'");
                }

                m_CurrentIndexInStateOffsetToControlIndexMap = 0;
                m_CurrentControlStateBitOffset = 0;
                m_CurrentControl = null;

                // If the state format of the event does not match that of the device,
                // we need to go through the IInputStateCallbackReceiver machinery to adapt.
                if (stateFormat != m_Device.m_StateBlock.format)
                {
                    var stateOffset = 0u;
                    if (m_Device.hasStateCallbacks &&
                        ((IInputStateCallbackReceiver)m_Device).GetStateOffsetForEvent(null, m_EventPtr, ref stateOffset))
                    {
                        m_CurrentControlStateBitOffset = stateOffset * 8;
                        if (m_CurrentState != null)
                            m_CurrentState += stateOffset;
                        if (m_DefaultState != null)
                            m_DefaultState += stateOffset;
                        if (m_NoiseMask != null)
                            m_NoiseMask += stateOffset;
                    }
                    else
                        throw new InvalidOperationException(
                            $"{eventType} event with state format {stateFormat} cannot be used with device '{m_Device}'");
                }

                // NOTE: We *could* run a CheckDefault() or even CheckCurrent() over the entire event here to rule
                //       it out entirely. However, we don't do so based on the assumption that *in general* this will
                //       only *add* time. Rationale:
                //
                //       - We assume that it is very rare for devices to send events matching the state the device
                //         already has (i.e. *entire* event is just == current state).
                //       - We assume that it is less common than the opposite for devices to send StateEvents containing
                //         nothing but default state. This happens frequently for the keyboard but is very uncommon for mice,
                //         touchscreens, and gamepads (where the sticks will almost never be exactly at default).
                //       - We assume that for DeltaStateEvents it is in fact quite common to contain only default state but
                //         that since in most cases these will contain state for either a very small set of controls or even
                //         just a single one, the work we do in MoveNext somewhat closely matches that we'd here with a CheckXXX()
                //         call but that we'd add work to every DeltaStateEvent if we were to have the upfront comparison here.
            }

            public void Dispose()
            {
                m_EventPtr = default;
            }

            public InputControl Current => m_CurrentControl;

            object IEnumerator.Current => Current;
        }

        // Undocumented APIs. Meant to be used only by auto-generated, precompiled layouts.
        // These APIs exist solely to keep access to the various properties/fields internal
        // and only allow their contents to be modified in a controlled manner.
        #region Undocumented

        public static ControlBuilder Setup(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (control.isSetupFinished)
                throw new InvalidOperationException($"The setup of {control} cannot be modified; control is already in use");

            return new ControlBuilder { control = control };
        }

        public static DeviceBuilder Setup(this InputDevice device, int controlCount, int usageCount, int aliasCount)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (device.isSetupFinished)
                throw new InvalidOperationException($"The setup of {device} cannot be modified; control is already in use");
            if (controlCount < 1)
                throw new ArgumentOutOfRangeException(nameof(controlCount));
            if (usageCount < 0)
                throw new ArgumentOutOfRangeException(nameof(usageCount));
            if (aliasCount < 0)
                throw new ArgumentOutOfRangeException(nameof(aliasCount));

            device.m_Device = device;

            device.m_ChildrenForEachControl = new InputControl[controlCount];
            if (usageCount > 0)
            {
                device.m_UsagesForEachControl = new InternedString[usageCount];
                device.m_UsageToControl = new InputControl[usageCount];
            }
            if (aliasCount > 0)
                device.m_AliasesForEachControl = new InternedString[aliasCount];

            return new DeviceBuilder { device = device };
        }

        public struct ControlBuilder
        {
            public InputControl control { get; internal set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder At(InputDevice device, int index)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (device == null)
                    throw new ArgumentNullException(nameof(device));
                if (index < 0 || index >= device.m_ChildrenForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));
                #endif
                device.m_ChildrenForEachControl[index] = control;
                control.m_Device = device;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithParent(InputControl parent)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (parent == null)
                    throw new ArgumentNullException(nameof(parent));
                if (parent == control)
                    throw new ArgumentException("Control cannot be its own parent", nameof(parent));
                #endif
                control.m_Parent = parent;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithName(string name)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException(nameof(name));
                #endif
                control.m_Name = new InternedString(name);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithDisplayName(string displayName)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(displayName))
                    throw new ArgumentNullException(nameof(displayName));
                #endif
                control.m_DisplayNameFromLayout = new InternedString(displayName);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithShortDisplayName(string shortDisplayName)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(shortDisplayName))
                    throw new ArgumentNullException(nameof(shortDisplayName));
                #endif
                control.m_ShortDisplayNameFromLayout = new InternedString(shortDisplayName);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithLayout(InternedString layout)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (layout.IsEmpty())
                    throw new ArgumentException("Layout name cannot be empty", nameof(layout));
                #endif
                control.m_Layout = layout;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithUsages(int startIndex, int count)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (startIndex < 0 || startIndex >= control.device.m_UsagesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                if (count < 0 || startIndex + count > control.device.m_UsagesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(count));
                #endif
                control.m_UsageStartIndex = startIndex;
                control.m_UsageCount = count;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithAliases(int startIndex, int count)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (startIndex < 0 || startIndex >= control.device.m_AliasesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                if (count < 0 || startIndex + count > control.device.m_AliasesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(count));
                #endif
                control.m_AliasStartIndex = startIndex;
                control.m_AliasCount = count;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithChildren(int startIndex, int count)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (startIndex < 0 || startIndex >= control.device.m_ChildrenForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                if (count < 0 || startIndex + count > control.device.m_ChildrenForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(count));
                #endif
                control.m_ChildStartIndex = startIndex;
                control.m_ChildCount = count;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithStateBlock(InputStateBlock stateBlock)
            {
                control.m_StateBlock = stateBlock;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithDefaultState(PrimitiveValue value)
            {
                control.m_DefaultState = value;
                control.m_Device.hasControlsWithDefaultState = true;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithMinAndMax(PrimitiveValue min, PrimitiveValue max)
            {
                control.m_MinValue = min;
                control.m_MaxValue = max;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder WithProcessor<TProcessor, TValue>(TProcessor processor)
                where TValue : struct
                where TProcessor : InputProcessor<TValue>
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (processor == null)
                    throw new ArgumentNullException(nameof(processor));
                #endif
                ////REVIEW: have a parameterized version of ControlBuilder<TValue> so we don't need the cast?
                ////TODO: size array to exact needed size before-hand
                ((InputControl<TValue>)control).m_ProcessorStack.Append(processor);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder IsNoisy(bool value)
            {
                control.noisy = value;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder IsSynthetic(bool value)
            {
                control.synthetic = value;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder DontReset(bool value)
            {
                control.dontReset = value;
                if (value)
                    control.m_Device.hasDontResetControls = true;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ControlBuilder IsButton(bool value)
            {
                control.isButton = value;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Finish()
            {
                control.isSetupFinished = true;
            }
        }

        public struct DeviceBuilder
        {
            public InputDevice device { get; internal set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithName(string name)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException(nameof(name));
                #endif
                device.m_Name = new InternedString(name);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithDisplayName(string displayName)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(displayName))
                    throw new ArgumentNullException(nameof(displayName));
                #endif
                device.m_DisplayNameFromLayout = new InternedString(displayName);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithShortDisplayName(string shortDisplayName)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (string.IsNullOrEmpty(shortDisplayName))
                    throw new ArgumentNullException(nameof(shortDisplayName));
                #endif
                device.m_ShortDisplayNameFromLayout = new InternedString(shortDisplayName);
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithLayout(InternedString layout)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (layout.IsEmpty())
                    throw new ArgumentException("Layout name cannot be empty", nameof(layout));
                #endif
                device.m_Layout = layout;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithChildren(int startIndex, int count)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (startIndex < 0 || startIndex >= device.device.m_ChildrenForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                if (count < 0 || startIndex + count > device.device.m_ChildrenForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(count));
                #endif
                device.m_ChildStartIndex = startIndex;
                device.m_ChildCount = count;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithStateBlock(InputStateBlock stateBlock)
            {
                device.m_StateBlock = stateBlock;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder IsNoisy(bool value)
            {
                device.noisy = value;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithControlUsage(int controlIndex, InternedString usage, InputControl control)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (controlIndex < 0 || controlIndex >= device.m_UsagesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(controlIndex));
                if (usage.IsEmpty())
                    throw new ArgumentException(nameof(usage));
                if (control == null)
                    throw new ArgumentNullException(nameof(control));
                #endif
                device.m_UsagesForEachControl[controlIndex] = usage;
                device.m_UsageToControl[controlIndex] = control;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithControlAlias(int controlIndex, InternedString alias)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (controlIndex < 0 || controlIndex >= device.m_AliasesForEachControl.Length)
                    throw new ArgumentOutOfRangeException(nameof(controlIndex));
                if (alias.IsEmpty())
                    throw new ArgumentException(nameof(alias));
                #endif
                device.m_AliasesForEachControl[controlIndex] = alias;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public DeviceBuilder WithStateOffsetToControlIndexMap(uint[] map)
            {
                device.m_StateOffsetToControlMap = map;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Finish()
            {
                device.isSetupFinished = true;
            }
        }

        #endregion
    }
}
