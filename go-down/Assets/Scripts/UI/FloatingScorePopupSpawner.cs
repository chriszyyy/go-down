using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns floating score popups (like damage numbers) when score is awarded.
/// Auto-bootstraps itself at runtime.
/// </summary>
public class FloatingScorePopupSpawner : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("Seconds the popup stays alive.")]
    public float lifetimeSeconds = 0.8f;

    [Tooltip("How far (in UI pixels) the text drifts upward over its lifetime.")]
    public float driftUpPixels = 80f;

    [Tooltip("Use unscaled time so popups still animate during reward/slow-mo UI.")]
    public bool useUnscaledTime = true;

    [Header("Text")]
    [Tooltip("Extra font size added to all popups (useful for mobile readability).")]
    public int extraFontSize = 40;

    [Tooltip("Default font size for normal blocks.")]
    public int normalFontSize = 22;

    [Tooltip("Font size for special/rainbow block popups.")]
    public int specialFontSize = 36;

    [Tooltip("Font size for popups during reward mode.")]
    public int rewardFontSize = 34;

    [Tooltip("Normal popup color when not using rainbow.")]
    public Color normalColor = Color.white;

    [Tooltip("Special popup color when not using rainbow.")]
    public Color specialColor = new Color(1f, 0.92f, 0.2f, 1f);

    [Tooltip("Whether special/rainbow blocks use animated rainbow color.")]
    public bool specialUseRainbow = true;

    [Tooltip("Whether reward mode popups use animated rainbow color.")]
    public bool rewardUseRainbow = true;

    [Tooltip("How many hue cycles happen over the popup lifetime when rainbow is enabled.")]
    public float rainbowCyclesOverLifetime = 1.5f;

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
        if (FindFirstObjectByType<FloatingScorePopupSpawner>() != null) return;

        GameObject go = new GameObject("FloatingScorePopupSpawner");
        go.AddComponent<FloatingScorePopupSpawner>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        ScoreManager.OnScoreGained += HandleScoreGained;
    }

    private void Start()
    {
        EnsureUI();
    }

    private void OnDisable()
    {
        ScoreManager.OnScoreGained -= HandleScoreGained;
    }

    private float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void HandleScoreGained(Vector3 worldPos, int delta, bool isSpecialBlock, bool isRewardMode)
    {
        if (delta == 0) return;

        EnsureUI();

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPos);
        if (screen.z < 0f) return; // behind camera

        SpawnPopup(delta, screen, isSpecialBlock, isRewardMode);
    }

    private void SpawnPopup(int delta, Vector3 screenPoint, bool isSpecialBlock, bool isRewardMode)
    {
        if (canvas == null || popupRoot == null) return;

        GameObject go = new GameObject("ScorePopup", typeof(RectTransform), typeof(Text), typeof(FloatingScorePopup));
        go.transform.SetParent(popupRoot, false);

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
        // Priority: special/rainbow block > reward mode > normal.
        int size = isSpecialBlock ? specialFontSize : (isRewardMode ? rewardFontSize : normalFontSize);
        size += extraFontSize;
        t.fontSize = Mathf.Max(8, size);
        t.font = fontOverride != null ? fontOverride : (TryGetBuiltinFont() ?? GetFallbackFont());
        bool useRainbow = (isRewardMode && rewardUseRainbow) || (isSpecialBlock && specialUseRainbow);
        t.color = useRainbow ? Color.white : (isSpecialBlock ? specialColor : normalColor);
        t.text = delta > 0 ? $"{delta}" : delta.ToString();

        // Ensure the rect is large enough so Unity's Text generator won't truncate.
        float w = Mathf.Clamp(t.preferredWidth + 16f, 80f, 1000f);
        float h = Mathf.Clamp(t.preferredHeight + 8f, 30f, 300f);
        rt.sizeDelta = new Vector2(w, h);

        FloatingScorePopup popup = go.GetComponent<FloatingScorePopup>();
        float startHue = Random.value;
        popup.Initialize(
            rt,
            t,
            Now(),
            Mathf.Max(0.05f, lifetimeSeconds),
            driftUpPixels,
            useUnscaledTime,
            useRainbow,
            startHue,
            rainbowCyclesOverLifetime
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
            GameObject root = new GameObject("FloatingScorePopups", typeof(RectTransform));
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
