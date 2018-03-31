using System;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: hook up all sensor controls to noise suppression

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
        public float samplingFrequency
        {
            get
            {
                var command = QuerySamplingFrequencyCommand.Create();
                if (OnDeviceCommand(ref command) >= 0)
                    return command.frequency;
                throw new NotSupportedException(string.Format("Device '{0}' does not support querying sampling frequency", this));
            }
            set
            {
                var command = SetSamplingFrequencyCommand.Create(value);
                OnDeviceCommand(ref command);
            }
        }
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
