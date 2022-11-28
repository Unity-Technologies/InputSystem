using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static GameObjectBuilder;

/// <summary>
/// Set up a default UI test scene containing:
///     - a main camera
///     - a canvas in ScreenSpaceOverlay mode
///     - an event system using the InputSystemUIInputModule input module
/// </summary>
internal class UITestScene
{
    private readonly CoreTestsFixture m_Fixture;

    public UITestScene(CoreTestsFixture fixture)
    {
        m_Fixture = fixture;
        MakeGameObject<Camera>("MainCamera");
        MakeGameObject<TestEventSystem, InputSystemUIInputModule>("EventSystem");
        MakeGameObject<GraphicRaycaster>("Canvas", g => g.gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay);

        ((TestEventSystem)eventSystem).OnApplicationFocus(true);
    }

    public Camera camera => GameObject.Find("MainCamera").GetComponent<Camera>();
    public EventSystem eventSystem => GameObject.Find("EventSystem").GetComponent<EventSystem>();
    public Canvas canvas => GameObject.Find("Canvas").GetComponent<Canvas>();
    public InputSystemUIInputModule uiInputModule => eventSystem.gameObject.GetComponent<InputSystemUIInputModule>();

    public RectTransform AddImage(string name = "")
    {
        var rectTransform = new GameObject(name, typeof(Image)).GetComponent<RectTransform>();
        rectTransform.SetParent(canvas.transform);

        // center the image in the canvas
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    public IEnumerator PressAndDrag(RectTransform uiObject, Vector2 distance)
    {
        m_Fixture.BeginTouch(1, uiObject.position);
        yield return null;
        ((TestEventSystem)eventSystem).Update();

        m_Fixture.MoveTouch(1, (Vector2)uiObject.position + distance);
        ((TestEventSystem)eventSystem).Update();
    }

    private class TestEventSystem : EventSystem
    {
        public new void OnApplicationFocus(bool hasFocus)
        {
            base.OnApplicationFocus(hasFocus);
        }

        public new void Update()
        {
            base.Update();
        }
    }
}
