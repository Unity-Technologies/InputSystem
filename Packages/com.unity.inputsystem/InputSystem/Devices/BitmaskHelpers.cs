using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Processors;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input
{
    public class NoiseFilter
    {
        public const int kFullThreshold = int.MaxValue;

        internal enum EElementType
        {
            TypeUnknown = 0,
            TypeFull,
            TypeFloat,
            TypeVec2
        }

        internal struct FilteredElement
        {
            public float threshold;
            public int controlIndex;
            public EElementType type;
            public uint offset;
            public uint size;

            public void Apply(IntPtr noiseFilterBuffer, InputDevice device)
            {
                if (type == EElementType.TypeFull)
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
            }

            public void Reset()
            {
                controlIndex = -1;
                type = EElementType.TypeUnknown;
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
                                        //TODO I need to verify against a specific state Ptr
                                        float value = castedControl.ReadValueFrom(inputEvent, false);
                                        if (value > threshold)
                                            return true;
                                    }
                                }
                                break;
                            case EElementType.TypeVec2:
                                {
                                    InputControl<Vector2> castedControl = control as InputControl<Vector2>;
                                    if (castedControl != null)
                                    {
                                        Vector2 value = castedControl.ReadValueFrom(inputEvent, false);
                                        if (Vector2.SqrMagnitude(value) > threshold * threshold)
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
                    newElement.threshold = kFullThreshold;
                    newElement.type = EElementType.TypeFull;
                    InputStateBlock stateblock = control.stateBlock;
                    newElement.offset = stateblock.byteOffset;
                    newElement.size = stateblock.sizeInBits;
                    elementsToAdd[elementCount++] = newElement;
                }
                else
                {
                    /*
                    InputControl<float> controlAsFloat = control as InputControl<float>;
                    if(controlAsFloat != null)
                    {
                        IInputControlProcessor<float>[] processors = controlAsFloat.processors;
                        if(processors != null)
                        {
                            for (int j = 0; j < processors.Length; j++)
                            {
                                // What Constitutes a float deadzone?
                            }
                        }  
                    }
                    else
                    */
                    {
                        InputControl<Vector2> controlAsVec2 = control as InputControl<Vector2>;
                        if (controlAsVec2 != null)
                        {
                            IInputControlProcessor<Vector2>[] processors = controlAsVec2.processors;
                            if(processors != null)
                            {
                                for (int j = 0; j < processors.Length; j++)
                                {
                                    DeadzoneProcessor deadzoneProcessor = processors[j] as DeadzoneProcessor;

                                    if(deadzoneProcessor != null)
                                    {
                                        FilteredElement newElement;
                                        newElement.controlIndex = i;
                                        newElement.threshold = deadzoneProcessor.min;
                                        newElement.type = EElementType.TypeVec2;
                                        InputStateBlock stateblock = control.stateBlock;
                                        newElement.offset = stateblock.byteOffset;
                                        newElement.size = stateblock.sizeInBits;
                                        elementsToAdd[elementCount++] = newElement;
                                    }                              
                                }
                            }                         
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

        internal void Reset()
        {
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i].Reset();
            }
        }

        public unsafe bool HasValidData(InputDevice device, InputEventPtr inputEvent, uint offset, uint sizeInbytes)
        {
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
                    if (BitmaskHelpers.CheckForMaskedValues(ptrToEventState, noiseFilterPtr, offset, sizeInbytes * 8))
                    {
                        return true;
                    }

                    if (device != null)
                    {
                        for (int i = 0; i < elements.Length || result; i++)
                        {
                            result = elements[i].HasValidData(inputEvent, device);
                        }
                    }
                }
            }
                
            return result;
        }
    }

    public unsafe struct BitmaskHelpers
    {
        static public void Whitelist(IntPtr filterBuffer, InputControl control)
        {
            MarkInBuffer(filterBuffer, control.stateBlock.byteOffset, control.stateBlock.sizeInBits, true);
        }

        static public void Blacklist(IntPtr filterBuffer, InputControl control)
        {
            MarkInBuffer(filterBuffer, control.stateBlock.byteOffset, control.stateBlock.sizeInBits, false);
        }

        static public void MarkInBuffer(IntPtr filterBuffer, uint byteOffset, uint sizeInBits, bool state)
        {
            uint sizeRemaining = sizeInBits;

            uint* filterIter = (uint*)((filterBuffer.ToInt64() + (Int64)byteOffset));
            while (sizeRemaining >= 32)
            {
                *filterIter = state ? 0xFFFFFFFF : 0;
                filterIter++;
                sizeRemaining -= 32;
            }

            uint mask = (uint)((1 << (int)sizeRemaining) - 1);
            if (state)
            {
                *filterIter |= mask;
            }
            else
            {
                *filterIter &= ~mask;
            }
        }

        static public bool CheckForMaskedValues(IntPtr eventBuffer , IntPtr maskPtr, uint offsetBytes, uint sizeInBits)
        {
            uint sizeRemaining = sizeInBits;

            uint* eventIter = (uint*)eventBuffer.ToPointer();

            uint* maskIter = (uint*)(new IntPtr(maskPtr.ToInt64() + (Int64)offsetBytes).ToPointer());

            while (sizeRemaining >= 32)
            {
                if ((*eventIter & *maskIter) != 0)
                    return true;

                eventIter++;
                maskIter++;

                sizeRemaining -= 32;
            }

            //Find the remaining bytes to check
            // Mask it in the state iterator and noise
            uint remainingState = *eventIter;
            uint remainingMask = *maskIter;

            int mask = ((1 >> (int)sizeRemaining) - 1);


            if ((remainingState & (remainingMask & mask)) != 0)
                return true;

            return false;
        }
    }
}
