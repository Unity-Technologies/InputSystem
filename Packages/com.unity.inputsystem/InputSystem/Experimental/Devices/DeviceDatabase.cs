using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Editor
{
    public interface IDeviceDatabase
    {
    }

    [Serializable]
    public class DeviceDatabase : ScriptableObject, IDeviceDatabase
    {
        public struct RemappingItem
        {
            public ushort from;
            public ushort to;
        }
        
        public struct DeviceItem
        {
            public uint os;
            public uint api;
            public ushort vendorId;
            public ushort productId;
            public uint remappingStart;
            public uint remappingEnd;
        }

        [SerializeField] private List<DeviceItem> m_Devices;
        [SerializeField] private List<RemappingItem> m_Remapping;
        [NonSerialized] private uint m_DeviceIndex;
        
        public uint AddDevice(uint os, uint api, ushort vendorId, ushort productId)
        {
            m_Devices ??= new List<DeviceItem>();
            m_Devices.Add(new DeviceItem()
            {
                os = os,
                api = api,
                vendorId = vendorId,
                productId = productId,
            });
            return m_DeviceIndex;
        }
    }
}