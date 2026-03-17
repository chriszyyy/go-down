using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper to layout HUD stats in a single top row.
/// Usage:
/// - Create an empty GameObject under your SafeArea panel, e.g. "TopLeftStats"
/// - Put stat containers as children (e.g. HighestStat / ScoreStat / CoinStat)
/// - Each stat container should contain Icon(Image) + Value(Text)
/// - Add this component to the parent.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class HudStatsStackUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Spacing between stat containers (in UI pixels).")]
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

    private bool dirty = true;
    private int lastChildCount = -1;
    private float lastSpacing;
    private int lastPaddingLeft;
    private int lastPaddingRight;
    private int lastPaddingTop;
    private int lastPaddingBottom;
    private bool lastForceTopLeftAnchor;

    private void OnEnable()
    {
        MarkDirty();
        RefreshIfDirty(force: true);
    }

    private void OnTransformChildrenChanged()
    {
        MarkDirty();
        RefreshIfDirty(force: false);
    }

    private void OnValidate()
    {
        MarkDirty();
        RefreshIfDirty(force: false);
    }

    private void Update()
    {
        // Keep it stable in edit mode without doing work every frame in play mode.
        if (Application.isPlaying) return;
        RefreshIfDirty(force: false);
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    private void RefreshIfDirty(bool force)
    {
        if (!force)
        {
            bool settingsChanged =
                lastChildCount != transform.childCount ||
                !Mathf.Approximately(lastSpacing, spacing) ||
                lastPaddingLeft != paddingLeft ||
                lastPaddingRight != paddingRight ||
                lastPaddingTop != paddingTop ||
                lastPaddingBottom != paddingBottom ||
                lastForceTopLeftAnchor != forceTopLeftAnchor;

            if (!dirty && !settingsChanged) return;
        }

        EnsureLayoutComponents();

        dirty = false;
        lastChildCount = transform.childCount;
        lastSpacing = spacing;
        lastPaddingLeft = paddingLeft;
        lastPaddingRight = paddingRight;
        lastPaddingTop = paddingTop;
        lastPaddingBottom = paddingBottom;
        lastForceTopLeftAnchor = forceTopLeftAnchor;
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

        // Container-only mode: each direct child is a stat container (Icon + Value Text).
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            // Give each item equal flexible width so they space out.
            LayoutElement le = child.GetComponent<LayoutElement>();
            if (le == null) le = child.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            Text t = child.GetComponentInChildren<Text>(includeInactive: true);
            if (t == null) continue;

            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.alignment = TextAnchor.MiddleLeft;
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
