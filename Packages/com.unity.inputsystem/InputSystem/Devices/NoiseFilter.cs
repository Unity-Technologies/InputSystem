using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    /// <summary>
    /// A filter for individual devices to check if events or device states contain significant, relevant changes.
    /// Irrelevant changes are any updates controls that are tagged as 'noisy', and any state change that turns into a no-operation change once processors are applied (e.g. in deadzone joystick movements).
    /// </summary>
    public class NoiseFilter
    {
        /// <summary>
        /// Simple cached value identifying how to filter this element (Bitmask, or individual type filtered).
        /// </summary>
        public enum EElementType
        {
            TypeUnknown = 0,
            TypeFull,
            TypeFloat,
            TypeVec2
        }

        /// <summary>
        /// A filter for a single InputControl that gets it's value filtered.
        /// </summary>
        public struct FilteredElement
        {
            /// <summary>
            /// The index in InputDevice.allControls for the filtered control.
            /// </summary>
            public int controlIndex;

            /// <summary>
            /// The type of filtering to perform.
            /// </summary>
            public EElementType type;

            /// <summary>
            /// Called when the NoiseFilter gets applied to a device, marks out any controls that can be wholly bitmasked.
            /// </summary>
            /// <param name="noiseFilterBuffer">The Noisefilter buffer for doing whole control filtering.</param>
            /// <param name="device">The device you want to apply filtering to.</param>
            public void Apply(IntPtr noiseFilterBuffer, InputDevice device)
            {
                if (controlIndex >= device.allControls.Count)
                    throw new IndexOutOfRangeException("NoiseFilter has array index beyond total size of device's controls");

                InputControl control = device.allControls[controlIndex];
                BitmaskHelpers.Blacklist(noiseFilterBuffer, control);
            }

            /// <summary>
            /// Checking an individual input event for significant InputControl changes.
            /// </summary>
            /// <param name="inputEvent">The input event being checked for changes</param>
            /// <param name="device">The input device being checked against </param>
            /// <returns>True if any changes exist in the event once the device has been filtered through for noise and non-significant changes.  False otherwise.</returns>
            public bool HasValidData(InputEventPtr inputEvent, InputDevice device)
            {
                if (!inputEvent.valid)
                    throw new ArgumentException("Invalid or unset event being checked.", "inputEvent");

                if (device == null)
                    throw new ArgumentException("No device passed in to check if inputEvent has valid data", "device");

                InputControl control = device.allControls[controlIndex];
                if (control != null)
                {
                    switch (type)
                    {
                        case EElementType.TypeFloat:
                            {
                                InputControl<float> castedControl = control as InputControl<float>;
                                if (castedControl != null)
                                {
                                    float value;
                                    if (castedControl.ReadValueFrom(inputEvent, out value) && Mathf.Abs(value) > float.Epsilon)
                                        return true;
                                }
                            }
                            break;
                        case EElementType.TypeVec2:
                            {
                                InputControl<Vector2> castedControl = control as InputControl<Vector2>;
                                if (castedControl != null)
                                {
                                    Vector2 value;
                                    if (castedControl.ReadValueFrom(inputEvent, out value) && Vector2.SqrMagnitude(value) > float.Epsilon)
                                        return true;
                                }
                            }
                            break;
                    }
                }

                return false;
            }
        }

        public static unsafe NoiseFilter CreateDefaultNoiseFilter(InputDevice device)
        {
            if (device == null)
                throw new ArgumentException("No device supplied to create default noise filter for", "device");

            NoiseFilter filter = new NoiseFilter();
            FilteredElement* elementsToAdd = stackalloc FilteredElement[device.allControls.Count];
            int elementCount = 0;
            ReadOnlyArray<InputControl> controls = device.allControls;
            for(int i = 0; i < controls.Count; i++)
            {
                InputControl control = controls[i];
                if(control.noisy)
                {
                    FilteredElement newElement;
                    newElement.controlIndex = i;
                    newElement.type = EElementType.TypeFull;
                    InputStateBlock stateblock = control.stateBlock;
                    elementsToAdd[elementCount++] = newElement;
                }
                else
                {
                    InputControl<float> controlAsFloat = control as InputControl<float>;
                    if(controlAsFloat != null && controlAsFloat.processors != null)
                    {
                        if(controlAsFloat.processors != null)
                        {
                            FilteredElement newElement;
                            newElement.controlIndex = i;
                            newElement.type = EElementType.TypeFloat;
                            InputStateBlock stateblock = control.stateBlock;
                            elementsToAdd[elementCount++] = newElement;
                        }  
                    }
                    else
                    {
                        InputControl<Vector2> controlAsVec2 = control as InputControl<Vector2>;
                        if (controlAsVec2 != null && controlAsVec2.processors != null)
                        {
                            FilteredElement newElement;
                            newElement.controlIndex = i;
                            newElement.type = EElementType.TypeVec2;
                            InputStateBlock stateblock = control.stateBlock;
                            elementsToAdd[elementCount++] = newElement;                    
                        }
                    }
                }
            }

            filter.elements = new FilteredElement[elementCount];
            for (int j = 0; j < elementCount; j++)
            {
                filter.elements[j] = elementsToAdd[j];
            }

            return filter;
        }

        /// <summary>
        /// The list of elements to be checked for.  Each element represents a single InputControl.
        /// </summary>
        public FilteredElement[] elements;

        /// <summary>
        /// Called when the NoiseFilter gets applied to a device, calls down to any individual FilteredElements that need to do any work.
        /// </summary>
        /// <param name="device">The device you want to apply filtering to.</param>
        internal void Apply(InputDevice device)
        {
            if (device == null)
                throw new ArgumentException("No device supplied to apply NoiseFilter to.", "device");

            IntPtr noiseFilterPtr = InputStateBuffers.s_NoiseFilterBuffer;
            if (noiseFilterPtr != IntPtr.Zero)
            {
                BitmaskHelpers.Whitelist(noiseFilterPtr, device);

                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i].Apply(noiseFilterPtr, device);
                }
            }
        }

        /// <summary>
        /// Resets a device to unfiltered
        /// </summary>
        /// <param name="device">The device you want reset</param>
        internal void Reset(InputDevice device)
        {
            IntPtr noiseFilterPtr = InputStateBuffers.s_NoiseFilterBuffer;
            if (noiseFilterPtr != IntPtr.Zero)
            {
                BitmaskHelpers.Whitelist(noiseFilterPtr, device);
            }
        }


        /// <summary>
        /// Checks an Input Event for any significant changes that would be considered user activity.
        /// </summary>
        /// <param name="inputEvent">The input event being checked for changes</param>
        /// <param name="device">The input device being checked against </param>
        /// <param name="offset">The offset into the device that the event is placed</param>
        /// <param name="sizeInbytes">The size of the event in bytes</param>
        /// <returns>True if any changes exist in the event once the device has been filtered through for noise and non-significant changes.  False otherwise.</returns>
        public unsafe bool HasValidData(InputDevice device, InputEventPtr inputEvent, uint offset, uint sizeInbytes)
        {
            if (elements.Length == 0)
                return true;

            if ((offset + sizeInbytes) * 8 > device.stateBlock.sizeInBits)
                return false;

            bool result = false;

            IntPtr noiseFilterPtr = InputStateBuffers.s_NoiseFilterBuffer;
            if (noiseFilterPtr != IntPtr.Zero)
            {
                IntPtr ptrToEventState = IntPtr.Zero;
                if(inputEvent.IsA<StateEvent>())
                {
                    StateEvent* stateEvent = StateEvent.From(inputEvent);
                    ptrToEventState = stateEvent->state;
                }
                else if (inputEvent.IsA<DeltaStateEvent>())
                {
                    DeltaStateEvent* stateEvent = DeltaStateEvent.From(inputEvent);
                    ptrToEventState = stateEvent->deltaState;
                }

                if(ptrToEventState != IntPtr.Zero)
                {
                    result = BitmaskHelpers.CheckForMaskedValues(ptrToEventState, noiseFilterPtr, offset, sizeInbytes * 8);
                    for (int i = 0; i < elements.Length && !result; i++)
                    {
                        result = elements[i].HasValidData(inputEvent, device);
                    }
                }
            }
                
            return result;
        }
    }
}
