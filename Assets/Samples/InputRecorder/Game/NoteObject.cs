using UnityEngine;

public class NoteObject : MonoBehaviour
{
    public enum NoteType { Normal, Hold }
    public NoteType noteType = NoteType.Normal;

    public bool CanBePressed { get; private set; }
    public Vector3 startPosition;

    private Collider col;
    private MeshRenderer _meshRenderer; 

    private ButtonController _currentButtonController;

    private bool _holdStarted = false;
    private bool _isHolding = false;

    public Color defaultNoteColor = Color.white;
    public float displayDelaySeconds = 1f;

    public bool IsHoldStarted() => _holdStarted;
    public bool IsHolding() => _isHolding;

    private void Awake()
    {
        startPosition = transform.position;
        col = GetComponent<Collider>();
        _meshRenderer = GetComponent<MeshRenderer>(); 
    }

    public void SetCanBePressed(bool state, ButtonController controller)
    {
        CanBePressed = state;
        _currentButtonController = controller;

        _meshRenderer.material.color = defaultNoteColor; 
        if (noteType == NoteType.Hold)
        {
            _holdStarted = false;
            _isHolding = false;
        }
    }

    public void ProcessNoteHit(float hitZoneXPositionOfController)
    {
        if (!CanBePressed)
        {
            return;
        }

        if (noteType == NoteType.Normal)
        {
            float hitDistance = Mathf.Abs(transform.position.x - hitZoneXPositionOfController);

            if (hitDistance <= 0.05f)
            {
                GameManager.instance.NotePerfectHit(this); 
            }
            else if (hitDistance <= 0.25f)
            {
                GameManager.instance.NoteGoodHit(this); 
            }
            else
            {
                GameManager.instance.NoteNormalHit(this);
            }

        }
        else if (noteType == NoteType.Hold)
        {
            if (!_holdStarted)
            {
                _holdStarted = true;
                _isHolding = true;
                GameManager.instance.NoteHoldStarted(this); 
            }
        }
    }

    public void ProcessNoteRelease()
    {
        if (noteType == NoteType.Hold)
        {
            if (_isHolding)
            {
                _isHolding = false;
                if (CanBePressed)
                {
                    GameManager.instance.NoteHoldMissedEarly(this);
                }
            }
        }
    }

    public void DisplayHitResult(Color color)
    {
        _meshRenderer.material.color = color; 
        CanBePressed = false; 

        CancelInvoke("DeactivateNote");
        Invoke("DeactivateNote", displayDelaySeconds);
    }

    public void DisplayHoldStarted(Color color)
    {
        _meshRenderer.material.color = color; 
    }

    private void DeactivateNote()
    {
        gameObject.SetActive(false);
        _currentButtonController?.NotifyNoteDeactivated(this);
        ResetNoteStateInternal();
    }

    public void ResetNoteStateInternal()
    {
        CanBePressed = false;
        _holdStarted = false;
        _isHolding = false;
    }

    public void ResetNote()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;

        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = defaultNoteColor;
        }

        CancelInvoke("DeactivateNote");

        if (_currentButtonController != null)
        {
            _currentButtonController.NotifyNoteDeactivated(this);
            _currentButtonController = null;
        }

        ResetNoteStateInternal();
    }

    private void OnDestroy()
    {
        CancelInvoke("DeactivateNote");
        if (_currentButtonController != null)
        {
            _currentButtonController.NotifyNoteDeactivated(this);
        }
    }
}
