#if UNITY_2022_3_OR_NEWER
using System.Collections.Generic;
using UnityEngine.InputSystem;

public static class TestDataGenerators
{
    public static InputActionAsset WithControlScheme(this InputActionAsset asset, InputControlScheme controlScheme)
    {
        asset.AddControlScheme(controlScheme);
        return asset;
    }

    public static Generator<InputActionAsset> WithControlScheme(this Generator<InputActionAsset> generator,
        Generator<InputControlScheme> controlSchemeGenerator)
    {
        return new Generator<InputActionAsset>(() => generator.Generate().WithControlScheme(controlSchemeGenerator.Generate()));
    }

    public static Generator<InputActionAsset> WithControlSchemes(this Generator<InputActionAsset> generator,
        Generator<IEnumerable<InputControlScheme>> controlSchemeGenerator)
    {
        return new Generator<InputActionAsset>(() =>
        {
            var asset = generator.Generate();
            foreach (var controlScheme in controlSchemeGenerator.Generate())
            {
                asset = asset.WithControlScheme(controlScheme);
            }
            return asset;
        });
    }

    public static Generator<InputControlScheme> WithOptionalDevice(this Generator<InputControlScheme> generator)
    {
        return new Generator<InputControlScheme>(() => generator.Generate().WithOptionalDevice());
    }

    public static Generator<InputControlScheme> WithOptionalDevices(this Generator<InputControlScheme> generator,
        Generator<IEnumerable<InputControlScheme.DeviceRequirement>> deviceGenerator)
    {
        return new Generator<InputControlScheme>(() =>
        {
            var controlScheme = generator.Generate();

            foreach (var _ in deviceGenerator.Generate())
            {
                controlScheme = controlScheme.WithOptionalDevice();
            }
            return controlScheme;
        });
    }

    public static InputControlScheme WithOptionalDevice(this InputControlScheme controlScheme)
    {
        return controlScheme.WithOptionalDevice($"<{TestData.alphaNumericString.Generate()}>");
    }

    public static InputControlScheme WithRequiredDevice(this InputControlScheme controlScheme)
    {
        return controlScheme.WithRequiredDevice($"<{TestData.alphaNumericString.Generate()}>");
    }

    public static InputControlScheme WithName(this InputControlScheme controlScheme, string name)
    {
        return new InputControlScheme(name, controlScheme.deviceRequirements, controlScheme.bindingGroup);
    }
}
#endif
