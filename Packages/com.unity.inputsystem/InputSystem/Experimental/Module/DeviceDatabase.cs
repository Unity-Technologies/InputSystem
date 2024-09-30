using System;
using System.Collections.Generic;

namespace UnityEngine.InputSystem.Experimental.Editor
{
    public interface IDeviceDatabase
    {
        DeviceDatabase.Query QueryDevice(uint os, uint api, ushort vendorId, ushort productId);
    }

    public interface IDeviceDatabaseDeviceItem
    {
        /// <summary>
        /// Maps a button from one index to another index.
        /// </summary>
        /// <param name="fromIndex">The source index.</param>
        /// <returns>The destination index.</returns>
        /// <remarks>If the returned value is equal to <paramref name="fromIndex"/> the button is not remapped.</remarks>
        int MapButton(int fromIndex);
        
        /// <summary>
        /// Maps a value from one index to another index.
        /// </summary>
        /// <param name="fromIndex">The source index.</param>
        /// <returns>The destination index.</returns>
        /// <remarks>If the returned value is equal to <paramref name="fromIndex"/> the value is not remapped.</remarks>
        int MapValue(int fromIndex);
    }

    public class DeviceDatabase : IDeviceDatabase
    {
        private static DeviceDatabase _instance;
        
        public struct Query : IDisposable
        {
            private int m_Handle;

            public void Dispose()
            {
                if (m_Handle == 0) return;
                _instance.Release(m_Handle);
                m_Handle = 0;
            }
        }
        
        internal DeviceDatabase instance
        {
            get => _instance ??= new DeviceDatabase();
            set => _instance = value;
        }

        internal void Release(int handle)
        {
            
        }

        public Query QueryDevice(uint os, uint api, ushort vendorId, ushort productId)
        {
            throw new NotImplementedException();
        }
    }

    // TODO Fine to have a ScriptableObject version but should be raw file serializable to support run-time modification if desirable?
    // TODO Design so that native may query the device
    [Serializable]
    public class DeviceDatabaseAsset : ScriptableObject
    {
        [Serializable]
        public struct RemappingItem
        {
            public ushort from;
            public ushort to;
        }
        
        [Serializable]
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