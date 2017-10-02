#if UNITY_STANDALONE || UNITY_EDITOR
namespace ISX
{
    ////REVIEW: do we actually need a device for this? why not just stick with the generic InputDevice class?
    public class GenericHID : InputDevice
    {
        public GenericHID()
        {
        }
    }
}
#endif
