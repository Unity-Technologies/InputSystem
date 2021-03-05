using System;
using UnityEngine.Serialization;

namespace UnityEngine.InputSystem.DmytroRnD
{
    [Serializable]
    internal struct NativeDeviceDescriptor
    {
        [FormerlySerializedAs("interface")] public string interfaceName;
        public string type;
        public string product;
        public string manufacturer;
        public string serial;
        public string version;
        public string capabilities;
    }
}