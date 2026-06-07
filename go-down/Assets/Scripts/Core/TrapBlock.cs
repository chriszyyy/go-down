using UnityEngine;

/// <summary>
/// 陷阱方块行为：附加到单格方块上（由 TowerBuilder 在每批塔生成时按概率植入）。
/// 当该方块被消除（点击触发 TowerBlock.DestroyBlock）时，一并消除与其“边相邻”
/// （上、下、左、右最多四个方向、互相接触）的方块，增加游戏难度，让玩家尽量避开它。
/// 外观（黑色）由 TowerBuilder 在生成时设置；本组件只负责运行时的连带消除逻辑。
///
/// 采用订阅 TowerBlock.OnBlockDestroyed 的方式（而非自身再写一个 OnMouseDown），
/// 避免同一 GameObject 上两个 OnMouseDown 的执行顺序问题。
/// </summary>
[RequireComponent(typeof(TowerBlock))]
public class TrapBlock : MonoBehaviour
{
    [Tooltip("探测相邻方块时，向每条边外延伸的距离（世界单位）")]
    public float edgeProbeDepth = 0.25f;

    [Tooltip("探测盒沿边方向的覆盖比例（相对自身尺寸），避免误伤仅对角接触的方块")]
    [Range(0.1f, 1f)]
    public float edgeProbeWidthRatio = 0.85f;

    private TowerBlock self;
    private Collider2D selfCollider;
    private bool triggered;

    // 缓存最近一次“有效”的碰撞盒 bounds：TowerBlock.DestroyBlock 会在触发 OnBlockDestroyed
    // 之前先禁用碰撞器并缩小 scale，故不能在事件里才去读 bounds。
    private Bounds cachedBounds;
    private bool hasCachedBounds;

    private void Awake()
    {
        self = GetComponent<TowerBlock>();
        selfCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (triggered) return;
        if (selfCollider != null && selfCollider.enabled)
        {
            cachedBounds = selfCollider.bounds;
            hasCachedBounds = true;
        }
    }

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    private void HandleBlockDestroyed(TowerBlock destroyed)
    {
        if (triggered) return;
        if (destroyed != self) return; // 只在“自己”被消除时触发连带消除

        triggered = true;
        DestroyAdjacentBlocks();
    }

    private void DestroyAdjacentBlocks()
    {
        if (!hasCachedBounds) return;

        Bounds b = cachedBounds;
        Vector2 center = b.center;
        Vector2 size = b.size;
        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;
        float widthX = size.x * edgeProbeWidthRatio;
        float widthY = size.y * edgeProbeWidthRatio;

        // 右
        ProbeEdge(new Vector2(center.x + halfX + edgeProbeDepth * 0.5f, center.y),
                  new Vector2(edgeProbeDepth, widthY));
        // 左
        ProbeEdge(new Vector2(center.x - halfX - edgeProbeDepth * 0.5f, center.y),
                  new Vector2(edgeProbeDepth, widthY));
        // 上
        ProbeEdge(new Vector2(center.x, center.y + halfY + edgeProbeDepth * 0.5f),
                  new Vector2(widthX, edgeProbeDepth));
        // 下
        ProbeEdge(new Vector2(center.x, center.y - halfY - edgeProbeDepth * 0.5f),
                  new Vector2(widthX, edgeProbeDepth));
    }

    private void ProbeEdge(Vector2 boxCenter, Vector2 boxSize)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            TowerBlock tb = hit.GetComponentInParent<TowerBlock>();
            if (tb == null || tb == self) continue;
            if (tb.IsDestroying) continue;
            tb.DestroyBlock();
        }
    }
}
