using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent floating overlay that lets the player return to the main menu (build
/// index 0) from any scene.  Created automatically by <see cref="SceneMenu"/> when
/// a scene is loaded and destroys itself when returning to the menu.
/// </summary>
public class ReturnToMenuOverlay : MonoBehaviour
{
    static ReturnToMenuOverlay s_Instance;

    InputAction m_BackAction;
    GameObject m_ConfirmPanel;
    Canvas m_Canvas;
    bool m_PanelVisible;

    static readonly Color kOverlay  = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color kBtnNorm  = new Color32(42, 42, 56, 230);
    static readonly Color kBtnHover = new Color32(60, 70, 100, 240);
    static readonly Color kPrimary  = new Color32(80, 140, 255, 255);
    static readonly Color kText     = new Color32(230, 230, 240, 255);
    static readonly Color kTextDim  = new Color32(160, 160, 180, 255);

    public static void Show()
    {
        if (s_Instance != null) return;
        var go = new GameObject("[ReturnToMenuOverlay]");
        DontDestroyOnLoad(go);
        s_Instance = go.AddComponent<ReturnToMenuOverlay>();
    }

    public static void Hide()
    {
        if (s_Instance == null) return;
        Destroy(s_Instance.gameObject);
        s_Instance = null;
    }

    void Awake()
    {
        BuildUI();
        SetupInput();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_BackAction?.Dispose();
        if (s_Instance == this) s_Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            Hide();
            return;
        }
        EnsureEventSystem();
        SetPanelVisible(false);
    }

    #region Input

    void SetupInput()
    {
        m_BackAction = new InputAction("BackToMenu", InputActionType.Button);
        m_BackAction.AddBinding("<Keyboard>/escape");
        m_BackAction.AddBinding("<Gamepad>/select");
        m_BackAction.performed += _ => TogglePanel();
        m_BackAction.Enable();
    }

    void TogglePanel()
    {
        SetPanelVisible(!m_PanelVisible);
    }

    void SetPanelVisible(bool visible)
    {
        m_PanelVisible = visible;
        if (m_ConfirmPanel != null)
            m_ConfirmPanel.SetActive(visible);
    }

    #endregion

    #region UI

    void BuildUI()
    {
        // Canvas
        m_Canvas = gameObject.AddComponent<Canvas>();
        m_Canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 999;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        BuildMenuButton();
        BuildConfirmPanel();
    }

    void BuildMenuButton()
    {
        var go = new GameObject("MenuBtn", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>();
        img.color = kBtnNorm;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(16, -16);
        rt.sizeDelta        = new Vector2(120, 44);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor      = kBtnNorm;
        c.highlightedColor = kBtnHover;
        c.pressedColor     = kPrimary;
        c.fadeDuration     = 0.08f;
        btn.colors = c;
        btn.onClick.AddListener(TogglePanel);

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text      = "\u25C4 Menu";
        tmp.fontSize  = 18;
        tmp.color     = kText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        StretchRT(txt);
    }

    void BuildConfirmPanel()
    {
        // Full-screen dimmer
        m_ConfirmPanel = new GameObject("ConfirmPanel", typeof(RectTransform));
        m_ConfirmPanel.transform.SetParent(transform, false);
        var dimmer = m_ConfirmPanel.AddComponent<Image>();
        dimmer.color = kOverlay;
        StretchRT(m_ConfirmPanel);

        // Prevent clicks passing through
        m_ConfirmPanel.AddComponent<Button>().onClick.AddListener(() => SetPanelVisible(false));

        // Center card
        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(m_ConfirmPanel.transform, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color32(32, 32, 44, 255);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin        = new Vector2(0.5f, 0.5f);
        cr.anchorMax        = new Vector2(0.5f, 0.5f);
        cr.pivot            = new Vector2(0.5f, 0.5f);
        cr.sizeDelta        = new Vector2(420, 200);

        // Title
        var title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(card.transform, false);
        var titleTMP = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "Return to Main Menu?";
        titleTMP.fontSize  = 24;
        titleTMP.color     = kText;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 0.55f);
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20, 0);
        trt.offsetMax = new Vector2(-20, -16);

        // Hint
        var hint = new GameObject("Hint", typeof(RectTransform));
        hint.transform.SetParent(card.transform, false);
        var hintTMP = hint.AddComponent<TextMeshProUGUI>();
        hintTMP.text      = "Press Escape again or tap outside to cancel";
        hintTMP.fontSize  = 13;
        hintTMP.color     = kTextDim;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.raycastTarget = false;
        var hrt = hint.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 0.38f);
        hrt.anchorMax = new Vector2(1, 0.55f);
        hrt.offsetMin = new Vector2(20, 0);
        hrt.offsetMax = new Vector2(-20, 0);

        // Buttons row
        var row = new GameObject("Buttons", typeof(RectTransform));
        row.transform.SetParent(card.transform, false);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 16;
        hlg.padding = new RectOffset(24, 24, 0, 0);
        var rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = new Vector2(1, 0.38f);
        rrt.offsetMin = new Vector2(0, 16);
        rrt.offsetMax = Vector2.zero;

        MakeDialogButton(row.transform, "Cancel", new Color32(52, 52, 68, 255), kTextDim,
            () => SetPanelVisible(false));
        MakeDialogButton(row.transform, "Return to Menu", kPrimary, Color.white,
            ReturnToMenu);

        m_ConfirmPanel.SetActive(false);
    }

    void MakeDialogButton(Transform parent, string label, Color bg, Color textColor, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 17;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        StretchRT(txt);
    }

    #endregion

    #region Navigation

    void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    #endregion

    static void StretchRT(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.sizeDelta        = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
    }
}
