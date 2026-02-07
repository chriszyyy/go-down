using UnityEngine;

/// <summary>
/// Fits this RectTransform to the device safe area (notches, rounded corners, system UI).
/// Usage (portrait mobile):
/// - Create a UI Panel named "SafeArea" under your Canvas
/// - Stretch it to full screen (anchors min=(0,0) max=(1,1))
/// - Add this component to it
/// - Put all HUD elements under that panel.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        ApplySafeArea(force: true);
    }

    private void Update()
    {
        ApplySafeArea(force: false);
    }

    private void OnRectTransformDimensionsChange()
    {
        // Re-apply in case the canvas size changed.
        ApplySafeArea(force: false);
    }

    private void ApplySafeArea(bool force)
    {
        if (rt == null) return;

        Rect safe = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        if (!force && safe == lastSafeArea && screenSize == lastScreenSize && orientation == lastOrientation)
        {
            return;
        }

        lastSafeArea = safe;
        lastScreenSize = screenSize;
        lastOrientation = orientation;

        float w = Mathf.Max(1f, screenSize.x);
        float h = Mathf.Max(1f, screenSize.y);

        Vector2 anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
        Vector2 anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);

        // Clamp just in case of odd platform values.
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
