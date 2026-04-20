using UnityEngine;
using UnityEditor;

/// <summary>
/// 背景层级预览窗口 — 在Play模式下通过Y滑块即时跳转摄像机位置，
/// 预览不同深度的背景颜色和粒子效果。
/// 菜单：Tools > Background Layer Preview
/// </summary>
public class BackgroundLayerPreview : EditorWindow
{
    private float sliderY = 0f;
    private bool overrideActive = false;
    private CameraFollower cachedFollower;

    // 各层特效的Y范围（与BackgroundController.UpdateLayerEffects保持一致）
    private static readonly string[] layerNames = { "星云", "云层", "碎屑", "火星" };
    private static readonly float[] fadeInYs =    { -20f,   -420f,   -1100f, -2400f };
    private static readonly float[] fullStartYs = { -60f,   -500f,   -1200f, -2550f };
    private static readonly float[] fullEndYs =   { -350f,  -1050f,  -2400f, -3900f };
    private static readonly float[] fadeOutYs =   { -480f,  -1200f,  -2700f, -4200f };

    [MenuItem("Tools/Background Layer Preview")]
    public static void ShowWindow()
    {
        GetWindow<BackgroundLayerPreview>("背景层级预览");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        RestoreCameraFollower();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            overrideActive = false;
            cachedFollower = null;
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请在Play模式下使用此窗口。", MessageType.Warning);
            return;
        }

        // ── 摄像机Y控制 ──
        GUILayout.Label("摄像机Y坐标控制", EditorStyles.boldLabel);

        bool wasOverride = overrideActive;
        overrideActive = EditorGUILayout.Toggle("启用摄像机覆盖", overrideActive);

        if (overrideActive && !wasOverride)
            DisableCameraFollower();
        else if (!overrideActive && wasOverride)
            RestoreCameraFollower();

        EditorGUI.BeginDisabledGroup(!overrideActive);

        sliderY = EditorGUILayout.Slider("Y 位置", sliderY, -4500f, 20f);

        // ── 快速跳转按钮 ──
        GUILayout.Label("快速跳转", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("外太空\n(Y:10)")) sliderY = 10f;
        if (GUILayout.Button("星云\n(Y:-40)")) sliderY = -40f;
        if (GUILayout.Button("云层\n(Y:-460)")) sliderY = -460f;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("碎屑\n(Y:-1150)")) sliderY = -1150f;
        if (GUILayout.Button("火星\n(Y:-2475)")) sliderY = -2475f;
        if (GUILayout.Button("岩浆\n(Y:-4000)")) sliderY = -4000f;
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // ── 当前状态 ──
        GUILayout.Label("当前状态", EditorStyles.boldLabel);

        // 背景颜色预览
        var bgCtrl = Object.FindObjectOfType<BackgroundController>();
        if (bgCtrl != null && bgCtrl.zones != null && bgCtrl.zones.Length > 0)
        {
            float[] positions = new float[bgCtrl.zones.Length];
            Color[] colors = new Color[bgCtrl.zones.Length];
            for (int i = 0; i < bgCtrl.zones.Length; i++)
            {
                positions[i] = bgCtrl.zones[i].yPosition;
                colors[i] = bgCtrl.zones[i].color;
            }

            Color bgColor = BackgroundMathUtils.EvaluateBackgroundColor(sliderY, positions, colors);
            Rect colorRect = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(colorRect, bgColor);

            // 当前所处区域名称
            string zoneName = GetCurrentZoneName(bgCtrl, sliderY);
            EditorGUILayout.LabelField("当前区域", zoneName);
        }

        // 星星透明度
        float starsAlpha = BackgroundMathUtils.CalculateStarsAlpha(sliderY, 20f, -900f);
        Rect starsRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(starsRect, starsAlpha, $"星星: {starsAlpha:P0}");

        GUILayout.Space(4);

        // 各层特效可见度
        for (int i = 0; i < layerNames.Length; i++)
        {
            float vis = BackgroundMathUtils.CalculateEffectVisibility(
                sliderY, fadeInYs[i], fullStartYs[i], fullEndYs[i], fadeOutYs[i]);
            Rect rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, vis, $"{layerNames[i]}: {vis:P0}");
            GUILayout.Space(2);
        }

        // ── 应用摄像机位置 ──
        if (overrideActive)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 pos = cam.transform.position;
                pos.y = sliderY;
                cam.transform.position = pos;
            }
            Repaint();
        }
    }

    private string GetCurrentZoneName(BackgroundController bgCtrl, float y)
    {
        if (bgCtrl.zones == null || bgCtrl.zones.Length == 0)
            return "无";

        if (y >= bgCtrl.zones[0].yPosition)
            return bgCtrl.zones[0].name;

        for (int i = 0; i < bgCtrl.zones.Length - 1; i++)
        {
            if (y <= bgCtrl.zones[i].yPosition && y >= bgCtrl.zones[i + 1].yPosition)
                return $"{bgCtrl.zones[i].name} → {bgCtrl.zones[i + 1].name}";
        }

        return bgCtrl.zones[bgCtrl.zones.Length - 1].name;
    }

    private void DisableCameraFollower()
    {
        if (cachedFollower == null)
            cachedFollower = Object.FindObjectOfType<CameraFollower>();

        if (cachedFollower != null)
            cachedFollower.enabled = false;
    }

    private void RestoreCameraFollower()
    {
        if (cachedFollower != null)
        {
            cachedFollower.enabled = true;
            cachedFollower = null;
        }
    }
}
