using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 商店面板控制器（UI Toolkit）。
/// 占位实现：所有点击都打 Debug.Log，等真实数据接入后再扩展。
/// 可通过 <see cref="Show"/> / <see cref="Hide"/> 由起始菜单等其他面板触发。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance { get; private set; }

    /// <summary>关闭本面板时希望返回的目标 GameObject（一般是 StartMenu）。</summary>
    [Tooltip("返回时激活的 GameObject（一般是 StartMenu）。")]
    public GameObject returnTarget;

    private Button backButton;
    private Button tabBalls;
    private Button tabBlocks;
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
        UIPause.Acquire(); // 面板可见 → 暂停游戏

        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        backButton = root.Q<Button>("back-btn");
        tabBalls = root.Q<Button>("tab-balls");
        tabBlocks = root.Q<Button>("tab-blocks");
        navShop = root.Q<Button>("nav-shop");
        navRate = root.Q<Button>("nav-rate");
        navSettings = root.Q<Button>("nav-settings");

        if (backButton != null) backButton.clicked += OnBack;
        if (tabBalls != null) tabBalls.clicked += () => SelectTab(true);
        if (tabBlocks != null) tabBlocks.clicked += () => SelectTab(false);
        if (navShop != null) navShop.clicked += () => Debug.Log("[Shop] nav: shop");
        if (navRate != null) navRate.clicked += () => Debug.Log("[Shop] nav: rate");
        if (navSettings != null) navSettings.clicked += OnNavSettings;

        // 商品按钮：批量绑定，点击时打日志（占位）
        foreach (var card in root.Query<Button>(className: "item-card").ToList())
        {
            string id = card.name;
            card.clicked += () => Debug.Log($"[Shop] item clicked: {id}");
        }
    }

    private void OnDisable()
    {
        UIPause.Release();

        if (backButton != null) backButton.clicked -= OnBack;
        // tabBalls/tabBlocks/nav-* 用 lambda 不易精确解绑，
        // 占位实现里依赖 OnEnable 重新查询不会重复绑定（每次 Q<> 拿到的是同一个元素，
        // 但 lambda 是新实例 → 这里不主动解绑，关闭时 root 会被清掉）。
    }

    private void OnBack()
    {
        Debug.Log("[Shop] Back clicked");
        Hide();
    }

    private void OnNavSettings()
    {
        Debug.Log("[Shop] nav: settings");
        if (SettingsPanel.Instance != null)
        {
            SettingsPanel.Instance.Show(returnTarget);
            Hide();
        }
    }

    private void SelectTab(bool ballsActive)
    {
        if (tabBalls == null || tabBlocks == null) return;
        tabBalls.EnableInClassList("tab-btn--active", ballsActive);
        tabBlocks.EnableInClassList("tab-btn--active", !ballsActive);
        Debug.Log($"[Shop] Tab: {(ballsActive ? "BALLS" : "BLOCKS")}");
    }

    /// <summary>显示本面板，并记住关闭时要返回的对象。</summary>
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
