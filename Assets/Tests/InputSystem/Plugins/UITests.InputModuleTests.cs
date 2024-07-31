#if UNITY_INPUT_SYSTEM_SENDPOINTERHOVERTOPARENT
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

internal partial class UITests
{
#pragma warning disable CS0649
    public class InputModuleTests : CoreTestsFixture
    {
        private InputSystemUIInputModule m_InputModule;
        private Mouse m_Mouse;
        private Image m_Image;
        private Image m_NestedImage;

        private bool sendPointerHoverToParent
        {
            set => m_InputModule.sendPointerHoverToParent = value;
        }

        private Vector2 mousePosition
        {
            set { Set(m_Mouse.position, value); }
        }

        public override void Setup()
        {
            base.Setup();

            var canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            m_Image = new GameObject("Image").AddComponent<Image>();
            m_Image.gameObject.transform.SetParent(canvas.transform);
            RectTransform imageRectTransform = m_Image.GetComponent<RectTransform>();
            imageRectTransform.sizeDelta = new Vector2(400f, 400f);
            imageRectTransform.localPosition = Vector3.zero;

            m_NestedImage = new GameObject("NestedImage").AddComponent<Image>();
            m_NestedImage.gameObject.transform.SetParent(m_Image.transform);
            RectTransform nestedImageRectTransform = m_NestedImage.GetComponent<RectTransform>();
            nestedImageRectTransform.sizeDelta = new Vector2(200f, 200f);
            nestedImageRectTransform.localPosition = Vector3.zero;

            GameObject go = new GameObject("Event System");
            var eventSystem = go.AddComponent<EventSystem>();
            eventSystem.pixelDragThreshold = 1;

            m_InputModule = go.AddComponent<InputSystemUIInputModule>();
            Cursor.lockState = CursorLockMode.None;

            m_Mouse = InputSystem.AddDevice<Mouse>();
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var uiActions = actions.AddActionMap("UI");
            var pointAction = uiActions.AddAction("point", type: InputActionType.PassThrough);
            pointAction.AddBinding("<Mouse>/position");
            pointAction.Enable();
            m_InputModule.point = InputActionReference.Create(pointAction);
        }

        [UnityTest]
        public IEnumerator PointerEnterChildShouldNotFullyExit_NotSendPointerEventToParent()
        {
            sendPointerHoverToParent = false;
            PointerExitCallbackCheck callbackCheck = m_Image.gameObject.AddComponent<PointerExitCallbackCheck>();
            m_NestedImage.gameObject.AddComponent<PointerExitCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            mousePosition = screenMiddle;
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData.fullyExited == false);
        }

        [UnityTest]
        public IEnumerator PointerEnterChildShouldNotExit_SendPointerEventToParent()
        {
            sendPointerHoverToParent = true;
            PointerExitCallbackCheck callbackCheck = m_Image.gameObject.AddComponent<PointerExitCallbackCheck>();
            m_NestedImage.gameObject.AddComponent<PointerExitCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            mousePosition = screenMiddle;
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData == null);
        }

        [UnityTest]
        public IEnumerator PointerEnterChildShouldNotReenter()
        {
            PointerEnterCallbackCheck callbackCheck =
                m_NestedImage.gameObject.AddComponent<PointerEnterCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            mousePosition = screenMiddle;
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData.reentered == false);
        }

        [UnityTest]
        public IEnumerator PointerExitChildShouldReenter_NotSendPointerEventToParent()
        {
            sendPointerHoverToParent = false;
            PointerEnterCallbackCheck callbackCheck =
                m_Image.gameObject.AddComponent<PointerEnterCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            mousePosition = screenMiddle;
            yield return null;
            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData.reentered == true);
        }

        [UnityTest]
        public IEnumerator PointerExitChildShouldNotSendEnter_SendPointerEventToParent()
        {
            sendPointerHoverToParent = true;
            m_NestedImage.gameObject.AddComponent<PointerEnterCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle;
            yield return null;
            PointerEnterCallbackCheck callbackCheck =
                m_Image.gameObject.AddComponent<PointerEnterCallbackCheck>();
            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData == null);
        }

        [UnityTest]
        public IEnumerator PointerExitChildShouldFullyExit()
        {
            PointerExitCallbackCheck callbackCheck =
                m_NestedImage.gameObject.AddComponent<PointerExitCallbackCheck>();
            var screenMiddle = new Vector2(Screen.width / 2, Screen.height / 2);

            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            mousePosition = screenMiddle;
            yield return null;
            mousePosition = screenMiddle - new Vector2(150, 150);
            yield return null;
            Assert.IsTrue(callbackCheck.pointerData.fullyExited == true);
        }

        public class PointerExitCallbackCheck : MonoBehaviour, IPointerExitHandler
        {
            public PointerEventData pointerData { get; private set; }

            public void OnPointerExit(PointerEventData eventData)
            {
                pointerData = eventData;
            }
        }

        public class PointerEnterCallbackCheck : MonoBehaviour, IPointerEnterHandler
        {
            public PointerEventData pointerData { get; private set; }

            public void OnPointerEnter(PointerEventData eventData)
            {
                pointerData = eventData;
            }
        }
    }
}
#endif
