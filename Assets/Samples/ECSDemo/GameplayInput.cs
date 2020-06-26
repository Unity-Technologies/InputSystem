using Unity.Collections;
using Unity.Entities;
using Unity.Input;

[GenerateAuthoringComponent]
public struct GameplayInput : IComponentData, IInputData
{
    public int PlayerNumber;

    public AxisInput Move;
    public AxisInput Look;
    public ButtonInput Fire;

    public enum Id : uint
    {
        Move = 32,
        Look = 96,
        Fire = 160,
    }
    public DOTSInput.InputPipeline InputPipelineParts
    {
        get
        {
            var structMappings = new NativeArray<DOTSInput.InputStructMapping>(kNumStructMappings, Allocator.Persistent);
            var transforms = new NativeArray<DOTSInput.InputTransform>(kNumTransforms, Allocator.Persistent);
            return new DOTSInput.InputPipeline { Transforms = transforms, StructMappings = structMappings };
        }
    }
    private const int kNumStructMappings = 0;
    private const int kNumTransforms = 0;
}
/*
public class GameplayInputUpdate : InputSystem<GameplayInput>
{
}
*/
