using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 起始菜单（UI Toolkit 版本）。
/// 挂在带 <see cref="UIDocument"/> 的 GameObject 上，
/// 通过 Q&lt;T&gt;("name") 找到 UXML 中的元素并绑定回调。
/// 顶部的最高分 / 钻石 / 金币会从现有 Manager 同步。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StartMenuPanel : MonoBehaviour
{
    // ---- 底部三个主按钮 ----
    private Button playButton;
    private Button shopButton;
    private Button optionsButton;

    // ---- 顶部 +号 ----
    private Button addGemsButton;
    private Button addCoinsButton;

    // ---- 侧边栏 ----
    private Button settingSideButton;
    private Button rankingButton;
    private Button dailyButton;
    private Button freeGiftButton;
    private Button shopSideButton;

    // ---- 顶部数值 Label ----
    private Label highScoreValue;
    private Label gemValue;
    private Label coinValue;

    private CoinManager subscribedCoinManager;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        // 底部主按钮
        playButton = root.Q<Button>("play-btn");
        shopButton = root.Q<Button>("shop-btn");
        optionsButton = root.Q<Button>("options-btn");

        // 顶部
        addGemsButton = root.Q<Button>("add-gems-btn");
        addCoinsButton = root.Q<Button>("add-coins-btn");
        highScoreValue = root.Q<Label>("high-score-value");
        gemValue = root.Q<Label>("gem-value");
        coinValue = root.Q<Label>("coin-value");

        // 侧边
        settingSideButton = root.Q<Button>("setting-side-btn");
        rankingButton = root.Q<Button>("ranking-btn");
        dailyButton = root.Q<Button>("daily-btn");
        freeGiftButton = root.Q<Button>("free-gift-btn");
        shopSideButton = root.Q<Button>("shop-side-btn");

        if (playButton != null) playButton.clicked += OnPlay;
        if (shopButton != null) shopButton.clicked += OnShop;
        if (optionsButton != null) optionsButton.clicked += OnOptions;
        if (addGemsButton != null) addGemsButton.clicked += OnAddGems;
        if (addCoinsButton != null) addCoinsButton.clicked += OnAddCoins;
        if (settingSideButton != null) settingSideButton.clicked += OnOptions;
        if (rankingButton != null) rankingButton.clicked += OnRanking;
        if (dailyButton != null) dailyButton.clicked += OnDaily;
        if (freeGiftButton != null) freeGiftButton.clicked += OnFreeGift;
        if (shopSideButton != null) shopSideButton.clicked += OnShop;

        // CoinManager.OnCoinsChanged 是实例事件，需要拿到单例实例后再订阅
        if (CoinManager.Instance != null)
        {
            subscribedCoinManager = CoinManager.Instance;
            subscribedCoinManager.OnCoinsChanged += HandleCoinsChanged;
        }

        RefreshStats();
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= OnPlay;
        if (shopButton != null) shopButton.clicked -= OnShop;
        if (optionsButton != null) optionsButton.clicked -= OnOptions;
        if (addGemsButton != null) addGemsButton.clicked -= OnAddGems;
        if (addCoinsButton != null) addCoinsButton.clicked -= OnAddCoins;
        if (settingSideButton != null) settingSideButton.clicked -= OnOptions;
        if (rankingButton != null) rankingButton.clicked -= OnRanking;
        if (dailyButton != null) dailyButton.clicked -= OnDaily;
        if (freeGiftButton != null) freeGiftButton.clicked -= OnFreeGift;
        if (shopSideButton != null) shopSideButton.clicked -= OnShop;

        if (subscribedCoinManager != null)
        {
            subscribedCoinManager.OnCoinsChanged -= HandleCoinsChanged;
            subscribedCoinManager = null;
        }
    }

    /// <summary>从现有 Manager 拉取数值刷新顶部状态栏。</summary>
    private void RefreshStats()
    {
        if (highScoreValue != null)
        {
            int high = PlayerPrefs.GetInt("HighScore", 0);
            highScoreValue.text = high.ToString();
        }

        if (coinValue != null && CoinManager.Instance != null)
        {
            coinValue.text = CoinManager.Instance.CurrentCoins.ToString();
        }

        if (gemValue != null)
        {
            // 当前没有钻石系统，先固定为 0；接入后改这里。
            gemValue.text = PlayerPrefs.GetInt("Gems", 0).ToString();
        }
    }

    private void HandleCoinsChanged(int newAmount)
    {
        if (coinValue != null) coinValue.text = newAmount.ToString();
    }

    // ---------------- 点击处理 ----------------
    private void OnPlay()
    {
        Debug.Log("[StartMenu] Play clicked");
        // 隐藏菜单 → 进入游戏
        gameObject.SetActive(false);
    }

    private void OnShop() => Debug.Log("[StartMenu] Shop clicked");
    private void OnOptions() => Debug.Log("[StartMenu] Settings/Options clicked");
    private void OnAddGems() => Debug.Log("[StartMenu] + Gems clicked");
    private void OnAddCoins() => Debug.Log("[StartMenu] + Coins clicked");
    private void OnRanking() => Debug.Log("[StartMenu] Ranking clicked");
    private void OnDaily() => Debug.Log("[StartMenu] Daily clicked");
    private void OnFreeGift() => Debug.Log("[StartMenu] Free Gift clicked");
}
