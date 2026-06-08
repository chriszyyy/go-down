using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 首次进入游戏的新手教程引导（运行时构建的 uGUI 覆盖层）。
/// 三步：1) 介绍六边形球；2) 点击消除方块；3) 道具使用。
/// 每步用半透明蒙层 + 镂空高亮目标元素 + 说明文字；点击任意处进入下一步。
/// 通过 PlayerPrefs 记录是否已看过，只在第一次显示。教程期间用 UIPause 暂停游戏。
/// </summary>
public class TutorialOverlay : MonoBehaviour
{
    private const string KEY_SEEN = "Tutorial_Seen_v1";

    private static TutorialOverlay s_instance;

    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject root;

    // 蒙层四块 + 高亮边框 + 说明卡
    private Image dimTop, dimBottom, dimLeft, dimRight;
    private Image[] frame; // 4 条高亮边
    private GameObject card;
    private Text cardText;
    private Text tapHint;

    private int step;
    private bool active;
    private bool pauseAcquired;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_instance = null;
    }

    /// <summary>由 StartMenuPanel.OnPlay 在首次开始游戏时调用。</summary>
    public static void TryShowFirstTime()
    {
        if (PlayerPrefs.GetInt(KEY_SEEN, 0) != 0) return;
        EnsureInstance();
        s_instance.Begin();
    }

    /// <summary>调试用：清除教程已看标记，下次开始游戏会重新引导。</summary>
    public static void ResetSeenFlag()
    {
        PlayerPrefs.DeleteKey(KEY_SEEN);
        PlayerPrefs.Save();
    }

    private static void EnsureInstance()
    {
        if (s_instance != null) return;
        var go = new GameObject("TutorialOverlay");
        s_instance = go.AddComponent<TutorialOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(gameObject); return; }
        s_instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        BuildUi();
        if (root != null) root.SetActive(false);
    }

    private void Begin()
    {
        EnsureEventSystem();
        if (!pauseAcquired) { UIPause.Acquire(); pauseAcquired = true; }
        active = true;
        step = 0;
        if (root != null) root.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(ShowStepWhenReady());
    }

    private IEnumerator ShowStepWhenReady()
    {
        // 等待塔与球生成（gameRoot 激活后 TowerBuilder.Start 在本帧稍后才跑）
        float t = 0f;
        while (t < 2f && Camera.main == null)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // 给一帧让球/方块就位
        yield return null;
        BuildStep();
    }

    private void LateUpdate()
    {
        // 教程激活时每帧重算高亮：跟踪球/方块/道具的实际屏幕位置，
        // 兼顾延迟生成与相机移动，避免依赖单次协程时机。
        if (active && root != null && root.activeSelf)
        {
            BuildStep();
        }
    }

    // ---------------- 步骤 ----------------

    private void BuildStep()
    {
        Rect highlight;
        string text;
        switch (step)
        {
            case 0:
                highlight = GetBallScreenRect();
                text = "This is your hexagon ball. Keep it balanced on top of the tower — don't let it fall off the sides!";
                break;
            case 1:
                highlight = GetBlocksScreenRect();
                text = "Tap blocks to remove them and the hexagon drops down. Watch out for dark TRAP blocks — clearing one also blows up the blocks around it!";
                break;
            default:
                highlight = GetToolsScreenRect();
                text = "Two tools to help you:\n• Reset — re-centers the hexagon ball.\n• Rainbow — turns nearby blocks into rainbow blocks (they build a combo for bonus score multipliers) and clears any visible trap blocks.";
                break;
        }

        ApplyHighlight(highlight, text);
    }

    private void Advance()
    {
        if (!active) return;
        step++;
        if (step >= 3)
        {
            Finish();
            return;
        }
        BuildStep();
    }

    private void Finish()
    {
        active = false;
        PlayerPrefs.SetInt(KEY_SEEN, 1);
        PlayerPrefs.Save();
        if (root != null) root.SetActive(false);
        if (pauseAcquired) { UIPause.Release(); pauseAcquired = false; }
    }

    // ---------------- 目标矩形（屏幕像素） ----------------

    private Rect GetBallScreenRect()
    {
        var cam = Camera.main;
        var ball = FindFirstObjectByType<HexagonBall>();
        if (cam == null || ball == null) return CenterFallbackRect();

        Vector3 c = ball.transform.position;
        Vector3 sc = cam.WorldToScreenPoint(c);
        // 用球半径估算屏幕尺寸
        Vector3 edge = cam.WorldToScreenPoint(c + cam.transform.right * 1.0f);
        float r = Mathf.Max(40f, Vector2.Distance(new Vector2(sc.x, sc.y), new Vector2(edge.x, edge.y)));
        float pad = r * 0.6f;
        float half = r + pad;
        return new Rect(sc.x - half, sc.y - half, half * 2f, half * 2f);
    }

    private Rect GetBlocksScreenRect()
    {
        var cam = Camera.main;
        if (cam == null) return CenterFallbackRect();

        // 取屏幕中上部、在球下方的一片方块区域
        var blocks = FindObjectsByType<TowerBlock>(FindObjectsSortMode.None);
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        int count = 0;
        foreach (var b in blocks)
        {
            if (b == null) continue;
            Vector3 sc = cam.WorldToScreenPoint(b.transform.position);
            if (sc.z < 0f) continue;
            if (sc.y < Screen.height * 0.25f || sc.y > Screen.height * 0.85f) continue;
            minX = Mathf.Min(minX, sc.x); maxX = Mathf.Max(maxX, sc.x);
            minY = Mathf.Min(minY, sc.y); maxY = Mathf.Max(maxY, sc.y);
            count++;
        }
        if (count == 0) return CenterFallbackRect();

        float padX = Screen.width * 0.06f;
        float padY = Screen.height * 0.03f;
        return new Rect(minX - padX, minY - padY,
                        (maxX - minX) + padX * 2f, (maxY - minY) + padY * 2f);
    }

    private Rect GetToolsScreenRect()
    {
        var toolbar = FindFirstObjectByType<RightToolbarUI>(FindObjectsInactive.Include);
        var rects = new List<RectTransform>();
        if (toolbar != null)
        {
            if (toolbar.resetButton != null) rects.Add(toolbar.resetButton.GetComponent<RectTransform>());
            if (toolbar.rainbowButton != null) rects.Add(toolbar.rainbowButton.GetComponent<RectTransform>());
        }
        if (rects.Count == 0)
        {
            // 兜底：屏幕右侧中部
            float w = Screen.width * 0.26f;
            float h = Screen.height * 0.22f;
            return new Rect(Screen.width - w - Screen.width * 0.03f, Screen.height * 0.4f, w, h);
        }

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        var corners = new Vector3[4];
        foreach (var rt in rects)
        {
            if (rt == null) continue;
            var cam = GetCanvasCamera(rt);
            rt.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                minX = Mathf.Min(minX, sp.x); maxX = Mathf.Max(maxX, sp.x);
                minY = Mathf.Min(minY, sp.y); maxY = Mathf.Max(maxY, sp.y);
            }
        }
        float padX = Screen.width * 0.025f;
        float padY = Screen.height * 0.015f;
        return new Rect(minX - padX, minY - padY,
                        (maxX - minX) + padX * 2f, (maxY - minY) + padY * 2f);
    }

    private static Camera GetCanvasCamera(RectTransform rt)
    {
        var cv = rt.GetComponentInParent<Canvas>();
        if (cv == null) return null;
        return cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.worldCamera;
    }

    private Rect CenterFallbackRect()
    {
        float w = Screen.width * 0.5f;
        float h = Screen.width * 0.5f;
        return new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h);
    }

    // ---------------- 应用高亮 ----------------

    private void ApplyHighlight(Rect screenRect, string text)
    {
        // 屏幕矩形 -> 画布本地坐标
        Vector2 bl = ScreenToCanvas(new Vector2(screenRect.xMin, screenRect.yMin));
        Vector2 tr = ScreenToCanvas(new Vector2(screenRect.xMax, screenRect.yMax));
        float hxMin = Mathf.Min(bl.x, tr.x), hxMax = Mathf.Max(bl.x, tr.x);
        float hyMin = Mathf.Min(bl.y, tr.y), hyMax = Mathf.Max(bl.y, tr.y);

        float W = canvasRect.rect.width, H = canvasRect.rect.height;
        float left = -W * 0.5f, right = W * 0.5f, bottom = -H * 0.5f, top = H * 0.5f;

        // 夹紧到画布范围
        hxMin = Mathf.Clamp(hxMin, left, right);
        hxMax = Mathf.Clamp(hxMax, left, right);
        hyMin = Mathf.Clamp(hyMin, bottom, top);
        hyMax = Mathf.Clamp(hyMax, bottom, top);

        SetLocalRect(dimTop.rectTransform, left, hyMax, right, top);
        SetLocalRect(dimBottom.rectTransform, left, bottom, right, hyMin);
        SetLocalRect(dimLeft.rectTransform, left, hyMin, hxMin, hyMax);
        SetLocalRect(dimRight.rectTransform, hxMax, hyMin, right, hyMax);

        // 高亮边框（四条）
        float fw = 6f;
        SetLocalRect(frame[0].rectTransform, hxMin - fw, hyMax, hxMax + fw, hyMax + fw); // top
        SetLocalRect(frame[1].rectTransform, hxMin - fw, hyMin - fw, hxMax + fw, hyMin); // bottom
        SetLocalRect(frame[2].rectTransform, hxMin - fw, hyMin, hxMin, hyMax);           // left
        SetLocalRect(frame[3].rectTransform, hxMax, hyMin, hxMax + fw, hyMax);           // right

        cardText.text = text;

        // 说明卡放在高亮区上方或下方（取空间较大的一侧）
        float cardH = 420f;
        float margin = 40f;
        var cardRect = card.GetComponent<RectTransform>();
        bool below = (hyMin - bottom) > (top - hyMax); // 下方空间更大
        float cyMax, cyMin;
        if (below)
        {
            cyMax = hyMin - margin;
            cyMin = cyMax - cardH;
        }
        else
        {
            cyMin = hyMax + margin;
            cyMax = cyMin + cardH;
        }
        cyMin = Mathf.Clamp(cyMin, bottom + 20f, top - cardH - 20f);
        cyMax = cyMin + cardH;
        float cardMarginX = W * 0.08f;
        SetLocalRect(cardRect, left + cardMarginX, cyMin, right - cardMarginX, cyMax);
    }

    private Vector2 ScreenToCanvas(Vector2 screenPoint)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out local);
        return local;
    }

    private static void SetLocalRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
    }

    // ---------------- 构建 UI ----------------

    private void BuildUi()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000; // 低于退出确认(32767)，高于普通 HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasRect = (RectTransform)transform;

        root = new GameObject("TutorialRoot");
        root.transform.SetParent(transform, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero; rootRect.offsetMax = Vector2.zero;

        Color dim = new Color(0f, 0f, 0f, 0.74f);
        dimTop = CreateImage("DimTop", root.transform, dim, true);
        dimBottom = CreateImage("DimBottom", root.transform, dim, true);
        dimLeft = CreateImage("DimLeft", root.transform, dim, true);
        dimRight = CreateImage("DimRight", root.transform, dim, true);

        // 高亮边框
        Color frameCol = new Color(1f, 0.95f, 0.5f, 0.95f);
        frame = new Image[4];
        for (int i = 0; i < 4; i++)
            frame[i] = CreateImage("Frame" + i, root.transform, frameCol, false);

        // 说明卡
        card = new GameObject("Card");
        card.transform.SetParent(root.transform, false);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.09f, 0.24f, 0.97f);

        cardText = CreateText("CardText", card.transform, "", 34, FontStyle.Bold);
        var ctRect = cardText.rectTransform;
        ctRect.anchorMin = new Vector2(0.07f, 0.30f);
        ctRect.anchorMax = new Vector2(0.93f, 0.92f);
        ctRect.offsetMin = Vector2.zero; ctRect.offsetMax = Vector2.zero;

        tapHint = CreateText("TapHint", card.transform, "Tap anywhere to continue ▶", 26, FontStyle.Normal);
        tapHint.color = new Color(1f, 0.95f, 0.5f, 1f);
        var thRect = tapHint.rectTransform;
        thRect.anchorMin = new Vector2(0.07f, 0.08f);
        thRect.anchorMax = new Vector2(0.93f, 0.26f);
        thRect.offsetMin = Vector2.zero; thRect.offsetMax = Vector2.zero;

        // 顶层透明按钮：点击任意处进入下一步
        var advance = new GameObject("AdvanceCatcher");
        advance.transform.SetParent(root.transform, false);
        var advImg = advance.AddComponent<Image>();
        advImg.color = new Color(0f, 0f, 0f, 0f);
        advImg.raycastTarget = true;
        var advRect = advImg.rectTransform;
        advRect.anchorMin = Vector2.zero; advRect.anchorMax = Vector2.one;
        advRect.offsetMin = Vector2.zero; advRect.offsetMax = Vector2.zero;
        var advBtn = advance.AddComponent<Button>();
        advBtn.transition = Selectable.Transition.None;
        advBtn.onClick.AddListener(Advance);

        // Skip 按钮（右上角）
        var skip = CreateButton("Skip", root.transform, "SKIP", new Color(0.2f, 0.2f, 0.28f, 0.9f));
        var skipRect = skip.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.74f, 0.93f);
        skipRect.anchorMax = new Vector2(0.97f, 0.985f);
        skipRect.offsetMin = Vector2.zero; skipRect.offsetMax = Vector2.zero;
        skip.onClick.AddListener(Finish);
        skip.transform.SetAsLastSibling();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private Image CreateImage(string name, Transform parent, Color color, bool raycast)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        return label;
    }

    private Button CreateButton(string name, Transform parent, string text, Color color)
    {
        var image = CreateImage(name, parent, color, true);
        var button = image.gameObject.AddComponent<Button>();
        var label = CreateText("Label", image.transform, text, 26, FontStyle.Bold);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
        return button;
    }
}
