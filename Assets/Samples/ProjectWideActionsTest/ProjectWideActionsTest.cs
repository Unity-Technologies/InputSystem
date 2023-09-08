#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

using UnityEngine;

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
        // Project-Wide Actions via Source Generated type-safe API
        if (InputActions.player.attack.wasPressedThisFrame)
        {
            cube.GetComponent<Renderer>().material.color = Color.red;
        }
        else if (InputActions.player.attack.wasReleasedThisFrame)
        {
            cube.GetComponent<Renderer>().material.color = Color.green;
        }

        if (InputActions.player.move.value.x < 0.0f)
        {
            cube.transform.Translate(new Vector3(-10 * Time.deltaTime, 0, 0));
        }
        else if (InputActions.player.move.value.x > 0.0f)
        {
            cube.transform.Translate(new Vector3(10 * Time.deltaTime, 0, 0));
        }
        if (InputActions.player.move.value.y < 0.0f)
        {
            cube.transform.Translate(new Vector3(0, -10 * Time.deltaTime, 0));
        }
        else if (InputActions.player.move.value.y > 0.0f)
        {
            cube.transform.Translate(new Vector3(0, 10 * Time.deltaTime, 0));
        }
    }
}
#endif
