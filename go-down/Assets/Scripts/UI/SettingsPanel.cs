using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 设置面板控制器（UI Toolkit）。
/// 占位实现：滑块 / 开关 / 难度按钮 / 链接行的状态改变都打 Debug.Log，
/// 等接入真实 GameUserSettings 等系统后再把数值持久化。
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
    private Button musicMuteButton;
    private Button sfxMuteButton;

    private Toggle vibrationToggle;
    private Toggle hapticToggle;

    private Button diffEasy;
    private Button diffNormal;
    private Button diffHard;

    private Button rowLanguage;
    private Button rowRate;
    private Button rowPrivacy;
    private Button resetButton;

    private Button navShop;
    private Button navRate;
    private Button navSettings;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        backButton = root.Q<Button>("back-btn");
        musicSlider = root.Q<Slider>("music-slider");
        sfxSlider = root.Q<Slider>("sfx-slider");
        musicMuteButton = root.Q<Button>("music-mute-btn");
        sfxMuteButton = root.Q<Button>("sfx-mute-btn");
        vibrationToggle = root.Q<Toggle>("vibration-toggle");
        hapticToggle = root.Q<Toggle>("haptic-toggle");
        diffEasy = root.Q<Button>("diff-easy");
        diffNormal = root.Q<Button>("diff-normal");
        diffHard = root.Q<Button>("diff-hard");
        rowLanguage = root.Q<Button>("row-language");
        rowRate = root.Q<Button>("row-rate");
        rowPrivacy = root.Q<Button>("row-privacy");
        resetButton = root.Q<Button>("reset-btn");
        navShop = root.Q<Button>("nav-shop");
        navRate = root.Q<Button>("nav-rate");
        navSettings = root.Q<Button>("nav-settings");

        if (backButton != null) backButton.clicked += OnBack;
        if (musicMuteButton != null) musicMuteButton.clicked += () => Debug.Log("[Settings] music mute");
        if (sfxMuteButton != null) sfxMuteButton.clicked += () => Debug.Log("[Settings] sfx mute");
        if (musicSlider != null) musicSlider.RegisterValueChangedCallback(e => Debug.Log($"[Settings] music = {e.newValue:F2}"));
        if (sfxSlider != null) sfxSlider.RegisterValueChangedCallback(e => Debug.Log($"[Settings] sfx = {e.newValue:F2}"));
        if (vibrationToggle != null) vibrationToggle.RegisterValueChangedCallback(e => Debug.Log($"[Settings] vibration = {e.newValue}"));
        if (hapticToggle != null) hapticToggle.RegisterValueChangedCallback(e => Debug.Log($"[Settings] haptic = {e.newValue}"));

        if (diffEasy != null) diffEasy.clicked += () => SelectDifficulty(0);
        if (diffNormal != null) diffNormal.clicked += () => SelectDifficulty(1);
        if (diffHard != null) diffHard.clicked += () => SelectDifficulty(2);

        if (rowLanguage != null) rowLanguage.clicked += () => Debug.Log("[Settings] Language clicked");
        if (rowRate != null) rowRate.clicked += () => Debug.Log("[Settings] Rate Us clicked");
        if (rowPrivacy != null) rowPrivacy.clicked += () => Debug.Log("[Settings] Privacy Policy clicked");
        if (resetButton != null) resetButton.clicked += () => Debug.Log("[Settings] Reset Progress clicked (placeholder)");

        if (navShop != null) navShop.clicked += OnNavShop;
        if (navRate != null) navRate.clicked += () => Debug.Log("[Settings] nav: rate");
        if (navSettings != null) navSettings.clicked += () => Debug.Log("[Settings] nav: settings (already here)");
    }

    private void OnDisable()
    {
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
            Hide();
            ShopPanel.Instance.Show(returnTarget);
        }
    }

    private void SelectDifficulty(int level)
    {
        if (diffEasy != null) diffEasy.EnableInClassList("segment-btn--active", level == 0);
        if (diffNormal != null) diffNormal.EnableInClassList("segment-btn--active", level == 1);
        if (diffHard != null) diffHard.EnableInClassList("segment-btn--active", level == 2);
        Debug.Log($"[Settings] difficulty = {level}");
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
        gameObject.SetActive(false);
        if (returnTarget != null) returnTarget.SetActive(true);
    }
}
