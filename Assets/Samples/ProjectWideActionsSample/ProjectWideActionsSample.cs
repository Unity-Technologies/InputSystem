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
            if (ProjectActions.player.attack.wasPressedThisFrame)
            {
                cube.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (ProjectActions.player.attack.wasReleasedThisFrame)
            {
                cube.GetComponent<Renderer>().material.color = Color.green;
            }

            if (ProjectActions.player.move.value.x < 0.0f)
            {
                cube.transform.Translate(new Vector3(-10 * Time.deltaTime, 0, 0));
            }
            else if (ProjectActions.player.move.value.x > 0.0f)
            {
                cube.transform.Translate(new Vector3(10 * Time.deltaTime, 0, 0));
            }
            if (ProjectActions.player.move.value.y < 0.0f)
            {
                cube.transform.Translate(new Vector3(0, -10 * Time.deltaTime, 0));
            }
            else if (ProjectActions.player.move.value.y > 0.0f)
            {
                cube.transform.Translate(new Vector3(0, 10 * Time.deltaTime, 0));
            }
        }
    }
}
#endif
