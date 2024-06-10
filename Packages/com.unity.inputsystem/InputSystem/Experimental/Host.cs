namespace UnityEngine.InputSystem.Experimental
{
    public static class Host
    {
        private static uint _minTick;
        private static uint _tick;

        public static uint tick => ++_tick;
        
        public static void Report(uint minTick)
        {
            _minTick = minTick; // TODO Should really compute min(contexts...)
        }
    }
}