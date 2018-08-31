// THIS FILE HAS BEEN AUTO-GENERATED
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
[InputControlLayout(stateType = typeof(SteamDemoControllerState))]
public class SteamDemoController : SteamController
{
    private static InputDeviceMatcher deviceMatcher
    {
        get { return new InputDeviceMatcher().WithInterface("Steam").WithProduct("SteamDemoController"); }
    }

#if UNITY_EDITOR
    static SteamDemoController()
    {
        InputSystem.RegisterLayout<SteamDemoController>(matches: deviceMatcher);
    }

#endif

    [RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeInitializeOnLoad()
    {
        InputSystem.RegisterLayout<SteamDemoController>(matches: deviceMatcher);
    }

    public StickControl move { get; protected set; }
    public Vector2Control look { get; protected set; }
    public ButtonControl fire { get; protected set; }

    protected override void FinishSetup(InputDeviceBuilder builder)
    {
        base.FinishSetup(builder);
        move = builder.GetControl<StickControl>("move");
        look = builder.GetControl<Vector2Control>("look");
        fire = builder.GetControl<ButtonControl>("fire");
    }

    public override void ResolveActions(ISteamControllerAPI api)
    {
        m_SetHandle_gameplay = api.GetActionSetHandle("gameplay");
        m_ActionHandle_move = api.GetAnalogActionHandle("move");
        m_ActionHandle_look = api.GetAnalogActionHandle("look");
        m_ActionHandle_fire = api.GetDigitalActionHandle("fire");
    }

    private ulong m_SetHandle_gameplay;
    private ulong m_ActionHandle_move;
    private ulong m_ActionHandle_look;
    private ulong m_ActionHandle_fire;

    public override void Update(ISteamControllerAPI api)
    {
        ////TODO
    }
}
public unsafe struct SteamDemoControllerState : IInputStateTypeInfo
{
    public FourCC GetFormat()
    {
        return new FourCC('S', 't', 'e', 'a');
    }

    [InputControl(name = "move", layout = "Stick")]
    public Vector2 move;
    [InputControl(name = "look", layout = "Vector2")]
    public Vector2 look;
    [InputControl(name = "fire", layout = "Button", bit = 0)]
    public fixed byte buttons[1];
}
#endif
