using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 金币图标的弹出反应：快速放大(回弹) -> 上漂 + 淡出。
/// 配合 CoinFloatingPopupSpawner 在获得金币（如撞到彩虹金币方块）时生成。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class CoinIconPopup : MonoBehaviour
{
    private RectTransform rt;
    private Image image;

    private float startTime;
    private float duration;
    private float driftUp;
    private bool useUnscaled;

    private Vector2 startPos;
    private float baseScale;

    public void Initialize(RectTransform rectTransform, Image img, float now,
                           float lifetimeSeconds, float driftUpPixels, bool unscaled, float scale)
    {
        rt = rectTransform;
        image = img;
        startTime = now;
        duration = Mathf.Max(0.05f, lifetimeSeconds);
        driftUp = driftUpPixels;
        useUnscaled = unscaled;
        baseScale = scale;
        startPos = rt.anchoredPosition;
        rt.localScale = Vector3.zero;
    }

    private float Now() => useUnscaled ? Time.unscaledTime : Time.time;

    private void Update()
    {
        if (rt == null || image == null) { Destroy(gameObject); return; }

        float t = Mathf.Clamp01((Now() - startTime) / duration);

        // 弹出缩放：0 -> 1.25 -> 1.0（前 25% 时间回弹）
        float s;
        if (t < 0.25f)
        {
            float k = t / 0.25f;
            s = Mathf.Lerp(0f, 1.25f, k);
        }
        else
        {
            float k = (t - 0.25f) / 0.75f;
            s = Mathf.Lerp(1.25f, 1.0f, k);
        }
        rt.localScale = Vector3.one * (baseScale * s);

        // 上漂
        rt.anchoredPosition = startPos + new Vector2(0f, driftUp * t);

        // 后半程淡出
        Color c = image.color;
        c.a = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
        image.color = c;

        if (t >= 1f) Destroy(gameObject);
    }
}
