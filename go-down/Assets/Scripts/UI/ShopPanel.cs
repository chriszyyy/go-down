using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 商店面板控制器（UI Toolkit）。
/// - 顶部 stat 绑定 ScoreManager / CoinManager（最高分 / 当前分 / 金币）。
/// - 5 个 tab：BALLS / BLOCKS / ITEMS / COINS / NO ADS。
/// - ITEMS：用 CoinManager + ToolUsageInventory 实际购买；点击卡片弹出"BUY ITEM"模态。
/// - COINS / NO ADS：占位，TODO 接入 IAP。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopPanel : MonoBehaviour
{
    public enum ShopTab { Balls, Blocks, Items, Coins, NoAds }

    public static ShopPanel Instance { get; private set; }

    [Tooltip("返回时激活的 GameObject（一般是 StartMenu；游戏内打开 = null）。")]
    public GameObject returnTarget;

    // —— 顶部 stat ——
    private Label highScoreValue;
    private Label scoreValue;
    private Label coinValue;

    // —— 标题/导航 ——
    private Button backButton;
    private Button navShop;
    private Button navSettings;

    // —— Tabs ——
    private Button tabBalls;
    private Button tabBlocks;
    private Button tabItems;
    private Button tabCoins;
    private Button tabNoAds;
    private VisualElement contentBalls;
    private VisualElement contentBlocks;
    private VisualElement contentItems;
    private VisualElement contentCoins;
    private VisualElement contentNoAds;

    // —— ITEMS tab：工具卡片 ——
    private Label itemResetCount;
    private Label itemRainbowCount;

    // —— Buy modal ——
    private VisualElement buyModal;
    private VisualElement buyView;
    private VisualElement boughtView;
    private VisualElement buyIcon;
    private VisualElement boughtIcon;
    private Label buyItemName;
    private Label buyQtyValue;
    private Label buyTotal;
    private Label boughtSummary;
    private Button buyClose;
    private Button buyQtyMinus;
    private Button buyQtyPlus;
    private Button buyQtyMax;
    private Button buyConfirm;
    private Button buyOk;

    // 当前购买中的工具
    private string currentBuyToolId;       // "reset" / "rainbow"
    private int currentBuyUnitPrice;
    private int currentBuyQty = 1;

    // 进入面板时希望默认聚焦的 tab；由调用方通过 Show(returnTo, tab) 指定
    private ShopTab pendingTab = ShopTab.Balls;

    // 工具单价（与原 ShopPanelUI 保持一致）
    private const int RESET_PRICE = 100;
    private const int RAINBOW_PRICE = 50;

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
        UIPause.Acquire();

        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        QueryElements(root);
        WireButtons();
        SelectTab(pendingTab);
        RefreshStats();
        RefreshToolCounts();

        ToolUsageInventory.OnUsesChanged += RefreshToolCounts;
        if (CoinManager.Instance != null) CoinManager.Instance.OnCoinsChanged += HandleCoinsChanged;
    }

    private void OnDisable()
    {
        UIPause.Release();

        ToolUsageInventory.OnUsesChanged -= RefreshToolCounts;
        if (CoinManager.Instance != null) CoinManager.Instance.OnCoinsChanged -= HandleCoinsChanged;

        HideBuyModal();
    }

    // ---------------- Public API ----------------

    /// <summary>显示本面板。returnTo = null 表示从游戏内打开（关闭后不重新激活别的面板）。</summary>
    public void Show(GameObject returnTo = null, ShopTab tab = ShopTab.Balls)
    {
        returnTarget = returnTo;
        pendingTab = tab;

        bool wasActive = gameObject.activeSelf;
        gameObject.SetActive(true);

        // 已经 active 时 OnEnable 不会再跑，需要手动应用 tab
        if (wasActive) SelectTab(pendingTab);
    }

    public void Hide()
    {
        if (returnTarget != null) returnTarget.SetActive(true);
        gameObject.SetActive(false);
    }

    // ---------------- 元素查找 + 绑定 ----------------

    private void QueryElements(VisualElement root)
    {
        highScoreValue = root.Q<Label>("high-score-value");
        scoreValue = root.Q<Label>("score-value");
        coinValue = root.Q<Label>("coin-value");

        backButton = root.Q<Button>("back-btn");
        navShop = root.Q<Button>("nav-shop");
        navSettings = root.Q<Button>("nav-settings");

        tabBalls = root.Q<Button>("tab-balls");
        tabBlocks = root.Q<Button>("tab-blocks");
        tabItems = root.Q<Button>("tab-items");
        tabCoins = root.Q<Button>("tab-coins");
        tabNoAds = root.Q<Button>("tab-noads");

        contentBalls = root.Q<VisualElement>("content-balls");
        contentBlocks = root.Q<VisualElement>("content-blocks");
        contentItems = root.Q<VisualElement>("content-items");
        contentCoins = root.Q<VisualElement>("content-coins");
        contentNoAds = root.Q<VisualElement>("content-noads");

        itemResetCount = root.Q<Label>("item-reset-count");
        itemRainbowCount = root.Q<Label>("item-rainbow-count");

        buyModal = root.Q<VisualElement>("buy-modal");
        buyView = root.Q<VisualElement>("buy-view");
        boughtView = root.Q<VisualElement>("bought-view");
        buyIcon = root.Q<VisualElement>("buy-icon");
        boughtIcon = root.Q<VisualElement>("bought-icon");
        buyItemName = root.Q<Label>("buy-item-name");
        buyQtyValue = root.Q<Label>("buy-qty-value");
        buyTotal = root.Q<Label>("buy-total");
        boughtSummary = root.Q<Label>("bought-summary");
        buyClose = root.Q<Button>("buy-close");
        buyQtyMinus = root.Q<Button>("buy-qty-minus");
        buyQtyPlus = root.Q<Button>("buy-qty-plus");
        buyQtyMax = root.Q<Button>("buy-qty-max");
        buyConfirm = root.Q<Button>("buy-confirm");
        buyOk = root.Q<Button>("buy-ok");
    }

    private void WireButtons()
    {
        if (backButton != null) backButton.clicked += OnBack;

        if (tabBalls != null) tabBalls.clicked += () => SelectTab(ShopTab.Balls);
        if (tabBlocks != null) tabBlocks.clicked += () => SelectTab(ShopTab.Blocks);
        if (tabItems != null) tabItems.clicked += () => SelectTab(ShopTab.Items);
        if (tabCoins != null) tabCoins.clicked += () => SelectTab(ShopTab.Coins);
        if (tabNoAds != null) tabNoAds.clicked += () => SelectTab(ShopTab.NoAds);

        if (navShop != null) navShop.clicked += () => Debug.Log("[Shop] nav: shop (already here)");
        if (navSettings != null) navSettings.clicked += OnNavSettings;

        var root = GetComponent<UIDocument>().rootVisualElement;
        foreach (var card in root.Query<Button>(className: "item-card").ToList())
        {
            string id = card.name;
            if (string.IsNullOrEmpty(id)) continue;

            if (id == "item-tool-reset")
            {
                card.clicked += () => OpenBuyModal("reset");
                continue;
            }
            if (id == "item-tool-rainbow")
            {
                card.clicked += () => OpenBuyModal("rainbow");
                continue;
            }
            if (id.StartsWith("coin-pack-"))
            {
                string packId = id;
                // TODO: 接入 IAP，购买金币包
                card.clicked += () => Debug.Log($"[Shop] TODO: 接入 IAP，购买金币包 {packId}");
                continue;
            }

            // BALLS / BLOCKS 占位
            card.clicked += () => Debug.Log($"[Shop] item clicked: {id}");
        }

        // NO ADS 区域按钮 (不属于 .item-card)
        var noAdsLifetime = root.Q<Button>("noads-lifetime");
        if (noAdsLifetime != null)
        {
            // TODO: 接入 IAP，购买终身去广告
            noAdsLifetime.clicked += () => Debug.Log("[Shop] TODO: 接入 IAP，购买终身去广告");
        }
        var noAdsRestore = root.Q<Button>("noads-restore");
        if (noAdsRestore != null)
        {
            // TODO: 接入 IAP，恢复购买
            noAdsRestore.clicked += () => Debug.Log("[Shop] TODO: 接入 IAP，恢复购买");
        }

        if (buyClose != null) buyClose.clicked += HideBuyModal;
        if (buyQtyMinus != null) buyQtyMinus.clicked += () => AdjustQty(-1);
        if (buyQtyPlus != null) buyQtyPlus.clicked += () => AdjustQty(+1);
        if (buyQtyMax != null) buyQtyMax.clicked += SetQtyToMax;
        if (buyConfirm != null) buyConfirm.clicked += ConfirmPurchase;
        if (buyOk != null) buyOk.clicked += HideBuyModal;
    }

    // ---------------- 顶部 stat 数据 ----------------

    private void HandleCoinsChanged(int _)
    {
        RefreshStats();
        UpdateBuyTotal(); // 余额变化时也刷新 CONFIRM 可用状态
    }

    private void RefreshStats()
    {
        if (highScoreValue != null)
            highScoreValue.text = (ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0).ToString();
        if (scoreValue != null)
            scoreValue.text = (ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0).ToString();
        if (coinValue != null)
            coinValue.text = (CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0).ToString();
    }

    private void RefreshToolCounts()
    {
        int reset = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.ResetUses : 0;
        int rainbow = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.RainbowUses : 0;
        if (itemResetCount != null) itemResetCount.text = "x" + reset;
        if (itemRainbowCount != null) itemRainbowCount.text = "x" + rainbow;
    }

    // ---------------- Tab 切换 ----------------

    private void SelectTab(ShopTab tab)
    {
        pendingTab = tab;

        SetTabActive(tabBalls, tab == ShopTab.Balls);
        SetTabActive(tabBlocks, tab == ShopTab.Blocks);
        SetTabActive(tabItems, tab == ShopTab.Items);
        SetTabActive(tabCoins, tab == ShopTab.Coins);
        SetTabActive(tabNoAds, tab == ShopTab.NoAds);

        SetContentVisible(contentBalls, tab == ShopTab.Balls);
        SetContentVisible(contentBlocks, tab == ShopTab.Blocks);
        SetContentVisible(contentItems, tab == ShopTab.Items);
        SetContentVisible(contentCoins, tab == ShopTab.Coins);
        SetContentVisible(contentNoAds, tab == ShopTab.NoAds);
    }

    private static void SetTabActive(Button btn, bool active)
    {
        if (btn == null) return;
        btn.EnableInClassList("tab-btn--active", active);
    }

    private static void SetContentVisible(VisualElement el, bool visible)
    {
        if (el == null) return;
        el.EnableInClassList("tab-content--hidden", !visible);
        el.EnableInClassList("tab-content--active", visible);
    }

    // ---------------- Buy Modal ----------------

    private void OpenBuyModal(string toolId)
    {
        currentBuyToolId = toolId;
        currentBuyUnitPrice = (toolId == "reset") ? RESET_PRICE : RAINBOW_PRICE;
        currentBuyQty = 1;

        if (buyItemName != null) buyItemName.text = toolId == "reset" ? "RESET BALL" : "RANDOM BLOCK";

        SwapIconClass(buyIcon, toolId);
        SwapIconClass(boughtIcon, toolId);

        if (buyView != null) buyView.RemoveFromClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");

        if (buyModal != null) buyModal.RemoveFromClassList("buy-modal--hidden");

        UpdateBuyTotal();
    }

    private void HideBuyModal()
    {
        if (buyModal != null) buyModal.AddToClassList("buy-modal--hidden");
    }

    private void AdjustQty(int delta)
    {
        currentBuyQty = Mathf.Max(1, Mathf.Min(99, currentBuyQty + delta));
        UpdateBuyTotal();
    }

    private void SetQtyToMax()
    {
        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        int unit = Mathf.Max(1, currentBuyUnitPrice);
        currentBuyQty = Mathf.Clamp(coins / unit, 1, 99);
        UpdateBuyTotal();
    }

    private void UpdateBuyTotal()
    {
        if (buyQtyValue != null) buyQtyValue.text = "x" + currentBuyQty;
        if (buyTotal != null) buyTotal.text = (currentBuyUnitPrice * currentBuyQty).ToString();

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        bool canBuy = coins >= currentBuyUnitPrice * currentBuyQty;
        if (buyConfirm != null) buyConfirm.SetEnabled(canBuy);
    }

    private void ConfirmPurchase()
    {
        int total = currentBuyUnitPrice * currentBuyQty;
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(total))
        {
            Debug.Log("[Shop] 金币不足，购买失败");
            return;
        }

        if (ToolUsageInventory.Instance != null)
        {
            if (currentBuyToolId == "reset") ToolUsageInventory.Instance.AddResetUses(currentBuyQty);
            else ToolUsageInventory.Instance.AddRainbowUses(currentBuyQty);
        }

        int newCount = currentBuyToolId == "reset"
            ? (ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.ResetUses : 0)
            : (ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.RainbowUses : 0);

        if (boughtSummary != null) boughtSummary.text = $"You now have {newCount}";
        if (buyView != null) buyView.AddToClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.RemoveFromClassList("buy-modal__view--hidden");
    }

    private static readonly string[] s_iconClasses = new[]
    {
        "item-thumb--tool-reset",
        "item-thumb--tool-rainbow",
    };

    private static void SwapIconClass(VisualElement icon, string toolId)
    {
        if (icon == null) return;
        foreach (var c in s_iconClasses) icon.RemoveFromClassList(c);
        icon.AddToClassList(toolId == "reset" ? "item-thumb--tool-reset" : "item-thumb--tool-rainbow");
    }

    // ---------------- Back / Nav ----------------

    private void OnBack()
    {
        Hide();
    }

    private void OnNavSettings()
    {
        var panel = SettingsPanel.Instance ?? FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (panel == null) return;

        panel.Show(returnTarget);
        gameObject.SetActive(false);
    }
}
