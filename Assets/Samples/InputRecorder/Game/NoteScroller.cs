using UnityEngine;
using UnityEngine.InputSystem;

public class NoteScroller : MonoBehaviour
{
    [Header("Tempo Settings")]
    public float beatTempo;
    public bool hasStarted;
    [HideInInspector] public Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    public void FullResetScroller()
    {
        transform.position = startPosition;

        hasStarted = false;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
            NoteObject noteObject = child.GetComponent<NoteObject>();
            if (noteObject != null)
            {
                noteObject.ResetNote();
            }
        }
    }

    public void LoopResetScroller()
    {
        transform.position = startPosition;

        hasStarted = true;

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
            NoteObject noteObject = child.GetComponent<NoteObject>();
            if (noteObject != null)
            {
                noteObject.ResetNote();
            }
        }
    }

    public void SetScrollerStarted(bool started)
    {
        hasStarted = started;
    }

    private void Start()
    {
        beatTempo = beatTempo / 60f;
    }

    private void Update()
    {
        if (!hasStarted)
        {

        }
        else
        {
            transform.position -= new Vector3(-1 * beatTempo * Time.deltaTime, 0f, 0f);
        }
    }
}
