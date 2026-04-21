using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#pragma warning disable CS0618 // Several TMP/Object APIs are deprecated in 6000.x but required for 2022.3 compatibility

/// <summary>
/// Drop-in main menu that discovers every scene in Build Settings and presents them
/// in a searchable, categorised grid.  Creates all UI at runtime — no prefabs or
/// manual wiring required.  Works on desktop, mobile, and console players.
/// </summary>
public class SceneMenu : MonoBehaviour
{
    #region Theme

    static readonly Color kBackground    = new Color32(24, 24, 32, 255);
    static readonly Color kSurface       = new Color32(38, 38, 50, 255);
    static readonly Color kSurfaceHover  = new Color32(52, 56, 72, 255);
    static readonly Color kPrimary       = new Color32(80, 140, 255, 255);
    static readonly Color kPrimaryPress  = new Color32(60, 110, 220, 255);
    static readonly Color kTextPrimary   = new Color32(230, 230, 240, 255);
    static readonly Color kTextSecondary = new Color32(130, 130, 155, 255);
    static readonly Color kSearchBg      = new Color32(32, 32, 42, 255);
    static readonly Color kScrollHandle  = new Color32(70, 70, 92, 255);
    static readonly Color kScrollTrack   = new Color32(32, 32, 42, 255);
    static readonly Color kDivider       = new Color32(50, 50, 65, 255);

    #endregion

    #region Types

    struct SceneEntry
    {
        public string displayName;
        public string subcategory;
        public string category;
        public int buildIndex;
        public string searchKey;
    }

    struct CategoryUI
    {
        public GameObject header;
        public GameObject grid;
        public TextMeshProUGUI countLabel;
        public TextMeshProUGUI arrow;
        public bool collapsed;
        public int totalCount;
    }

    #endregion

    #region State

    readonly List<SceneEntry> m_Scenes = new List<SceneEntry>();
    readonly Dictionary<string, CategoryUI> m_Categories = new Dictionary<string, CategoryUI>();
    readonly Dictionary<int, GameObject> m_Buttons = new Dictionary<int, GameObject>();
    readonly HashSet<GameObject> m_ButtonSet = new HashSet<GameObject>();
    Canvas m_Canvas;
    Sprite m_RoundSprite;
    TextMeshProUGUI m_Badge;
    ScrollRect m_ScrollRect;
    GameObject m_FirstButton;

    static readonly string[] kExcludedSegments = { "Core Platform Menu" };
    static readonly string[] kExcludedRoots    = { "Assets/Tests/", "ExternalSampleProjects/", "Packages/" };

    #endregion

    #region Lifecycle

    void Awake()
    {
        SetupCamera();
        SetupEventSystem();
        SetupCanvas();
        m_RoundSprite = CreateRoundedSprite(12);
        DiscoverScenes();
        BuildUI();

        if (m_FirstButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(m_FirstButton);
    }

    void OnDestroy()
    {
        if (m_RoundSprite != null)
        {
            if (m_RoundSprite.texture != null)
                Destroy(m_RoundSprite.texture);
            Destroy(m_RoundSprite);
        }
    }

    void LateUpdate()
    {
        if (m_ScrollRect == null || EventSystem.current == null) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !m_ButtonSet.Contains(selected)) return;

        var selectedRT = selected.GetComponent<RectTransform>();
        var contentRT  = m_ScrollRect.content;
        var viewportRT = m_ScrollRect.viewport;

        Vector3[] corners = new Vector3[4];
        selectedRT.GetWorldCorners(corners);
        float selWorldTop    = corners[1].y;
        float selWorldBottom = corners[0].y;

        viewportRT.GetWorldCorners(corners);
        float vpWorldTop    = corners[1].y;
        float vpWorldBottom = corners[0].y;

        if (selWorldBottom < vpWorldBottom)
        {
            float delta = vpWorldBottom - selWorldBottom + 10f;
            contentRT.anchoredPosition += new Vector2(0, delta / m_ScrollRect.transform.lossyScale.y);
        }
        else if (selWorldTop > vpWorldTop)
        {
            float delta = selWorldTop - vpWorldTop + 10f;
            contentRT.anchoredPosition += new Vector2(0, -delta / m_ScrollRect.transform.lossyScale.y);
        }
    }

    #endregion

    #region Infrastructure

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = kBackground;
        cam.cullingMask = 0;
    }

    static void SetupEventSystem()
    {
        var activeScene = SceneManager.GetActiveScene();
        bool hasSceneES = false;

        foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (es.gameObject.scene == activeScene)
            {
                hasSceneES = true;
            }
            else if (es.gameObject.name == "[OverlayEventSystem]")
            {
                // Clean up the ReturnToMenuOverlay's DDOL EventSystem that may
                // linger after returning to the menu.  Other persistent
                // EventSystems (e.g. from a global manager) are left alone.
                es.gameObject.SetActive(false);
                Destroy(es.gameObject);
            }
        }

        if (hasSceneES) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    void SetupCanvas()
    {
        var go = new GameObject("SceneMenuCanvas");
        go.transform.SetParent(transform, false);

        m_Canvas = go.AddComponent<Canvas>();
        m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        m_Canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    #endregion

    #region Scene Discovery

    void DiscoverScenes()
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (IsExcluded(path)) continue;

            var entry = new SceneEntry
            {
                displayName  = FormatDisplayName(path),
                subcategory  = ExtractSubcategory(path),
                category     = Categorize(path),
                buildIndex   = i,
            };
            entry.searchKey = string.Concat(entry.displayName, "\0", entry.subcategory, "\0", entry.category)
                .ToLowerInvariant();

            m_Scenes.Add(entry);
        }

        m_Scenes.Sort((a, b) =>
        {
            int c = CategoryOrder(a.category).CompareTo(CategoryOrder(b.category));
            return c != 0 ? c : string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase);
        });
    }

    static bool IsExcluded(string path)
    {
        for (int i = 0; i < kExcludedSegments.Length; i++)
            if (!string.IsNullOrEmpty(kExcludedSegments[i]) && path.Contains(kExcludedSegments[i], StringComparison.OrdinalIgnoreCase)) return true;
        for (int i = 0; i < kExcludedRoots.Length; i++)
            if (path.StartsWith(kExcludedRoots[i], StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string Categorize(string path)
    {
        if (path.StartsWith("Assets/Samples/", StringComparison.Ordinal)) return "Samples";
        if (path.StartsWith("Assets/QA/", StringComparison.Ordinal))      return "QA Tests";
        return "Other";
    }

    static int CategoryOrder(string cat)
    {
        switch (cat)
        {
            case "Samples":  return 0;
            case "QA Tests": return 1;
            default:         return 2;
        }
    }

    static string ExtractSubcategory(string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(dir)) return "";
        dir = dir.Replace('\\', '/');
        string[] parts = dir.Split('/');
        if (parts.Length < 2) return "";
        string folder = parts[parts.Length - 1];
        return SpacifyPascal(folder).Replace('_', ' ');
    }

    static string FormatDisplayName(string path)
    {
        string n = Path.GetFileNameWithoutExtension(path);

        if (n.StartsWith("ISX_", StringComparison.Ordinal))
            n = n.Substring(4);
        if (n.StartsWith("SimpleDemo_", StringComparison.Ordinal))
            n = n.Substring(11);

        string[] trimSuffixes = { "SampleScene", "Sample", "TestScene", "Scene" };
        foreach (string s in trimSuffixes)
        {
            if (n.Length > s.Length && n.EndsWith(s, StringComparison.Ordinal))
            {
                n = n.Substring(0, n.Length - s.Length);
                break;
            }
        }

        n = n.Replace('_', ' ');
        return SpacifyPascal(n).Trim();
    }

    static string SpacifyPascal(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new System.Text.StringBuilder(text.Length + 8);
        sb.Append(text[0]);
        for (int i = 1; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsUpper(c))
            {
                bool prevLower = char.IsLower(text[i - 1]);
                bool nextLower = i + 1 < text.Length && char.IsLower(text[i + 1]);
                if (prevLower || nextLower)
                    sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    #endregion

    #region UI Construction

    void BuildUI()
    {
        var root = MakePanel("Root", m_Canvas.transform, kBackground);
        Stretch(root);

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth       = true;
        vlg.childControlHeight      = true;
        vlg.childForceExpandWidth   = true;
        vlg.childForceExpandHeight  = false;
        vlg.padding  = new RectOffset(60, 60, 36, 24);
        vlg.spacing  = 16;

        BuildHeader(root.transform);
        BuildSearchBar(root.transform);
        BuildDivider(root.transform);
        BuildScrollArea(root.transform);
    }

    // ── Header ──────────────────────────────────────────────────

    void BuildHeader(Transform parent)
    {
        var go = new GameObject("Header", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetLayout(go, prefH: 90);

        var title = MakeText("Title", go.transform, "INPUT SYSTEM", 20, kPrimary,
            TextAlignmentOptions.BottomLeft);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 6;
        var tr = Rect(title);
        tr.anchorMin = new Vector2(0, 0.52f);
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        var sub = MakeText("Subtitle", go.transform, "Scene Browser", 34, kTextPrimary,
            TextAlignmentOptions.TopLeft);
        sub.fontStyle = FontStyles.Bold;
        var sr = Rect(sub);
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = new Vector2(0.75f, 0.55f);
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        m_Badge = MakeText("Badge", go.transform,
            m_Scenes.Count + (m_Scenes.Count == 1 ? " scene" : " scenes"),
            15, kTextSecondary, TextAlignmentOptions.BottomRight);
        var br = Rect(m_Badge);
        br.anchorMin = new Vector2(0.65f, 0.52f);
        br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
    }

    // ── Search ──────────────────────────────────────────────────

    void BuildSearchBar(Transform parent)
    {
        var bar = MakePanel("SearchBar", parent, kSearchBg);
        SetSliced(bar, m_RoundSprite);
        SetLayout(bar, prefH: 50);

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(bar.transform, false);
        textArea.AddComponent<RectMask2D>();
        var ta = Rect(textArea);
        ta.anchorMin = Vector2.zero;
        ta.anchorMax = Vector2.one;
        ta.offsetMin = new Vector2(20, 4);
        ta.offsetMax = new Vector2(-20, -4);

        var ph = MakeText("Placeholder", textArea.transform, "Search scenes\u2026", 18,
            kTextSecondary, TextAlignmentOptions.MidlineLeft);
        ph.fontStyle = FontStyles.Italic;
        ph.enableWordWrapping = false;
        ph.overflowMode = TextOverflowModes.Ellipsis;
        Stretch(ph.gameObject);

        var txt = MakeText("Text", textArea.transform, "", 18,
            kTextPrimary, TextAlignmentOptions.MidlineLeft);
        txt.enableWordWrapping = false;
        Stretch(txt.gameObject);

        var input = bar.AddComponent<TMP_InputField>();
        input.textViewport  = ta;
        input.textComponent = txt;
        input.placeholder   = ph;
        input.navigation    = new Navigation { mode = Navigation.Mode.None };
        input.onValueChanged.AddListener(OnSearchChanged);
    }

    // ── Divider ─────────────────────────────────────────────────

    void BuildDivider(Transform parent)
    {
        var d = MakePanel("Divider", parent, kDivider);
        SetLayout(d, prefH: 1);
    }

    // ── Scroll area ─────────────────────────────────────────────

    void BuildScrollArea(Transform parent)
    {
        var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
        scrollGo.transform.SetParent(parent, false);
        SetLayout(scrollGo, flexH: 1);

        m_ScrollRect = scrollGo.AddComponent<ScrollRect>();
        m_ScrollRect.horizontal        = false;
        m_ScrollRect.movementType      = ScrollRect.MovementType.Clamped;
        m_ScrollRect.inertia           = true;
        m_ScrollRect.decelerationRate  = 0.135f;
        m_ScrollRect.scrollSensitivity = 12;
        var sr = m_ScrollRect;

        // Viewport — needs a raycast-target Image so scroll events register
        // even when the mouse is over empty space between buttons.
        var vp = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(scrollGo.transform, false);
        var vpImg = vp.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        vp.AddComponent<RectMask2D>();
        Stretch(vp);
        sr.viewport = Rect(vp);

        // Content
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(vp.transform, false);
        var cr = Rect(content);
        cr.anchorMin = new Vector2(0, 1);
        cr.anchorMax = new Vector2(1, 1);
        cr.pivot     = new Vector2(0.5f, 1);
        cr.sizeDelta = Vector2.zero;

        var cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth       = true;
        cvlg.childControlHeight      = true;
        cvlg.childForceExpandWidth   = true;
        cvlg.childForceExpandHeight  = false;
        cvlg.spacing = 6;
        cvlg.padding = new RectOffset(0, 20, 8, 40);

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content = cr;

        BuildVerticalScrollbar(scrollGo.transform, sr);

        // Categories
        var groups = m_Scenes
            .GroupBy(s => s.category)
            .OrderBy(g => CategoryOrder(g.Key));

        bool first = true;
        foreach (var g in groups)
        {
            if (!first) MakeSpacer(content.transform, 12);
            first = false;
            BuildCategory(content.transform, g.Key, g.ToList());
        }

        if (m_Scenes.Count == 0)
        {
            var empty = MakeText("Empty", content.transform,
                "No scenes found in Build Settings.\n" +
                "Scenes are added automatically when entering Play Mode or building the player.",
                18, kTextSecondary, TextAlignmentOptions.Center);
            SetLayout(empty.gameObject, prefH: 120);
        }
    }

    // ── Scrollbar ───────────────────────────────────────────────

    void BuildVerticalScrollbar(Transform parent, ScrollRect sr)
    {
        var track = MakePanel("Scrollbar", parent, kScrollTrack);
        var tRect = Rect(track);
        tRect.anchorMin = new Vector2(1, 0);
        tRect.anchorMax = Vector2.one;
        tRect.pivot     = new Vector2(1, 0.5f);
        tRect.sizeDelta = new Vector2(8, 0);
        tRect.anchoredPosition = Vector2.zero;

        var sb = track.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;

        var sliding = new GameObject("SlidingArea", typeof(RectTransform));
        sliding.transform.SetParent(track.transform, false);
        Stretch(sliding);

        var handle = MakePanel("Handle", sliding.transform, kScrollHandle);
        SetSliced(handle, m_RoundSprite);
        Stretch(handle);

        sb.targetGraphic = handle.GetComponent<Image>();
        sb.handleRect    = Rect(handle);

        var sbColors = sb.colors;
        sbColors.normalColor      = kScrollHandle;
        sbColors.highlightedColor = new Color32(90, 90, 115, 255);
        sbColors.pressedColor     = kPrimary;
        sb.colors = sbColors;

        sr.verticalScrollbar           = sb;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        sr.verticalScrollbarSpacing    = 4;
    }

    // ── Category ────────────────────────────────────────────────

    void BuildCategory(Transform parent, string category, List<SceneEntry> scenes)
    {
        // Header
        var headerGo = new GameObject("Cat_" + category, typeof(RectTransform));
        headerGo.transform.SetParent(parent, false);
        headerGo.AddComponent<Image>().color = Color.clear;
        SetLayout(headerGo, prefH: 36);

        var headerBtn = headerGo.AddComponent<Button>();
        headerBtn.transition = Selectable.Transition.None;
        headerBtn.navigation = new Navigation { mode = Navigation.Mode.None };

        var arrow = MakeText("Arrow", headerGo.transform, "v", 14, kPrimary,
            TextAlignmentOptions.MidlineLeft);
        var ar = Rect(arrow);
        ar.anchorMin = Vector2.zero;
        ar.anchorMax = new Vector2(0, 1);
        ar.offsetMin = Vector2.zero;
        ar.offsetMax = new Vector2(22, 0);

        var label = MakeText("Label", headerGo.transform, category.ToUpperInvariant(), 16, kPrimary,
            TextAlignmentOptions.MidlineLeft);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 3;
        var lr = Rect(label);
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(26, 0);
        lr.offsetMax = new Vector2(-80, 0);

        var countTxt = MakeText("Count", headerGo.transform,
            "(" + scenes.Count + ")", 13, kTextSecondary, TextAlignmentOptions.MidlineRight);
        var crr = Rect(countTxt);
        crr.anchorMin = new Vector2(0.75f, 0);
        crr.anchorMax = Vector2.one;
        crr.offsetMin = crr.offsetMax = Vector2.zero;

        // Grid
        var gridGo = new GameObject("Grid_" + category, typeof(RectTransform));
        gridGo.transform.SetParent(parent, false);

        var grid = gridGo.AddComponent<GridLayoutGroup>();
        grid.cellSize       = new Vector2(280, 82);
        grid.spacing        = new Vector2(10, 10);
        grid.constraint     = GridLayoutGroup.Constraint.Flexible;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.startAxis      = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner    = GridLayoutGroup.Corner.UpperLeft;

        foreach (var scene in scenes)
            BuildSceneButton(gridGo.transform, scene);

        // Store state
        string key = category;
        m_Categories[key] = new CategoryUI
        {
            header     = headerGo,
            grid       = gridGo,
            countLabel = countTxt,
            arrow      = arrow,
            collapsed  = false,
            totalCount = scenes.Count,
        };

        headerBtn.onClick.AddListener(() => ToggleCategory(key));
    }

    // ── Scene button ────────────────────────────────────────────

    void BuildSceneButton(Transform parent, SceneEntry entry)
    {
        var go = MakePanel("Scene_" + entry.buildIndex, parent, kSurface);
        SetSliced(go, m_RoundSprite);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        var c = btn.colors;
        c.normalColor      = kSurface;
        c.highlightedColor = kSurfaceHover;
        c.pressedColor     = kPrimaryPress;
        c.selectedColor    = kPrimary;
        c.fadeDuration     = 0.08f;
        btn.colors = c;

        int idx = entry.buildIndex;
        btn.onClick.AddListener(() => LoadScene(idx));

        if (m_FirstButton == null)
            m_FirstButton = go;

        // Display name
        var nameT = MakeText("Name", go.transform, entry.displayName, 15, kTextPrimary,
            TextAlignmentOptions.Center);
        nameT.overflowMode      = TextOverflowModes.Ellipsis;
        nameT.enableWordWrapping = false;
        var nr = Rect(nameT);
        nr.anchorMin = new Vector2(0.04f, 0.38f);
        nr.anchorMax = new Vector2(0.96f, 0.94f);
        nr.offsetMin = nr.offsetMax = Vector2.zero;

        // Subcategory hint
        if (!string.IsNullOrEmpty(entry.subcategory))
        {
            var subT = MakeText("Sub", go.transform, entry.subcategory, 11, kTextSecondary,
                TextAlignmentOptions.Center);
            subT.overflowMode      = TextOverflowModes.Ellipsis;
            subT.enableWordWrapping = false;
            var srr = Rect(subT);
            srr.anchorMin = new Vector2(0.04f, 0.06f);
            srr.anchorMax = new Vector2(0.96f, 0.38f);
            srr.offsetMin = srr.offsetMax = Vector2.zero;
        }

        m_Buttons[entry.buildIndex] = go;
        m_ButtonSet.Add(go);
    }

    #endregion

    #region Interaction

    void ToggleCategory(string key)
    {
        if (!m_Categories.TryGetValue(key, out var state)) return;
        state.collapsed = !state.collapsed;
        state.grid.SetActive(!state.collapsed);
        state.arrow.text = state.collapsed ? ">" : "v";
        m_Categories[key] = state;
    }

    void OnSearchChanged(string raw)
    {
        string query = (raw ?? "").Trim().ToLowerInvariant();
        bool filtering = query.Length > 0;

        foreach (var s in m_Scenes)
            m_Buttons[s.buildIndex].SetActive(!filtering || s.searchKey.Contains(query));

        int totalVisible = 0;
        foreach (var kvp in m_Categories)
        {
            var st = kvp.Value;
            int vis = 0;
            foreach (var s in m_Scenes)
                if (s.category == kvp.Key && m_Buttons[s.buildIndex].activeSelf) vis++;
            totalVisible += vis;

            st.header.SetActive(vis > 0);
            st.grid.SetActive(vis > 0 && (filtering || !st.collapsed));
            st.countLabel.text = filtering
                ? "(" + vis + "/" + st.totalCount + ")"
                : "(" + st.totalCount + ")";
        }

        if (m_Badge != null)
        {
            m_Badge.text = filtering
                ? totalVisible + " matching"
                : m_Scenes.Count + (m_Scenes.Count == 1 ? " scene" : " scenes");
        }
    }

    void LoadScene(int buildIndex)
    {
        ReturnToMenuOverlay.Show();
        SceneManager.LoadScene(buildIndex);
    }

    #endregion

    #region UI Helpers

    static GameObject MakePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size,
        Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = size;
        t.color         = color;
        t.alignment     = align;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.sizeDelta        = Vector2.zero;
        r.anchoredPosition = Vector2.zero;
    }

    static RectTransform Rect(Component c) => c.GetComponent<RectTransform>();
    static RectTransform Rect(GameObject g) => g.GetComponent<RectTransform>();

    static void SetLayout(GameObject go, float prefH = -1, float flexH = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefH >= 0) le.preferredHeight = prefH;
        if (flexH >= 0) le.flexibleHeight  = flexH;
    }

    static void MakeSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetLayout(go, prefH: height);
    }

    static void SetSliced(GameObject go, Sprite sprite)
    {
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type   = Image.Type.Sliced;
    }

    /// <summary>
    /// Generates a white rounded-rectangle 9-slice sprite at runtime.
    /// Tint it via Image.color; corners stay crisp at any size.
    /// </summary>
    static Sprite CreateRoundedSprite(int radius)
    {
        int size = radius * 2 + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius + 0.5f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    #endregion
}
