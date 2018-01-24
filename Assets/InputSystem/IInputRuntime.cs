using System;
using ISX.Utilities;

namespace ISX.LowLevel
{
    /// <summary>
    /// Input functions that have to be performed by the underlying input runtime.
    /// </summary>
    public interface IInputRuntime
    {
        int AllocateDeviceId();
        void Update(InputUpdateType type);

        ////TODO: add API to send events in bulk rather than one by one
        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// This method has to be thread-safe.
        /// </remarks>
        /// <param name="ptr"></param>
        void QueueEvent(IntPtr ptr);

        /// <summary>
        /// Perform an I/O transaction directly against a specific device.
        /// </summary>
        /// <remarks>
        /// This function is used to set up device-specific communication controls between
        /// a device and the user of a device. The interface does not dictate a set of supported
        /// IOCTL control codes.
        /// </remarks>
        /// <param name="deviceId"></param>
        /// <param name="code"></param>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        long IOCTL(int deviceId, FourCC code, IntPtr buffer, int size);

        /// <summary>
        /// Set the delegate to be called on input updates.
        /// </summary>
        Action<InputUpdateType, int, IntPtr> onUpdate { set; }
        Action<InputUpdateType> onBeforeUpdate { set; }
        Action<int, string> onDeviceDiscovered { set; }

        /// <summary>
        /// Set the background polling frequency for devices that have to be polled.
        /// </summary>
        float PollingFrequency { set; }
    }
}
