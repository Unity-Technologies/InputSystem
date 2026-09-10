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

    private bool _isButtonHeld = false;
    private float _buttonPressTime = 0f;
    private const float MIN_HOLD_DURATION = 0.05f; // Minimum 50ms to count as a real release

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
        _isButtonHeld = true;
        _buttonPressTime = Time.time;

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
                    if (distance < smallestHoldDistance)
                    {
                        smallestHoldDistance = distance;
                        bestHoldNote = note;
                    }
                }
            }
        }

        NoteObject bestNoteToHit = bestNormalNote != null ? bestNormalNote : bestHoldNote;

        if (bestNoteToHit != null)
        {
            bestNoteToHit.ProcessNoteHit(WorldXPosition);
        }
    }

    private void ProcessButtonRelease()
    {
        float holdDuration = Time.time - _buttonPressTime;

        if (holdDuration < MIN_HOLD_DURATION)
        {
            return;
        }

        _isButtonHeld = false;

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
            // Check actual input device state using multiple methods
            float rawInputValue = pressAction != null ? pressAction.action.ReadValue<float>() : 0f;
            // Read raw values from input devices (works for both keyboard and UI/mouse input)
            float mouseValue = Mouse.current != null ? Mouse.current.leftButton.ReadValue() : 0f;
            float pointerValue = Pointer.current != null ? Pointer.current.press.ReadValue() : 0f;
            bool actionPressed = pressAction != null && pressAction.action.IsPressed();
            bool isButtonCurrentlyPressed = mouseValue > 0.5f || pointerValue > 0.5f || actionPressed || rawInputValue > 0.5f;

            notesInLane.Remove(note);

            if (note.gameObject.activeInHierarchy && note.CanBePressed)
            {
                if (note.noteType == NoteObject.NoteType.Normal)
                {
                    GameManager.instance.NoteMissed(note);
                }
                else if (note.noteType == NoteObject.NoteType.Hold)
                {
                    if (note.IsHoldStarted())
                    {
                        if (isButtonCurrentlyPressed)
                        {
                            GameManager.instance.NoteHoldPerfect(note);
                        }
                        else
                        {
                            GameManager.instance.NoteHoldMissedEarly(note);
                        }
                    }
                    else
                    {
                        GameManager.instance.NoteMissed(note); // Never started holding
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
