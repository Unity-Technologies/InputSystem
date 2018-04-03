using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using UnityEngine.Experimental.Input.Haptics;
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

            var deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);
            switch (deviceDescriptor.deviceRole)
            {
                case DeviceRole.LeftHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.LeftHand);
                    leftHand = this;
                    break;
                }
                case DeviceRole.RightHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.RightHand);
                    rightHand = this;
                    break;
                }
                default:
                    break;
            }

            base.FinishSetup(setup);
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

    public class XRControllerWithRumble : XRController, IHaptics
    {
        SimpleXRRumble m_Rumble;
        public SimpleXRRumble rumble { get { return m_Rumble; } }

        protected override void FinishSetup(InputControlSetup setup)
        {
            base.FinishSetup(setup);
            m_Rumble = new SimpleXRRumble(this);
        }

        public void SetIntensity(float intensity)
        {
            m_Rumble.intensity = intensity;
        }

        public void PauseHaptics()
        {
            m_Rumble.isPaused = true;
        }

        public void ResumeHaptics()
        {
            m_Rumble.isPaused = false;
        }

        public void ResetHaptics()
        {
            m_Rumble.Reset();
        }
    }
}
