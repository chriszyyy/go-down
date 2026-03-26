using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Right-side toolbar with tool buttons.
/// - Reset tool: move HexagonBall to horizontal center.
/// - Rainbow tool: pick 2 visible blocks and turn them into special/rainbow blocks.
/// This component expects the toolbar UI (panel + two Buttons) to be authored in the scene.
/// </summary>
public class RightToolbarUI : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Button that triggers the Reset tool.")]
    public Button resetButton;

    [Tooltip("Button that triggers the Rainbow tool.")]
    public Button rainbowButton;

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

    private void OnEnable()
    {
        EnsureEventSystem();
        WireUpButtons();
    }

    private void OnDisable()
    {
        if (resetButton != null) resetButton.onClick.RemoveListener(OnClickReset);
        if (rainbowButton != null) rainbowButton.onClick.RemoveListener(OnClickRainbow);
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

        List<TowerBlock> candidates = new List<TowerBlock>(256);
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

            candidates.Add(b);
        }

        if (candidates.Count == 0) return;

        int convert = Mathf.Min(need, candidates.Count);
        bool convertedAny = false;
        for (int i = 0; i < convert; i++)
        {
            int idx = Random.Range(0, candidates.Count);
            TowerBlock picked = candidates[idx];
            candidates.RemoveAt(idx);

            if (picked != null)
            {
                ApplySpecialBlock(picked.gameObject);
                convertedAny = true;
            }
        }

        if (convertedAny && GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayRainbowToolSfx();
        }
    }

    private static void ApplySpecialBlock(GameObject block)
    {
        if (block == null) return;

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

}
