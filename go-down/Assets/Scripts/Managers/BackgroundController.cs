using UnityEngine;
using System;

/// <summary>
/// 背景控制器 — 根据摄像机Y位置在不同"区域"之间平滑过渡背景颜色。
/// 区域从上到下：外太空 → 星云/银河 → 星球带 → 地球大气层 → 地面 → 地下 → 地壳/岩浆
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

    [Header("星星")]
    [Tooltip("是否显示星星粒子")]
    public bool enableStars = true;

    [Tooltip("星星完全消失的Y位置（接近大气层时淡出）")]
    public float starsFadeOutY = -600f;

    [Tooltip("星星完全可见的Y位置")]
    public float starsFullVisibleY = -100f;

    [Tooltip("星星粒子系统（如果为空则自动创建）")]
    public ParticleSystem starsParticleSystem;

    [Tooltip("星星数量")]
    public int starsCount = 200;

    [Tooltip("星星散布范围")]
    public Vector2 starsSpreadRange = new Vector2(30f, 20f);

    [Tooltip("星星视差系数（0=完全跟随摄像机不动, 1=完全不跟随）")]
    [Range(0f, 1f)]
    public float starsParallaxFactor = 0.95f;

    [Tooltip("星星最大尺寸")]
    public float starsMaxSize = 0.08f;

    [Tooltip("星星最小尺寸")]
    public float starsMinSize = 0.02f;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (enableStars && starsParticleSystem == null)
            CreateStarsParticleSystem();

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

        // 更新星星
        if (enableStars && starsParticleSystem != null)
        {
            UpdateStars(camY);
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

    /// <summary>
    /// 更新星星的位置和透明度
    /// </summary>
    private void UpdateStars(float camY)
    {
        // 星星始终跟随摄像机XY（发射器在摄像机周围持续生成），Z推到背景
        Vector3 camPos = targetCamera.transform.position;
        starsParticleSystem.transform.position = new Vector3(camPos.x, camPos.y, 50f);

        // 根据深度淡出星星
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

        var main = starsParticleSystem.main;
        Color startColor = main.startColor.color;
        startColor.a = alpha;
        main.startColor = new ParticleSystem.MinMaxGradient(startColor);
    }

    /// <summary>
    /// 自动创建简单的星星粒子系统
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
        main.startLifetime = 8f;
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

        // 关闭不需要的模块
        var velocityOverLifetime = starsParticleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = false;

        // 渲染器设置
        var renderer = starsParticleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -100;
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = Color.white;
        renderer.material.SetFloat("_Mode", 1); // Additive

        starsParticleSystem.Play();
    }
}
