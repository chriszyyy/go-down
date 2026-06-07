using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Right-side toolbar with tool buttons.
/// - Reset tool: move HexagonBall to horizontal center.
/// - Rainbow tool: pick visible blocks and convert to special/rainbow blocks.
/// This component expects the toolbar UI (panel + two Buttons) to be authored in the scene.
/// </summary>
public class RightToolbarUI : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Button that triggers the Reset tool.")]
    public Button resetButton;

    [Tooltip("Button that triggers the Rainbow tool.")]
    public Button rainbowButton;

    [Tooltip("Optional label to show remaining Reset uses.")]
    public Text resetUsesText;

    [Tooltip("Optional label to show remaining Rainbow uses.")]
    public Text rainbowUsesText;

    [Header("Rainbow Tool")]
    [Tooltip("How many visible blocks to convert to rainbow.")]
    public int rainbowConvertCount = 2;

    [Tooltip("Detect already-special blocks by scoreMultiplier >= this value.")]
    public int specialDetectScoreMultiplier = 10;

    [Header("UI")]
    [Tooltip("Toolbar width/height in UI pixels.")]
    public Vector2 toolbarSize = new Vector2(220f, 220f);

    [Tooltip("Inset from the right edge of the safe area.")]
    public float rightInset = 24f;

    [Tooltip("Vertical spacing between buttons.")]
    public float buttonSpacing = 14f;

    [Tooltip("Button height.")]
    public float buttonHeight = 84f;

    [Tooltip("Button font size.")]
    public int buttonFontSize = 30;

    [Tooltip("Remaining uses label format. {0}=uses count")]
    public string usesLabelFormat = "x{0}";

    private void OnEnable()
    {
        EnsureEventSystem();
        WireUpButtons();
        ToolUsageInventory.OnUsesChanged += HandleUsesChanged;
        RefreshUsesUI();
    }

    private void Start()
    {
        // OnEnable 时 ToolUsageInventory.Instance 可能尚未就绪，Start 时再刷新一次
        RefreshUsesUI();
    }

    private void OnDisable()
    {
        if (resetButton != null) resetButton.onClick.RemoveListener(OnClickReset);
        if (rainbowButton != null) rainbowButton.onClick.RemoveListener(OnClickRainbow);
        ToolUsageInventory.OnUsesChanged -= HandleUsesChanged;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private void WireUpButtons()
    {
        if (resetButton == null || rainbowButton == null)
        {
            Debug.LogWarning("RightToolbarUI: please assign Reset/Rainbow Button references in the Inspector.", this);
            return;
        }

        resetButton.onClick.RemoveListener(OnClickReset);
        rainbowButton.onClick.RemoveListener(OnClickRainbow);

        resetButton.onClick.AddListener(OnClickReset);
        rainbowButton.onClick.AddListener(OnClickRainbow);
    }

    private void OnClickReset()
    {
        if (ToolUsageInventory.Instance == null || !ToolUsageInventory.Instance.TryConsumeResetUse())
        {
            Debug.Log("RightToolbarUI: no Reset tool uses left. Buy more in Shop.");
            return;
        }

        TowerBuilder builder = FindFirstObjectByType<TowerBuilder>();
        HexagonBall ball = FindFirstObjectByType<HexagonBall>();
        if (ball == null) return;

        float centerX = builder != null ? (builder.layerWidth / 2f) : 0f;

        Vector3 p = ball.transform.position;
        p.x = centerX;
        ball.transform.position = p;

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ball.transform.rotation = Quaternion.identity;

        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayResetToolSfx();
        }
    }

    private void OnClickRainbow()
    {
        if (ToolUsageInventory.Instance == null || !ToolUsageInventory.Instance.TryConsumeRainbowUse())
        {
            Debug.Log("RightToolbarUI: no Rainbow tool uses left. Buy more in Shop.");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        int need = Mathf.Max(1, rainbowConvertCount);

        // 可视范围内的候选：优先陷阱方块（trapCandidates），其次普通方块（normalCandidates）
        List<TowerBlock> trapCandidates = new List<TowerBlock>(64);
        List<TowerBlock> normalCandidates = new List<TowerBlock>(256);
        TowerBlock[] blocks = FindObjectsByType<TowerBlock>(FindObjectsSortMode.None);

        for (int i = 0; i < blocks.Length; i++)
        {
            TowerBlock b = blocks[i];
            if (b == null) continue;

            Collider2D col = b.GetComponent<Collider2D>();
            if (col != null && !col.enabled) continue;

            if (Mathf.Max(1, b.scoreMultiplier) >= Mathf.Max(1, specialDetectScoreMultiplier))
                continue;

            Vector3 vp = cam.WorldToViewportPoint(b.transform.position);
            if (vp.z < 0f) continue;
            if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;

            if (b.GetComponent<TrapBlock>() != null)
                trapCandidates.Add(b);
            else
                normalCandidates.Add(b);
        }

        if (trapCandidates.Count == 0 && normalCandidates.Count == 0) return;

        bool convertedAny = false;
        int remaining = need;

        // 先把陷阱方块变成彩色方块（优先），再用剩余配额转换普通方块
        remaining -= ConvertRandomFromList(trapCandidates, remaining, ref convertedAny);
        if (remaining > 0)
            ConvertRandomFromList(normalCandidates, remaining, ref convertedAny);

        if (convertedAny && GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayRainbowToolSfx();
        }
    }

    /// <summary>
    /// 从 list 中随机抽取最多 count 个方块转换为彩色特殊方块，返回实际转换数量。
    /// </summary>
    private static int ConvertRandomFromList(List<TowerBlock> list, int count, ref bool convertedAny)
    {
        int convert = Mathf.Min(count, list.Count);
        for (int i = 0; i < convert; i++)
        {
            int idx = Random.Range(0, list.Count);
            TowerBlock picked = list[idx];
            list.RemoveAt(idx);

            if (picked != null)
            {
                ApplySpecialBlock(picked.gameObject);
                convertedAny = true;
            }
        }
        return convert;
    }

    private static void ApplySpecialBlock(GameObject block)
    {
        if (block == null) return;

        // 若该方块是陷阱方块，转换前先移除陷阱行为（不再连带消除相邻方块）
        TrapBlock trap = block.GetComponent<TrapBlock>();
        if (trap != null) Destroy(trap);

        // Attach animated rainbow gradient + glow (without a compile-time dependency on Visuals asmdef).
        if (block.GetComponent("RainbowGlowVisual") == null)
        {
            var t = System.Type.GetType("RainbowGlowVisual, GoDown.Visuals");
            if (t != null)
            {
                var comp = block.AddComponent(t);

                // Keep the normal inset highlight sprite enabled as a light outline.
                var field = t.GetField("disableInsetHighlight");
                if (field != null)
                {
                    field.SetValue(comp, false);
                }
            }
        }

        // Use the same palette as normal blocks for the inset highlight (outline), but keep the rainbow fill.
        Component style = block.GetComponent("BlockVisualStyle");
        if (style != null)
        {
            style.SendMessage("ApplyRandomStyle", SendMessageOptions.DontRequireReceiver);

            SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                style.SendMessage("ApplyStyleAndLock", sr.color, SendMessageOptions.DontRequireReceiver);
            }
        }

        TowerBlock tb = block.GetComponent<TowerBlock>();
        if (tb != null)
        {
            tb.scoreMultiplier = 10;
        }

        RainbowCoinReward coinReward = block.GetComponent<RainbowCoinReward>();
        if (coinReward == null)
        {
            coinReward = block.AddComponent<RainbowCoinReward>();
        }

        coinReward.coinsPerActivation = 5;
        coinReward.hexagonBallTag = "HexagonBall";
    }

    private void HandleUsesChanged()
    {
        RefreshUsesUI();
    }

    private void RefreshUsesUI()
    {
        int resetUses = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.ResetUses : 0;
        int rainbowUses = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.RainbowUses : 0;

        if (resetUsesText != null)
        {
            resetUsesText.text = string.Format(usesLabelFormat, resetUses);
        }

        if (rainbowUsesText != null)
        {
            rainbowUsesText.text = string.Format(usesLabelFormat, rainbowUses);
        }

        if (resetButton != null)
        {
            resetButton.interactable = resetUses > 0;
        }

        if (rainbowButton != null)
        {
            rainbowButton.interactable = rainbowUses > 0;
        }
    }
}
