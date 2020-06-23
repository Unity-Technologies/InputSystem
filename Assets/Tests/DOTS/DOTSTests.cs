using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Input;

public class DOTSTests
{
    [Test]
    [Category("DOTS")]
    public unsafe void DOTS_CanConvertPs4HidInputToGamepadInput()
    {
        var hidInputs = new PS4ControllerHidInput();
        var gamepadInputs = new GamepadInput();

        //this is the stuff that should be auto-generated
        var mappingsPass1 = new NativeArray<DOTSInput.InputMapping>(1, Allocator.Temp);
        var mappingsPass2 = new NativeArray<DOTSInput.InputMapping>(1, Allocator.Temp);
        var mappingsPass3 = new NativeArray<DOTSInput.InputMapping>(1, Allocator.Temp);

        mappingsPass1[0] = new DOTSInput.InputMapping
        {
            Operation = DOTSInput.ToOperation(DOTSInput.Conversion.ByteToAxis),
            InputId1 = (uint)PS4ControllerHidInput.Id.LeftStickX,
            OutputId = (uint)GamepadInput.Id.LeftStickX,
        };
        //passes could be combined if the input and output are the same
        mappingsPass2[0] = new DOTSInput.InputMapping
        {
            Operation = DOTSInput.ToOperation(DOTSInput.Conversion.NegativeAxisToHalfAxis),
            InputId1 = (uint)GamepadInput.Id.LeftStickX,
            OutputId = (uint)GamepadInput.Id.LeftStickLeft,
        };
        mappingsPass3[0] = new DOTSInput.InputMapping
        {
            Operation = DOTSInput.ToOperation(DOTSInput.Conversion.HalfAxisToButton),
            InputId1 = (uint)GamepadInput.Id.LeftStickLeft,
            OutputId = (uint)GamepadInput.Id.ButtonLeftStickLeft,
        };

        // Pass 1.
        DOTSInput.Map(UnsafeUtility.AddressOf(ref hidInputs), UnsafeUtility.AddressOf(ref gamepadInputs), mappingsPass1.Length, (DOTSInput.InputMapping*)mappingsPass1.GetUnsafeReadOnlyPtr());

        Assert.That(gamepadInputs.LeftStickLeft.Value, Is.Zero);
        Assert.That(gamepadInputs.ButtonLeftStickLeft.IsPressed, Is.False);
        Assert.That(gamepadInputs.ButtonLeftStickLeft.WasJustPressed, Is.False);
        Assert.That(gamepadInputs.LeftStickX, Is.EqualTo(-1f).Within(0.00001));

        // Pass 2.
        DOTSInput.Map(UnsafeUtility.AddressOf(ref gamepadInputs), UnsafeUtility.AddressOf(ref gamepadInputs), mappingsPass2.Length, (DOTSInput.InputMapping*)mappingsPass2.GetUnsafeReadOnlyPtr());

        Assert.That(gamepadInputs.LeftStickLeft.Value, Is.EqualTo(1f).Within(0.00001));
        Assert.That(gamepadInputs.ButtonLeftStickLeft.IsPressed, Is.False);
        Assert.That(gamepadInputs.ButtonLeftStickLeft.WasJustPressed, Is.False);
        Assert.That(gamepadInputs.LeftStickX, Is.EqualTo(-1f).Within(0.00001));

        // Pass 3.
        DOTSInput.Map(UnsafeUtility.AddressOf(ref gamepadInputs), UnsafeUtility.AddressOf(ref gamepadInputs), mappingsPass3.Length, (DOTSInput.InputMapping*)mappingsPass3.GetUnsafeReadOnlyPtr());

        Assert.That(gamepadInputs.LeftStickLeft.Value, Is.EqualTo(1f).Within(0.00001));
        Assert.That(gamepadInputs.ButtonLeftStickLeft.IsPressed, Is.True);
        Assert.That(gamepadInputs.ButtonLeftStickLeft.WasJustPressed, Is.True);
        Assert.That(gamepadInputs.LeftStickX, Is.EqualTo(-1f).Within(0.00001));

        mappingsPass1.Dispose();
        mappingsPass2.Dispose();
        mappingsPass3.Dispose();
    }

    [Test]
    [Category("DOTS")]
    public unsafe void DOTS_CanConvertGamepadInputToGameplayInput()
    {
        Assert.Fail();
    }
}
