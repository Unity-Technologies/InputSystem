namespace ISX.VR
{
    public static class VRSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<OculusTouch>();
            InputSystem.RegisterTemplate<ViveController>();
        }
    }
}
