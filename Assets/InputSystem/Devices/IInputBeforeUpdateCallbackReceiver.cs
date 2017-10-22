namespace ISX
{
    public interface IInputBeforeUpdateCallbackReceiver
    {
        ////REVIEW: omit update type arg?
        void OnUpdate(InputUpdateType updateType);
    }
}
