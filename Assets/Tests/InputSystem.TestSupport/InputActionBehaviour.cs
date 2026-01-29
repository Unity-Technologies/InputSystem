using UnityEngine;
using UnityEngine.InputSystem;

// This class and this assembly should NOT be considered API, it is only public and its own assembly for the following
// reasons:
// 1) Unity only supports serialization of public types stored in a file with the same name.
//    If this wasn't the case, this type would be internal to the test assembly.
// 2) Editor test assemblies cannot contain MonoBehaviour that should be added to scene game objects.
//    Hence, this assembly needs to be a regular assembly and referenced by editor test assembly to use this
//    MonoBehaviour in a scene.
//
// Note that we serialize both as field and reference. Note that this should not make any difference for Unity.Object
// types, but is included for completeness.
public sealed class InputActionBehaviour : MonoBehaviour
{
    [SerializeField] public InputActionReference referenceAsField;
    [SerializeReference] public InputActionReference referenceAsReference;
}
