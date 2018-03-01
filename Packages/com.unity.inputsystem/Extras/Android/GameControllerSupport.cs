namespace ISX.Android
{
    [InputPlugin]
    public static class GameControllerSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<GameController>("AndroidGameController");
        }
    }
}
