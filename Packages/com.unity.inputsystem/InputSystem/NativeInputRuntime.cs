using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngineInternal.Input;

// This should be the only file referencing the API at UnityEngineInternal.Input.

namespace ISX.LowLevel
{
    internal class NativeInputRuntime : IInputRuntime
    {
        public static NativeInputRuntime instance = new NativeInputRuntime();

        public int AllocateDeviceId()
        {
            return NativeInputSystem.AllocateDeviceId();
        }

        public void Update(InputUpdateType updateType)
        {
            if ((updateType & InputUpdateType.Dynamic) == InputUpdateType.Dynamic)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Dynamic);
            }
            if ((updateType & InputUpdateType.Fixed) == InputUpdateType.Fixed)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Fixed);
            }
            if ((updateType & InputUpdateType.BeforeRender) == InputUpdateType.BeforeRender)
            {
                NativeInputSystem.Update(NativeInputUpdateType.BeforeRender);
            }
#if UNITY_EDITOR
            if ((updateType & InputUpdateType.Editor) == InputUpdateType.Editor)
            {
                NativeInputSystem.Update(NativeInputUpdateType.Editor);
            }
#endif
        }

        public void QueueEvent(IntPtr ptr)
        {
            NativeInputSystem.QueueInputEvent(ptr);
        }

        public unsafe long DeviceCommand<TCommand>(int deviceId, ref TCommand command)
            where TCommand : struct, IInputDeviceCommandInfo
        {
            var commandPtr = (InputDeviceCommand*)UnsafeUtility.AddressOf(ref command);
            return NativeInputSystem.IOCTL(deviceId, commandPtr->type, (IntPtr)commandPtr->payloadPtr,
                commandPtr->sizeInBytes - InputDeviceCommand.kBaseCommandSize);
        }

        public Action<InputUpdateType, int, IntPtr> onUpdate
        {
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onUpdate = (updateType, eventCount, eventPtr) =>
                        value((InputUpdateType)updateType, eventCount, eventPtr);
                else
                    NativeInputSystem.onUpdate = null;
            }
        }

        public Action<InputUpdateType> onBeforeUpdate
        {
            set
            {
                // This is stupid but the enum prevents us from jacking the delegate in directly.
                // This we get a double dispatch here :(
                if (value != null)
                    NativeInputSystem.onBeforeUpdate = updateType => value((InputUpdateType)updateType);
                else
                    NativeInputSystem.onBeforeUpdate = null;
            }
        }

        public Action<int, string> onDeviceDiscovered
        {
            set { NativeInputSystem.onDeviceDiscovered = value; }
        }

        public float PollingFrequency
        {
            set { NativeInputSystem.SetPollingFrequency(value); }
        }
    }
}
