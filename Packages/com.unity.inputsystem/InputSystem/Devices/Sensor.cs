using System;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

////TODO: gyro and accelerometer (and potentially other sensors) need adjusting for screen orientation

////TODO: hook up all sensor controls to noise suppression (actually... for sensors we probably do NOT want that)

namespace UnityEngine.Experimental.Input.LowLevel
{
    public struct AccelerometerState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('A', 'C', 'C', 'L'); }
        }

        [InputControl(processors = "CompensateDirection")]
        public Vector3 acceleration;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct GyroscopeState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('G', 'Y', 'R', 'O'); }
        }

        [InputControl(processors = "CompensateDirection")]
        public Vector3 angularVelocity;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct GravityState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('G', 'R', 'V', ' '); }
        }

        [InputControl(processors = "CompensateDirection")]
        public Vector3 gravity;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct AttitudeState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('A', 'T', 'T', 'D'); }
        }

        [InputControl(processors = "CompensateRotation")]
        public Quaternion attitude;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public struct LinearAccelerationState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('L', 'A', 'A', 'C'); }
        }

        [InputControl(processors = "CompensateDirection")]
        public Vector3 acceleration;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    public class CompensateDirectionProcessor : IInputControlProcessor<Vector3>
    {
        public virtual Vector3 Process(Vector3 value, InputControl control)
        {
            if (!InputConfiguration.CompensateSensorsForScreenOrientation)
                return value;

            var rotation = Quaternion.identity;
            switch (InputRuntime.s_Instance.screenOrientation)
            {
                case ScreenOrientation.PortraitUpsideDown: rotation = Quaternion.Euler(0, 0, 180); break;
                case ScreenOrientation.LandscapeLeft: rotation = Quaternion.Euler(0, 0, 90); break;
                case ScreenOrientation.LandscapeRight: rotation = Quaternion.Euler(0, 0, 270); break;
            }
            return rotation * value;
        }
    }

    public class CompensateRotationProcessor : IInputControlProcessor<Quaternion>
    {
        public virtual Quaternion Process(Quaternion value, InputControl control)
        {
            if (!InputConfiguration.CompensateSensorsForScreenOrientation)
                return value;

            const float kSqrtOfTwo = 1.4142135623731f;
            var q = Quaternion.identity;

            switch (InputRuntime.s_Instance.screenOrientation)
            {
                case ScreenOrientation.PortraitUpsideDown: q = new Quaternion(0.0f, 0.0f, 1.0f /*sin(pi/2)*/, 0.0f /*cos(pi/2)*/); break;
                case ScreenOrientation.LandscapeLeft:      q = new Quaternion(0.0f, 0.0f, kSqrtOfTwo * 0.5f /*sin(pi/4)*/, -kSqrtOfTwo * 0.5f /*cos(pi/4)*/); break;
                case ScreenOrientation.LandscapeRight:     q = new Quaternion(0.0f, 0.0f, -kSqrtOfTwo * 0.5f /*sin(3pi/4)*/, -kSqrtOfTwo * 0.5f /*cos(3pi/4)*/); break;
            }

            return value * q;
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
                if (ExecuteCommand(ref command) >= 0)
                    return command.frequency;
                throw new NotSupportedException(string.Format("Device '{0}' does not support querying sampling frequency", this));
            }
            set
            {
                var command = SetSamplingFrequencyCommand.Create(value);
                ExecuteCommand(ref command);
            }
        }
    }

    [InputControlLayout(stateType = typeof(AccelerometerState))]
    public class Accelerometer : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        public static Accelerometer current { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }
    }

    [InputControlLayout(stateType = typeof(GyroscopeState))]
    public class Gyroscope : Sensor
    {
        public Vector3Control angularVelocity { get; private set; }

        public static Gyroscope current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            angularVelocity = builder.GetControl<Vector3Control>("angularVelocity");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(GravityState))]
    public class Gravity : Sensor
    {
        public Vector3Control gravity { get; private set; }

        public static Gravity current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            gravity = builder.GetControl<Vector3Control>("gravity");
            base.FinishSetup(builder);
        }
    }

    //// REVIEW: Is this name good enough, possible other name RotationVector, here's how Android docs describe it. "A rotation vector sensor reports the orientation of the device relative to the East-North-Up coordinates frame."
    ////         This is the same as https://docs.unity3d.com/ScriptReference/Gyroscope-attitude.html
    [InputControlLayout(stateType = typeof(AttitudeState))]
    public class Attitude : Sensor
    {
        public QuaternionControl attitude { get; private set; }

        public static Attitude current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            attitude = builder.GetControl<QuaternionControl>("attitude");
            base.FinishSetup(builder);
        }
    }

    [InputControlLayout(stateType = typeof(LinearAccelerationState))]
    public class LinearAcceleration : Sensor
    {
        public Vector3Control acceleration { get; private set; }

        public static LinearAcceleration current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (current == this)
                current = null;
        }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            acceleration = builder.GetControl<Vector3Control>("acceleration");
            base.FinishSetup(builder);
        }
    }


    public class GPS : Sensor
    {
    }
}
