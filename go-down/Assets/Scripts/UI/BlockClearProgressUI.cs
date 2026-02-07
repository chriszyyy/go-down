using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Left-side tube progress bar that tracks how many blocks were destroyed
/// within a sliding time window (e.g., last 60 seconds).
/// </summary>
public class BlockClearProgressUI : MonoBehaviour
{
    [Header("Rolling Window")]
    [Tooltip("Sliding window duration in seconds (e.g., 60 for 1 minute).")]
    public float windowSeconds = 60f;

    [Tooltip("Target destroyed block count within the window to reach 100%.")]
    public int targetBlocks = 100;

    [Header("Progress Calculation")]
    [Tooltip("Use QPS (blocks per second) to compute progress ratio. currentQps = count/windowSeconds")]
    public bool useQps = true;

    [Tooltip("Target QPS to reach 100%. If <=0, falls back to targetBlocks/windowSeconds.")]
    public float targetQps = 0f;

    [Tooltip("Use unscaled time so the window continues during slow-mo/pause UI.")]
    public bool useUnscaledTime = true;

    [Header("Special Boost")]
    [Tooltip("When a rainbow/special block is clicked and destroyed, instantly add this % of the bar.")]
    [Range(0f, 1f)]
    public float rainbowClickBoostPercent = 0.5f;

    [Tooltip("Detect rainbow/special blocks by scoreMultiplier >= this value.")]
    public int rainbowDetectScoreMultiplier = 10;

    [Header("UI")]
    [Tooltip("If null, the script will auto-create a simple UI under the first Canvas it finds.")]
    public Image fillImage;

    [Tooltip("Optional numeric label (e.g., 34/100).")]
    public Text valueText;

    [Tooltip("Label format. {0}=current blocks, {1}=target blocks, {2}=percent (0-100).")]
    public string labelFormat = "{0}/{1}  {2:0}%";

    [Tooltip("Percent-only label format. {0}=percent (0-100).")]
    public string percentLabelFormat = "{0:0}%";

    [Header("Smoothing")]
    [Tooltip("How quickly the bar rises toward target (fill amount per second). Higher = faster.")]
    public float fillRiseSpeed = 6f;

    [Tooltip("How quickly the bar falls toward target (fill amount per second). Higher = faster.")]
    public float fillFallSpeed = 10f;

    [Header("Auto Layout (when auto-created)")]
    public Vector2 barSize = new Vector2(56f, 280f);
    public Vector2 barAnchoredPosition = new Vector2(26f, 0f);
    public Vector2 fillPadding = new Vector2(8f, 8f);

    private struct CountBucket
    {
        public float time;
        public int count;
    }

    private readonly Queue<CountBucket> buckets = new Queue<CountBucket>(256);
    private int windowCount;

    private static Sprite s_fallbackSprite;
    private static Font s_fallbackFont;

    private float displayedFill;

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
        GameStateManager.OnGameReset += HandleGameReset;
    }

    private void Start()
    {
        EnsureUI();
        displayedFill = fillImage != null ? fillImage.fillAmount : 0f;
        RefreshUI();
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
        GameStateManager.OnGameReset -= HandleGameReset;
    }

    private void Update()
    {
        PurgeOld();
        RefreshUI();
    }

    private void HandleBlockDestroyed(TowerBlock block)
    {
        int add = 1;

        if (block != null && Mathf.Max(1, block.scoreMultiplier) >= Mathf.Max(1, rainbowDetectScoreMultiplier))
        {
            // Rainbow click boost: add a fixed portion of the bar instantly.
            add = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, targetBlocks) * Mathf.Clamp01(rainbowClickBoostPercent)));
        }

        AddCount(add);
    }

    private void HandleGameReset()
    {
        ClearProgress();
    }

    private float Now()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private void PurgeOld()
    {
        float w = Mathf.Max(0.01f, windowSeconds);
        float cutoff = Now() - w;

        while (buckets.Count > 0 && buckets.Peek().time < cutoff)
        {
            CountBucket b = buckets.Dequeue();
            windowCount -= b.count;
        }

        if (windowCount < 0) windowCount = 0;
    }

    private int CurrentCount => windowCount;

    private void AddCount(int count)
    {
        int c = Mathf.Max(0, count);
        if (c == 0) return;

        buckets.Enqueue(new CountBucket { time = Now(), count = c });
        windowCount += c;
        PurgeOld();
        RefreshUI();
    }

    public void ClearProgress()
    {
        buckets.Clear();
        windowCount = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        float targetFill = GetTargetFill();

        // Smooth the visual fill amount.
        if (fillImage != null)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float speed = targetFill >= displayedFill ? Mathf.Max(0f, fillRiseSpeed) : Mathf.Max(0f, fillFallSpeed);

            // MoveTowards gives stable, frame-rate independent easing.
            displayedFill = Mathf.MoveTowards(displayedFill, targetFill, speed * Mathf.Max(0f, dt));
            fillImage.fillAmount = displayedFill;
        }

        if (valueText != null)
        {
            float percent = targetFill * 100f;

            // UI requirement: only show percent on the bar.
            valueText.text = string.Format(percentLabelFormat, percent);
        }
    }

    private float GetCurrentQps()
    {
        float w = Mathf.Max(0.01f, windowSeconds);
        return CurrentCount / w;
    }

    private float GetEffectiveTargetQps()
    {
        if (targetQps > 0f) return targetQps;

        float w = Mathf.Max(0.01f, windowSeconds);
        return Mathf.Max(0.0001f, Mathf.Max(0, targetBlocks) / w);
    }

    private float GetTargetFill()
    {
        if (useQps)
        {
            float tq = GetEffectiveTargetQps();
            if (tq <= 0f) return 0f;
            return Mathf.Clamp01(GetCurrentQps() / tq);
        }

        float denom = Mathf.Max(1, targetBlocks);
        return Mathf.Clamp01(CurrentCount / denom);
    }

    private void EnsureUI()
    {
        if (fillImage != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
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

        // Root
        GameObject root = new GameObject("BlockClearProgressUI", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = barSize;
        rt.anchoredPosition = barAnchoredPosition;

        // Don't use Resources.GetBuiltinResource<Sprite>(...) here: in some Unity versions,
        // missing built-in UI sprites spam errors even if we handle null.
        Sprite uiSprite = GetFallbackSprite();

        // Background (tube)
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImg = bg.GetComponent<Image>();
        bgImg.sprite = uiSprite;
        bgImg.type = Image.Type.Simple;
        bgImg.color = new Color(1f, 1f, 1f, 0.25f);

        // Fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(fillPadding.x, fillPadding.y);
        fillRt.offsetMax = new Vector2(-fillPadding.x, -fillPadding.y);

        Image fillImg = fill.GetComponent<Image>();
        fillImg.sprite = uiSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Vertical;
        fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillImg.fillAmount = 0f;
        fillImg.color = new Color(1f, 1f, 1f, 0.85f);

        // Value text
        GameObject txt = new GameObject("ValueText", typeof(RectTransform), typeof(Text));
        txt.transform.SetParent(root.transform, false);
        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0.5f, 1f);
        txtRt.anchorMax = new Vector2(0.5f, 1f);
        txtRt.pivot = new Vector2(0.5f, 0f);
        txtRt.anchoredPosition = new Vector2(0f, 6f);
        txtRt.sizeDelta = new Vector2(Mathf.Max(120f, barSize.x * 2f), 32f);

        Text t = txt.GetComponent<Text>();
        t.alignment = TextAnchor.MiddleCenter;
        t.font = TryGetBuiltinFont() ?? GetFallbackFont();
        t.fontSize = 18;
        t.color = Color.white;

        // Bind refs
        fillImage = fillImg;
        valueText = t;
    }

    private static Sprite GetFallbackSprite()
    {
        if (s_fallbackSprite != null) return s_fallbackSprite;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.name = "BlockClearProgressUI_Fallback";
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true);

        s_fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        s_fallbackSprite.name = "BlockClearProgressUI_Fallback";
        return s_fallbackSprite;
    }

    private static Font TryGetBuiltinFont()
    {
        // Unity versions vary. Some versions throw if the path is invalid.
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

        // As a last resort, create a default font. This should exist in most Unity versions.
        // (If it doesn't, the Text will render with missing font warnings.)
        s_fallbackFont = Font.CreateDynamicFontFromOSFont("Arial", 18);
        return s_fallbackFont;
    }
}
