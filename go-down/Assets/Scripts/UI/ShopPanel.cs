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
    private int currentIapCoins; // 当前 IAP 待发放金币数
    private bool currentIapIsAds; // 当前 IAP 是否为“去广告”购买（true 时不发币）

    // 金币包定义：id 后缀 → (coin amount, price text)
    private static readonly Dictionary<string, (int coins, string price)> s_coinPacks = new Dictionary<string, (int, string)>
    {
        { "coin-pack-100",  (100,  "$0.99") },
        { "coin-pack-500",  (500,  "$2.99") },
        { "coin-pack-1200", (1200, "$5.99") },
        { "coin-pack-2500", (2500, "$9.99") },
    };

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
        RefreshSkinCards();
        RefreshWatchAdState();
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
            noAdsLifetime.clicked += () => OpenAdsModal("$2.99");
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
        if (skinPrompt != null)
            skinPrompt.text = $"Unlock this hexagon skin for {HexagonSkinManager.UNLOCK_PRICE} coins?";

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        if (skinConfirm != null) skinConfirm.SetEnabled(coins >= HexagonSkinManager.UNLOCK_PRICE);

        if (buyView != null) buyView.AddToClassList("buy-modal__view--hidden");
        if (boughtView != null) boughtView.AddToClassList("buy-modal__view--hidden");
        if (iapView != null) iapView.AddToClassList("buy-modal__view--hidden");
        if (skinView != null) skinView.RemoveFromClassList("buy-modal__view--hidden");

        if (buyModal != null) buyModal.RemoveFromClassList("buy-modal--hidden");
    }

    private void ConfirmSkinPurchase()
    {
        if (string.IsNullOrEmpty(currentSkinId)) return;
        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpendCoins(HexagonSkinManager.UNLOCK_PRICE))
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
        currentIapIsAds = false;
        if (iapTitle != null) iapTitle.text = $"BUY {pack.coins} COINS";
        if (iapPrompt != null) iapPrompt.text = $"Purchase {pack.coins} coins?";
        if (iapPrice != null) iapPrice.text = pack.price;

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
        currentIapCoins = 0;
        currentIapIsAds = true;
        if (iapTitle != null) iapTitle.text = "REMOVE ADS";
        if (iapPrompt != null) iapPrompt.text = "Remove all ads forever?";
        if (iapPrice != null) iapPrice.text = priceText;

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
        // TODO: 接入真实 IAP（Google Play / App Store）。
        // 本地开发模式：默认购买成功，直接发金币 / 标记去广告，方便测试其它流程。
        if (currentIapIsAds)
        {
            // TODO: 标记 PlayerPrefs / 关闭广告 SDK。
            Debug.Log("[Shop] (Dev) Ads removal purchase succeeded");
        }
        else if (currentIapCoins > 0 && CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(currentIapCoins);
            Debug.Log($"[Shop] (Dev) IAP succeeded: +{currentIapCoins} coins");
        }

        HideBuyModal();
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

        // TODO: 接入广告 SDK；本地开发模式：直接奖励金币 + 启动冷却
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(WATCH_AD_REWARD);
            Debug.Log($"[Shop] (Dev) Watch ad reward granted: +{WATCH_AD_REWARD} coins");
        }
        SetWatchAdLastUtc(NowUtcSeconds());
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

        watchAdBtn.SetEnabled(!cooling);
        watchAdBtn.EnableInClassList("watch-ad-btn--cooldown", cooling);
        if (watchAdBtnWrap != null)
            watchAdBtnWrap.EnableInClassList("watch-ad-btn-wrap--cooldown", cooling);
        if (watchAdBadge != null)
            watchAdBadge.style.display = cooling ? DisplayStyle.None : DisplayStyle.Flex;

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
