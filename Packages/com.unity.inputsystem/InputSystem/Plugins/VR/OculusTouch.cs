namespace UnityEngine.Experimental.Input.VR
{
    public class OculusTouch : XRController
    {
        public new static OculusTouch leftHand
        {
            get { return XRController.leftHand as OculusTouch; }
        }

        public new static OculusTouch rightHand
        {
            get { return XRController.leftHand as OculusTouch; }
        }
    }
}
