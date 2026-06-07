using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 方块的 3D 立体视觉（与六边形球同一风格）：基于方块占用的网格单元，代码生成
/// “正面 + 外边界斜切(bevel) + 侧壁”的网格，配合自带世界空间光照的着色器
/// （GoDown/HexPrismLit）。内部相邻边保持平整，仅外轮廓有斜切，呈现中等立体感。
///
/// 方块会翻滚，但光照方向固定在世界空间，故旋转时光影始终正确。
/// 颜色来自方块原 SpriteRenderer 的颜色（由 BlockVisualStyle 着色），原 sprite 被隐藏。
/// </summary>
[RequireComponent(typeof(TowerBlock))]
[RequireComponent(typeof(SpriteRenderer))]
public class BlockPrismVisual : MonoBehaviour
{
    [Header("立体参数（中等立体感）")]
    [Tooltip("棱柱厚度（沿 Z）")]
    public float thickness = 0.45f;

    [Tooltip("外轮廓斜切在平面方向的宽度（格子单位）")]
    public float bevelWidth = 0.08f;

    [Tooltip("斜切沿 Z 方向的深度")]
    public float bevelDepth = 0.12f;

    [Tooltip("外凸角圆角半径（格子单位，0=直角）")]
    public float cornerRadius = 0.12f;

    [Header("光照（世界空间，固定方向）")]
    public Vector3 lightDir = new Vector3(0.35f, 0.65f, -0.7f);
    public Color lightColor = Color.white;
    [Range(0f, 1f)] public float ambient = 0.45f;
    [Range(0f, 1f)] public float halfLambert = 1f;
    [Range(0f, 1f)] public float edgeLight = 0.5f;
    [Range(0f, 1f)] public float edgeWhiten = 0.35f;
    [Range(0f, 1f)] public float faceDarken = 0.22f;
    [Range(1f, 128f)] public float specPower = 18f;
    [Range(0f, 2f)] public float specStrength = 0.22f;
    public Color rimColor = Color.white;
    [Range(0.5f, 8f)] public float rimPower = 3.5f;
    [Range(0f, 2f)] public float rimStrength = 0.14f;

    private TowerBlock block;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer highlightRenderer;
    private MeshRenderer meshRenderer;
    private Material materialInstance;
    private GameObject meshObject;
    private Color lastColor = new Color(-1, -1, -1, -1);

    private void Awake()
    {
        block = GetComponent<TowerBlock>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        Transform hl = transform.Find("Highlight");
        if (hl != null) highlightRenderer = hl.GetComponent<SpriteRenderer>();

        BuildVisual();
    }

    private void OnDestroy()
    {
        // 还原：销毁网格、恢复原 sprite（用于运行时转彩虹等场景移除本组件后回到 flat）
        if (meshObject != null) Destroy(meshObject);
        if (materialInstance != null) Destroy(materialInstance);
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (highlightRenderer != null) highlightRenderer.enabled = true;
    }

    private void LateUpdate()
    {
        // 跟踪 BlockVisualStyle / 陷阱 / 其它逻辑对颜色的修改
        if (materialInstance == null || spriteRenderer == null) return;
        Color c = spriteRenderer.color;
        if (c != lastColor)
        {
            lastColor = c;
            materialInstance.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1f));
        }
    }

    private void BuildVisual()
    {
        Shader shader = Shader.Find("GoDown/HexPrismLit");
        if (shader == null)
        {
            Debug.LogWarning("BlockPrismVisual: 未找到着色器 GoDown/HexPrismLit，保留 2D sprite。");
            return;
        }

        Mesh mesh = BuildPrismMesh();
        if (mesh == null) return;

        // 隐藏原始 sprite（base + highlight）
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (highlightRenderer != null) highlightRenderer.enabled = false;

        materialInstance = new Material(shader) { name = "BlockPrismLit (Instance)" };
        ApplyLightParams();
        Color c = spriteRenderer != null ? spriteRenderer.color : Color.white;
        lastColor = c;
        materialInstance.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1f));

        meshObject = new GameObject("BlockPrismMesh");
        meshObject.transform.SetParent(transform, false);
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
        meshObject.transform.localScale = Vector3.one;

        var mf = meshObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = materialInstance;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        if (spriteRenderer != null)
        {
            meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            meshRenderer.sortingOrder = spriteRenderer.sortingOrder;
        }
    }

    private void ApplyLightParams()
    {
        if (materialInstance == null) return;
        Vector3 d = lightDir.sqrMagnitude > 0.0001f ? lightDir.normalized : new Vector3(0.35f, 0.65f, -0.7f);
        materialInstance.SetVector("_LightDir", new Vector4(d.x, d.y, d.z, 0f));
        materialInstance.SetColor("_LightColor", lightColor);
        materialInstance.SetFloat("_Ambient", ambient);
        materialInstance.SetFloat("_HalfLambert", halfLambert);
        materialInstance.SetFloat("_EdgeLight", edgeLight);
        materialInstance.SetFloat("_EdgeWhiten", edgeWhiten);
        materialInstance.SetFloat("_FaceDarken", faceDarken);
        materialInstance.SetFloat("_SpecPower", specPower);
        materialInstance.SetFloat("_SpecStrength", specStrength);
        materialInstance.SetColor("_RimColor", rimColor);
        materialInstance.SetFloat("_RimPower", rimPower);
        materialInstance.SetFloat("_RimStrength", rimStrength);
    }

    /// <summary>
    /// 基于占用格子生成网格：先追踪整个形状的外轮廓（矩形多边形），按内外法线做 miter
    /// 内缩(inset)，凸角与凹角都能得到正确的斜切；正面用耳切三角化内缩多边形，沿轮廓
    /// 生成一圈斜面(bevel) + 侧壁。这样不会在凹角处出现交叠或破洞。
    /// </summary>
    private Mesh BuildPrismMesh()
    {
        var cellList = block.GetOccupiedCells(0f);
        if (cellList == null || cellList.Count == 0) return null;

        var cells = new HashSet<(int x, int y)>(cellList);

        float halfT = Mathf.Max(0.001f, thickness * 0.5f);
        float frontZ = -halfT;
        float bevelBackZ = frontZ + Mathf.Max(0f, bevelDepth);
        float backZ = halfT;
        float w = Mathf.Clamp(bevelWidth, 0f, 0.49f);

        var verts = new List<Vector3>(256);
        var norms = new List<Vector3>(256);
        var tris = new List<int>(512);

        // 追踪所有外轮廓闭环（CCW，内部在左侧）
        var loops = TraceOutlineLoops(cells);
        foreach (var rawLoop in loops)
        {
            // 外凸角做圆角处理（凹角保持直角）
            var loop = RoundCorners(rawLoop, cornerRadius);
            int n = loop.Count;
            if (n < 3) continue;

            // 每个轮廓顶点对应的内缩点（miter）
            var inset = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = loop[(i - 1 + n) % n];
                Vector2 cur = loop[i];
                Vector2 next = loop[(i + 1) % n];
                Vector2 dIn = (cur - prev).normalized;
                Vector2 dOut = (next - cur).normalized;
                // 内法线 = 方向左侧（内部在左）
                Vector2 nIn = new Vector2(-dIn.y, dIn.x);
                Vector2 nOut = new Vector2(-dOut.y, dOut.x);
                // 归一化 miter：偏移量沿平分线、垂直距离恒为 w（对圆弧上近共线点也正确）
                float denom = 1f + Vector2.Dot(nIn, nOut);
                if (denom < 0.0001f) denom = 0.0001f;
                inset[i] = cur + w * (nIn + nOut) / denom;
            }

            // 正面（内缩多边形）三角化，法线 -Z
            var poly = new List<Vector2>(n);
            for (int i = 0; i < n; i++) poly.Add(inset[i]);
            var triIdx = TriangulatePolygon(poly);
            int baseIdx = verts.Count;
            for (int i = 0; i < poly.Count; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, frontZ));
                norms.Add(new Vector3(0, 0, -1));
            }
            for (int t = 0; t < triIdx.Count; t += 3)
            {
                tris.Add(baseIdx + triIdx[t]);
                tris.Add(baseIdx + triIdx[t + 1]);
                tris.Add(baseIdx + triIdx[t + 2]);
            }

            // 沿每条轮廓边生成斜面 + 侧壁
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 a = loop[i], b = loop[j];      // 外轮廓边
                Vector2 ia = inset[i], ib = inset[j];   // 对应内缩边
                Vector2 edgeDir = (b - a).normalized;
                Vector2 outward = new Vector2(edgeDir.y, -edgeDir.x); // 右侧 = 外侧

                // 斜面：内缩边(frontZ) -> 外轮廓边(bevelBackZ)
                Vector3 bevelN = new Vector3(outward.x, outward.y, -1f).normalized;
                AddQuad(verts, norms, tris,
                    new Vector3(ia.x, ia.y, frontZ),
                    new Vector3(a.x, a.y, bevelBackZ),
                    new Vector3(b.x, b.y, bevelBackZ),
                    new Vector3(ib.x, ib.y, frontZ),
                    bevelN);

                // 侧壁：外轮廓边 bevelBackZ -> backZ
                Vector3 wallN = new Vector3(outward.x, outward.y, 0f);
                AddQuad(verts, norms, tris,
                    new Vector3(a.x, a.y, bevelBackZ),
                    new Vector3(a.x, a.y, backZ),
                    new Vector3(b.x, b.y, backZ),
                    new Vector3(b.x, b.y, bevelBackZ),
                    wallN);
            }
        }

        var mesh = new Mesh { name = "BlockPrism" };
        if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>
    /// 追踪一组网格单元的外轮廓闭环。每条边定向为“内部在左”，从而每个环为 CCW。
    /// 返回若干闭环（一般 1 个；带洞形状会有多个，本游戏形状无洞）。
    /// </summary>
    private static List<List<Vector2>> TraceOutlineLoops(HashSet<(int x, int y)> cells)
    {
        // 收集所有有向边界边：start -> end，内部在左
        var edges = new Dictionary<(int, int), (int, int)>(); // startPoint -> endPoint
        foreach (var (cx, cy) in cells)
        {
            int x0 = cx, x1 = cx + 1, y0 = cy, y1 = cy + 1;
            if (!cells.Contains((cx, cy - 1))) AddEdge(edges, (x0, y0), (x1, y0)); // 下：+x
            if (!cells.Contains((cx + 1, cy))) AddEdge(edges, (x1, y0), (x1, y1)); // 右：+y
            if (!cells.Contains((cx, cy + 1))) AddEdge(edges, (x1, y1), (x0, y1)); // 上：-x
            if (!cells.Contains((cx - 1, cy))) AddEdge(edges, (x0, y1), (x0, y0)); // 左：-y
        }

        var loops = new List<List<Vector2>>();
        var visited = new HashSet<(int, int)>();
        foreach (var kv in edges)
        {
            if (visited.Contains(kv.Key)) continue;
            var loopPts = new List<(int, int)>();
            var p = kv.Key;
            int guard = 0;
            while (!visited.Contains(p) && edges.ContainsKey(p) && guard++ < 100000)
            {
                visited.Add(p);
                loopPts.Add(p);
                p = edges[p];
            }
            if (loopPts.Count >= 3)
            {
                // 去掉共线的中间点（把连续同向的边合并，得到角点序列）
                var simplified = new List<Vector2>();
                int m = loopPts.Count;
                for (int i = 0; i < m; i++)
                {
                    var prev = loopPts[(i - 1 + m) % m];
                    var cur = loopPts[i];
                    var next = loopPts[(i + 1) % m];
                    int d1x = cur.Item1 - prev.Item1, d1y = cur.Item2 - prev.Item2;
                    int d2x = next.Item1 - cur.Item1, d2y = next.Item2 - cur.Item2;
                    bool collinear = (d1x == d2x && d1y == d2y);
                    if (!collinear) simplified.Add(new Vector2(cur.Item1, cur.Item2));
                }
                if (simplified.Count >= 3) loops.Add(simplified);
            }
        }
        return loops;
    }

    private static void AddEdge(Dictionary<(int, int), (int, int)> edges, (int, int) s, (int, int) e)
    {
        edges[s] = e;
    }

    /// <summary>
    /// 对外轮廓的“外凸角”（CCW 左转角）做圆角处理；凹角（右转）保持直角不变。
    /// 通过在凸角处插入一小段圆弧顶点实现，圆弧半径受相邻边长一半约束。
    /// </summary>
    private static List<Vector2> RoundCorners(List<Vector2> loop, float radius)
    {
        if (radius <= 0.0001f) return loop;
        int n = loop.Count;
        if (n < 3) return loop;

        const int SEG = 3; // 每个圆角分段数
        var result = new List<Vector2>(n * (SEG + 1));
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = loop[(i - 1 + n) % n];
            Vector2 cur = loop[i];
            Vector2 next = loop[(i + 1) % n];
            Vector2 din = (cur - prev).normalized;
            Vector2 dout = (next - cur).normalized;
            float cross = din.x * dout.y - din.y * dout.x; // CCW 凸角 > 0

            float lenIn = (cur - prev).magnitude;
            float lenOut = (next - cur).magnitude;
            float r = Mathf.Min(radius, lenIn * 0.5f, lenOut * 0.5f);

            if (cross > 0.5f && r > 0.0001f)
            {
                Vector2 a = cur - din * r;                         // 入边切点
                Vector2 b = cur + dout * r;                        // 出边切点
                Vector2 c = a + new Vector2(-din.y, din.x) * r;    // 圆心（内侧）
                float angA = Mathf.Atan2(a.y - c.y, a.x - c.x);
                float angB = Mathf.Atan2(b.y - c.y, b.x - c.x);
                float delta = angB - angA;
                while (delta <= 0f) delta += 2f * Mathf.PI;
                while (delta > Mathf.PI) delta -= 2f * Mathf.PI;   // 取短弧
                for (int s = 0; s <= SEG; s++)
                {
                    float t = (float)s / SEG;
                    float ang = angA + delta * t;
                    result.Add(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
                }
            }
            else
            {
                result.Add(cur); // 凹角 / 直线：保留
            }
        }
        return result;
    }

    /// <summary>
    /// 简单多边形耳切三角化（输入假设为简单、无自交的多边形）。返回相对 poly 的索引三元组。
    /// </summary>
    private static List<int> TriangulatePolygon(List<Vector2> poly)
    {
        var indices = new List<int>();
        int n = poly.Count;
        if (n < 3) return indices;

        var V = new int[n];
        if (PolygonArea(poly) > 0f)
            for (int i = 0; i < n; i++) V[i] = i;          // CCW
        else
            for (int i = 0; i < n; i++) V[i] = n - 1 - i;  // 反转为 CCW

        int nv = n;
        int count = 2 * nv;
        for (int v = nv - 1; nv > 2;)
        {
            if (count-- <= 0) break; // 退化多边形，避免死循环

            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int wq = v + 1; if (nv <= wq) wq = 0;

            if (Snip(poly, u, v, wq, nv, V))
            {
                indices.Add(V[u]);
                indices.Add(V[v]);
                indices.Add(V[wq]);
                for (int s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }
        return indices;
    }

    private static float PolygonArea(List<Vector2> poly)
    {
        int n = poly.Count;
        float a = 0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
            a += poly[p].x * poly[q].y - poly[q].x * poly[p].y;
        return a * 0.5f;
    }

    private static bool Snip(List<Vector2> poly, int u, int v, int w, int n, int[] V)
    {
        Vector2 A = poly[V[u]];
        Vector2 B = poly[V[v]];
        Vector2 C = poly[V[w]];
        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;
        for (int p = 0; p < n; p++)
        {
            if (p == u || p == v || p == w) continue;
            if (PointInTriangle(poly[V[p]], A, B, C)) return false;
        }
        return true;
    }

    private static bool PointInTriangle(Vector2 P, Vector2 A, Vector2 B, Vector2 C)
    {
        float ax = C.x - B.x, ay = C.y - B.y;
        float bx = A.x - C.x, by = A.y - C.y;
        float cx = B.x - A.x, cy = B.y - A.y;
        float apx = P.x - A.x, apy = P.y - A.y;
        float bpx = P.x - B.x, bpy = P.y - B.y;
        float cpx = P.x - C.x, cpy = P.y - C.y;
        float aCROSSbp = ax * bpy - ay * bpx;
        float cCROSSap = cx * apy - cy * apx;
        float bCROSScp = bx * cpy - by * cpx;
        return (aCROSSbp >= 0f) && (bCROSScp >= 0f) && (cCROSSap >= 0f);
    }

    private static void AddTriangle(
        List<Vector3> verts, List<Vector3> norms, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
    {
        int idx = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c);
        norms.Add(normal); norms.Add(normal); norms.Add(normal);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
    }

    // 四边形 a->b->c->d（从面法线方向看为逆时针）
    private static void AddQuad(
        List<Vector3> verts, List<Vector3> norms, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        int idx = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
        norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
    }
}
