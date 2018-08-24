using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    public class NoiseFilter
    {
        internal enum EElementType
        {
            TypeUnknown = 0,
            TypeFull,
            TypeFloat,
            TypeVec2
        }

        internal struct FilteredElement
        {
            public int controlIndex;
            public EElementType type;
            public uint offset;
            public uint size;

            public void Apply(IntPtr noiseFilterBuffer, InputDevice device)
            {
                if (device != null)
                {
                    InputControl control = device.allControls[controlIndex];
                    if (control != null)
                    {
                        BitmaskHelpers.Blacklist(noiseFilterBuffer, control);
                    }
                }
            }

            public bool HasValidData(InputEventPtr inputEvent, InputDevice device)
            {
                if (device != null)
                {
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
                                        float value = castedControl.ReadValueFrom(inputEvent, true);
                                        if (value > float.Epsilon)
                                            return true;
                                    }
                                }
                                break;
                            case EElementType.TypeVec2:
                                {
                                    InputControl<Vector2> castedControl = control as InputControl<Vector2>;
                                    if (castedControl != null)
                                    {
                                        Vector2 value = castedControl.ReadValueFrom(inputEvent, true);
                                        if (Vector2.SqrMagnitude(value) > float.Epsilon)
                                            return true;

                                    }
                                }
                                break;
                        }
                    }
                }

                return false;
            }
        }

        internal static unsafe NoiseFilter CreateDefaultNoiseFilter(InputDevice device)
        {
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
                    newElement.offset = stateblock.byteOffset;
                    newElement.size = stateblock.sizeInBits;
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
                            newElement.type = EElementType.TypeVec2;
                            InputStateBlock stateblock = control.stateBlock;
                            newElement.offset = stateblock.byteOffset;
                            newElement.size = stateblock.sizeInBits;
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
                            newElement.offset = stateblock.byteOffset;
                            newElement.size = stateblock.sizeInBits;
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

        private FilteredElement[] elements;

        internal void Apply(InputDevice device)
        {
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

        internal void Reset(InputDevice device)
        {
            IntPtr noiseFilterPtr = InputStateBuffers.s_NoiseFilterBuffer;
            if (noiseFilterPtr != IntPtr.Zero)
            {
                BitmaskHelpers.Whitelist(noiseFilterPtr, device);
            }
        }

        public unsafe bool HasValidData(InputDevice device, InputEventPtr inputEvent, uint offset, uint sizeInbytes)
        {
            if (elements.Length == 0)
                return true;

            bool result = false;

            // TODO Null and range check EVERYTHING!
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
