// THIS FILE HAS BEEN AUTO-GENERATED
#if (UNITY_EDITOR || UNITY_STANDALONE) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.Controls;
using UnityEngine.Experimental.Input.Layouts;
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
    public ButtonControl jump { get; protected set; }
    public ButtonControl menu { get; protected set; }
    public ButtonControl steamEnterMenu { get; protected set; }
    public Vector2Control navigate { get; protected set; }
    public ButtonControl click { get; protected set; }
    public ButtonControl steamExitMenu { get; protected set; }

    protected override void FinishSetup(InputDeviceBuilder builder)
    {
        base.FinishSetup(builder);
        move = builder.GetControl<StickControl>("move");
        look = builder.GetControl<Vector2Control>("look");
        fire = builder.GetControl<ButtonControl>("fire");
        jump = builder.GetControl<ButtonControl>("jump");
        menu = builder.GetControl<ButtonControl>("menu");
        steamEnterMenu = builder.GetControl<ButtonControl>("steamEnterMenu");
        navigate = builder.GetControl<Vector2Control>("navigate");
        click = builder.GetControl<ButtonControl>("click");
        steamExitMenu = builder.GetControl<ButtonControl>("steamExitMenu");
    }

    protected override void ResolveSteamActions(ISteamControllerAPI api)
    {
        gameplaySetHandle = api.GetActionSetHandle("gameplay");
        moveHandle = api.GetAnalogActionHandle("move");
        lookHandle = api.GetAnalogActionHandle("look");
        fireHandle = api.GetDigitalActionHandle("fire");
        jumpHandle = api.GetDigitalActionHandle("jump");
        menuHandle = api.GetDigitalActionHandle("menu");
        steamEnterMenuHandle = api.GetDigitalActionHandle("steamEnterMenu");
        menuSetHandle = api.GetActionSetHandle("menu");
        navigateHandle = api.GetAnalogActionHandle("navigate");
        clickHandle = api.GetDigitalActionHandle("click");
        steamExitMenuHandle = api.GetDigitalActionHandle("steamExitMenu");
    }

    public SteamHandle<InputActionMap> gameplaySetHandle { get; private set; }
    public SteamHandle<InputAction> moveHandle { get; private set; }
    public SteamHandle<InputAction> lookHandle { get; private set; }
    public SteamHandle<InputAction> fireHandle { get; private set; }
    public SteamHandle<InputAction> jumpHandle { get; private set; }
    public SteamHandle<InputAction> menuHandle { get; private set; }
    public SteamHandle<InputAction> steamEnterMenuHandle { get; private set; }
    public SteamHandle<InputActionMap> menuSetHandle { get; private set; }
    public SteamHandle<InputAction> navigateHandle { get; private set; }
    public SteamHandle<InputAction> clickHandle { get; private set; }
    public SteamHandle<InputAction> steamExitMenuHandle { get; private set; }
    private SteamActionSetInfo[] m_ActionSets;
    public override ReadOnlyArray<SteamActionSetInfo> steamActionSets
    {
        get
        {
            if (m_ActionSets == null)
                m_ActionSets = new[]
                {
                    new SteamActionSetInfo { name = "gameplay", handle = gameplaySetHandle },
                    new SteamActionSetInfo { name = "menu", handle = menuSetHandle },
                };
            return new ReadOnlyArray<SteamActionSetInfo>(m_ActionSets);
        }
    }

    protected override unsafe void Update(ISteamControllerAPI api)
    {
        SteamDemoControllerState state;
        state.move = api.GetAnalogActionData(steamControllerHandle, moveHandle).position;
        state.look = api.GetAnalogActionData(steamControllerHandle, lookHandle).position;
        if (api.GetDigitalActionData(steamControllerHandle, fireHandle).pressed)
            state.buttons[0] |= 0;
        if (api.GetDigitalActionData(steamControllerHandle, jumpHandle).pressed)
            state.buttons[0] |= 1;
        if (api.GetDigitalActionData(steamControllerHandle, menuHandle).pressed)
            state.buttons[0] |= 2;
        if (api.GetDigitalActionData(steamControllerHandle, steamEnterMenuHandle).pressed)
            state.buttons[0] |= 3;
        state.navigate = api.GetAnalogActionData(steamControllerHandle, navigateHandle).position;
        if (api.GetDigitalActionData(steamControllerHandle, clickHandle).pressed)
            state.buttons[0] |= 4;
        if (api.GetDigitalActionData(steamControllerHandle, steamExitMenuHandle).pressed)
            state.buttons[0] |= 5;
        InputSystem.QueueStateEvent(this, state);
    }
}
public unsafe struct SteamDemoControllerState : IInputStateTypeInfo
{
    public FourCC GetFormat()
    {
        return new FourCC('S', 't', 'e', 'a');
    }

    [InputControl(name = "fire", layout = "Button", bit = 0)]
    [InputControl(name = "jump", layout = "Button", bit = 1)]
    [InputControl(name = "menu", layout = "Button", bit = 2)]
    [InputControl(name = "steamEnterMenu", layout = "Button", bit = 3)]
    [InputControl(name = "click", layout = "Button", bit = 4)]
    [InputControl(name = "steamExitMenu", layout = "Button", bit = 5)]
    public fixed byte buttons[1];
    [InputControl(name = "move", layout = "Stick")]
    public Vector2 move;
    [InputControl(name = "look", layout = "Vector2")]
    public Vector2 look;
    [InputControl(name = "navigate", layout = "Vector2")]
    public Vector2 navigate;
}
#endif
