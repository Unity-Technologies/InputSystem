#if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Experimental.Input.Plugins.Steam;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[InputControlLayout(stateType = typeof(DemoControllerState))]
public class DemoController : SteamController, IInputUpdateCallbackReceiver
{
    private static InputDeviceMatcher deviceMatcher
    {
        get { return new InputDeviceMatcher().WithInterface("Steam").WithProduct("DemoController"); }
    }
#if UNITY_EDITOR
    static DemoController()
    {
        InputSystem.RegisterControlLayout<DemoController>(matches: deviceMatcher);
    }

#endif
    public void OnUpdate(InputUpdateType updateType)
    {
        ////TODO
    }

    [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitializeOnLoad()
    {
        InputSystem.RegisterControlLayout<DemoController>(matches: deviceMatcher);
    }

    public Vector2Control move { get; protected set; }
    public Vector2Control look { get; protected set; }
    public ButtonControl fire { get; protected set; }
    protected override void FinishSetup(InputDeviceBuilder builder)
    {
        base.FinishSetup(builder);
        move = builder.GetControl<Vector2Control>("move");
        look = builder.GetControl<Vector2Control>("look");
        fire = builder.GetControl<ButtonControl>("fire");
    }
}
public unsafe struct DemoControllerState : IInputStateTypeInfo
{
    public FourCC GetFormat()
    {
        return new FourCC('D', 'e', 'm', 'o');
    }

    [InputControl(name = "move", layout = "Vector2")]
    public Vector2 move;
    [InputControl(name = "look", layout = "Vector2")]
    public Vector2 look;
    [InputControl(name = "fire", layout = "Button", bit = 0)]
    public fixed byte buttons[1];
}
#endif
