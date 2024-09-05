using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Devices
{
    public class Device
    {
        private List<Usage> m_Interfaces;
        
        public Device(ushort deviceId, IEnumerable<Usage> usages)
        {
            this.deviceId = deviceId;
        }

        public ushort deviceId { get; }

        // TODO Gamepad should probably be class? Otherwise this doesn't really make sense.
        /*
        public T QueryInterface<T>()
        {
            return (T)QueryInterface(typeof(T));
        }

        public ValueType QueryInterface(Type type)
        {
            return 
        }*/
    }
}