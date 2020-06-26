using Unity.Collections;
using Unity.Entities;
using Unity.Input;

[GenerateAuthoringComponent]
public struct GameplayInput : IComponentData, IInputData
{
    public int PlayerNumber;

    public Float2Input Move;
    public Float2Input Look;
    public ButtonInput Fire;

    public enum Id : uint
    {
        Move = 32,
        Look = 96,
        Fire = 160,
    }

    public uint Format => 439083866;

    public DOTSInput.InputPipeline InputPipelineParts
    {
        get
        {
            var structMappings = new NativeArray<DOTSInput.InputStructMapping>(kNumStructMappings, Allocator.Persistent);
            var transforms = new NativeArray<DOTSInput.InputTransform>(kNumTransforms, Allocator.Persistent);
            transforms[0] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToCopyOperation(64),
                InputId1 = (uint)GamepadInput.Id.LeftStick,
                OutputId = (uint)Id.Move,
            };
            transforms[1] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToCopyOperation(64),
                InputId1 = (uint)GamepadInput.Id.RightStick,
                OutputId = (uint)Id.Look,
            };
            transforms[2] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToCopyOperation(8),
                InputId1 = (uint)GamepadInput.Id.ButtonSouth,
                OutputId = (uint)Id.Fire,
            };
            structMappings[0] = new DOTSInput.InputStructMapping
            {
                InputFormat = 623278190,
                OutputFormat = 439083866,
                TransformStartIndex = 0,
                TransformCount = 3,
            };
            return new DOTSInput.InputPipeline { Transforms = transforms, StructMappings = structMappings };
        }
    }
    private const int kNumStructMappings = 1;
    private const int kNumTransforms = 3;
}
public class GameplayInputUpdate : InputSystem<GameplayInput>
{
}
