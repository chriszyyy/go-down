using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 设置面板控制器（UI Toolkit）—— 简化版。
/// 只暴露音乐 / 音效两组：滑块控制音量，开关直接静音/恢复。
/// 占位实现：所有变更打 Debug.Log；接入 GameUserSettings 后替换为真实持久化。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance { get; private set; }

    [Tooltip("返回时激活的 GameObject（一般是 StartMenu）。")]
    public GameObject returnTarget;

    private Button backButton;

    private Slider musicSlider;
    private Slider sfxSlider;
    private Toggle musicToggle;
    private Toggle sfxToggle;

    // 动态插入到滑块 tracker 与 dragger 之间的填充结构（外层裁剪 wrap + 内层等宽 image）
    private VisualElement musicFillWrap;
    private VisualElement musicFillImage;
    private VisualElement sfxFillWrap;
    private VisualElement sfxFillImage;

    private Button navShop;
    private Button navRate;
    private Button navSettings;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        UIPause.Acquire(); // 面板可见 → 暂停游戏

        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        backButton = root.Q<Button>("back-btn");
        musicSlider = root.Q<Slider>("music-slider");
        sfxSlider = root.Q<Slider>("sfx-slider");
        musicToggle = root.Q<Toggle>("music-toggle");
        sfxToggle = root.Q<Toggle>("sfx-toggle");
        navShop = root.Q<Button>("nav-shop");
        navRate = root.Q<Button>("nav-rate");
        navSettings = root.Q<Button>("nav-settings");

        if (backButton != null) backButton.clicked += OnBack;

        // 滑块：在 tracker 与 dragger 之间插入一个填充结构，随值变化更新可见宽度
        WireSlider(musicSlider, ref musicFillWrap, ref musicFillImage, "music");
        WireSlider(sfxSlider, ref sfxFillWrap, ref sfxFillImage, "sfx");

        // —— 接入实际的游戏音频系统 ——
        // 1) 用持久化的设置初始化 UI
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameUserSettings.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameUserSettings.SfxVolume);
        if (musicToggle != null) musicToggle.SetValueWithoutNotify(GameUserSettings.MusicEnabled);
        if (sfxToggle != null) sfxToggle.SetValueWithoutNotify(GameUserSettings.SfxEnabled);

        // 2) 用户操作 → 写回设置（GameAudioController 会在 Update 里轮询应用）
        if (musicSlider != null) musicSlider.RegisterValueChangedCallback(e => GameUserSettings.MusicVolume = e.newValue);
        if (sfxSlider != null) sfxSlider.RegisterValueChangedCallback(e => GameUserSettings.SfxVolume = e.newValue);
        if (musicToggle != null) musicToggle.RegisterValueChangedCallback(e => GameUserSettings.MusicEnabled = e.newValue);
        if (sfxToggle != null) sfxToggle.RegisterValueChangedCallback(e => GameUserSettings.SfxEnabled = e.newValue);

        if (navShop != null) navShop.clicked += OnNavShop;
        if (navRate != null) navRate.clicked += () => Debug.Log("[Settings] nav: rate");
        if (navSettings != null) navSettings.clicked += () => Debug.Log("[Settings] nav: settings (already here)");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        UIPause.Release();

        if (backButton != null) backButton.clicked -= OnBack;
        // 占位实现，其它 lambda 订阅在面板隐藏后随 root 一起被销毁，无需精确解绑。
    }

    private void OnBack()
    {
        Debug.Log("[Settings] Back clicked");
        Hide();
    }

    private void OnNavShop()
    {
        Debug.Log("[Settings] nav: shop");
        if (ShopPanel.Instance != null)
        {
            ShopPanel.Instance.Show(returnTarget);
            Hide();
        }
    }

    /// <summary>
    /// 把一个 slider 接入填充贴图：插入 wrap+image，注册值变化与几何变化回调，
    /// 使填充 wrap 的宽度始终等于当前进度百分比，内层 image 保持 tracker 完整像素宽度。
    /// </summary>
    private static void WireSlider(Slider slider, ref VisualElement wrap, ref VisualElement image, string label)
    {
        if (slider == null) return;

        (wrap, image) = AttachSliderFill(slider);
        var capturedWrap = wrap;
        var capturedImage = image;

        slider.RegisterValueChangedCallback(e =>
        {
            UpdateSliderFill(slider, capturedWrap);
            Debug.Log($"[Settings] {label} = {e.newValue:F2}");
        });

        // tracker 决定可视宽度；几何变化时同步 wrap 百分比 + 内层 image 像素宽度
        var tracker = slider.Q(className: "unity-base-slider__tracker");
        if (tracker != null)
        {
            tracker.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                UpdateSliderFill(slider, capturedWrap);
                SyncFillImageWidth(tracker, capturedImage);
            });
        }

        // slider 自身首帧布局完成后也跑一次，确保初始进度立刻显示
        slider.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            UpdateSliderFill(slider, capturedWrap);
            SyncFillImageWidth(tracker, capturedImage);
        });
    }

    /// <summary>
    /// 在 slider 的 tracker 内插入一个"填充"结构。
    /// 把 wrap 作为 tracker 的 child 而不是 input 的 sibling —— 这样 dragger（input 的另一个 child）
    /// 自然在 tracker 整体之后渲染，永远在拖动球之下，不需要每帧 BringToFront。
    /// 为了不拉伸贴图本身，采用外层裁剪 + 内层锁定 tracker 完整像素宽度。
    /// </summary>
    private static (VisualElement wrap, VisualElement image) AttachSliderFill(Slider slider)
    {
        var tracker = slider.Q(className: "unity-base-slider__tracker");
        if (tracker == null) return (null, null);

        var wrap = new VisualElement
        {
            name = slider.name + "-fill",
            pickingMode = PickingMode.Ignore
        };
        wrap.AddToClassList("audio-slider__fill");

        var image = new VisualElement
        {
            name = slider.name + "-fill-image",
            pickingMode = PickingMode.Ignore
        };
        image.AddToClassList("audio-slider__fill-image");
        wrap.Add(image);

        // 直接挂到 tracker 内部。tracker 在 input 容器里早于 dragger，
        // 因此 dragger 总是在 tracker（含其全部子元素）之上渲染。
        tracker.Add(wrap);
        return (wrap, image);
    }

    /// <summary>根据 slider 的当前值更新填充 wrapper 的宽度。
    /// 用 “拖动球的左边缘位置” 作为填充末端以像素为单位精确对齐：
    /// 填充宽度 = pct × (trackerWidth − draggerWidth)。这样无论 slider 处于哪个位置，
    /// 填充的右边缘都能紧贴拖动球的左边缘。</summary>
    private static void UpdateSliderFill(Slider slider, VisualElement wrap)
    {
        if (slider == null || wrap == null) return;
        float lo = slider.lowValue, hi = slider.highValue;
        float pct = hi > lo ? Mathf.Clamp01((slider.value - lo) / (hi - lo)) : 0f;

        var tracker = slider.Q(className: "unity-base-slider__tracker");
        var dragger = slider.Q(className: "unity-base-slider__dragger");
        if (tracker == null) return;

        float trackerW = tracker.resolvedStyle.width;
        float draggerW = dragger != null ? dragger.resolvedStyle.width : 0f;
        float fillPx = Mathf.Max(0f, pct * Mathf.Max(0f, trackerW - draggerW));
        wrap.style.width = new StyleLength(fillPx);
    }

    /// <summary>同步内层 image 的实际像素宽度 = tracker 的像素宽度，这样无论 wrap 由多窄，
    /// 贴图都以原始比例完整渲染，只是被 wrap 裁剪出看到的部分。</summary>
    private static void SyncFillImageWidth(VisualElement tracker, VisualElement image)
    {
        if (tracker == null || image == null) return;
        float w = tracker.resolvedStyle.width;
        if (w > 0f)
        {
            image.style.width = new StyleLength(w);
        }
    }

    /// <summary>显示本面板。</summary>
    public void Show(GameObject returnTo = null)
    {
        if (returnTo != null) returnTarget = returnTo;
        gameObject.SetActive(true);
    }

    /// <summary>隐藏本面板，激活返回目标。</summary>
    public void Hide()
    {
        // 先打开返回目标再隐藏自己，保证 UIPause refcount 始终 >= 1。
        if (returnTarget != null) returnTarget.SetActive(true);
        gameObject.SetActive(false);
    }
}
