using System.Runtime.CompilerServices;

// contains casts between strongly-typed sample types and basic types

namespace Unity.InputSystem.Runtime
{
    public partial struct InputButtonControlSample
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(InputButtonControlSample sample)
        {
            return sample.value != Native.InputButtonControlSampleReleased.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator InputButtonControlSample(bool value)
        {
            return value ? Native.InputButtonControlSamplePressed : Native.InputButtonControlSampleReleased;
        }
    }
    
    public partial struct InputAxisOneWayControlSample
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(InputAxisOneWayControlSample sample)
        {
            return sample.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator InputAxisOneWayControlSample(float value)
        {
            return new InputAxisOneWayControlSample {value = value};
        }
    }
    
    public partial struct InputAxisTwoWayControlSample
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(InputAxisTwoWayControlSample sample)
        {
            return sample.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator InputAxisTwoWayControlSample(float value)
        {
            return new InputAxisTwoWayControlSample {value = value};
        }
    }
    
    public partial struct InputDeltaAxisTwoWayControlSample
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(InputDeltaAxisTwoWayControlSample sample)
        {
            return sample.value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator InputDeltaAxisTwoWayControlSample(float value)
        {
            return new InputDeltaAxisTwoWayControlSample {value = value};
        }
    }
    

}