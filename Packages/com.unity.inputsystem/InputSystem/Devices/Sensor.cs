using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    public struct AccelerometerState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('A', 'C', 'C', 'L'); }
        }

        [InputControl]
        public Vector3 acceleration;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    public abstract class Sensor : InputDevice
    {
    }

    [InputTemplate(stateType = typeof(AccelerometerState))]
    public class Accelerometer : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        public static Accelerometer current { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            acceleration = setup.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(setup);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }

    public class Gyroscope : Sensor
    {
    }
}
