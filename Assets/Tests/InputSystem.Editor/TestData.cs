#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;
using InputAnalytics = UnityEngine.InputSystem.InputAnalytics;
using Random = UnityEngine.Random;

public static class TestData
{
    private const string kAlphaNumericCharacters =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static Generator<string> alphaNumericString =
        new(() =>
        {
            var length = (int)(Random.value * 25 + 1);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = kAlphaNumericCharacters[(int)(Random.value * kAlphaNumericCharacters.Length)];
            }
            return new string(chars);
        });

    public static Generator<InputActionAsset> inputActionAsset = new(() =>
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var actionMap = asset.AddActionMap(alphaNumericString.Generate());
        var action = actionMap.AddAction(alphaNumericString.Generate());
        action.AddBinding("<Gamepad>/leftStick");
        return asset;
    });

    internal static Generator<InputActionsEditorState> editorState =
        new(() => new InputActionsEditorState(
            new InputEditorAnalytics.InputActionsEditorSessionAnalytic(InputEditorAnalytics.InputActionsEditorSessionData.Kind.FreeFloatingEditorWindow),
            new SerializedObject(ScriptableObject.CreateInstance<InputActionAsset>())));

    internal static Generator<InputActionsEditorState> EditorStateWithAsset(ScriptableObject asset)
    {
        return new Generator<InputActionsEditorState>(() => new InputActionsEditorState(null, new SerializedObject(asset)));
    }

    public static Generator<InputControlScheme.DeviceRequirement> deviceRequirement =
        new(() => new InputControlScheme.DeviceRequirement
        {
            controlPath = $"<{alphaNumericString.Generate()}>"
        });

    public static Generator<InputControlScheme> controlScheme = new(() => new InputControlScheme(alphaNumericString.Generate()));

    public static Generator<InputControlScheme> controlSchemeWithOneDeviceRequirement = new(() =>
        new InputControlScheme(alphaNumericString.Generate(), deviceRequirement.Generate(1)));

    public static Generator<InputControlScheme> controlSchemeWithTwoDeviceRequirements = new(() =>
        new InputControlScheme(alphaNumericString.Generate(), deviceRequirement.Generate(2)));

    public static Generator<IEnumerable<T>> N<T>(Generator<T> generator, int count)
    {
        return new Generator<IEnumerable<T>>(() =>
        {
            var array = new T[count];
            for (var i = 0; i < count; i++)
            {
                array[i] = generator.Generate();
            }

            return array;
        });
    }
}
#endif
