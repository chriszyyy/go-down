using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper to prevent overlapping HUD Text elements by laying out children in a single top row.
/// Usage:
/// - Create an empty GameObject under your SafeArea panel, e.g. "TopLeftStats"
/// - Put Coins/Score/HighScore Text objects as children of it
/// - Add this component to the parent.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class HudStatsStackUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Spacing between each line (in UI pixels).")]
    public float spacing = 24f;

    [Tooltip("Padding (left).")]
    public int paddingLeft = 24;

    [Tooltip("Padding (right).")]
    public int paddingRight = 24;

    [Tooltip("Padding (top).")]
    public int paddingTop = 24;

    [Tooltip("Padding (bottom).")]
    public int paddingBottom = 0;

    [Tooltip("If true, forces this container to anchor to the top and stretch full width.")]
    public bool forceTopLeftAnchor = true;

    private void OnEnable()
    {
        EnsureLayoutComponents();
    }

    private void OnTransformChildrenChanged()
    {
        EnsureLayoutComponents();
    }

    private void Update()
    {
        // Keep it stable in edit mode and play mode.
        EnsureLayoutComponents();
    }

    private void EnsureLayoutComponents()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;

        if (forceTopLeftAnchor)
        {
            // Top, stretch full width.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
        }

        // Unity only allows ONE LayoutGroup component per GameObject.
        // If an older setup already has VerticalLayoutGroup (or other groups), remove them.
        RemoveNonHorizontalLayoutGroups();

        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();

        if (hlg == null)
        {
            // If something unexpected prevents adding, avoid null refs.
            return;
        }

        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.spacing = spacing;
        hlg.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        // Expand width so the 3 items can spread across the row.
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;

        // ContentSizeFitter often conflicts with stretched width; keep it off.
        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        // Make sure child Texts won't clip unexpectedly.
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            Text t = child.GetComponent<Text>();
            if (t == null) continue;

            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            // Give each item equal flexible width so they space out.
            LayoutElement le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            // Optional: align left/center/right by index for nicer HUD.
            if (i == 0) t.alignment = TextAnchor.UpperLeft;
            else if (i == transform.childCount - 1) t.alignment = TextAnchor.UpperRight;
            else t.alignment = TextAnchor.UpperCenter;
        }
    }

    private void RemoveNonHorizontalLayoutGroups()
    {
        LayoutGroup[] groups = GetComponents<LayoutGroup>();
        if (groups == null || groups.Length == 0) return;

        for (int i = 0; i < groups.Length; i++)
        {
            LayoutGroup g = groups[i];
            if (g == null) continue;
            if (g is HorizontalLayoutGroup) continue;

            if (Application.isPlaying)
            {
                Destroy(g);
            }
            else
            {
                DestroyImmediate(g);
            }
        }
    }
}
