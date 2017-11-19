namespace ISX.Steam
{
    public static class SteamSupport
    {
        public static void Initialize()
        {
            // We use this as a base template.
            InputSystem.RegisterTemplate<SteamController>();
        }
    }
}
