using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.XR.Haptics;
using UnityEngine.Experimental.Input.Haptics;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    [InputControlLayout]
    public class XRHMD : InputDevice
    {
        public static XRHMD current { get; private set; }

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

    [InputControlLayout(commonUsages = new[] { "LeftHand", "RightHand" })]
    public class XRController : InputDevice
    {
        public static XRController leftHand { get; private set; }
        public static XRController rightHand { get; private set; }

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);

            var deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);
            switch (deviceDescriptor.deviceRole)
            {
                case DeviceRole.LeftHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.LeftHand);
                    break;
                }
                case DeviceRole.RightHanded:
                {
                    InputSystem.SetUsage(this, CommonUsages.RightHand);
                    break;
                }
                default:
                    break;
            }
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

        protected override void OnRemoved()
        {
            base.OnRemoved();
            if (leftHand == this)
                leftHand = null;
            else if (rightHand == this)
                rightHand = null;
        }
    }

    public class XRControllerWithRumble : XRController, IHaptics
    {
        SimpleXRRumble m_Rumble;

        protected override void FinishSetup(InputDeviceBuilder builder)
        {
            base.FinishSetup(builder);
            m_Rumble = new SimpleXRRumble(this);
        }

        public void SetIntensity(float intensity)
        {
            m_Rumble.intensity = intensity;
        }

        public bool isHapticsPaused
        {
            get
            {
                return m_Rumble.isPaused;
            }
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
