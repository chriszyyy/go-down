using UnityEngine;

/// <summary>
/// 六边形球的 3D 立体视觉：在 2D 正交视角下，用代码生成一个“带厚度 + 斜切边(bevel)”的
/// 六棱柱网格，配合自带光照的着色器（GoDown/HexPrismLit）。
///
/// 由于球只绕 Z 轴旋转：
/// - 正面（法线 -Z）在旋转下不变 → 稳定的彩色平面；
/// - 斜切边的法线在 XY 平面随旋转转动 → 高光沿六边形的边扫过，呈现正确的立体光影。
///
/// 网格作为球的子物体，自动跟随球的 Z 旋转；原 SpriteRenderer 在运行时被隐藏。
/// 皮肤颜色来自 HexagonSkinManager.SelectedSkinId。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HexPrismVisual : MonoBehaviour
{
    [Header("六边形尺寸")]
    [Tooltip("六边形外接半径（顶点到中心）。默认与碰撞体一致 0.9")]
    public float radius = 0.9f;

    [Tooltip("棱柱厚度（沿 Z）")]
    public float thickness = 0.5f;

    [Header("斜切边 (bevel)")]
    [Tooltip("斜切边在平面方向的宽度（从外缘向内）")]
    public float bevelWidth = 0.22f;

    [Tooltip("斜切边沿 Z 方向的深度（越大立体感越强）")]
    public float bevelDepth = 0.16f;

    [Tooltip("正面中心凸起高度（朝相机方向，形成六个三角切面）。0=平面")]
    public float crownHeight = 0.06f;

    [Tooltip("正面内圈高光窄面宽度（形成类似设计图的白色描边切面）。0=关闭")]
    public float trimWidth = 0.016f;

    [Header("光照")]
    [Tooltip("世界空间光照方向（指向光源）。固定不随球旋转，故旋转时光影正确")]
    public Vector3 lightDir = new Vector3(0.3f, 0.65f, -0.6f);
    public Color lightColor = Color.white;
    [Range(0f, 1f)] public float ambient = 0.4f;
    [Range(0f, 1f)] public float halfLambert = 0.28f;
    [Range(0f, 1f)] public float edgeLight = 0.1f;
    [Range(0f, 1f)] public float edgeWhiten = 0.2f;
    [Range(0f, 1f)] public float faceDarken = 0.18f;
    [Range(1f, 128f)] public float specPower = 36f;
    [Range(0f, 2f)] public float specStrength = 0.5f;
    public Color rimColor = Color.white;
    [Range(0.5f, 8f)] public float rimPower = 3f;
    [Range(0f, 2f)] public float rimStrength = 0.1f;

    [Header("钻石/宝石质感")]
    [Tooltip("启用宝石质感（菲涅尔透亮边 + 多面闪光 + 色散火彩）")]
    public bool gem = true;
    [Range(0f, 3f)] public float gemFresnel = 1.8f;
    [Range(0f, 3f)] public float gemSparkle = 1.6f;
    [Range(1f, 256f)] public float gemSparklePower = 80f;
    [Range(0f, 1f)] public float gemDispersion = 0.18f;
    [Range(0f, 1f)] public float gemTint = 0.2f;

    [Header("真实高光窄面")]
    [Tooltip("内环、外环、连接线这些窄面的高光强度")]
    [Range(0f, 2f)] public float trimLight = 0.55f;
    [Tooltip("高光窄面朝白色偏移的程度。越低越保留本色，越高越像白描边")]
    [Range(0f, 1f)] public float trimWhiten = 0.38f;

    [Header("皮肤颜色 (与 HexagonSkinManager 的 skinId 对应)")]
    public Color goldColor = new Color(1.00f, 0.78f, 0.20f);
    public Color blueColor = new Color(0.16f, 0.52f, 0.96f);
    public Color greenColor = new Color(0.22f, 0.80f, 0.40f);
    public Color purpleColor = new Color(0.64f, 0.35f, 0.95f);
    public Color redColor = new Color(0.93f, 0.23f, 0.36f);

    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Material materialInstance;
    private GameObject meshObject;
    private MeshFilter meshFilter;
    private float lastRadius;
    private float lastThickness;
    private float lastBevelWidth;
    private float lastBevelDepth;
    private float lastCrownHeight;
    private float lastTrimWidth;

    [Header("调试")]
    [Tooltip("勾选后，Play 模式下每帧重新推送参数到材质，可在 Inspector 实时调整外观（确定数值后取消以省性能）")]
    public bool liveTweak = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        BuildVisual();
        ApplySkinColor();
    }

    private void Update()
    {
        // 实时调参：勾选 liveTweak 时每帧重推材质参数，方便在 Play 中拖滑块看效果
        if (liveTweak && materialInstance != null)
        {
            RebuildMeshIfGeometryChanged();
            ApplyLightParams();
            ApplySkinColor();
        }
    }

    private void OnEnable()
    {
        HexagonSkinManager.OnChanged += ApplySkinColor;
    }

    private void OnDisable()
    {
        HexagonSkinManager.OnChanged -= ApplySkinColor;
    }

    private void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }

    private void BuildVisual()
    {
        // 隐藏原 2D sprite，改用 3D 棱柱
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Shader shader = Shader.Find("GoDown/HexPrismLit");
        if (shader == null)
        {
            Debug.LogWarning("HexPrismVisual: 未找到着色器 GoDown/HexPrismLit，保留 2D sprite。");
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            return;
        }

        materialInstance = new Material(shader) { name = "HexPrismLit (Instance)" };
        ApplyLightParams();

        meshObject = new GameObject("HexPrismMesh");
        meshObject.transform.SetParent(transform, false);
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
        meshObject.transform.localScale = Vector3.one;

        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = BuildHexPrismMesh();
        CacheGeometryParams();

        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = materialInstance;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        // 与原 sprite 的排序保持一致，确保在 2D Renderer 下层级正确
        if (spriteRenderer != null)
        {
            meshRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            meshRenderer.sortingOrder = spriteRenderer.sortingOrder;
        }
    }

    private void RebuildMeshIfGeometryChanged()
    {
        if (meshFilter == null) return;
        if (Mathf.Approximately(lastRadius, radius) &&
            Mathf.Approximately(lastThickness, thickness) &&
            Mathf.Approximately(lastBevelWidth, bevelWidth) &&
            Mathf.Approximately(lastBevelDepth, bevelDepth) &&
            Mathf.Approximately(lastCrownHeight, crownHeight) &&
            Mathf.Approximately(lastTrimWidth, trimWidth))
            return;

        Mesh old = meshFilter.sharedMesh;
        meshFilter.sharedMesh = BuildHexPrismMesh();
        CacheGeometryParams();
        if (old != null) Destroy(old);
    }

    private void CacheGeometryParams()
    {
        lastRadius = radius;
        lastThickness = thickness;
        lastBevelWidth = bevelWidth;
        lastBevelDepth = bevelDepth;
        lastCrownHeight = crownHeight;
        lastTrimWidth = trimWidth;
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
        materialInstance.SetFloat("_Gem", gem ? 1f : 0f);
        materialInstance.SetFloat("_GemFresnel", gemFresnel);
        materialInstance.SetFloat("_GemSparkle", gemSparkle);
        materialInstance.SetFloat("_GemSparklePower", gemSparklePower);
        materialInstance.SetFloat("_GemDispersion", gemDispersion);
        materialInstance.SetFloat("_GemTint", gemTint);
        materialInstance.SetFloat("_TrimLight", trimLight);
        materialInstance.SetFloat("_TrimWhiten", trimWhiten);
    }

    private void ApplySkinColor()
    {
        if (materialInstance == null) return;
        string skin = HexagonSkinManager.Instance != null
            ? HexagonSkinManager.Instance.SelectedSkinId
            : HexagonSkinManager.DefaultSkinId;
        materialInstance.SetColor("_BaseColor", GetColorForSkin(skin));

        if (skin == "rainbow")
        {
            materialInstance.SetFloat("_RainbowSkin", 1f);
            materialInstance.SetFloat("_GemDispersion", 1f);
            materialInstance.SetFloat("_GemTint", 0.45f);
            materialInstance.SetFloat("_GemSparkle", Mathf.Max(gemSparkle, 1.8f));
        }
        else
        {
            materialInstance.SetFloat("_RainbowSkin", 0f);
            materialInstance.SetFloat("_GemDispersion", gemDispersion);
            materialInstance.SetFloat("_GemTint", gemTint);
            materialInstance.SetFloat("_GemSparkle", gemSparkle);
        }
    }

    private Color GetColorForSkin(string skinId)
    {
        switch (skinId)
        {
            case "blue": return blueColor;
            case "green": return greenColor;
            case "purple": return purpleColor;
            case "red": return redColor;
            case "rainbow": return new Color(1f, 0.78f, 0.25f, 1f);
            case "gold":
            default: return goldColor;
        }
    }

    /// <summary>
    /// 生成带斜切边的六棱柱网格。相机沿 +Z 看，正面朝 -Z（最靠近相机）。
    /// 六个顶点角度 = 60°*i（与碰撞体一致：左右为尖角，上下为平边）。
    /// </summary>
    private Mesh BuildHexPrismMesh()
    {
        float rOuter = Mathf.Max(0.01f, radius);
        float rInner = Mathf.Max(0.001f, rOuter - Mathf.Max(0f, bevelWidth));
        float rTrimInner = Mathf.Max(0.001f, rInner - Mathf.Clamp(trimWidth, 0f, rInner * 0.45f));
        float halfT = Mathf.Max(0.001f, thickness * 0.5f);
        float frontZ = -halfT;                  // 正面（朝相机）
        float bevelBackZ = frontZ + Mathf.Max(0f, bevelDepth);
        float backZ = halfT;

        // 6 个角的方向
        Vector2[] dir = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Deg2Rad * (60f * i);
            dir[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        var verts = new System.Collections.Generic.List<Vector3>(64);
        var norms = new System.Collections.Generic.List<Vector3>(64);
        var tris = new System.Collections.Generic.List<int>(128);
        var colors = new System.Collections.Generic.List<Color>(64);

        // ---- 正面宝石冠面：中心向相机方向凸起，形成 6 个三角切面 ----
        // 相机沿 +Z 看，越小的 Z 越靠近相机，所以 crownZ = frontZ - crownHeight。
        float crownZ = frontZ - Mathf.Max(0f, crownHeight);
        Vector3 crown = new Vector3(0f, 0f, crownZ);
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            Vector3 a = new Vector3(dir[i].x * rTrimInner, dir[i].y * rTrimInner, frontZ);
            Vector3 b = new Vector3(dir[j].x * rTrimInner, dir[j].y * rTrimInner, frontZ);
            Vector3 n = Vector3.Cross(b - crown, a - crown).normalized;
            if (n.z > 0f) n = -n; // 始终朝向相机方向(-Z)

            int idx = verts.Count;
            verts.Add(crown); verts.Add(b); verts.Add(a);
            norms.Add(n); norms.Add(n); norms.Add(n);
            colors.Add(Color.black); colors.Add(Color.black); colors.Add(Color.black);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        }

        // ---- 正面内圈高光窄面：在冠面与外斜切边之间加入一圈真实小面 ----
        // 这些面几何上很窄，法线略向外倾斜，因此会被 shader 识别为“edge”，产生亮描边。
        if (rInner > rTrimInner + 0.0001f)
        {
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 1) % 6;
                Vector3 innerA = new Vector3(dir[i].x * rTrimInner, dir[i].y * rTrimInner, frontZ);
                Vector3 innerB = new Vector3(dir[j].x * rTrimInner, dir[j].y * rTrimInner, frontZ);
                Vector3 outerA = new Vector3(dir[i].x * rInner, dir[i].y * rInner, frontZ);
                Vector3 outerB = new Vector3(dir[j].x * rInner, dir[j].y * rInner, frontZ);

                Vector2 edgeMid = ((dir[i] + dir[j]) * 0.5f).normalized;
                Vector3 trimNormal = new Vector3(edgeMid.x * 0.45f, edgeMid.y * 0.45f, -1f).normalized;
                AddQuad(verts, norms, colors, tris, innerA, innerB, outerB, outerA, trimNormal, 1f);
            }
        }

        float trimZBias = 0.004f; // 稍微靠近相机，避免和 bevel 面 z-fight

        // ---- 斜切边（每个面独立顶点，平面着色，法线朝外 + 朝相机）----
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            Vector3 vInnerA = new Vector3(dir[i].x * rInner, dir[i].y * rInner, frontZ);
            Vector3 vInnerB = new Vector3(dir[j].x * rInner, dir[j].y * rInner, frontZ);
            Vector3 vOuterA = new Vector3(dir[i].x * rOuter, dir[i].y * rOuter, bevelBackZ);
            Vector3 vOuterB = new Vector3(dir[j].x * rOuter, dir[j].y * rOuter, bevelBackZ);

            // 面法线：朝外（XY）并朝相机（-Z）
            Vector3 faceNormal = Vector3.Cross(vOuterA - vInnerA, vInnerB - vInnerA).normalized;
            if (faceNormal.z > 0f) faceNormal = -faceNormal; // 确保朝 -Z

            AddQuad(verts, norms, colors, tris, vInnerA, vInnerB, vOuterB, vOuterA, faceNormal, 0.25f);

            // 外圈高光窄面：沿 bevel 外缘切出一条真实小面，形成连续外环亮边。
            float outerTrim = Mathf.Min(Mathf.Max(0.006f, trimWidth * 0.65f), bevelWidth * 0.45f);
            float t0 = 1f - (outerTrim / Mathf.Max(0.0001f, bevelWidth));
            Vector3 oInnerA = Vector3.Lerp(vInnerA, vOuterA, t0);
            Vector3 oInnerB = Vector3.Lerp(vInnerB, vOuterB, t0);
            // 轻微前移，避免与主 bevel 面 z-fight；不改变形状，仅用于显示高光
            oInnerA.z -= trimZBias; oInnerB.z -= trimZBias;
            Vector3 oOuterA = vOuterA; oOuterA.z -= trimZBias;
            Vector3 oOuterB = vOuterB; oOuterB.z -= trimZBias;
            AddQuad(verts, norms, colors, tris, oInnerA, oInnerB, oOuterB, oOuterA, faceNormal, 1f);
        }

        // ---- 六条连接线高光窄面：只覆盖 bevel 中段，避开内/外环端点，避免白色三角 ----
        float radialHalfWidth = Mathf.Max(0.0025f, trimWidth * 0.12f);
        // 连接线要从内圈拉到外圈，但保持极窄，避免在角点形成白色三角块
        float radialStartT = 0.02f;
        float radialEndT = 0.98f;
        for (int i = 0; i < 6; i++)
        {
            Vector2 radial = dir[i].normalized;
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            float r0 = Mathf.Lerp(rInner, rOuter, radialStartT);
            float r1 = Mathf.Lerp(rInner, rOuter, radialEndT);
            float z0 = Mathf.Lerp(frontZ, bevelBackZ, radialStartT) - trimZBias * 2f;
            float z1 = Mathf.Lerp(frontZ, bevelBackZ, radialEndT) - trimZBias * 2f;
            Vector3 ia = new Vector3(radial.x * r0 + tangent.x * radialHalfWidth,
                                     radial.y * r0 + tangent.y * radialHalfWidth, z0);
            Vector3 ib = new Vector3(radial.x * r0 - tangent.x * radialHalfWidth,
                                     radial.y * r0 - tangent.y * radialHalfWidth, z0);
            Vector3 ob = new Vector3(radial.x * r1 - tangent.x * radialHalfWidth,
                                     radial.y * r1 - tangent.y * radialHalfWidth, z1);
            Vector3 oa = new Vector3(radial.x * r1 + tangent.x * radialHalfWidth,
                                     radial.y * r1 + tangent.y * radialHalfWidth, z1);
            Vector3 n = new Vector3(radial.x * 0.55f, radial.y * 0.55f, -1f).normalized;
            AddQuad(verts, norms, colors, tris, ia, ib, ob, oa, n, 0.65f);
        }

        // ---- 侧壁（提供厚度/轮廓，法线径向，正交视角下基本侧视）----
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            Vector3 vTopA = new Vector3(dir[i].x * rOuter, dir[i].y * rOuter, bevelBackZ);
            Vector3 vTopB = new Vector3(dir[j].x * rOuter, dir[j].y * rOuter, bevelBackZ);
            Vector3 vBotA = new Vector3(dir[i].x * rOuter, dir[i].y * rOuter, backZ);
            Vector3 vBotB = new Vector3(dir[j].x * rOuter, dir[j].y * rOuter, backZ);

            Vector3 faceNormal = ((dir[i] + dir[j]) * 0.5f).normalized;
            Vector3 n3 = new Vector3(faceNormal.x, faceNormal.y, 0f);

            AddQuad(verts, norms, colors, tris, vTopA, vTopB, vBotB, vBotA, n3, 0f);
        }

        var mesh = new Mesh { name = "HexPrism" };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // 添加一个四边形（两个三角形），四个顶点按 a->b->c->d 顺序（从面法线方向看为逆时针）
    private static void AddQuad(
        System.Collections.Generic.List<Vector3> verts,
        System.Collections.Generic.List<Vector3> norms,
        System.Collections.Generic.List<Color> colors,
        System.Collections.Generic.List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, float trimMask = 0f)
    {
        int idx = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
        norms.Add(normal); norms.Add(normal); norms.Add(normal); norms.Add(normal);
        Color mask = new Color(trimMask, 0f, 0f, 1f);
        colors.Add(mask); colors.Add(mask); colors.Add(mask); colors.Add(mask);
        tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
        tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
    }
}
