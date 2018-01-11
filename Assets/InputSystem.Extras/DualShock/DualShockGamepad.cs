namespace ISX.DualShock
{
    public struct DualShockGamepadState
    {
    }

    [InputTemplate(stateType = typeof(DualShockGamepadState))]
    public class DualShockGamepad : Gamepad
    {
        public Vector3Control gyro { get; private set; }
        public Vector3Control accelerometer { get; private set; }
        public ColorControl lightbar { get; private set; }
        public AudioControl speaker { get; private set; }
        //two-point touchpad
    }
}
