using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns floating gold coin popups when coins are gained from a world interaction.
/// Auto-bootstraps itself at runtime.
/// </summary>
public class CoinFloatingPopupSpawner : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("Seconds the popup stays alive.")]
    public float lifetimeSeconds = 0.8f;

    [Tooltip("How far (in UI pixels) the text drifts upward over its lifetime.")]
    public float driftUpPixels = 80f;

    [Tooltip("Use unscaled time so popups still animate during slow-mo/pause UI.")]
    public bool useUnscaledTime = true;

    [Header("Text")]
    [Tooltip("Extra font size added to all coin popups (useful for mobile readability).")]
    public int extraFontSize = 40;

    [Tooltip("Base font size before extraFontSize.")]
    public int baseFontSize = 24;

    [Tooltip("Gold color for coin popups.")]
    public Color goldColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Text format. {0}=delta coins.")]
    public string format = "+{0}";

    [Tooltip("Optional override font. If null, tries built-in fonts then OS fallback.")]
    public Font fontOverride;

    [Tooltip("If null, uses Camera.main.")]
    public Camera worldCamera;

    private Canvas canvas;
    private RectTransform popupRoot;

    private static Font s_fallbackFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CoinFloatingPopupSpawner>() != null) return;

        GameObject go = new GameObject("CoinFloatingPopupSpawner");
        go.AddComponent<CoinFloatingPopupSpawner>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        CoinManager.OnCoinsGained += HandleCoinsGained;
    }

    private void Start()
    {
        EnsureUI();
    }

    private void OnDisable()
    {
        CoinManager.OnCoinsGained -= HandleCoinsGained;
    }

    private float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void HandleCoinsGained(Vector3 worldPos, int delta)
    {
        if (delta == 0) return;

        EnsureUI();

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPos);
        if (screen.z < 0f) return; // behind camera

        SpawnPopup(delta, screen);
    }

    private void SpawnPopup(int delta, Vector3 screenPoint)
    {
        if (canvas == null || popupRoot == null) return;

        GameObject go = new GameObject("CoinPopup", typeof(RectTransform), typeof(Text), typeof(FloatingScorePopup));
        go.transform.SetParent(popupRoot, false);
        go.transform.SetAsLastSibling();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupRoot, screenPoint, uiCam, out Vector2 localPoint))
        {
            rt.anchoredPosition = localPoint;
        }

        Text t = go.GetComponent<Text>();
        t.raycastTarget = false;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        int size = Mathf.Max(8, baseFontSize + extraFontSize);
        t.fontSize = size;
        t.font = fontOverride != null ? fontOverride : (TryGetBuiltinFont() ?? GetFallbackFont());
        t.color = goldColor;
        t.text = string.Format(format, delta);

        float w = Mathf.Clamp(t.preferredWidth + 16f, 80f, 1000f);
        float h = Mathf.Clamp(t.preferredHeight + 8f, 30f, 300f);
        rt.sizeDelta = new Vector2(w, h);

        FloatingScorePopup popup = go.GetComponent<FloatingScorePopup>();
        popup.Initialize(
            rt,
            t,
            Now(),
            Mathf.Max(0.05f, lifetimeSeconds),
            driftUpPixels,
            useUnscaledTime,
            useAnimatedRainbow: false,
            rainbowStartHue: 0f,
            rainbowCyclesOverLifetime: 0f
        );
    }

    private void EnsureUI()
    {
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject c = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = c.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        if (popupRoot == null)
        {
            GameObject root = new GameObject("FloatingCoinPopups", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            popupRoot = root.GetComponent<RectTransform>();
            popupRoot.anchorMin = Vector2.zero;
            popupRoot.anchorMax = Vector2.one;
            popupRoot.offsetMin = Vector2.zero;
            popupRoot.offsetMax = Vector2.zero;
        }
    }

    private static Font TryGetBuiltinFont()
    {
        Font f;

        try
        {
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null) return f;
        }
        catch { }

        try
        {
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f != null) return f;
        }
        catch { }

        return null;
    }

    private static Font GetFallbackFont()
    {
        if (s_fallbackFont != null) return s_fallbackFont;
        s_fallbackFont = Font.CreateDynamicFontFromOSFont("Arial", 18);
        return s_fallbackFont;
    }
}
