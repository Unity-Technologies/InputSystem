namespace ISX.Plugins.iOS
{
    public static class GameControllerSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<GameController>("iOSGameController");
        }
    }
}
