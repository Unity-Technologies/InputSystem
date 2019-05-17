namespace UnityEngine.InputSystem.Plugins.XR.Haptics
{
    public struct BufferedRumble
    {
        public HapticCapabilities capabilities { get; private set; }
        InputDevice device { get; set; }

        public BufferedRumble(InputDevice device)
        {
            this.device = device;

            var command = GetHapticCapabilitiesCommand.Create();
            device.ExecuteCommand(ref command);
            capabilities = command.capabilities;
        }

        public void EnqueueRumble(byte[] samples)
        {
            var command = SendBufferedHapticCommand.Create(samples);
            device.ExecuteCommand(ref command);
        }
    }
}
