////TODO: set displayNames of the controls according to PlayStation controller standards

namespace ISX.DualShock
{
    /// <summary>
    /// A PS4 controller.
    /// </summary>
    public class DualShockGamepad : Gamepad
    {
        public Vector3Control gyro { get; private set; }
        public Vector3Control accelerometer { get; private set; }
        public ColorControl lightbar { get; private set; }
        public AudioControl speaker { get; private set; }
        //two-point touchpad
    }
}
