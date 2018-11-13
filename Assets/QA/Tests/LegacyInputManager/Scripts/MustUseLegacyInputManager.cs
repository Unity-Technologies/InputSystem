using UnityEngine;

public class MustUseLegacyInputManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("This Scene must be run with the Legacy InputManager set as the input path in PlayerSettings.");
    }
}
