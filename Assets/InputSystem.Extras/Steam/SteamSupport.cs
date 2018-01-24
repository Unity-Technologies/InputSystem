////TODO: write a ScriptedImporter for VDF files which automatically generates a template

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
