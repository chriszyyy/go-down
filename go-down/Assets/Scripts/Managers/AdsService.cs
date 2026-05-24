using System;
using UnityEngine;

#if ADMOB_ENABLED
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMob 广告服务。
///
/// 接入步骤（未完成前 ADMOB_ENABLED 未定义，本服务以 Dev 模式工作 —— 广告调用直接返回成功）：
///   1. 导入 Google Mobile Ads Unity Plugin
///      https://github.com/googleads/googleads-mobile-unity/releases
///   2. Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
///      添加  ADMOB_ENABLED
///   3. 在下方常量里填入真实 App ID 和 Ad Unit ID（先用 TEST_* 跑通流程）
///   4. AdMob console 设置 App ID + Ad Units（Rewarded, Interstitial 各一组 Android/iOS）
///   5. iOS 发版前 verify Info.plist 含 SKAdNetworkIdentifier 列表 + ATT 描述
///
/// 当前实现：
/// - Rewarded：点击「WATCH AD」→ 看广告 → 回调发放金币
/// - Interstitial：监听 GameStateManager.OnGameOver；按频率策略弹窗
/// - 已购买 NoAds（PlayerPrefs key = "NoAds_Removed"）时跳过所有插屏（rewarded 不受影响，玩家主动看的）
/// </summary>
public class AdsService : MonoBehaviour
{
    public static AdsService Instance { get; private set; }

    // ============================================================
    // Ad Unit IDs（TODO: 拿到真实 ID 后替换；测试 ID 永远显示测试广告，开发期安全）
    // ============================================================
    // 测试 ID（Google 官方公开测试广告，可以无限点）
    private const string TEST_ANDROID_REWARDED = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_IOS_REWARDED = "ca-app-pub-3940256099942544/1712485313";
    private const string TEST_ANDROID_INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_IOS_INTERSTITIAL = "ca-app-pub-3940256099942544/4411468910";

    // TODO: 上线前换成真实 Ad Unit ID
    private const string PROD_ANDROID_REWARDED = "";
    private const string PROD_IOS_REWARDED = "";
    private const string PROD_ANDROID_INTERSTITIAL = "";
    private const string PROD_IOS_INTERSTITIAL = "";

    // 是否始终使用测试 ID（开发期保持 true；上线前改 false 走 PROD_*）
    private const bool USE_TEST_ADS = true;

    // ============================================================
    // 插屏频率策略
    // ============================================================
    [Tooltip("启动后前 N 局不显示插屏（让新用户先适应）")]
    public int interstitialSkipFirstGames = 2;

    [Tooltip("每 N 局触发一次插屏检查")]
    public int interstitialEveryNGames = 2;

    [Tooltip("两次插屏之间最小时间间隔（秒）")]
    public float interstitialMinIntervalSeconds = 60f;

    // 运行时状态
    private int gameOverCountThisSession;
    private float lastInterstitialTime = -999f;

    // ============================================================
    // AdMob 实例（仅在 ADMOB_ENABLED 时生效）
    // ============================================================
#if ADMOB_ENABLED
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private bool sdkInitialized;
#endif

    private const string KEY_NO_ADS = "NoAds_Removed";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<AdsService>() != null) return;
        var go = new GameObject("AdsService");
        go.AddComponent<AdsService>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameStateManager.OnGameOver += HandleGameOver;
        InitializeSdk();
    }

    private void OnDestroy()
    {
        GameStateManager.OnGameOver -= HandleGameOver;
    }

    // ============================================================
    // 初始化
    // ============================================================
    private void InitializeSdk()
    {
#if ADMOB_ENABLED
        MobileAds.Initialize(status =>
        {
            sdkInitialized = true;
            Debug.Log("[Ads] AdMob SDK initialized");
            LoadRewarded();
            LoadInterstitial();
        });
#else
        Debug.Log("[Ads] Dev mode — AdMob 未启用，广告调用将直接成功（define ADMOB_ENABLED 启用 SDK）");
#endif
    }

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>
    /// 显示 Rewarded（看广告拿奖励）。
    /// onReward: 真正看完广告时调用；onFail: 加载失败 / 用户跳过 / SDK 未就绪。
    /// </summary>
    public void ShowRewarded(Action onReward, Action onFail = null)
    {
#if ADMOB_ENABLED
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show(reward =>
            {
                Debug.Log($"[Ads] Rewarded earned: {reward.Type} x{reward.Amount}");
                onReward?.Invoke();
            });
            // Show 后该实例失效，预加载下一条
            rewardedAd.OnAdFullScreenContentClosed += () => LoadRewarded();
            rewardedAd.OnAdFullScreenContentFailed += _ => { LoadRewarded(); onFail?.Invoke(); };
            return;
        }
        Debug.Log("[Ads] Rewarded not ready");
        onFail?.Invoke();
        LoadRewarded();
#else
        // Dev：模拟看完
        Debug.Log("[Ads] (Dev) Rewarded auto-success");
        onReward?.Invoke();
#endif
    }

    /// <summary>
    /// 显示 Interstitial（局间插屏），频率策略与已购买去广告判断都在内部完成。
    /// </summary>
    public void ShowInterstitial()
    {
        if (IsNoAdsPurchased())
        {
            return;
        }

        // 频率：跳过前 N 局
        if (gameOverCountThisSession <= interstitialSkipFirstGames) return;
        // 频率：每 N 局
        if (gameOverCountThisSession % interstitialEveryNGames != 0) return;
        // 频率：最小间隔
        if (Time.realtimeSinceStartup - lastInterstitialTime < interstitialMinIntervalSeconds) return;

#if ADMOB_ENABLED
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
            lastInterstitialTime = Time.realtimeSinceStartup;
            interstitialAd.OnAdFullScreenContentClosed += () => LoadInterstitial();
            interstitialAd.OnAdFullScreenContentFailed += _ => LoadInterstitial();
            return;
        }
        Debug.Log("[Ads] Interstitial not ready");
        LoadInterstitial();
#else
        Debug.Log("[Ads] (Dev) Interstitial would show now (skipped — AdMob not enabled)");
        lastInterstitialTime = Time.realtimeSinceStartup;
#endif
    }

    public bool IsRewardedReady
    {
        get
        {
#if ADMOB_ENABLED
            return rewardedAd != null && rewardedAd.CanShowAd();
#else
            return true; // dev 模式永远 ready
#endif
        }
    }

    // ============================================================
    // 内部：加载广告
    // ============================================================
#if ADMOB_ENABLED
    private void LoadRewarded()
    {
        if (!sdkInitialized) return;
        rewardedAd?.Destroy();
        rewardedAd = null;

        string adUnit = GetRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnit)) { Debug.LogWarning("[Ads] Rewarded ad unit ID empty"); return; }

        var request = new AdRequest();
        RewardedAd.Load(adUnit, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Rewarded load failed: {error}");
                return;
            }
            rewardedAd = ad;
            Debug.Log("[Ads] Rewarded loaded");
        });
    }

    private void LoadInterstitial()
    {
        if (!sdkInitialized) return;
        interstitialAd?.Destroy();
        interstitialAd = null;

        string adUnit = GetInterstitialAdUnitId();
        if (string.IsNullOrEmpty(adUnit)) { Debug.LogWarning("[Ads] Interstitial ad unit ID empty"); return; }

        var request = new AdRequest();
        InterstitialAd.Load(adUnit, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Interstitial load failed: {error}");
                return;
            }
            interstitialAd = ad;
            Debug.Log("[Ads] Interstitial loaded");
        });
    }
#endif

    private static string GetRewardedAdUnitId()
    {
#if UNITY_ANDROID
        return USE_TEST_ADS ? TEST_ANDROID_REWARDED : PROD_ANDROID_REWARDED;
#elif UNITY_IPHONE
        return USE_TEST_ADS ? TEST_IOS_REWARDED : PROD_IOS_REWARDED;
#else
        return USE_TEST_ADS ? TEST_ANDROID_REWARDED : PROD_ANDROID_REWARDED;
#endif
    }

    private static string GetInterstitialAdUnitId()
    {
#if UNITY_ANDROID
        return USE_TEST_ADS ? TEST_ANDROID_INTERSTITIAL : PROD_ANDROID_INTERSTITIAL;
#elif UNITY_IPHONE
        return USE_TEST_ADS ? TEST_IOS_INTERSTITIAL : PROD_IOS_INTERSTITIAL;
#else
        return USE_TEST_ADS ? TEST_ANDROID_INTERSTITIAL : PROD_ANDROID_INTERSTITIAL;
#endif
    }

    // ============================================================
    // 事件钩子
    // ============================================================
    private void HandleGameOver(string reason)
    {
        gameOverCountThisSession++;
        ShowInterstitial();
    }

    // ============================================================
    // NoAds 持久化
    // ============================================================
    public static bool IsNoAdsPurchased()
    {
        return PlayerPrefs.GetInt(KEY_NO_ADS, 0) == 1;
    }
}
