namespace ISX.DualShock
{
    [InputPlugin]
    public static class DualShockSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<DualShockGamepad>();
        }
    }
}
