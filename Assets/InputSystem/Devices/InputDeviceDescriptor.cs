using System;

namespace InputSystem
{
    public struct InputDeviceDescriptor
    {
        public string interfaceName;
        public string deviceClass;
        public string manufacturer;
        public string product;
        public string serial;
        public string version;
        public string fullDescriptor;

        public static InputDeviceDescriptor Parse(string json)
        {
            throw new NotImplementedException();
        }
    }
}