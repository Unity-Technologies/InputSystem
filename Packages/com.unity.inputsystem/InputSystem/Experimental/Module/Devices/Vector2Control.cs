using System;

namespace UnityEngine.InputSystem.Experimental
{
    [Serializable]
    public struct Vector2Control
    {
        [SerializeField] private ushort deviceId;
        [SerializeField] private Usage xUsage;
        [SerializeField] private Usage yUsage;
        
        public Vector2Control(ushort deviceId, Usage x, Usage y)
        {
            this.deviceId = deviceId;
            this.xUsage = x;
            this.yUsage = y;
        }
        
        public ObservableInput<float> x => new(Endpoint.FromDeviceAndUsage(deviceId, xUsage));
        public ObservableInput<float> y => new(Endpoint.FromDeviceAndUsage(deviceId, yUsage));
    }
}