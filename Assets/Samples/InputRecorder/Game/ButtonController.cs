using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Collider))]
public class ButtonController : MonoBehaviour
{
    private MeshRenderer meshR;

    [Header("Textures")]
    public Texture2D defaultTexture;
    public Texture2D pressedTexture;

    [Header("Input System")]
    public InputActionReference pressAction;

    private System.Collections.Generic.List<NoteObject> notesInLane = new System.Collections.Generic.List<NoteObject>();

    public float WorldXPosition => transform.position.x;

    private void Awake()
    {
        meshR = GetComponent<MeshRenderer>();

        Collider buttonCollider = GetComponent<Collider>();
        if (buttonCollider != null)
        {
            buttonCollider.isTrigger = true;
            if (GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
        }
        else
        {
            Debug.LogError("ButtonController needs a Collider component set to Is Trigger and a Rigidbody!", this);
        }

        if (defaultTexture != null)
            meshR.material.mainTexture = defaultTexture;
    }

    private void OnEnable()
    {
        if (pressAction != null)
        {
            pressAction.action.performed += OnInputPressed;
            pressAction.action.canceled += OnInputReleased;
            pressAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pressAction != null)
        {
            pressAction.action.performed -= OnInputPressed;
            pressAction.action.canceled -= OnInputReleased;
            pressAction.action.Disable();
        }
    }

    private void OnInputPressed(InputAction.CallbackContext ctx)
    {
        ProcessButtonPress();
    }

    private void OnInputReleased(InputAction.CallbackContext ctx)
    {
        ProcessButtonRelease();
    }

    public void UIButton_Press()
    {
        ProcessButtonPress();
    }

    public void UIButton_Release()
    {
        ProcessButtonRelease();
    }

    private void ProcessButtonPress()
    {
        if (pressedTexture != null)
            meshR.material.mainTexture = pressedTexture;

        NoteObject bestNormalNote = null;
        NoteObject bestHoldNote = null;
        float smallestNormalDistance = float.MaxValue;
        float smallestHoldDistance = float.MaxValue;

        for (int i = notesInLane.Count - 1; i >= 0; i--)
        {
            NoteObject note = notesInLane[i];

            if (note != null && note.gameObject.activeInHierarchy && note.CanBePressed)
            {
                if (note.noteType == NoteObject.NoteType.Normal)
                {
                    float distance = Mathf.Abs(note.transform.position.x - WorldXPosition);
                    if (distance < smallestNormalDistance)
                    {
                        smallestNormalDistance = distance;
                        bestNormalNote = note;
                    }
                }
                else if (note.noteType == NoteObject.NoteType.Hold && !note.IsHoldStarted())
                {
                    float distance = Mathf.Abs(note.transform.position.x - WorldXPosition);
                    if (distance <= 0.25f && distance < smallestHoldDistance)
                    {
                        smallestHoldDistance = distance;
                        bestHoldNote = note;
                    }
                }
            }
        }

        // Prioritize Normal notes over Hold notes
        NoteObject bestNoteToHit = bestNormalNote != null ? bestNormalNote : bestHoldNote;

        if (bestNoteToHit != null)
        {
            bestNoteToHit.ProcessNoteHit(WorldXPosition);
        }
    }

    private void ProcessButtonRelease()
    {
        if (defaultTexture != null)
            meshR.material.mainTexture = defaultTexture;

        for (int i = notesInLane.Count - 1; i >= 0; i--)
        {
            NoteObject note = notesInLane[i];
            if (note != null && note.gameObject.activeInHierarchy && note.noteType == NoteObject.NoteType.Hold && note.IsHolding())
            {
                note.ProcessNoteRelease();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        NoteObject note = other.GetComponent<NoteObject>();
        if (note != null)
        {
            if (!notesInLane.Contains(note))
            {
                notesInLane.Add(note);
                note.SetCanBePressed(true, this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NoteObject note = other.GetComponent<NoteObject>();
        if (note != null)
        {
            notesInLane.Remove(note);

            if (note.gameObject.activeInHierarchy && note.CanBePressed)
            {
                if (note.noteType == NoteObject.NoteType.Normal)
                {
                    GameManager.instance.NoteMissed(note); // Pass 'note'
                }
                else if (note.noteType == NoteObject.NoteType.Hold)
                {
                    if (note.IsHoldStarted() && note.IsHolding())
                    {
                        GameManager.instance.NoteHoldPerfect(note); // Pass 'note'
                        // note.gameObject.SetActive(false); Removed: NoteObject will handle deactivation
                    }
                    else if (!note.IsHoldStarted())
                    {
                        GameManager.instance.NoteMissed(note); // Pass 'note'
                    }
                }
            }
            note.ResetNoteStateInternal();
        }
    }

    public void NotifyNoteDeactivated(NoteObject note)
    {
        if (notesInLane.Contains(note))
        {
            notesInLane.Remove(note);
        }
    }
}
