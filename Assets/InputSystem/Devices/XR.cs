using System.Runtime.InteropServices;

namespace ISX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TrackingState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('T', 'R', 'A', 'K');

        [InputControl] public Pose pose;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HMDState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('H', 'E', 'A', 'D');

        public TrackingState head;

        [InputControl] public Pose leftEye;
        [InputControl] public Pose rightEye;
        [InputControl] public Pose centerEye;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [InputState(typeof(TrackingState))]
    public abstract class TrackedDevice : InputDevice
    {
    }

    // A head tracking device.
    [InputState(typeof(HMDState))]
    public class HMD : TrackedDevice
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XRControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat => new FourCC('C', 'T', 'R', 'L');

        [InputControl] public Pose pose;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    // A hand and interaction tracking device.
    [InputState(typeof(XRControllerState))]
    //[InputControl(variant = "LeftHand", usage = "LeftHand")]
    //[InputControl(variant = "RightHand", usage = "RightHand")]
    public class XRController : TrackedDevice
    {
        public static XRController leftHand { get; private set; }
        public static XRController rightHand { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            //check usages and set leftHand or rightHand (or none if none applies)
        }
    }
}
