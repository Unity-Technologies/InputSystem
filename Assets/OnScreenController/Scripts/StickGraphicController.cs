using UnityEngine;
using UnityEngine.EventSystems;

public class StickGraphicController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    Vector3 m_StartPos;
    public int MovementRange = 100;

    // Use this for initialization
    void Start()
    {
        m_StartPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPointerDown(PointerEventData data) {}

    public void OnPointerUp(PointerEventData data) { transform.position = m_StartPos; }

    public void OnDrag(PointerEventData data)
    {
        Vector3 newPos = Vector3.zero;

        {
            int delta = (int)(data.position.x - m_StartPos.x);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.x = delta;
        }

        {
            int delta = (int)(data.position.y - m_StartPos.y);
            delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            newPos.y = delta;
        }
        transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
    }
}
