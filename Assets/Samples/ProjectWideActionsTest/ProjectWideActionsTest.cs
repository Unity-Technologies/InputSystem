#if UNITY_2020_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.InputSystem.Input;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] public GameObject cube;

    Input<UnityEngine.Vector2> move;
    Input<UnityEngine.Vector2> look;
    Input<Single> attack;
    Input<Single> jump;
    Input<Single> interact;
    Input<Single> next;
    Input<Single> previous;
    Input<Single> sprint;
    Input<Single> crouch;

    // Start is called before the first frame update
    void Start()
    {
        // Project-Wide Actions Direct (InputSystem.actions)
        move = new Input<UnityEngine.Vector2>(InputSystem.actions.FindAction("Player/Move"));
        look = new Input<UnityEngine.Vector2>(InputSystem.actions.FindAction("Player/Look"));
        attack = new Input<Single>(InputSystem.actions.FindAction("Player/Attack"));
        jump = new Input<Single>(InputSystem.actions.FindAction("Player/Jump"));
        interact = new Input<Single>(InputSystem.actions.FindAction("Player/Interact"));
        next = new Input<Single>(InputSystem.actions.FindAction("Player/Next"));
        previous = new Input<Single>(InputSystem.actions.FindAction("Player/Previous"));
        sprint = new Input<Single>(InputSystem.actions.FindAction("Player/Sprint"));
        crouch = new Input<Single>(InputSystem.actions.FindAction("Player/Crouch"));
    }

    // Update is called once per frame
    void Update()
    {
        // Device API
        if (Input.WasPressedThisFrame(Inputs.Mouse_Left))
        {
            cube.GetComponent<Renderer>().material.color = Color.red;
        }
        else if (Input.WasReleasedThisFrame(Inputs.Mouse_Left))
        {
            cube.GetComponent<Renderer>().material.color = Color.green;
        }

        // Project-Wide Actions Direct (InputSystem.actions)
        if (sprint.isPressed)
        {
            cube.GetComponent<Renderer>().material.color = Color.blue;
        }

        // Project-Wide Actions via API
        if (Input.IsPressed("Crouch", "Player"))
        {
            cube.GetComponent<Renderer>().material.color = Color.gray;
        }

        // Project-Wide Actions via Source Generated type-safe API
        if (InputActions.player.move.value.x < 0.0f)
        {
            cube.transform.Translate(new Vector3 (-10 * Time.deltaTime, 0, 0));
        }
        else if (InputActions.player.move.value.x > 0.0f)
        {
            cube.transform.Translate(new Vector3 (10 * Time.deltaTime, 0, 0));
        }
        if (InputActions.player.move.value.y < 0.0f)
        {
            cube.transform.Translate(new Vector3 (0, -10 * Time.deltaTime, 0));
        }
        else if (InputActions.player.move.value.y > 0.0f)
        {
            cube.transform.Translate(new Vector3 (0, 10 * Time.deltaTime, 0));
        }

    }
}
#endif
