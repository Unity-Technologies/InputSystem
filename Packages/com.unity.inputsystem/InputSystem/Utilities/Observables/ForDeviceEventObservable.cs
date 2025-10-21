using System;
using UnityEngine.InputSystem.LowLevel;

namespace UnityEngine.InputSystem.Utilities
{
    internal class ForDeviceEventObservable : IObservable<InputEventPtr>
    {
        private IObservable<InputEventPtr> m_Source;
        private InputDevice m_Device;
        private Type m_DeviceType;

        public ForDeviceEventObservable(IObservable<InputEventPtr> source, Type deviceType, InputDevice device)
        {
            m_Source = source;
            m_DeviceType = deviceType;
            m_Device = device;
        }

        public IDisposable Subscribe(IObserver<InputEventPtr> observer)
        {
            return m_Source.Subscribe(new ForDevice(m_DeviceType, m_Device, observer));
        }

        private class ForDevice : IObserver<InputEventPtr>
        {
            private IObserver<InputEventPtr> m_Observer;
            private InputDevice m_Device;
            private Type m_DeviceType;

            public ForDevice(Type deviceType, InputDevice device, IObserver<InputEventPtr> observer)
            {
                m_Device = device;
                m_DeviceType = deviceType;
                m_Observer = observer;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
                Debug.LogException(error);
            }

            public void OnNext(InputEventPtr value)
            {
                if (m_DeviceType != null)
                {
                    var device = InputSystem.GetDeviceById(value.deviceId);
                    if (device == null)
                        return;

                    if (!m_DeviceType.IsInstanceOfType(device))
                        return;
                }

                if (m_Device != null && value.deviceId != m_Device.deviceId)
                    return;

                m_Observer.OnNext(value);
            }
        }
    }
}
