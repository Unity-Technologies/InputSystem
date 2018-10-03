namespace UnityEngine.Experimental.Input.Plugins.XR.Haptics
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

        public void EnqueueRumble(int channel, byte[] samples)
        {
            var command = SendBufferedHapticCommand.Create(channel, samples);
            device.ExecuteCommand(ref command);
        }
    }
}
