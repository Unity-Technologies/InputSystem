using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionTypeTest : MonoBehaviour
{
    [SerializeField] public GameObject cube;

    InputAction move;
    InputAction colorChange;

    // Start is called before the first frame update
    void Start()
    {
        // Project-Wide Actions
        if (InputSystem.actions)
        {
            move = InputSystem.actions.FindAction("Player/Move");
            colorChange = InputSystem.actions.FindAction("Player/ColorChange");
        }
        else
        {
            Debug.Log("Setup Project Wide Input Actions in the Player Settings, Input System section");
        }

        // Handle input by responding to callbacks
        if (colorChange != null)
        {
            colorChange.performed += OnColorChangePerformed;
            colorChange.canceled += OnColorChangeCanceled;
        }
    }

    private void OnColorChangePerformed(InputAction.CallbackContext ctx)
    {
        cube.GetComponent<Renderer>().material.color = Color.red;
    }

    private void OnColorChangeCanceled(InputAction.CallbackContext ctx)
    {
        cube.GetComponent<Renderer>().material.color = Color.green;
    }

    void OnDestroy()
    {
        if (colorChange != null)
        {
            colorChange.performed -= OnColorChangePerformed;
            colorChange.canceled -= OnColorChangeCanceled;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Handle input by polling each frame
        if (move != null)
        {
            var moveVal = move.ReadValue<Vector2>() * 10.0f * Time.deltaTime;
            cube.transform.Translate(new Vector3(moveVal.x, moveVal.y, 0));
        }
    }
}
