using UnityEngine;
using System;

/// <summary>
/// 背景控制器 — 根据摄像机Y位置在不同"区域"之间平滑过渡背景颜色。
/// 区域从上到下：外太空 → 星云/银河 → 星球带 → 地球大气层 → 地面 → 地下 → 地壳/岩浆
///
/// 功能：
/// - 背景颜色渐变
/// - 星星粒子视差滚动 + 随机闪烁/淡出
/// - 各层特效粒子（星云尘埃、云层、泥土碎屑、岩浆火星）
/// </summary>
public class BackgroundController : MonoBehaviour
{
    [Serializable]
    public class BackgroundZone
    {
        [Tooltip("区域名称（方便编辑器查看）")]
        public string name;

        [Tooltip("该区域对应的Y坐标（摄像机位置）。区域按Y从大到小排列。")]
        public float yPosition;

        [Tooltip("该位置对应的背景颜色")]
        public Color color = Color.black;
    }

    [Header("引用")]
    [Tooltip("如果为空则自动获取 Camera.main")]
    public Camera targetCamera;

    [Header("区域配置（按Y值从大到小排列）")]
    [Tooltip("定义不同深度的背景颜色。相邻两个区域之间会自动做渐变插值。")]
    public BackgroundZone[] zones = new BackgroundZone[]
    {
        // 外太空（起始位置附近）
        new BackgroundZone { name = "外太空", yPosition = 10f,
            color = new Color(0.02f, 0.02f, 0.06f) },

        // 深空星云
        new BackgroundZone { name = "深空星云", yPosition = -50f,
            color = new Color(0.05f, 0.02f, 0.12f) },

        // 银河系
        new BackgroundZone { name = "银河系", yPosition = -150f,
            color = new Color(0.08f, 0.06f, 0.18f) },

        // 星球带
        new BackgroundZone { name = "星球带", yPosition = -300f,
            color = new Color(0.04f, 0.08f, 0.22f) },

        // 接近地球 — 深蓝
        new BackgroundZone { name = "近地轨道", yPosition = -500f,
            color = new Color(0.02f, 0.05f, 0.25f) },

        // 大气层 — 渐变到天蓝
        new BackgroundZone { name = "大气层", yPosition = -800f,
            color = new Color(0.35f, 0.65f, 0.92f) },

        // 地面 — 浅绿/棕
        new BackgroundZone { name = "地面", yPosition = -1200f,
            color = new Color(0.45f, 0.55f, 0.30f) },

        // 地下 — 深棕
        new BackgroundZone { name = "地下", yPosition = -1800f,
            color = new Color(0.30f, 0.18f, 0.08f) },

        // 地壳深处 — 暗红/橙
        new BackgroundZone { name = "地壳", yPosition = -2500f,
            color = new Color(0.40f, 0.12f, 0.05f) },

        // 岩浆层
        new BackgroundZone { name = "岩浆", yPosition = -4000f,
            color = new Color(0.60f, 0.15f, 0.02f) },
    };

    // ─── 星星设置 ─────────────────────────────────────────────
    [Header("星星")]
    [Tooltip("是否显示星星粒子")]
    public bool enableStars = true;

    [Tooltip("星星完全消失的Y位置（接近大气层时淡出）")]
    public float starsFadeOutY = -900f;

    [Tooltip("星星完全可见的Y位置")]
    public float starsFullVisibleY = 20f;

    [Tooltip("星星粒子系统（如果为空则自动创建）")]
    public ParticleSystem starsParticleSystem;

    [Tooltip("星星数量")]
    public int starsCount = 200;

    [Tooltip("星星散布范围")]
    public Vector2 starsSpreadRange = new Vector2(30f, 20f);

    [Tooltip("星星视差系数（0=完全跟随，看起来静止; 1=完全不跟随，滚得最快）")]
    [Range(0f, 1f)]
    public float starsParallaxFactor = 0.3f;

    [Tooltip("星星最大尺寸")]
    public float starsMaxSize = 0.25f;

    [Tooltip("星星最小尺寸")]
    public float starsMinSize = 0.05f;

    // ─── 层效果设置 ─────────────────────────────────────────────
    [Header("层过渡特效")]
    [Tooltip("是否启用层过渡粒子效果")]
    public bool enableLayerEffects = true;

    // 内部生成的层特效粒子系统
    private ParticleSystem nebulaParticles;   // 星云尘埃 (Y: -20 ~ -480)
    private ParticleSystem cloudParticles;    // 大气层云朵 (Y: -420 ~ -1200)
    private ParticleSystem debrisParticles;   // 地下碎屑 (Y: -1100 ~ -2700)
    private ParticleSystem emberParticles;    // 岩浆火星 (Y: -2400 ~ -4200)

    // 记录摄像机初始Y，用于计算视差偏移
    private float cameraStartY;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            cameraStartY = targetCamera.transform.position.y;

        if (enableStars && starsParticleSystem == null)
            CreateStarsParticleSystem();

        if (enableLayerEffects)
            CreateLayerEffects();

        // 立即应用一次
        UpdateBackground();
    }

    private void LateUpdate()
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (targetCamera == null) return;

        float camY = targetCamera.transform.position.y;

        // 更新背景颜色
        targetCamera.backgroundColor = EvaluateBackgroundColor(camY);

        // 更新星星（视差 + 淡出）
        if (enableStars && starsParticleSystem != null)
        {
            UpdateStars(camY);
        }

        // 更新层过渡特效
        if (enableLayerEffects)
        {
            UpdateLayerEffects(camY);
        }
    }

    /// <summary>
    /// 根据Y位置在区域之间插值计算背景颜色
    /// </summary>
    private Color EvaluateBackgroundColor(float y)
    {
        if (zones == null || zones.Length == 0)
            return Color.black;

        // 在第一个区域之上
        if (y >= zones[0].yPosition)
            return zones[0].color;

        // 在最后一个区域之下
        if (y <= zones[zones.Length - 1].yPosition)
            return zones[zones.Length - 1].color;

        // 找到y落在哪两个区域之间
        for (int i = 0; i < zones.Length - 1; i++)
        {
            float upperY = zones[i].yPosition;
            float lowerY = zones[i + 1].yPosition;

            if (y <= upperY && y >= lowerY)
            {
                float t = (upperY - y) / (upperY - lowerY);
                return Color.Lerp(zones[i].color, zones[i + 1].color, t);
            }
        }

        return zones[zones.Length - 1].color;
    }

    // ═══════════════════════════════════════════════════════════
    //  星 星 系 统
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 更新星星的视差位置和整体透明度。
    /// 视差原理：摄像机往下移动时，星星跟随得慢一些，所以在屏幕上看起来是往上飘的。
    /// </summary>
    private void UpdateStars(float camY)
    {
        Vector3 camPos = targetCamera.transform.position;

        // 视差：星星只跟随摄像机的一部分移动量，制造深度差异感
        // parallaxFactor=0 → 完全跟随（不滚动）
        // parallaxFactor=1 → 完全不跟随（最大滚动）
        float deltaY = camY - cameraStartY;
        float parallaxY = camY - deltaY * starsParallaxFactor;

        starsParticleSystem.transform.position = new Vector3(camPos.x, parallaxY, 50f);

        // 根据深度淡出星星 —— 通过调节发射率来实现渐进淡出
        // 已有粒子通过 Color Over Lifetime 自己闪烁/随机淡出
        float alpha;
        if (camY >= starsFullVisibleY)
        {
            alpha = 1f;
        }
        else if (camY <= starsFadeOutY)
        {
            alpha = 0f;
        }
        else
        {
            alpha = (camY - starsFadeOutY) / (starsFullVisibleY - starsFadeOutY);
        }

        // 调节发射率：深处不再生成新星星
        var emission = starsParticleSystem.emission;
        emission.rateOverTime = (starsCount / 6f) * alpha;

        // 调节起始颜色的alpha：新生成的星星更暗
        var main = starsParticleSystem.main;
        Color startCol = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 0.9f));
        main.startColor = new ParticleSystem.MinMaxGradient(startCol);

        // 完全淡出后停止发射
        if (alpha <= 0f && starsParticleSystem.isPlaying)
        {
            starsParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        else if (alpha > 0f && !starsParticleSystem.isPlaying)
        {
            starsParticleSystem.Play();
        }
    }

    /// <summary>
    /// 自动创建星星粒子系统，带随机闪烁/淡出效果
    /// </summary>
    private void CreateStarsParticleSystem()
    {
        GameObject starsGO = new GameObject("Stars");
        starsGO.transform.SetParent(transform);
        starsGO.transform.localPosition = Vector3.zero;

        starsParticleSystem = starsGO.AddComponent<ParticleSystem>();

        // 停止默认播放，先配置
        starsParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = starsParticleSystem.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 10f); // 随机生命周期
        main.startSpeed = 0.02f;
        main.startSize = new ParticleSystem.MinMaxCurve(starsMinSize, starsMaxSize);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.maxParticles = starsCount;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;
        main.gravityModifier = 0f;

        // 持续发射 + 初始burst填满屏幕
        var emission = starsParticleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = starsCount / 6f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, starsCount)
        });

        // 形状：矩形区域
        var shape = starsParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(starsSpreadRange.x, starsSpreadRange.y, 1f);

        // ── 核心改进：Color Over Lifetime 实现随机闪烁/淡出 ──
        var colorOverLifetime = starsParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;

        // 两条alpha曲线，粒子在这两条之间随机取值 → 每颗星星闪烁节奏不同
        Gradient gradientMin = new Gradient();
        gradientMin.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.1f, 0f),    // 淡入
                new GradientAlphaKey(0.6f, 0.15f),  // 亮起
                new GradientAlphaKey(0.1f, 0.5f),   // 暗掉
                new GradientAlphaKey(0.4f, 0.75f),  // 再亮
                new GradientAlphaKey(0f,   1f)      // 消失
            }
        );

        Gradient gradientMax = new Gradient();
        gradientMax.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),     // 淡入
                new GradientAlphaKey(1f,   0.2f),   // 最亮
                new GradientAlphaKey(0.7f, 0.4f),   // 微暗
                new GradientAlphaKey(1f,   0.7f),   // 再亮
                new GradientAlphaKey(0f,   1f)      // 消失
            }
        );

        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradientMin, gradientMax);

        // 关闭不需要的模块
        var velocityOverLifetime = starsParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        // Size Over Lifetime：轻微的大小波动模拟闪烁
        var sizeOverLifetime = starsParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.5f, 0.7f),
            new Keyframe(0.8f, 1f),
            new Keyframe(1f, 0.3f)
        ));

        // 渲染器设置
        var renderer = starsParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -100;
        renderer.material = CreateAdditiveMaterial();

        starsParticleSystem.Play();
    }

    // ═══════════════════════════════════════════════════════════
    //  层 过 渡 特 效
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 创建各层的视觉特效粒子系统
    /// </summary>
    private void CreateLayerEffects()
    {
        nebulaParticles = CreateEffectSystem("NebulaEffect",
            color1: new Color(0.4f, 0.2f, 0.8f, 0.18f),
            color2: new Color(0.2f, 0.5f, 0.9f, 0.14f),
            count: 50, sizeMin: 0.4f, sizeMax: 1.2f,
            spread: new Vector2(25f, 18f), lifetime: 6f,
            speed: 0.1f, sortOrder: -99);

        cloudParticles = CreateEffectSystem("CloudEffect",
            color1: new Color(1f, 1f, 1f, 0.25f),
            color2: new Color(0.8f, 0.9f, 1f, 0.2f),
            count: 60, sizeMin: 2.0f, sizeMax: 5.5f,
            spread: new Vector2(30f, 15f), lifetime: 8f,
            speed: 0.2f, sortOrder: -98);

        debrisParticles = CreateEffectSystem("DebrisEffect",
            color1: new Color(0.6f, 0.4f, 0.2f, 0.45f),
            color2: new Color(0.4f, 0.3f, 0.15f, 0.3f),
            count: 80, sizeMin: 0.12f, sizeMax: 0.35f,
            spread: new Vector2(20f, 15f), lifetime: 4f,
            speed: 0.15f, sortOrder: -97);

        emberParticles = CreateEffectSystem("EmberEffect",
            color1: new Color(1f, 0.4f, 0.1f, 0.7f),
            color2: new Color(1f, 0.7f, 0.2f, 0.5f),
            count: 80, sizeMin: 0.08f, sizeMax: 0.25f,
            spread: new Vector2(20f, 12f), lifetime: 3f,
            speed: 0.5f, sortOrder: -96);

        // 火星需要向上飘动
        if (emberParticles != null)
        {
            var vel = emberParticles.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        }
    }

    /// <summary>
    /// 通用特效粒子系统创建方法
    /// </summary>
    private ParticleSystem CreateEffectSystem(string objectName,
        Color color1, Color color2,
        int count, float sizeMin, float sizeMax,
        Vector2 spread, float lifetime, float speed, int sortOrder)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(color1, color2);
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = count / lifetime;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(spread.x, spread.y, 1f);

        // 淡入淡出
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient fadeGrad = new Gradient();
        fadeGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f,  0f),
                new GradientAlphaKey(1f,  0.15f),
                new GradientAlphaKey(1f,  0.75f),
                new GradientAlphaKey(0f,  1f)
            }
        );
        colorOverLife.color = fadeGrad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortOrder;
        renderer.material = CreateAdditiveMaterial();

        // 默认不播放，由UpdateLayerEffects按需开启
        return ps;
    }

    /// <summary>
    /// 创建Additive混合模式的粒子材质，内含程序化生成的圆形渐变纹理
    /// </summary>
    private Material CreateAdditiveMaterial()
    {
        // 使用URP粒子着色器（项目使用URP渲染管线）
        var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        // URP粒子着色器的Additive设置
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 2);   // Additive
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.SetColor("_BaseColor", Color.white);

        // 程序化生成圆形渐变纹理，避免粒子渲染成方块
        mat.SetTexture("_BaseMap", CreateCircleTexture(32));

        return mat;
    }

    /// <summary>
    /// 程序化生成一张圆形渐变纹理（中心亮，边缘淡出到透明）
    /// </summary>
    private static Texture2D circleTextureCache;
    private Texture2D CreateCircleTexture(int size)
    {
        if (circleTextureCache != null) return circleTextureCache;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size * 0.5f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                // 从中心到边缘：白色柔和渐变到透明
                float alpha = Mathf.Clamp01(1f - dist * dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply(false, true); // 设为不可读以节省内存
        circleTextureCache = tex;
        return tex;
    }

    /// <summary>
    /// 根据摄像机深度启用/禁用各层特效，并控制透明度
    /// </summary>
    private void UpdateLayerEffects(float camY)
    {
        Vector3 camPos = targetCamera.transform.position;

        // ── 星云尘埃 (Y: -20 ~ -480) ──
        UpdateEffectSystem(nebulaParticles, camY, camPos,
            fadeInY: -20f, fullStartY: -60f, fullEndY: -350f, fadeOutY: -480f,
            parallax: 0.2f);

        // ── 大气层云朵 (Y: -420 ~ -1200) ──
        UpdateEffectSystem(cloudParticles, camY, camPos,
            fadeInY: -420f, fullStartY: -500f, fullEndY: -1050f, fadeOutY: -1200f,
            parallax: 0.15f);

        // ── 地下碎屑 (Y: -1100 ~ -2700) ──
        UpdateEffectSystem(debrisParticles, camY, camPos,
            fadeInY: -1100f, fullStartY: -1200f, fullEndY: -2400f, fadeOutY: -2700f,
            parallax: 0.1f);

        // ── 岩浆火星 (Y: -2400 ~ -4200) ──
        UpdateEffectSystem(emberParticles, camY, camPos,
            fadeInY: -2400f, fullStartY: -2550f, fullEndY: -3900f, fadeOutY: -4200f,
            parallax: 0.05f);
    }

    /// <summary>
    /// 更新单个特效系统：位置（视差）、可见性、发射率
    /// fadeInY → fullStartY: 淡入
    /// fullStartY → fullEndY: 完全可见
    /// fullEndY → fadeOutY: 淡出
    /// </summary>
    private void UpdateEffectSystem(ParticleSystem ps, float camY, Vector3 camPos,
        float fadeInY, float fullStartY, float fullEndY, float fadeOutY,
        float parallax)
    {
        if (ps == null) return;

        // 计算可见度
        float visibility = 0f;
        if (camY > fadeInY || camY < fadeOutY)
        {
            visibility = 0f;
        }
        else if (camY <= fadeInY && camY > fullStartY)
        {
            // 淡入区间
            visibility = (fadeInY - camY) / (fadeInY - fullStartY);
        }
        else if (camY <= fullStartY && camY >= fullEndY)
        {
            // 完全可见
            visibility = 1f;
        }
        else if (camY < fullEndY && camY >= fadeOutY)
        {
            // 淡出区间
            visibility = (camY - fadeOutY) / (fullEndY - fadeOutY);
        }

        visibility = Mathf.Clamp01(visibility);

        // 控制播放/停止
        if (visibility <= 0f)
        {
            if (ps.isPlaying)
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            return;
        }

        if (!ps.isPlaying)
            ps.Play();

        // 视差定位
        float deltaY = camY - cameraStartY;
        float parallaxY = camY - deltaY * parallax;
        ps.transform.position = new Vector3(camPos.x, parallaxY, 45f);

        // 根据可见度调节发射率
        var emission = ps.emission;
        var main = ps.main;
        float baseRate = main.maxParticles / main.startLifetime.constantMax;
        emission.rateOverTime = baseRate * visibility;
    }
}
