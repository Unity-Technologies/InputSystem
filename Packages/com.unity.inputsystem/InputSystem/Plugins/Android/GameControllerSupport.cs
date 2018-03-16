namespace ISX.Plugins.Android
{
    public static class GameControllerSupport
    {
        public static void Initialize()
        {
            InputSystem.RegisterTemplate<GameController>("AndroidGameController");
        }
    }
}
