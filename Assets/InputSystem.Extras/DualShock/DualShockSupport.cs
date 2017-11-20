namespace ISX.DualShock
{
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<DualShockGamepad>();
        }
    }
}
