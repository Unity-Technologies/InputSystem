using UnityEngine;
using UnityEngine.InputSystem.HighLevel;
using Input = UnityEngine.InputSystem.HighLevel.Input;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] public GameObject cube;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // BASIC API
        if (Input.IsControlDown(Inputs.Mouse_Left))
        {
            cube.GetComponent<Renderer>().material.color = Color.red;
        }
        else if (Input.IsControlUp(Inputs.Mouse_Left))
        {
            cube.GetComponent<Renderer>().material.color = Color.green;
        }

        // GLOBAL API
        if (InputActions.FPS.move.value.x < 0.0f)
        {
            cube.transform.Translate(new Vector3 (-10 * Time.deltaTime, 0, 0));
        }
        else if (InputActions.FPS.move.value.x > 0.0f)
        {
            cube.transform.Translate(new Vector3 (10 * Time.deltaTime, 0, 0));
        }
        if (InputActions.FPS.move.value.y < 0.0f)
        {
            cube.transform.Translate(new Vector3 (0, -10 * Time.deltaTime, 0));
        }
        else if (InputActions.FPS.move.value.y > 0.0f)
        {
            cube.transform.Translate(new Vector3 (0, 10 * Time.deltaTime, 0));
        }

    }
}
