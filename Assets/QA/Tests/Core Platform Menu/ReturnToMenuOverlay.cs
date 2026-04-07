using System.Collections;
using System.Collections.Generic;
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
///
/// When the confirm panel is open the active scene's root objects are disabled so
/// that scene-level input handling (VirtualMouseInput, custom pointer logic, etc.)
/// cannot interfere with the overlay buttons.
///
/// The floating Menu button uses raw <c>Mouse.current</c> / <c>Touchscreen.current</c>
/// polling in <c>Update()</c> so it responds to clicks even in scenes where the
/// EventSystem doesn't route events to this overlay's canvas.
/// </summary>
public class ReturnToMenuOverlay : MonoBehaviour
{
    static ReturnToMenuOverlay s_Instance;

    InputAction m_BackAction;
    GameObject m_ConfirmPanel;
    GameObject m_ReturnButton;
    Button m_DimmerButton;
    RectTransform m_MenuButtonRect;
    bool m_PanelVisible;
    float m_LastToggleTime;

    Camera m_HelperCamera;
    EventSystem m_OwnEventSystem;
    readonly List<GameObject> m_SuspendedRoots = new List<GameObject>();

    static readonly Color kOverlay  = new Color(0f, 0f, 0f, 0.7f);
    static readonly Color kBtnNorm  = new Color32(42, 42, 56, 230);
    static readonly Color kBtnHover = new Color32(60, 70, 100, 240);
    static readonly Color kPrimary  = new Color32(80, 140, 255, 255);
    static readonly Color kText     = new Color32(230, 230, 240, 255);
    static readonly Color kTextDim  = new Color32(160, 160, 180, 255);

    // ── Public API ──────────────────────────────────────────────

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

    // ── Lifecycle ───────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        SetupInput();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        ResumeActiveScene();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        m_BackAction?.Dispose();
        if (m_OwnEventSystem != null)
            Destroy(m_OwnEventSystem.gameObject);
        if (s_Instance == this) s_Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            Hide();
            return;
        }
        SetPanelVisible(false);
    }

    // ── Input ───────────────────────────────────────────────────

    void SetupInput()
    {
        m_BackAction = new InputAction("BackToMenu", InputActionType.Button);
        m_BackAction.AddBinding("<Keyboard>/escape");
        m_BackAction.AddBinding("<Gamepad>/select");
        m_BackAction.AddBinding("<Gamepad>/start").WithInteraction("Hold(duration=0.5)");
        m_BackAction.performed += _ => TogglePanel();
        m_BackAction.Enable();
    }

    /// <summary>
    /// Raw pointer polling that bypasses the EventSystem entirely.  This ensures the
    /// Menu button responds to clicks even in scenes where a VirtualMouse or other
    /// synthetic pointer prevents normal EventSystem raycasting to this overlay.
    /// </summary>
    void Update()
    {
        if (m_PanelVisible || m_MenuButtonRect == null) return;

        Vector2 pos = default;
        bool pressed = false;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            pressed = true;
        }

        if (!pressed)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                pos = touch.primaryTouch.position.ReadValue();
                pressed = true;
            }
        }

        if (pressed && RectTransformUtility.RectangleContainsScreenPoint(m_MenuButtonRect, pos, null))
            TogglePanel();
    }

    void TogglePanel()
    {
        if (Time.unscaledTime - m_LastToggleTime < 0.3f) return;
        m_LastToggleTime = Time.unscaledTime;
        SetPanelVisible(!m_PanelVisible);
    }

    void SetPanelVisible(bool visible)
    {
        m_PanelVisible = visible;

        if (visible)
            SuspendActiveScene();
        else
            ResumeActiveScene();

        if (m_ConfirmPanel != null)
            m_ConfirmPanel.SetActive(visible);

        if (visible)
        {
            // Disable dimmer clicks until the pointer that opened the panel is
            // released, otherwise the trailing PointerUp registers as a click on
            // the dimmer and immediately closes the panel.
            if (m_DimmerButton != null)
            {
                m_DimmerButton.interactable = false;
                StartCoroutine(EnableDimmerAfterRelease());
            }

            if (m_ReturnButton != null)
                StartCoroutine(SelectNextFrame(m_ReturnButton));
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    IEnumerator EnableDimmerAfterRelease()
    {
        while ((Mouse.current != null && Mouse.current.leftButton.isPressed) ||
               (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
            yield return null;

        yield return null;

        if (m_DimmerButton != null && m_PanelVisible)
            m_DimmerButton.interactable = true;
    }

    IEnumerator SelectNextFrame(GameObject target)
    {
        yield return null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(target);
    }

    // ── Scene suspend / resume ──────────────────────────────────

    void SuspendActiveScene()
    {
        m_SuspendedRoots.Clear();
        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.activeSelf)
            {
                root.SetActive(false);
                m_SuspendedRoots.Add(root);
            }
        }

        if (m_HelperCamera != null)
            m_HelperCamera.enabled = true;

        ActivateOwnEventSystem();
    }

    void ResumeActiveScene()
    {
        DeactivateOwnEventSystem();

        if (m_HelperCamera != null)
            m_HelperCamera.enabled = false;

        foreach (var root in m_SuspendedRoots)
        {
            if (root != null)
                root.SetActive(true);
        }
        m_SuspendedRoots.Clear();
    }

    void ActivateOwnEventSystem()
    {
        if (m_OwnEventSystem != null)
        {
            m_OwnEventSystem.gameObject.SetActive(true);
            return;
        }

        var existing = FindAnyObjectByType<EventSystem>();
        if (existing != null && existing.GetComponent<InputSystemUIInputModule>() != null)
            return;

        var go = new GameObject("[OverlayEventSystem]");
        DontDestroyOnLoad(go);
        m_OwnEventSystem = go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    void DeactivateOwnEventSystem()
    {
        if (m_OwnEventSystem != null)
            m_OwnEventSystem.gameObject.SetActive(false);
    }

    // ── UI construction ─────────────────────────────────────────

    void BuildUI()
    {
        // Helper camera activates while the scene is suspended so Unity doesn't
        // show the "No cameras rendering" editor overlay behind the dialog.
        var camGo = new GameObject("OverlayCamera");
        camGo.transform.SetParent(transform, false);
        m_HelperCamera = camGo.AddComponent<Camera>();
        m_HelperCamera.clearFlags       = CameraClearFlags.SolidColor;
        m_HelperCamera.backgroundColor  = new Color32(18, 18, 24, 255);
        m_HelperCamera.cullingMask      = 0;
        m_HelperCamera.depth            = -100;
        m_HelperCamera.enabled          = false;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

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

        m_MenuButtonRect = go.GetComponent<RectTransform>();
        m_MenuButtonRect.anchorMin        = new Vector2(0, 1);
        m_MenuButtonRect.anchorMax        = new Vector2(0, 1);
        m_MenuButtonRect.pivot            = new Vector2(0, 1);
        m_MenuButtonRect.anchoredPosition = new Vector2(16, -16);
        m_MenuButtonRect.sizeDelta        = new Vector2(120, 44);

        // Button component provides visual hover/press feedback only.  No onClick
        // listener — the actual click is detected by raw polling in Update() which
        // fires on press.  Adding onClick would cause a double-toggle (open on
        // press via Update, close on release via onClick) if the hold exceeds the
        // cooldown window.
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor      = kBtnNorm;
        c.highlightedColor = kBtnHover;
        c.pressedColor     = kPrimary;
        c.fadeDuration     = 0.08f;
        btn.colors = c;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text          = "Menu";
        tmp.fontSize      = 18;
        tmp.color         = kText;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        StretchRT(txt);
    }

    void BuildConfirmPanel()
    {
        m_ConfirmPanel = new GameObject("ConfirmPanel", typeof(RectTransform));
        m_ConfirmPanel.transform.SetParent(transform, false);
        var dimmer = m_ConfirmPanel.AddComponent<Image>();
        dimmer.color = kOverlay;
        StretchRT(m_ConfirmPanel);

        m_DimmerButton = m_ConfirmPanel.AddComponent<Button>();
        m_DimmerButton.onClick.AddListener(() => SetPanelVisible(false));

        // Center card
        var card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(m_ConfirmPanel.transform, false);
        card.AddComponent<Image>().color = new Color32(32, 32, 44, 255);
        var cr = card.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(420, 200);

        // Title
        var title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(card.transform, false);
        var titleTMP = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text          = "Return to Main Menu?";
        titleTMP.fontSize      = 24;
        titleTMP.color         = kText;
        titleTMP.alignment     = TextAlignmentOptions.Center;
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
        hintTMP.text          = "Press Escape or hold Start to cancel";
        hintTMP.fontSize      = 13;
        hintTMP.color         = kTextDim;
        hintTMP.alignment     = TextAlignmentOptions.Center;
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

        var cancelBtn = MakeDialogButton(row.transform, "Cancel", new Color32(52, 52, 68, 255), kTextDim,
            () => SetPanelVisible(false));
        m_ReturnButton = MakeDialogButton(row.transform, "Return to Menu", kPrimary, Color.white,
            ReturnToMenu);

        var cancelNav = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = m_ReturnButton.GetComponent<Button>()
        };
        cancelBtn.GetComponent<Button>().navigation = cancelNav;

        var returnNav = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = cancelBtn.GetComponent<Button>()
        };
        m_ReturnButton.GetComponent<Button>().navigation = returnNav;

        m_ConfirmPanel.SetActive(false);
    }

    GameObject MakeDialogButton(Transform parent, string label, Color bg, Color textColor,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        btn.onClick.AddListener(action);

        var colors = btn.colors;
        colors.selectedColor    = kBtnHover;
        colors.highlightedColor = kBtnHover;
        btn.colors = colors;

        var txt = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 17;
        tmp.color         = textColor;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        StretchRT(txt);

        return go;
    }

    // ── Navigation ──────────────────────────────────────────────

    void ReturnToMenu()
    {
        SceneManager.LoadScene(0);
    }

    // ── Helpers ─────────────────────────────────────────────────

    static void StretchRT(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.sizeDelta        = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
    }
}
