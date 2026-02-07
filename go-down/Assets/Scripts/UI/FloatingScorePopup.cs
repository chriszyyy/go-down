using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a single floating score popup: drifts up and fades out.
/// </summary>
public class FloatingScorePopup : MonoBehaviour
{
    private RectTransform rt;
    private Text text;

    private float startTime;
    private float duration;
    private float driftUp;
    private bool useUnscaledTime;

    private Vector2 startPos;
    private Color startColor;

    private bool useRainbow;
    private float startHue;
    private float hueCycles;

    public void Initialize(
        RectTransform rectTransform,
        Text uiText,
        float now,
        float lifetimeSeconds,
        float driftUpPixels,
        bool useUnscaled,
        bool useAnimatedRainbow,
        float rainbowStartHue,
        float rainbowCyclesOverLifetime)
    {
        rt = rectTransform;
        text = uiText;

        startTime = now;
        duration = Mathf.Max(0.05f, lifetimeSeconds);
        driftUp = driftUpPixels;
        useUnscaledTime = useUnscaled;

        startPos = rt != null ? rt.anchoredPosition : Vector2.zero;
        startColor = text != null ? text.color : Color.white;

        useRainbow = useAnimatedRainbow;
        startHue = rainbowStartHue;
        hueCycles = rainbowCyclesOverLifetime;
    }

    private float Now() => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void Update()
    {
        if (rt == null || text == null)
        {
            Destroy(gameObject);
            return;
        }

        float t = Mathf.Clamp01((Now() - startTime) / duration);

        rt.anchoredPosition = startPos + new Vector2(0f, driftUp * t);

        float alpha = Mathf.Lerp(1f, 0f, t);

        if (useRainbow)
        {
            float hue = Mathf.Repeat(startHue + (hueCycles * t), 1f);
            Color rgb = Color.HSVToRGB(hue, 1f, 1f);
            rgb.a = alpha;
            text.color = rgb;
        }
        else
        {
            Color c = startColor;
            c.a = alpha;
            text.color = c;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
