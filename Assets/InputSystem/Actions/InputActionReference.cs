using UnityEngine;

namespace ISX
{
    // Object that represents a specific action in a specific action
    // set *without* containing the action. This is useful for passing
    // around references to actions as objects.
    //
    // Example: put an InputActionReference field on your MonoBehaviour and
    //          then drop an action from an .inputaction asset onto your
    //          MonoBehaviour in the inspector.
    public class InputActionReference : ScriptableObject
    {
        [SerializeField] internal InputActionAsset m_Asset;

        ////FIXME: this is unstable... if either is renamed, the reference is borked; use GUID instead?
        [SerializeField] internal string m_SetName;
        [SerializeField] internal string m_ActionName;
    }
}
