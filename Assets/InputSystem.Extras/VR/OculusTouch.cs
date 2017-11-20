namespace ISX.VR
{
    public class OculusTouch : XRController
    {
        public new static OculusTouch leftHand => XRController.leftHand as OculusTouch;
        public new static OculusTouch rightHand => XRController.leftHand as OculusTouch;
    }
}
