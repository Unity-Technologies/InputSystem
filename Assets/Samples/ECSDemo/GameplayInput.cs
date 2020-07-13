using Unity.Collections;
using Unity.Entities;
using Unity.Input;

[GenerateAuthoringComponent]
public struct GameplayInput : IComponentData, IInputData
{
    public int PlayerNumber;

    public Float2Input Move;
    public AxisInput UpDown;
    public AxisInput LeftRight;
    public ButtonInput Jump;

    public enum Id : uint
    {
        Move = 32,
        UpDown = 96,
        LeftRight = 128,
        Jump = 160,
    }

    public uint Format => 439083866;

    public DOTSInput.InputPipeline InputPipelineParts
    {
        get
        {
            var structMappings = new NativeArray<DOTSInput.InputStructMapping>(kNumStructMappings, Allocator.Persistent);
            var transforms = new NativeArray<DOTSInput.InputTransform>(kNumTransforms, Allocator.Persistent);

            // Gamepad
            transforms[0] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToCopyOperation(64),
                InputId1 = (uint)GamepadInput.Id.LeftStick,
                OutputId = (uint)Id.Move
            };
            transforms[1] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToCopyOperation(8),
                InputId1 = (uint)GamepadInput.Id.ButtonSouth,
                OutputId = (uint)Id.Jump
            };
            structMappings[0] = new DOTSInput.InputStructMapping
            {
                InputFormat = 623278190,
                OutputFormat = 439083866,
                TransformStartIndex = 0,
                TransformCount = 2
            };

            // Keyboard
            transforms[2] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToTransformOperation(DOTSInput.Combination.FourButtonsToOneFloat2),
                InputId1 = (uint)KeyboardInput.Id.W,
                InputId2 = (uint)KeyboardInput.Id.A,
                InputId3 = (uint)KeyboardInput.Id.S,
                InputId4 = (uint)KeyboardInput.Id.D,
                OutputId = (uint)Id.Move
            };
            transforms[3] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToTransformOperation(DOTSInput.Combination.TwoButtonsToOneAxis),
                InputId1 = (uint)KeyboardInput.Id.S,
                InputId2 = (uint)KeyboardInput.Id.W,
                OutputId = (uint)Id.UpDown
            };
            transforms[4] = new DOTSInput.InputTransform
            {
                Operation = DOTSInput.ToTransformOperation(DOTSInput.Combination.TwoButtonsToOneAxis),
                InputId1 = (uint)KeyboardInput.Id.A,
                InputId2 = (uint)KeyboardInput.Id.D,
                OutputId = (uint)Id.LeftRight
            };
            structMappings[1] = new DOTSInput.InputStructMapping
            {
                InputFormat = 1097922105,
                OutputFormat = 439083866,
                TransformStartIndex = 2,
                TransformCount = 3
            };
            return new DOTSInput.InputPipeline { Transforms = transforms, StructMappings = structMappings };
        }
    }
    private const int kNumStructMappings = 2;
    private const int kNumTransforms = 5;
}
public class GameplayInputUpdate : InputSystem<GameplayInput>
{
}
