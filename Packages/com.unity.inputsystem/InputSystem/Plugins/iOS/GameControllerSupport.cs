namespace ISX.iOS
{
    [InputPlugin]
    public static class GameControllerSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<GameController>("iOSGameController");
        }
    }
}
