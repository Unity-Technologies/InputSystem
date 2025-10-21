using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Say you want to distinguish a device not only by its type (e.g. "PS4 Controller")
// but also by the way it is used. This is a common scenario for VR controllers, for
// example, where the same type of controller may be used once in the left hand and
// once in the right hand. However, the need for distinguishing devices in a similar
// manner can pop up in a variety of situations. For example, on Switch it is used
// to distinguish the current orientation of the Joy-Con controller ("Horizontal" vs.
// "Vertical") allowing you to take orientation into account when binding actions.
//
// The input system allows you to distinguish devices based on the "usages" assigned
// to them. This is a generic mechanism that can be used to tag devices with arbitrary
// custom usages.
//
// To make this more concrete, let's say we have a game where two players control
// the game together each one using a gamepad but each receiving control over half
// the actions in the game.
//
// NOTE: What we do here is only one way to achieve this kind of setup. We could
//       alternatively go and just create one control scheme for the first player
//       and one control scheme for the second one and then have two PlayerInputs
//       each using one of the two.
//
// So, what we'd like to do is tag one gamepad with "Player1" and one gamepad with
// with "Player2". Then, in the actions we can set up a binding scheme specifically
// for this style of play and bind actions such that are driven either from the
// first player's gamepad or from the second player's gamepad (or from either).
//
// The first bit we need for this is to tell the input system that "Player1" and
// "Player2" are usages that we intend to apply to gamepads. For this, we need
// to modify the "Gamepad" layout. We do so by applying what's called a "layout
// override". This needs to happen during initialization so here we go:
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class InitCustomDeviceUsages
{
    static InitCustomDeviceUsages()
    {
        Initialize();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Here we register the layout override with the system.
        //
        // The layout override is just a fragment of layout information
        // in JSON format.
        //
        // The key property here is "commonUsages" which tells the system
        // that "Player1" and "Player2" are possible usages applied to devices
        // using the given layout ("Gamepad" in our case).
        InputSystem.RegisterLayoutOverride(@"
            {
                ""name"" : ""GamepadPlayerUsageTags"",
                ""extend"" : ""Gamepad"",
                ""commonUsages"" : [
                    ""Player1"", ""Player2""
                ]
            }
        ");

        // Now that we have done this, you will see that when using the
        // control picker in the action editor, that there is now a
        // "Gamepad (Player1)" and "Gamepad (Player2)" entry underneath
        // "Gamepad". When you select those, you can bind specifically
        // to a gamepad with the respective device usage.
        //
        // Also, you will now be able to *require* a device with the
        // given usage in a control scheme. So, when creating a control
        // scheme representing the shared Player1+Player2 controls,
        // you can add one "Gamepad (Player1)" and one "Gamepad (Player2)"
        // requirement.
        //
        // You can see an example setup for how this would look in an
        // .inputactions file in the TwoPlayerControls.inputactions file
        // that is part of this sample.
    }
}

// However, we are still missing a piece. At runtime, no gamepad will
// receive either the "Player1" or the "Player2" usage assignment yet.
// So none of the bindings will work yet.
//
// To assign the usage tags to the devices, we need to call
// InputSystem.AddDeviceUsage or SetDeviceUsage.
//
// We could set this up any which way. As a demonstration, let's create
// a MonoBehaviour here that simply associates a specific tag with a
// specific gamepad index.
//
// In practice, you would probably want to do the assignment in a place
// where you handle your player setup/joining.
public class CustomDeviceUsages : MonoBehaviour
{
    public int gamepadIndex;
    public string usageTag;

    private Gamepad m_Gamepad;

    protected void OnEnable()
    {
        if (gamepadIndex >= 0 && gamepadIndex < Gamepad.all.Count)
        {
            m_Gamepad = Gamepad.all[gamepadIndex];
            InputSystem.AddDeviceUsage(m_Gamepad, usageTag);
        }
    }

    protected void OnDisable()
    {
        // If we grabbed a gamepad and it's still added to the system,
        // remove the usage tag we added.
        if (m_Gamepad != null && m_Gamepad.added)
            InputSystem.RemoveDeviceUsage(m_Gamepad, usageTag);
        m_Gamepad = null;
    }
}
