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
    [Tooltip("Sliding window duration in seconds (e.g., 10 for 10 seconds).")]
    public float windowSeconds = 10f;

    [Tooltip("Target destroyed block count within the window to reach 100%.")]
    public int targetBlocks = 30;

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

    [Header("Reward Mode")]
    [Tooltip("When progress reaches 100%, enter reward mode for this many seconds.")]
    public float rewardDurationSeconds = 5f;

    [Tooltip("During reward mode, all destroyed blocks score is multiplied by this value.")]
    public int rewardScoreMultiplier = 3;

    [Header("UI")]
    [Tooltip("Fill image used as progress bar visual. This is required.")]
    public Image fillImage;

    [Tooltip("Optional visibility root for manual scene setup (e.g., ComboProgressBar). If empty, tries to infer from fillImage parent.")]
    public GameObject progressRoot;

    [Header("Smoothing")]
    [Tooltip("How quickly the bar rises toward target (fill amount per second). Higher = faster.")]
    public float fillRiseSpeed = 6f;

    [Tooltip("How quickly the bar falls toward target (fill amount per second). Higher = faster.")]
    public float fillFallSpeed = 10f;

    private struct CountBucket
    {
        public float time;
        public int count;
    }

    private readonly Queue<CountBucket> buckets = new Queue<CountBucket>(256);
    private int windowCount;

    private static Shader s_rainbowShader;
    private static Material s_rainbowUIMaterial;

    private float displayedFill;

    private bool rewardActive;
    private float rewardStartTime;
    private float rewardEndTime;
    private Material normalFillMaterial;
    private Color normalFillColor;

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
        GameStateManager.OnGameReset += HandleGameReset;
    }

    private void Start()
    {
        if (fillImage == null)
        {
            Debug.LogError("BlockClearProgressUI: fillImage is not assigned.");
            enabled = false;
            return;
        }

        EnsureFillImageConfig();

        if (progressRoot == null)
        {
            progressRoot = ResolveManualProgressRoot();
        }

        displayedFill = fillImage.fillAmount;
        normalFillMaterial = fillImage.material;
        normalFillColor = fillImage.color;

        RefreshUI();
    }

    private void EnsureFillImageConfig()
    {
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillClockwise = true;
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
        GameStateManager.OnGameReset -= HandleGameReset;
    }

    private void Update()
    {
        if (rewardActive)
        {
            UpdateRewardMode();
            return;
        }

        PurgeOld();

        // If we reach 100%, enter reward mode.
        if (GetTargetFill() >= 1f)
        {
            StartRewardMode();
            return;
        }

        RefreshUI();
    }

    private void HandleBlockDestroyed(TowerBlock block)
    {
        if (rewardActive)
        {
            // During reward mode we pause accumulating QPS/count.
            // But destroying a special/rainbow block refills reward time to full.
            if (block != null && Mathf.Max(1, block.scoreMultiplier) >= Mathf.Max(1, rainbowDetectScoreMultiplier))
            {
                float now = Now();
                rewardStartTime = now;
                rewardEndTime = now + Mathf.Max(0.01f, rewardDurationSeconds);

                displayedFill = 1f;
                fillImage.fillAmount = 1f;
            }

            return;
        }

        int add = 1;

        if (block != null && Mathf.Max(1, block.scoreMultiplier) >= Mathf.Max(1, rainbowDetectScoreMultiplier))
        {
            // Rainbow click boost: add a fixed portion of the bar instantly.
            add = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, targetBlocks) * Mathf.Clamp01(rainbowClickBoostPercent)));
        }

        AddCount(add);

        // Trigger reward immediately when threshold is reached via this event.
        if (!rewardActive && GetTargetFill() >= 1f)
        {
            StartRewardMode();
        }
    }

    private void HandleGameReset()
    {
        EndRewardMode(force: true);
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

    /// <summary>
    /// Show or hide the progress bar UI (e.g. hide during GameOver modal).
    /// </summary>
    public void SetVisible(bool visible)
    {
        GameObject target = progressRoot != null ? progressRoot : ResolveManualProgressRoot();
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private GameObject ResolveManualProgressRoot()
    {
        if (fillImage == null) return null;

        // Prefer parent container (commonly ComboProgressBar) so background+fill hide together.
        Transform p = fillImage.transform.parent;
        if (p != null) return p.gameObject;

        return fillImage.gameObject;
    }

    private void RefreshUI()
    {
        if (rewardActive)
        {
            // Reward mode visuals are handled in UpdateRewardMode.
            return;
        }

        float targetFill = GetTargetFill();

        // Smooth the visual fill amount.
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float speed = targetFill >= displayedFill ? Mathf.Max(0f, fillRiseSpeed) : Mathf.Max(0f, fillFallSpeed);

        // MoveTowards gives stable, frame-rate independent easing.
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, speed * Mathf.Max(0f, dt));
        fillImage.fillAmount = displayedFill;
    }

    private void StartRewardMode()
    {
        if (rewardActive) return;

        rewardActive = true;
        rewardStartTime = Now();
        rewardEndTime = rewardStartTime + Mathf.Max(0.01f, rewardDurationSeconds);

        // Pause counting and visually switch to rainbow fill.
        if (fillImage != null)
        {
            normalFillMaterial = fillImage.material;
            normalFillColor = fillImage.color;

            Material rainbowMat = GetRainbowUIMaterial();
            if (rainbowMat != null)
            {
                fillImage.material = rainbowMat;
                fillImage.color = Color.white;
            }
        }

        // Apply score bonus.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.GlobalScoreMultiplier = Mathf.Max(1, rewardScoreMultiplier);
        }

        // Start at 100%.
        displayedFill = 1f;
        fillImage.fillAmount = 1f;
    }

    private void UpdateRewardMode()
    {
        float now = Now();
        if (now >= rewardEndTime)
        {
            EndRewardMode(force: false);
            return;
        }

        float duration = Mathf.Max(0.01f, rewardDurationSeconds);
        float remaining = Mathf.Clamp(rewardEndTime - now, 0f, duration);
        float fill = remaining / duration;

        displayedFill = fill;
        fillImage.fillAmount = fill;
    }

    private void EndRewardMode(bool force)
    {
        if (!rewardActive && !force) return;

        rewardActive = false;

        // Restore normal visuals.
        if (fillImage != null)
        {
            fillImage.material = normalFillMaterial;
            fillImage.color = normalFillColor;
        }

        // Remove score bonus.
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.GlobalScoreMultiplier = 1;
        }

        // After reward ends, restart counting from zero.
        ClearProgress();
        displayedFill = 0f;
        fillImage.fillAmount = 0f;
    }

    private static Material GetRainbowUIMaterial()
    {
        if (s_rainbowUIMaterial != null) return s_rainbowUIMaterial;

        if (s_rainbowShader == null)
        {
            s_rainbowShader = Shader.Find("GoDown/RainbowGlowSprite");
        }

        if (s_rainbowShader == null)
        {
            Debug.LogWarning("BlockClearProgressUI: Shader 'GoDown/RainbowGlowSprite' not found. Reward fill will use normal material.");
            return null;
        }

        s_rainbowUIMaterial = new Material(s_rainbowShader);
        s_rainbowUIMaterial.name = "RainbowGlowSprite (UI Shared)";

        // Give some variation so it doesn't look too static.
        s_rainbowUIMaterial.SetFloat("_HueOffset", Random.value);

        return s_rainbowUIMaterial;
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

}
