using UnityEngine;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Experimental.Input;
#endif

public class RunInBackgroundToggle : MonoBehaviour
{
    [SerializeField]
    private bool m_ShouldRunInBackground;

    private void Start()
    {
#if UNITY_2018_3_OR_NEWER
        InputSystem.runInBackground = m_ShouldRunInBackground;
#endif
    }
}
