using System;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    public class XRHMD : InputDevice
    {
        public static XRHMD active { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            active = this;
        }
    }

    [InputLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class XRController : InputDevice
    {
        public static XRController leftHand { get; private set; }
        public static XRController rightHand { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            try
            {
                XRDeviceDescriptor deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);

                switch (deviceDescriptor.deviceRole)
                {
                    case EDeviceRole.LeftHanded:
                    {
                        InputSystem.SetUsage(this, CommonUsages.LeftHand);
                        leftHand = this;
                        break;
                    }
                    case EDeviceRole.RightHanded:
                    {
                        InputSystem.SetUsage(this, CommonUsages.RightHand);
                        rightHand = this;
                        break;
                    }
                    default:
                        break;
                }
            }
            catch (Exception)
            {}

            base.FinishSetup(builder);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            if (usages.Contains(CommonUsages.LeftHand))
            {
                leftHand = this;
            }
            else if (leftHand == this)
            {
                leftHand = null;
            }

            if (usages.Contains(CommonUsages.RightHand))
            {
                rightHand = this;
            }
            else if (rightHand == this)
            {
                rightHand = null;
            }
        }
    }

    public class XRControllerWithRumble : XRController
    {
        public SimpleXRRumble rumble { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            rumble = new SimpleXRRumble(this);
        }
    }
}
