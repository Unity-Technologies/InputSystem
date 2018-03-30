using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using System.Text;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    public class XRHMD : InputDevice
    {
        public static XRHMD active { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
            active = this;
        }
    }

    [InputTemplate(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class XRController : InputDevice
    {
        public static XRController leftHand { get; private set; }
        public static XRController rightHand { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);

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
            { }

            base.FinishSetup(setup);
        }
    }

    public class XRControllerWithRumble : XRController
    {
        public SimpleXRRumble rumble { get; private set; }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
            rumble = new SimpleXRRumble(this);
        }
    }
}
