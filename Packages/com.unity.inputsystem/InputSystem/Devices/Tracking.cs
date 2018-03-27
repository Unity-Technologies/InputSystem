using System.Runtime.InteropServices;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;

namespace UnityEngine.Experimental.Input.LowLevel
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TrackingState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('T', 'R', 'A', 'K'); }
        }

        [InputControl] public Pose pose;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HMDState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('H', 'E', 'A', 'D'); }
        }

        [InputControl(name = "isWearing", template = "Button", bit = 0)]
        public int buttons;

        public TrackingState head;

        [InputControl] public Pose leftEye;
        [InputControl] public Pose rightEye;
        [InputControl] public Pose centerEye;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }

    /// <summary>
    /// Default state layout for <see cref="XRController">XR controllers</see>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XRControllerState : IInputStateTypeInfo
    {
        public static FourCC kFormat
        {
            get { return new FourCC('C', 'T', 'R', 'L'); }
        }

        [InputControl] public Pose pose;

        public FourCC GetFormat()
        {
            return kFormat;
        }
    }
}

namespace UnityEngine.Experimental.Input
{
    [InputTemplate(stateType = typeof(TrackingState))]
    public abstract class TrackedDevice : InputDevice
    {
        public PoseControl pose { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            pose = setup.GetControl<PoseControl>("pose");
            base.FinishSetup(setup);
        }

        ////REVIEW: have current?
    }

    // A head tracking device.
    [InputTemplate(stateType = typeof(HMDState))]
    public class HMD : TrackedDevice
    {
        public static HMD current { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }
    }

    /// <summary>
    /// A hand and interaction tracking device.
    /// </summary>
    [InputTemplate(stateType = typeof(XRControllerState), commonUsages = new[] { "LeftHand", "RightHand" })]
    public class XRController : TrackedDevice
    {
        public static XRController leftHand { get; internal set; }
        public static XRController rightHand { get; internal set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            if (usages.Contains(CommonUsages.LeftHand))
            {
                leftHand = this;
                if (ReferenceEquals(rightHand, this))
                    rightHand = null;
            }
            else if (usages.Contains(CommonUsages.RightHand))
            {
                rightHand = this;
                if (ReferenceEquals(leftHand, this))
                    leftHand = null;
            }
        }
    }
}
