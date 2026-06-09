using UnityEngine;

/// <summary>
/// 陷阱方块：由 TowerBuilder 在每批塔生成时按概率植入。
///
/// 行为：
/// 1) 连带消除：被点击消除时，一并消除“边相邻”（上下左右接触）的方块，增加难度。
/// 2) 外观：深色方块 + 中央骷髅警示图标（优先用 Resources/TrapSkull，否则程序化生成）。
/// 3) 隐身：进入相机可视范围后倒计时 3 秒，再用 2 秒从陷阱外观“蜕变”为普通方块的样子
///    （骷髅淡出 + 深色渐变为随机正常色），让玩家看不出哪一个是陷阱，但陷阱效果仍然有效。
///
/// 颜色通过设置根 SpriteRenderer.color 间接驱动 BlockPrismVisual（它每帧读取该色刷新 3D 材质）。
/// </summary>
[RequireComponent(typeof(TowerBlock))]
public class TrapBlock : MonoBehaviour
{
    [Tooltip("探测相邻方块时，向每条边外延伸的距离（世界单位）")]
    public float edgeProbeDepth = 0.25f;

    [Tooltip("探测盒沿边方向的覆盖比例（相对自身尺寸），避免误伤仅对角接触的方块")]
    [Range(0.1f, 1f)]
    public float edgeProbeWidthRatio = 0.85f;

    [Header("隐身")]
    [Tooltip("进入可视范围后，多少秒开始隐身蜕变")]
    public float stealthDelay = 2.5f;

    [Tooltip("隐身蜕变（陷阱外观 -> 正常方块）持续时长")]
    public float stealthFadeDuration = 1f;

    private TowerBlock self;
    private SpriteRenderer rootSprite;
    private Collider2D selfCollider;
    private bool triggered;

    // 连带消除用：缓存最近一次“有效”的碰撞盒
    private Bounds cachedBounds;
    private bool hasCachedBounds;

    // 隐身状态
    private Color trapColor = new Color(0.07f, 0.07f, 0.11f, 1f);
    private Color normalTarget = new Color(0.012f, 0.388f, 0.941f, 1f);
    private SpriteRenderer skull;
    private bool configured;
    private float visibleSince = -1f;
    private bool stealthDone;

    private static Sprite s_skullSprite;
    private static bool s_skullTried;

    private void Awake()
    {
        self = GetComponent<TowerBlock>();
        rootSprite = GetComponent<SpriteRenderer>();
        selfCollider = GetComponent<Collider2D>();
    }

    /// <summary>由 TowerBuilder 调用：设置陷阱色与隐身后伪装的正常色，并构建骷髅图标。</summary>
    public void Configure(Color trap, Color normal)
    {
        trapColor = trap;
        normalTarget = normal;
        configured = true;
        if (rootSprite != null) rootSprite.color = trapColor;
        EnsureSkull();
    }

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    private void OnDestroy()
    {
        // 被彩虹道具转化或销毁时，清理骷髅子物体
        if (skull != null) Destroy(skull.gameObject);
    }

    private void Update()
    {
        // 缓存有效碰撞盒（DestroyBlock 会先禁用碰撞器，故不能等事件里才读）
        if (!triggered && selfCollider != null && selfCollider.enabled)
        {
            cachedBounds = selfCollider.bounds;
            hasCachedBounds = true;
        }

        if (configured && !stealthDone)
            UpdateStealth();
    }

    // ---------------- 隐身蜕变 ----------------

    private void UpdateStealth()
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (visibleSince < 0f)
        {
            if (IsInView(cam)) visibleSince = Time.time;
            return;
        }

        float elapsed = Time.time - visibleSince;
        if (elapsed < stealthDelay) return;

        float t = stealthFadeDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - stealthDelay) / stealthFadeDuration);
        Color c = Color.Lerp(trapColor, normalTarget, t);
        if (rootSprite != null) rootSprite.color = c;
        if (self != null) self.OverrideOriginalColor(c);

        if (skull != null)
        {
            Color sc = skull.color;
            sc.a = 1f - t;
            skull.color = sc;
        }

        if (t >= 1f)
        {
            stealthDone = true;
            if (skull != null) skull.enabled = false;
        }
    }

    private bool IsInView(Camera cam)
    {
        Vector3 p = transform.position;
        if (hasCachedBounds) p = cachedBounds.center;
        Vector3 vp = cam.WorldToViewportPoint(p);
        return vp.z > 0f && vp.x >= -0.05f && vp.x <= 1.05f && vp.y >= -0.05f && vp.y <= 1.05f;
    }

    private void EnsureSkull()
    {
        if (skull != null) return;

        var go = new GameObject("TrapSkull");
        go.transform.SetParent(transform, false);

        // 放在方块（碰撞盒）中心、明显靠近相机（前面是 -Z；需比棱柱前面更靠前以免被遮挡）
        // 用 collider.offset 推算中心（编辑模式下 bounds 可能未同步，offset 始终可靠）
        Vector3 centerWorld = transform.position;
        var box = selfCollider as BoxCollider2D;
        if (box != null) centerWorld = transform.TransformPoint(box.offset);
        else if (selfCollider != null) centerWorld = transform.TransformPoint(selfCollider.offset);
        go.transform.position = new Vector3(centerWorld.x, centerWorld.y, transform.position.z - 0.4f);

        skull = go.AddComponent<SpriteRenderer>();
        skull.sprite = GetSkullSprite();
        skull.color = Color.white;
        // 用 Unlit sprite 材质：URP 2D 的默认 Sprite-Lit 需要 2D 光照，否则会渲染成黑色/不可见
        var unlit = Shader.Find("Sprites/Default");
        if (unlit != null) skull.sharedMaterial = new Material(unlit);
        if (rootSprite != null)
        {
            skull.sortingLayerID = rootSprite.sortingLayerID;
            skull.sortingOrder = rootSprite.sortingOrder + 3;
        }

        // 缩放到约 0.7 个格子
        float target = 0.7f;
        if (skull.sprite != null)
        {
            float spriteWorld = skull.sprite.bounds.size.x; // 世界单位
            if (spriteWorld > 0.0001f)
            {
                float s = target / spriteWorld;
                go.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    private static Sprite GetSkullSprite()
    {
        if (s_skullTried) return s_skullSprite;
        s_skullTried = true;
        // 优先用美术资源（用户可将生成的骷髅图保存为 Assets/Resources/TrapSkull.png）
        s_skullSprite = Resources.Load<Sprite>("TrapSkull");
        if (s_skullSprite == null) s_skullSprite = BuildProceduralSkull();
        return s_skullSprite;
    }

    /// <summary>程序化生成一个简单但可辨识的白色骷髅图标（透明背景）。</summary>
    private static Sprite BuildProceduralSkull()
    {
        int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[N * N];
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 dark = new Color32(20, 16, 28, 255);
        for (int i = 0; i < px.Length; i++) px[i] = clear;

        Vector2 headC = new Vector2(64, 74);
        float headR = 34f;
        Rect jaw = new Rect(44, 30, 40, 30); // x,y,w,h

        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                bool inHead = (new Vector2(x, y) - headC).sqrMagnitude <= headR * headR;
                bool inJaw = x >= jaw.xMin && x <= jaw.xMax && y >= jaw.yMin && y <= jaw.yMax;
                if (inJaw)
                {
                    float ny = (y - jaw.yMin) / jaw.height; // 0底 1顶
                    float inset = (1f - ny) * 8f;
                    if (x < jaw.xMin + inset || x > jaw.xMax - inset) inJaw = false;
                }
                if (!inHead && !inJaw) continue;

                Color32 c = white;

                // 眼窝
                Vector2 eL = new Vector2(52, 80), eR = new Vector2(76, 80);
                float eyeR = 10f;
                if (EllipseHit(x, y, eL, eyeR, eyeR * 1.15f) || EllipseHit(x, y, eR, eyeR, eyeR * 1.15f))
                    c = dark;

                // 鼻孔（倒三角）
                if (y < 70 && y > 60 && Mathf.Abs(x - 64) < (70 - y) * 0.5f)
                    c = dark;

                // 牙缝
                if (y < jaw.yMin + 18 && y > jaw.yMin)
                {
                    int rel = x - 50;
                    if (rel >= 0 && rel <= 28 && (rel % 9 == 0)) c = dark;
                }

                px[y * N + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N); // PPU=N -> 1 世界单位
    }

    private static bool EllipseHit(int x, int y, Vector2 c, float rx, float ry)
    {
        float dx = (x - c.x) / rx;
        float dy = (y - c.y) / ry;
        return dx * dx + dy * dy <= 1f;
    }

    // ---------------- 连带消除 ----------------

    private void HandleBlockDestroyed(TowerBlock destroyed)
    {
        if (triggered) return;
        if (destroyed != self) return;

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

        ProbeEdge(new Vector2(center.x + halfX + edgeProbeDepth * 0.5f, center.y), new Vector2(edgeProbeDepth, widthY));
        ProbeEdge(new Vector2(center.x - halfX - edgeProbeDepth * 0.5f, center.y), new Vector2(edgeProbeDepth, widthY));
        ProbeEdge(new Vector2(center.x, center.y + halfY + edgeProbeDepth * 0.5f), new Vector2(widthX, edgeProbeDepth));
        ProbeEdge(new Vector2(center.x, center.y - halfY - edgeProbeDepth * 0.5f), new Vector2(widthX, edgeProbeDepth));
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
