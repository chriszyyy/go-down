using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 商店面板控制器（UI Toolkit）。
/// - 顶部 stat 绑定 ScoreManager / CoinManager（最高分 / 当前分 / 金币）。
/// - 4 个 tab：BALLS / ITEMS / COINS / NO ADS。
/// - ITEMS：用 CoinManager + ToolUsageInventory 实际购买；点击卡片弹出"BUY ITEM"模态。
/// - COINS / NO ADS：占位，TODO 接入 IAP。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopPanel : MonoBehaviour
{
    public enum ShopTab { Balls, Items, Coins, NoAds }

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
    private Button tabItems;
    private Button tabCoins;
    private Button tabNoAds;
    private VisualElement contentBalls;
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

    // —— Skin (hex) ——
    private VisualElement skinView;
    private VisualElement skinIcon;
    private Label skinPrompt;
    private Button skinClose;
    private Button skinCancel;
    private Button skinConfirm;
    private readonly Dictionary<string, Button> hexSkinCards = new Dictionary<string, Button>();
    private string currentSkinId; // 当前在 skin-view 中待解锁的 skin id

    // —— IAP (coin pack) ——
    private VisualElement iapView;
    private VisualElement iapIcon;
    private Label iapTitle;
    private Label iapPrompt;
    private Label iapPrice;
    private Button iapClose;
    private Button iapCancel;
    private Button iapConfirm;
    private int currentIapCoins; // 当前 IAP 待发放金币数（仅用于「购买成功」文案展示）
    private string currentIapProductId; // 当前弹窗对应的 IAPService 产品 ID

    // 金币包定义：UI 卡片 id 后缀 → (coin amount, 默认价格文案, IAP 产品 ID)
    // 价格文案仅在 IAPService 未就绪时作为占位；就绪后用 GetLocalizedPrice() 覆盖
    private static readonly Dictionary<string, (int coins, string price, string productId)> s_coinPacks =
        new Dictionary<string, (int, string, string)>
    {
        { "coin-pack-100",  (100,  "$0.99", IAPService.PRODUCT_COIN_100) },
        { "coin-pack-500",  (500,  "$2.99", IAPService.PRODUCT_COIN_500) },
        { "coin-pack-1200", (1200, "$5.99", IAPService.PRODUCT_COIN_1200) },
        { "coin-pack-2500", (2500, "$9.99", IAPService.PRODUCT_COIN_2500) },
    };

    // 终身去广告默认价格文案（IAPService 就绪后用商店真实价格覆盖）
    private const string NO_ADS_DEFAULT_PRICE = "$2.99";

    // 当前购买中的工具
    private string currentBuyToolId;       // "reset" / "rainbow"
    private int currentBuyUnitPrice;
    private int currentBuyQty = 1;

    // 进入面板时希望默认聚焦的 tab；由调用方通过 Show(returnTo, tab) 指定
    private ShopTab pendingTab = ShopTab.Items;

    // 工具单价（与原 ShopPanelUI 保持一致）
    private const int RESET_PRICE = 50;
    private const int RAINBOW_PRICE = 35;

    // —— Watch Ad (free coins) ——
    private const int WATCH_AD_REWARD = 100;
    private const int WATCH_AD_COOLDOWN_SECONDS = 12 * 60 * 60; // 12 小时
    private const string KEY_WATCH_AD_LAST = "WatchAd_LastUtc";

    private Button watchAdBtn;
    private VisualElement watchAdBtnWrap;
    private Label watchAdBadge;
    private float watchAdTickAccumulator;

    // —— No Ads (lifetime purchase) ——
    private const string KEY_NO_ADS_REMOVED = "NoAds_Removed";

    private VisualElement noadsLifetimeCard;
    private VisualElement noadsActiveCard;

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
        HexagonSkinManager.OnChanged += RefreshSkinCards;
        AdsService.RewardedStateChanged += RefreshWatchAdState;
        IAPService.PurchaseCompleted += HandleIapCompleted;
        IAPService.PurchaseFailed += HandleIapFailed;
        IAPService.InitializeStateChanged += HandleIapInitialized;
        RefreshSkinCards();
        RefreshWatchAdState();
        RefreshNoAdsState();
        RefreshIapPrices();
    }

    private void Update()
    {
        if (watchAdBtn == null) return;
        // 冷却中每秒刷新一次倒计时文字
        watchAdTickAccumulator += Time.unscaledDeltaTime;
        if (watchAdTickAccumulator >= 1f)
        {
            watchAdTickAccumulator = 0f;
            RefreshWatchAdState();
        }
    }

    private void OnDisable()
    {
        UIPause.Release();

        ToolUsageInventory.OnUsesChanged -= RefreshToolCounts;
        if (CoinManager.Instance != null) CoinManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        HexagonSkinManager.OnChanged -= RefreshSkinCards;
        AdsService.RewardedStateChanged -= RefreshWatchAdState;
        IAPService.PurchaseCompleted -= HandleIapCompleted;
        IAPService.PurchaseFailed -= HandleIapFailed;
        IAPService.InitializeStateChanged -= HandleIapInitialized;

        HideBuyModal();
    }

    // ---------------- Public API ----------------

    /// <summary>显示本面板。returnTo = null 表示从游戏内打开（关闭后不重新激活别的面板）。</summary>
    public void Show(GameObject returnTo = null, ShopTab tab = ShopTab.Items)
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
        tabItems = root.Q<Button>("tab-items");
        tabCoins = root.Q<Button>("tab-coins");
        tabNoAds = root.Q<Button>("tab-noads");

        contentBalls = root.Q<VisualElement>("content-balls");
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

        skinView = root.Q<VisualElement>("skin-view");
        skinIcon = root.Q<VisualElement>("skin-icon");
        skinPrompt = root.Q<Label>("skin-prompt");
        skinClose = root.Q<Button>("skin-close");
        skinCancel = root.Q<Button>("skin-cancel");
        skinConfirm = root.Q<Button>("skin-confirm");

        iapView = root.Q<VisualElement>("iap-view");
        iapIcon = root.Q<VisualElement>("iap-icon");
        iapTitle = root.Q<Label>("iap-title");
        iapPrompt = root.Q<Label>("iap-prompt");
        iapPrice = root.Q<Label>("iap-price");
        iapClose = root.Q<Button>("iap-close");
        iapCancel = root.Q<Button>("iap-cancel");
        iapConfirm = root.Q<Button>("iap-confirm");

        watchAdBtn = root.Q<Button>("watch-ad-btn");
        watchAdBtnWrap = root.Q<VisualElement>("watch-ad-btn-wrap");
        watchAdBadge = watchAdBtnWrap != null ? watchAdBtnWrap.Q<Label>(null, "watch-ad-btn__badge") : null;

        noadsLifetimeCard = root.Q<VisualElement>("noads-lifetime-card");
        noadsActiveCard = root.Q<VisualElement>("noads-active-card");
    }

    private void WireButtons()
    {
        if (backButton != null) backButton.clicked += OnBack;

        if (tabBalls != null) tabBalls.clicked += () => SelectTab(ShopTab.Balls);
        if (tabItems != null) tabItems.clicked += () => SelectTab(ShopTab.Items);
        if (tabCoins != null) tabCoins.clicked += () => SelectTab(ShopTab.Coins);
        if (tabNoAds != null) tabNoAds.clicked += () => SelectTab(ShopTab.NoAds);

        if (navShop != null) navShop.clicked += () => Debug.Log("[Shop] nav: shop (already here)");
        if (navSettings != null) navSettings.clicked += OnNavSettings;

        var root = GetComponent<UIDocument>().rootVisualElement;
        hexSkinCards.Clear();
        foreach (var card in root.Query<Button>(className: "item-card").ToList())
        {
            string id = card.name;
            if (string.IsNullOrEmpty(id)) continue;

            if (id.StartsWith("item-hex-"))
            {
                string skinId = id.Substring("item-hex-".Length);
                hexSkinCards[skinId] = card;
                card.clicked += () => OnHexSkinCardClicked(skinId);
                continue;
            }

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
                card.clicked += () => OpenIapModal(packId);
                continue;
            }

            // BALLS 占位
            card.clicked += () => Debug.Log($"[Shop] item clicked: {id}");
        }

        // NO ADS 区域按钮 (不属于 .item-card)
        var noAdsLifetime = root.Q<Button>("noads-lifetime");
        if (noAdsLifetime != null)
        {
            noAdsLifetime.clicked += () => OpenAdsModal(NO_ADS_DEFAULT_PRICE);
        }
        var noAdsRestore = root.Q<Button>("noads-restore");
        if (noAdsRestore != null)
        {
            noAdsRestore.clicked += () =>
            {
                Debug.Log("[Shop] Restore purchases requested");
                if (IAPService.Instance != null) IAPService.Instance.RestorePurchases();
            };
        }

        if (buyClose != null) buyClose.clicked += HideBuyModal;
        if (buyQtyMinus != null) buyQtyMinus.clicked += () => AdjustQty(-1);
        if (buyQtyPlus != null) buyQtyPlus.clicked += () => AdjustQty(+1);
        if (buyQtyMax != null) buyQtyMax.clicked += SetQtyToMax;
        if (buyConfirm != null) buyConfirm.clicked += ConfirmPurchase;
        if (buyOk != null) buyOk.clicked += HideBuyModal;

        if (skinClose != null) skinClose.clicked += HideBuyModal;
        if (skinCancel != null) skinCancel.clicked += HideBuyModal;
        if (skinConfirm != null) skinConfirm.clicked += ConfirmSkinPurchase;

        if (iapClose != null) iapClose.clicked += HideBuyModal;
        if (iapCancel != null) iapCancel.clicked += HideBuyModal;
        if (iapConfirm != null) iapConfirm.clicked += ConfirmIapPurchase;

        if (watchAdBtn != null) watchAdBtn.clicked += OnWatchAdClicked;
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
        SetTabActive(tabItems, tab == ShopTab.Items);
        SetTabActive(tabCoins, tab == ShopTab.Coins);
        SetTabActive(tabNoAds, tab == ShopTab.NoAds);

        SetContentVisible(contentBalls, tab == ShopTab.Balls);
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

        if (buyItemName != null) buyItemName.text = toolId == "reset" ? "RESET BALL" : "RAINBOW BLOCK";

        SwapIconClass(buyIcon, toolId);
        SwapIconClass(boughtIcon, toolId);

        if (buyView != null) buyView.RemoveFromClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");
        if (skinView != null) skinView.AddToClassList("buy-modal__view--hidden");
        if (iapView != null) iapView.AddToClassList("buy-modal__view--hidden");

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

    private static readonly string[] s_coinPackIconClasses = new[]
    {
        "item-thumb--coin-pack-100",
        "item-thumb--coin-pack-500",
        "item-thumb--coin-pack-1200",
        "item-thumb--coin-pack-2500",
    };

    private static void SwapIconClass(VisualElement icon, string toolId)
    {
        if (icon == null) return;
        foreach (var c in s_iconClasses) icon.RemoveFromClassList(c);
        icon.AddToClassList(toolId == "reset" ? "item-thumb--tool-reset" : "item-thumb--tool-rainbow");
    }

    // ---------------- Skin (hex) ----------------

    private void OnHexSkinCardClicked(string skinId)
    {
        var mgr = HexagonSkinManager.Instance;
        if (mgr == null) return;

        if (mgr.IsUnlocked(skinId))
        {
            mgr.TrySelect(skinId);
        }
        else
        {
            OpenSkinModal(skinId);
        }
    }

    private void OpenSkinModal(string skinId)
    {
        currentSkinId = skinId;

        SwapHexIconClass(skinIcon, skinId);
        int price = HexagonSkinManager.GetUnlockPrice(skinId);
        if (skinPrompt != null)
            skinPrompt.text = $"Unlock this hexagon skin for {price} coins?";

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        if (skinConfirm != null) skinConfirm.SetEnabled(coins >= price);

        if (buyView != null) buyView.AddToClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");
        if (iapView != null) iapView.AddToClassList("buy-modal__view--hidden");
        if (skinView != null) skinView.RemoveFromClassList("buy-modal__view--hidden");

        if (buyModal != null) buyModal.RemoveFromClassList("buy-modal--hidden");
    }

    private void ConfirmSkinPurchase()
    {
        if (string.IsNullOrEmpty(currentSkinId)) return;
        int price = HexagonSkinManager.GetUnlockPrice(currentSkinId);
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(price))
        {
            Debug.Log("[Shop] 金币不足，皮肤解锁失败");
            return;
        }

        var mgr = HexagonSkinManager.Instance;
        if (mgr != null)
        {
            mgr.Unlock(currentSkinId);
            mgr.TrySelect(currentSkinId);
        }

        HideBuyModal();
    }

    private void RefreshSkinCards()
    {
        var mgr = HexagonSkinManager.Instance;
        if (mgr == null) return;

        foreach (var kvp in hexSkinCards)
        {
            string skinId = kvp.Key;
            var card = kvp.Value;
            if (card == null) continue;

            bool unlocked = mgr.IsUnlocked(skinId);
            bool selected = unlocked && mgr.SelectedSkinId == skinId;

            card.EnableInClassList("item-card--selected", selected);
            card.EnableInClassList("item-card--locked", !unlocked);

            // 直接控制内部元素显示（USS 同名规则有重复，cascade 不稳定，改用 inline style 保证生效）
            var name = card.Q<Label>(null, "item-card__name");
            var price = card.Q<VisualElement>(null, "item-card__price");
            var check = card.Q<VisualElement>(null, "item-card__check");
            if (name != null) name.style.display = unlocked ? DisplayStyle.Flex : DisplayStyle.None;
            if (price != null) price.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
            if (check != null) check.style.display = selected ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private static readonly string[] s_hexIconClasses = new[]
    {
        "item-thumb--hex-gold",
        "item-thumb--hex-blue",
        "item-thumb--hex-green",
        "item-thumb--hex-purple",
        "item-thumb--hex-red",
    };

    private static void SwapHexIconClass(VisualElement icon, string skinId)
    {
        if (icon == null) return;
        foreach (var c in s_hexIconClasses) icon.RemoveFromClassList(c);
        icon.AddToClassList("item-thumb--hex-" + skinId);
    }

    // ---------------- IAP (coin packs) ----------------

    private void OpenIapModal(string packId)
    {
        if (!s_coinPacks.TryGetValue(packId, out var pack))
        {
            Debug.LogWarning($"[Shop] 未知 coin pack: {packId}");
            return;
        }

        currentIapCoins = pack.coins;
        currentIapProductId = pack.productId;
        if (iapTitle != null) iapTitle.text = $"BUY {pack.coins} COINS";
        if (iapPrompt != null) iapPrompt.text = $"Purchase {pack.coins} coins?";
        // 商店就绪时优先用本地化价格；未就绪时退回硬编码占位
        string priceText = IAPService.Instance != null
            ? IAPService.Instance.GetLocalizedPrice(pack.productId, pack.price)
            : pack.price;
        if (iapPrice != null) iapPrice.text = priceText;

        // 切换金币包图标
        if (iapIcon != null)
        {
            foreach (var c in s_coinPackIconClasses) iapIcon.RemoveFromClassList(c);
            iapIcon.RemoveFromClassList("noads-feature__icon--shield");
            iapIcon.AddToClassList("item-thumb--" + packId); // packId 形如 "coin-pack-100"
        }

        if (buyView != null) buyView.AddToClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");
        if (skinView != null) skinView.AddToClassList("buy-modal__view--hidden");
        if (iapView != null) iapView.RemoveFromClassList("buy-modal__view--hidden");

        if (buyModal != null) buyModal.RemoveFromClassList("buy-modal--hidden");
    }

    /// <summary>
    /// 复用 IAP 弹窗确认 “终身去广告” 购买。
    /// </summary>
    private void OpenAdsModal(string priceText)
    {
        // 已购买 = 不再弹窗
        if (IsNoAdsRemoved()) return;

        currentIapCoins = 0;
        currentIapProductId = IAPService.PRODUCT_NO_ADS;
        if (iapTitle != null) iapTitle.text = "REMOVE ADS";
        if (iapPrompt != null) iapPrompt.text = "Remove all ads forever?";
        // 商店就绪时用真实价格；未就绪退回调用方传入的占位
        string finalPrice = IAPService.Instance != null
            ? IAPService.Instance.GetLocalizedPrice(IAPService.PRODUCT_NO_ADS, priceText)
            : priceText;
        if (iapPrice != null) iapPrice.text = finalPrice;

        // 切到盾牌图标
        if (iapIcon != null)
        {
            foreach (var c in s_coinPackIconClasses) iapIcon.RemoveFromClassList(c);
            iapIcon.AddToClassList("noads-feature__icon--shield");
        }

        if (buyView != null) buyView.AddToClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");
        if (skinView != null) skinView.AddToClassList("buy-modal__view--hidden");
        if (iapView != null) iapView.RemoveFromClassList("buy-modal__view--hidden");

        if (buyModal != null) buyModal.RemoveFromClassList("buy-modal--hidden");
    }

    private void ConfirmIapPurchase()
    {
        if (string.IsNullOrEmpty(currentIapProductId))
        {
            Debug.LogWarning("[Shop] ConfirmIapPurchase 无 product id");
            HideBuyModal();
            return;
        }

        // 防止重复点击：禁用按钮，等 PurchaseCompleted / PurchaseFailed 回来再处理
        if (iapConfirm != null) iapConfirm.SetEnabled(false);

        if (IAPService.Instance == null)
        {
            // 兜底：理论上 RuntimeInitializeOnLoadMethod 一定会创建实例
            Debug.LogWarning("[Shop] IAPService 缺失");
            HideBuyModal();
            return;
        }

        Debug.Log($"[Shop] Buying product: {currentIapProductId}");
        IAPService.Instance.BuyProduct(currentIapProductId);
    }

    private void HandleIapCompleted(string productId)
    {
        Debug.Log($"[Shop] IAP completed: {productId}");

        // 商品已发放（金币 / 去广告 entitlement）都在 IAPService 内部完成，UI 只负责反馈
        RefreshStats();
        RefreshNoAdsState();

        // 只关心当前弹窗对应的产品；其它产品（例如 RestorePurchases 回放的）静默刷新即可
        if (productId == currentIapProductId)
        {
            if (iapConfirm != null) iapConfirm.SetEnabled(true);
            HideBuyModal();
        }
    }

    private void HandleIapFailed(string productId, string reason)
    {
        Debug.LogWarning($"[Shop] IAP failed: {productId}, reason={reason}");
        if (productId == currentIapProductId)
        {
            if (iapConfirm != null) iapConfirm.SetEnabled(true);
            HideBuyModal();
        }
    }

    private void HandleIapInitialized()
    {
        // 商店就绪后回填非消耗品 entitlement，刷新 NoAds 卡片
        RefreshNoAdsState();
        // 商店就绪后用本地化价格覆盖卡片上的占位价（玩家看本地货币）
        RefreshIapPrices();
    }

    /// <summary>
    /// 用商店本地化价格刷新网格卡片上的价格文案（金币包 + 终身去广告）。
    /// 商店未就绪时退回各自的硬编码占位价。
    /// </summary>
    private void RefreshIapPrices()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc != null ? doc.rootVisualElement : null;
        if (root == null) return;

        foreach (var kvp in s_coinPacks)
        {
            var card = root.Q<Button>(kvp.Key);
            if (card == null) continue;
            var priceLabel = card.Q<Label>(null, "item-card__price-text");
            if (priceLabel == null) continue;
            priceLabel.text = IAPService.Instance != null
                ? IAPService.Instance.GetLocalizedPrice(kvp.Value.productId, kvp.Value.price)
                : kvp.Value.price;
        }

        var noAdsBtn = root.Q<Button>("noads-lifetime");
        if (noAdsBtn != null)
        {
            noAdsBtn.text = IAPService.Instance != null
                ? IAPService.Instance.GetLocalizedPrice(IAPService.PRODUCT_NO_ADS, NO_ADS_DEFAULT_PRICE)
                : NO_ADS_DEFAULT_PRICE;
        }
    }

    // ---------------- No Ads (state toggle) ----------------

    public static bool IsNoAdsRemoved()
    {
        return PlayerPrefs.GetInt(KEY_NO_ADS_REMOVED, 0) == 1;
    }

    public static void SetNoAdsRemoved(bool removed)
    {
        PlayerPrefs.SetInt(KEY_NO_ADS_REMOVED, removed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void RefreshNoAdsState()
    {
        bool active = IsNoAdsRemoved();

        if (noadsLifetimeCard != null)
            noadsLifetimeCard.style.display = active ? DisplayStyle.None : DisplayStyle.Flex;
        if (noadsActiveCard != null)
            noadsActiveCard.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------------- Watch Ad (free coins) ----------------

    private static long NowUtcSeconds()
    {
        return (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
    }

    private static long GetWatchAdLastUtc()
    {
        string s = PlayerPrefs.GetString(KEY_WATCH_AD_LAST, "0");
        return long.TryParse(s, out var v) ? v : 0L;
    }

    private static void SetWatchAdLastUtc(long utc)
    {
        PlayerPrefs.SetString(KEY_WATCH_AD_LAST, utc.ToString());
        PlayerPrefs.Save();
    }

    private void OnWatchAdClicked()
    {
        int remaining = GetWatchAdRemainingSeconds();
        if (remaining > 0) return; // 冷却中点击应被 disabled 阻挡，这里二次保护

        if (AdsService.Instance == null)
        {
            Debug.LogWarning("[Shop] AdsService missing; falling back to dev reward");
            GrantWatchAdReward();
            return;
        }

        if (!AdsService.Instance.IsRewardedReady && AdsService.Instance.IsRewardedLoading)
        {
            Debug.Log("[Shop] Watch ad clicked before rewarded is ready");
            RefreshWatchAdState();
            return;
        }

        AdsService.Instance.ShowRewarded(
            onReward: GrantWatchAdReward,
            onFail: () =>
            {
                Debug.Log("[Shop] Watch ad skipped / failed — cooldown not consumed");
                RefreshWatchAdState();
            });
    }

    private void GrantWatchAdReward()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(WATCH_AD_REWARD);
            Debug.Log($"[Shop] Watch ad reward granted: +{WATCH_AD_REWARD} coins");
        }
        SetWatchAdLastUtc(NowUtcSeconds());
        RefreshStats();
        RefreshWatchAdState();
    }

    private static int GetWatchAdRemainingSeconds()
    {
        long last = GetWatchAdLastUtc();
        if (last <= 0) return 0;
        long elapsed = NowUtcSeconds() - last;
        if (elapsed >= WATCH_AD_COOLDOWN_SECONDS) return 0;
        return (int)(WATCH_AD_COOLDOWN_SECONDS - elapsed);
    }

    private void RefreshWatchAdState()
    {
        if (watchAdBtn == null) return;
        int remaining = GetWatchAdRemainingSeconds();
        bool cooling = remaining > 0;
        bool adReady = AdsService.Instance == null || AdsService.Instance.IsRewardedReady;
        bool adLoading = AdsService.Instance != null && AdsService.Instance.IsRewardedLoading;
        bool canWatch = !cooling && (adReady || !adLoading);

        watchAdBtn.SetEnabled(canWatch);
        watchAdBtn.EnableInClassList("watch-ad-btn--cooldown", cooling);
        if (watchAdBtnWrap != null)
            watchAdBtnWrap.EnableInClassList("watch-ad-btn-wrap--cooldown", cooling);
        if (watchAdBadge != null)
            watchAdBadge.style.display = (cooling || !adReady) ? DisplayStyle.None : DisplayStyle.Flex;

        if (cooling)
        {
            int h = remaining / 3600;
            int m = (remaining % 3600) / 60;
            int s = remaining % 60;
            // 12 小时内冷却，最多两位时位
            watchAdBtn.text = h > 0
                ? string.Format("{0:D2}:{1:D2}:{2:D2}", h, m, s)
                : string.Format("{0:D2}:{1:D2}", m, s);
        }
        else if (adLoading)
        {
            watchAdBtn.text = "LOADING";
        }
        else if (!adReady)
        {
            watchAdBtn.text = "RETRY";
        }
        else
        {
            watchAdBtn.text = "FREE";
        }
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
