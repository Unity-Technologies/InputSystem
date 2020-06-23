using Unity.Entities;
using Unity.Input;

[GenerateAuthoringComponent]
struct GameplayInput : IComponentData
{
    AxisInput Move;
    AxisInput Look;
    ButtonInput Fire;
    public enum Id : uint
    {
        Move = 0,
        Look = 64,
        Fire = 128,
    }
}
