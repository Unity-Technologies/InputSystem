#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS

namespace UnityEngine.InputSystem.Samples.ProjectWideActionsSample
{
    public class ProjectWideActionsSample : MonoBehaviour
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
            if (Input.player.attack.wasPressedThisFrame)
            {
                cube.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (Input.player.attack.wasReleasedThisFrame)
            {
                cube.GetComponent<Renderer>().material.color = Color.green;
            }

            if (Input.player.move.value.x < 0.0f)
            {
                cube.transform.Translate(new Vector3(-10 * Time.deltaTime, 0, 0));
            }
            else if (Input.player.move.value.x > 0.0f)
            {
                cube.transform.Translate(new Vector3(10 * Time.deltaTime, 0, 0));
            }
            if (Input.player.move.value.y < 0.0f)
            {
                cube.transform.Translate(new Vector3(0, -10 * Time.deltaTime, 0));
            }
            else if (Input.player.move.value.y > 0.0f)
            {
                cube.transform.Translate(new Vector3(0, 10 * Time.deltaTime, 0));
            }
        }
    }
}
#endif
