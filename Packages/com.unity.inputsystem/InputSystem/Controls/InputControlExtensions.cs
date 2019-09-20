using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        /// Read the value of the given control from an event.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="inputEvent">Input event. This must be a <see cref="StateEvent"/> or <seealso cref="DeltaStateEvent"/>.
        /// Note that in the case of a <see cref="DeltaStateEvent"/>, the control may not actually be part of the event. In this
        /// case, the method returns false and stores <c>default(TValue)</c> in <paramref name="value"/>.</param>
        /// <param name="value">Variable that receives the control value.</param>
        /// <typeparam name="TValue"></typeparam>
        /// <returns>True if the value has been successfully read from the event, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
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

        public static unsafe bool CheckStateIsAtDefault(this InputControl control)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            return CheckStateIsAtDefault(control, control.currentStatePtr);
        }

        /// <summary>
        /// Check if the given state corresponds to the default state of the control.
        /// </summary>
        /// <param name="statePtr">Pointer to a state buffer containing the <see cref="InputControl.stateBlock"/> for <paramref name="control"/>.</param>
        /// <param name="maskPtr">If not null, only bits set to true in the buffer will be taken into account. This can be used
        /// to mask out noise.</param>
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
        /// Return true if the actual value
        /// </summary>
        /// <param name="statePtr"></param>
        /// <returns></returns>
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
                throw new ArgumentException("Event must be a state or delta state event", "eventPtr");
            }

            // Make sure we have a state event compatible with our device. The event doesn't
            // have to be specifically for our device (we don't require device IDs to match) but
            // the formats have to match and the size must be within range of what we're trying
            // to read.
            var device = control.device;
            if (stateFormat != device.m_StateBlock.format)
                throw new InvalidOperationException(
                    $"Cannot read control '{control.path}' from {eventPtr.type} with format {stateFormat}; device '{device}' expects format {device.m_StateBlock.format}");

            // Once a device has been added, global state buffer offsets are baked into control hierarchies.
            // We need to unsubtract those offsets here.
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
