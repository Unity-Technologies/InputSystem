using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HighLevel;
using UnityEngine.InputSystem.Interactions;
using Input = UnityEngine.InputSystem.HighLevel.Input;

public static class InputActions
{
    /// <summary>
    /// This action is currently bound to the following control paths:<br/>
    /// <br/>
    /// <example>
    /// <code>
    ///  Left Stick [Gamepad]
    ///  Primary2DAxis [XR Controller]
    ///  Stick [Joystick]
    ///  Composite action "WASD"
    ///    Up: W [Keyboard]
    ///    Up: Up Arrow [Keyboard]
    ///    Down: S [Keyboard]
    ///    Down: Down Arrow [Keyboard]
    ///    Left: A [Keyboard]
    ///    Left: Left Arrow [Keyboard]
    ///    Right: D [Keyboard]
    ///    Right: Right Arrow [Keyboard]
    /// </code>
    /// </example>
    /// </summary>
    public static Input<Vector2> move => new Input<Vector2>(Input.globalAsset.FindAction("Gameplay/Move"));

    /// <summary>
    /// This action is currently bound to the following control paths:<br/>
    /// <br/>
    /// <example>
    /// <code>
    ///     Left Button [Mouse]
    ///     Button South [Gamepad]
    /// </code>
    /// </example>
    /// and has the following interactions:
    /// <example>
    /// <code>
    ///     Hold (Duration = 0.4 seconds, Press point = 0.5)
    ///     Press (Trigger Behaviour = Press Only, Press point = 0.5)
    /// </code>
    /// </example>
    /// </summary>
    public static FireInput fire => new FireInput(Input.globalAsset.FindAction("Gameplay/Fire"));
    public static Input<float> sprint => new Input<float>(Input.globalAsset.FindAction("Gameplay/Sprint"));
    public static Input<float> jump => new Input<float>(Input.globalAsset.FindAction("Gameplay/Jump"));

    public static Input<float> join => new Input<float>(Input.globalAsset.FindAction("Player/Join"));

    public static Input<Vector2> navigate => new Input<Vector2>(Input.globalAsset.FindAction("UI/Navigate"));

    public static Input<float> clickAndHold => new Input<float>(Input.globalAsset.FindAction("Gameplay/ClickHold"));
    public static Input<float> releaseClick => new Input<float>(Input.globalAsset.FindAction("Gameplay/ReleaseClick"));

    public class FireInput : Input<Vector2>
    {
        public InputInteraction<HoldInteraction, Vector2> holdInteraction;
        public InputInteraction<TapInteraction, Vector2> tapInteraction;

        internal FireInput(InputAction action) : base(action)
        {
            holdInteraction = new InputInteraction<HoldInteraction, Vector2>(this);
            tapInteraction = new InputInteraction<TapInteraction, Vector2>(this);
        }
    }
}
