using System;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A filter for individual devices to check if events or device states contain significant, relevant changes.
    /// </summary>
    /// <remarks>
    /// Irrelevant changes are any updates controls that are tagged as 'noisy', and any state change that turns into
    /// a no-operation change once processors are applied (e.g. in deadzone joystick movements).
    /// </remarks>
    public struct InputNoiseFilter
    {
        /// <summary>
        /// Simple cached value identifying how to filter this element (Bitmask, or individual type filtered).
        /// </summary>
        public enum ElementType
        {
            Unknown = 0,
            EntireControl,
            FloatBelowEpsilon,
            Vector2MagnitudeBelowEpsilon
        }

        /// <summary>
        /// A filter for a single InputControl that gets it's value filtered.
        /// </summary>
        public struct FilterElement
        {
            /// <summary>
            /// The index in InputDevice.allControls for the filtered control.
            /// </summary>
            public int controlIndex;

            /// <summary>
            /// The type of filtering to perform.
            /// </summary>
            public ElementType type;

            /// <summary>
            /// Called when the InputNoiseFilter gets applied to a device, marks out any controls that can be wholly bitmasked.
            /// </summary>
            /// <param name="noiseFilterBuffer">The noise filter buffer for doing whole control filtering.</param>
            /// <param name="device">The device you want to apply filtering to.</param>
            public void Apply(IntPtr noiseFilterBuffer, InputDevice device)
            {
                if (controlIndex >= device.allControls.Count)
                    throw new IndexOutOfRangeException("InputNoiseFilter has array index beyond total size of device's controls");

                var control = device.allControls[controlIndex];
                MemoryHelpers.SetBitsInBuffer(noiseFilterBuffer, control, false);
            }

            /// <summary>
            /// Checking an individual input event for significant InputControl changes.
            /// </summary>
            /// <param name="inputEvent">The input event being checked for changes</param>
            /// <param name="device">The input device being checked against </param>
            /// <returns>True if any changes exist in the event once the device has been filtered through for noise and non-significant
            /// changes.  False otherwise.</returns>
            public bool EventHasValidData(InputEventPtr inputEvent, InputDevice device)
            {
                if (type == ElementType.EntireControl)
                    return false;

                var control = device.allControls[controlIndex];
                if (control == null)
                    return false;

                return control.HasSignificantChange(inputEvent);
            }
        }

        public static unsafe InputNoiseFilter CreateDefaultNoiseFilter(InputDevice device)
        {
            if (device == null)
                throw new ArgumentException("No device supplied to create default noise filter for", "device");

            var filter = new InputNoiseFilter();
            var elementsToAdd = stackalloc FilterElement[device.allControls.Count];
            var elementCount = 0;
            var controls = device.allControls;
            for (var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                if (control.noisy)
                {
                    FilterElement newElement;
                    newElement.controlIndex = i;
                    newElement.type = ElementType.EntireControl;
                    elementsToAdd[elementCount++] = newElement;
                }
                else
                {
                    var controlAsFloat = control as InputControl<float>;
                    if (controlAsFloat != null && controlAsFloat.processors != null)
                    {
                        if (controlAsFloat.processors != null)
                        {
                            FilterElement newElement;
                            newElement.controlIndex = i;
                            newElement.type = ElementType.FloatBelowEpsilon;
                            elementsToAdd[elementCount++] = newElement;
                        }
                    }
                    else
                    {
                        var controlAsVec2 = control as InputControl<Vector2>;
                        if (controlAsVec2 != null && controlAsVec2.processors != null)
                        {
                            FilterElement newElement;
                            newElement.controlIndex = i;
                            newElement.type = ElementType.Vector2MagnitudeBelowEpsilon;
                            elementsToAdd[elementCount++] = newElement;
                        }
                    }
                }
            }

            filter.elements = new FilterElement[elementCount];
            for (var j = 0; j < elementCount; j++)
            {
                filter.elements[j] = elementsToAdd[j];
            }

            return filter;
        }

        public bool IsEmpty()
        {
            return elements == null || elements.Length == 0;
        }

        /// <summary>
        /// The list of elements to be checked for.  Each element represents a single InputControl.
        /// </summary>
        public FilterElement[] elements;

        /// <summary>
        /// Called when the InputNoiseFilter gets applied to a device, calls down to any individual FilteredElements that need to do any work.
        /// </summary>
        /// <param name="device">The device you want to apply filtering to.</param>
        internal void Apply(InputDevice device)
        {
            if (device == null)
                throw new ArgumentException("No device supplied to apply InputNoiseFilter to.", "device");

            if (IsEmpty())
                return;

            var noiseBitmaskPtr = InputStateBuffers.s_NoiseBitmaskBuffer;
            if (noiseBitmaskPtr == IntPtr.Zero)
                return;

            MemoryHelpers.SetBitsInBuffer(noiseBitmaskPtr, device, true);

            for (var i = 0; i < elements.Length; i++)
            {
                elements[i].Apply(noiseBitmaskPtr, device);
            }
        }

        /// <summary>
        /// Called when removing a InputNoiseFilter from a device.  This resets any stateful data the InputNoiseFilter sets on the device or in any InputStateBuffers
        /// </summary>
        /// <param name="device">The device you want reset</param>
        internal void Reset(InputDevice device)
        {
            if (device == null)
                throw new ArgumentException("No device supplied to reset noise filter on.", "device");

            if (IsEmpty())
                return;

            var noiseBitmaskPtr = InputStateBuffers.s_NoiseBitmaskBuffer;
            if (noiseBitmaskPtr == IntPtr.Zero)
                return;

            MemoryHelpers.SetBitsInBuffer(noiseBitmaskPtr, device, true);
        }

        /// <summary>
        /// Checks an Input Event for any significant changes that would be considered user activity.
        /// </summary>
        /// <param name="inputEvent">The input event being checked for changes</param>
        /// <param name="device">The input device being checked against </param>
        /// <param name="offset">The offset into the device that the event is placed</param>
        /// <param name="sizeInBytes">The size of the event in bytes</param>
        /// <returns>True if any changes exist in the event once the device has been filtered through for noise and non-significant changes.  False otherwise.</returns>
        public unsafe bool EventHasValidData(InputDevice device, InputEventPtr inputEvent, uint offset, uint sizeInBytes)
        {
            if (!inputEvent.valid)
                throw new ArgumentException("Invalid or unset event being checked.", "inputEvent");

            if (device == null)
                throw new ArgumentNullException("device");

            if (IsEmpty())
                return true;

            if ((offset + sizeInBytes) * 8 > device.stateBlock.sizeInBits)
                return false;

            var noiseFilterPtr = InputStateBuffers.s_NoiseBitmaskBuffer;
            if (noiseFilterPtr == IntPtr.Zero)
                throw new Exception("Noise Filter Buffer is uninitialized while trying to check state events for data.");

            var ptrToEventState = IntPtr.Zero;
            if (inputEvent.IsA<StateEvent>())
            {
                var stateEvent = StateEvent.From(inputEvent);
                ptrToEventState = stateEvent->state;
            }
            else if (inputEvent.IsA<DeltaStateEvent>())
            {
                var stateEvent = DeltaStateEvent.From(inputEvent);
                ptrToEventState = stateEvent->deltaState;
            }
            else
            {
                throw new ArgumentException(string.Format(
                    "Invalid event type '{0}', we can only check for valid data on StateEvents and DeltaStateEvents.",
                    inputEvent.type),
                    "inputEvent");
            }

            if (MemoryHelpers.HasAnyNonZeroBitsAfterMaskingWithBuffer(ptrToEventState, noiseFilterPtr, offset, sizeInBytes * 8))
                return true;

            for (var i = 0; i < elements.Length; i++)
            {
                if (elements[i].EventHasValidData(inputEvent, device))
                    return true;
            }

            return false;
        }
    }
}
