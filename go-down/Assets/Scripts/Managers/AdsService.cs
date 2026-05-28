using System;
using System.Collections;
using System.Collections.Generic;
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
/// TODO before release:
/// - iOS ATT: use ATTrackingStatusBinding.RequestAuthorizationTracking() before ad requests.
/// - UMP / GDPR: use GoogleMobileAds.Ump.Api consent flow for EU/UK users.
/// - NoAds purchase sync: real IAP must query Google Play Billing / StoreKit entitlements on startup,
///   then write PlayerPrefs key "NoAds_Removed".
/// - Monitor AdMob policy / dashboard frequency warnings after launch; adjust interstitial caps if needed.
///
/// 当前实现：
/// - Rewarded：点击「WATCH AD」→ 看广告 → 回调发放金币
/// - Interstitial：监听 GameStateManager.OnGameOver；按频率策略弹窗
/// - 已购买 NoAds（PlayerPrefs key = "NoAds_Removed"）时跳过所有插屏（rewarded 不受影响，玩家主动看的）
/// </summary>
public class AdsService : MonoBehaviour
{
    public static AdsService Instance { get; private set; }
    public static event Action RewardedStateChanged;

    // ============================================================
    // Ad Unit IDs（TODO: 拿到真实 ID 后替换；测试 ID 永远显示测试广告，开发期安全）
    // ============================================================
    // 测试 ID（Google 官方公开测试广告，可以无限点）
    private const string TEST_ANDROID_REWARDED = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_IOS_REWARDED = "ca-app-pub-3940256099942544/1712485313";
    private const string TEST_ANDROID_INTERSTITIAL = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_IOS_INTERSTITIAL = "ca-app-pub-3940256099942544/4411468910";

    // TODO: 上线前换成真实 Ad Unit ID
    private const string PROD_ANDROID_REWARDED = "ca-app-pub-9908007989063237/1261783807";
    private const string PROD_IOS_REWARDED = "ca-app-pub-9908007989063237/7360288295";
    private const string PROD_ANDROID_INTERSTITIAL = "ca-app-pub-9908007989063237/1344024374";
    private const string PROD_IOS_INTERSTITIAL = "ca-app-pub-9908007989063237/6047206620";

    // 是否始终使用测试 ID（开发期保持 true；上线前改 false 走 PROD_*）
    private const bool USE_TEST_ADS = true;
    private const float SDK_INITIALIZE_DELAY_SECONDS = 1.5f;

    // ============================================================
    // 插屏频率策略
    // ============================================================
    [Tooltip("启动后前 N 局不显示插屏（让新用户先适应）")]
    public int interstitialSkipFirstGames = 4;

    [Tooltip("两次插屏之间至少间隔 N 局。满足后还需要同时满足 minInterval 才会真正播放")]
    public int interstitialEveryNGames = 8;

    [Tooltip("两次插屏之间最小时间间隔（秒）")]
    public float interstitialMinIntervalSeconds = 120f;

    // 运行时状态
    private int gameOverCountThisSession;
    private int lastInterstitialGameCount; // 上一次真正播放插屏时的局数，初始 0
    private float lastInterstitialTime = -999f;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

    // ============================================================
    // AdMob 实例（仅在 ADMOB_ENABLED 时生效）
    // ============================================================
#if ADMOB_ENABLED
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private bool sdkInitialized;
    private bool sdkInitializeStarted;
    private bool sdkInitializeTimeoutLogged;
    private float sdkInitializeStartedAt;
    private bool rewardedLoading;
    private bool interstitialLoading;
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
        StartCoroutine(InitializeSdkDelayed());
    }

    private void OnDestroy()
    {
        GameStateManager.OnGameOver -= HandleGameOver;
    }

    private void Update()
    {
        FlushMainThreadActions();

#if ADMOB_ENABLED
        if (sdkInitializeStarted && !sdkInitialized && !sdkInitializeTimeoutLogged &&
            Time.realtimeSinceStartup - sdkInitializeStartedAt > 10f)
        {
            sdkInitializeTimeoutLogged = true;
            Debug.LogWarning("[Ads] AdMob initialize callback not received after 10s. " +
                             "If this is a Huawei / non-certified GMS device, AdMob may never become ready. " +
                             "Check Google Play Services availability log above and logcat for DynamiteModule / GooglePlayServices errors.");
        }
#endif
    }

    // ============================================================
    // 初始化
    // ============================================================
    private IEnumerator InitializeSdkDelayed()
    {
        yield return null;
        yield return new WaitForSecondsRealtime(SDK_INITIALIZE_DELAY_SECONDS);
        InitializeSdk();
    }

    private void InitializeSdk()
    {
#if ADMOB_ENABLED
        if (sdkInitializeStarted) return;

        Debug.Log($"[Ads] Initializing AdMob. platform={Application.platform}, useTestAds={USE_TEST_ADS}, rewardedUnit={GetRewardedAdUnitId()}, interstitialUnit={GetInterstitialAdUnitId()}");
        LogGooglePlayServicesAvailability();
        sdkInitializeStarted = true;
        sdkInitializeStartedAt = Time.realtimeSinceStartup;
        NotifyRewardedStateChanged();
        MobileAds.Initialize(status =>
        {
            sdkInitialized = true;
            sdkInitializeTimeoutLogged = false;
            Debug.Log("[Ads] AdMob SDK initialized");
            NotifyRewardedStateChanged();
            LoadRewarded();
            LoadInterstitial();
        });
#else
        Debug.Log("[Ads] Dev mode — AdMob 未启用，广告调用将直接成功（define ADMOB_ENABLED 启用 SDK）");
        NotifyRewardedStateChanged();
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
        Debug.Log($"[Ads] ShowRewarded requested. ready={(rewardedAd != null && rewardedAd.CanShowAd())}");
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            var ad = rewardedAd;
            rewardedAd = null;

            bool rewardEarned = false;
            bool rewardGranted = false;
            bool failed = false;

            void GrantOnce(string source)
            {
                if (rewardGranted) return;
                rewardGranted = true;
                RunOnMainThread(() =>
                {
                    Debug.Log($"[Ads] Rewarded grant dispatched from {source}");
                    onReward?.Invoke();
                });
            }

            void FailOnce(string source)
            {
                RunOnMainThread(() =>
                {
                    Debug.Log($"[Ads] Rewarded failed / skipped from {source}");
                    onFail?.Invoke();
                });
            }

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log($"[Ads] Rewarded closed. earned={rewardEarned}, granted={rewardGranted}, failed={failed}");
                // AdMob (Android) 上 reward 回调有时会晚于 close 派发；如果在这里立刻销毁广告，
                // 晚到的 reward 回调会丢失。延迟 2 帧再做"判定 + 销毁 + 重载"，给 SDK 时间补派 reward。
                StartCoroutine(FinalizeAfterClose());
            };

            IEnumerator FinalizeAfterClose()
            {
                yield return null;
                yield return null;

                if (rewardEarned) GrantOnce("close (deferred)");
                else if (!failed) FailOnce("close without reward (deferred)");

                ad.Destroy();
                LoadRewarded();
            }
            ad.OnAdFullScreenContentFailed += error =>
            {
                failed = true;
                Debug.LogWarning($"[Ads] Rewarded show failed: {error}");
                ad.Destroy();
                LoadRewarded();
                FailOnce("show failed");
            };

            NotifyRewardedStateChanged();
            ad.Show(reward =>
            {
                rewardEarned = true;
                Debug.Log($"[Ads] Rewarded earned: {reward.Type} x{reward.Amount}");
                GrantOnce("reward callback");
            });
            return;
        }
        Debug.LogWarning("[Ads] Rewarded not ready. On Huawei devices without Google Play Services, AdMob ads usually cannot load.");
        RunOnMainThread(() => onFail?.Invoke());
        LoadRewarded();
#else
        // Dev：模拟看完
        Debug.Log("[Ads] (Dev) Rewarded auto-success");
        RunOnMainThread(() => onReward?.Invoke());
#endif
    }

    /// <summary>
    /// 显示 Interstitial（局间插屏），频率策略与已购买去广告判断都在内部完成。
    /// </summary>
    public void ShowInterstitial()
    {
        if (IsNoAdsPurchased())
        {
            Debug.Log("[Ads] Interstitial skipped: NoAds purchased.");
            return;
        }

        // 频率：跳过前 N 局
        if (gameOverCountThisSession <= interstitialSkipFirstGames)
        {
            Debug.Log($"[Ads] Interstitial skipped: first-games guard ({gameOverCountThisSession}/{interstitialSkipFirstGames}).");
            return;
        }
        // 频率：跟上次插屏至少间隔 N 局
        int gamesSinceLast = gameOverCountThisSession - lastInterstitialGameCount;
        if (gamesSinceLast < interstitialEveryNGames)
        {
            Debug.Log($"[Ads] Interstitial skipped: game interval guard. gamesSinceLast={gamesSinceLast}, need={interstitialEveryNGames}.");
            return;
        }
        // 频率：最小时间间隔（不满足不更新 lastInterstitialGameCount，下局会继续检查）
        if (Time.realtimeSinceStartup - lastInterstitialTime < interstitialMinIntervalSeconds)
        {
            Debug.Log($"[Ads] Interstitial skipped: time guard. elapsed={Time.realtimeSinceStartup - lastInterstitialTime:0.0}s, min={interstitialMinIntervalSeconds:0.0}s.");
            return;
        }

#if ADMOB_ENABLED
        Debug.Log($"[Ads] ShowInterstitial requested. ready={(interstitialAd != null && interstitialAd.CanShowAd())}");
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            lastInterstitialTime = Time.realtimeSinceStartup;
            lastInterstitialGameCount = gameOverCountThisSession;
            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                LoadInterstitial();
            };
            interstitialAd.OnAdFullScreenContentFailed += _ =>
            {
                LoadInterstitial();
            };
            interstitialAd.Show();
            return;
        }
        Debug.LogWarning("[Ads] Interstitial not ready. On Huawei devices without Google Play Services, AdMob ads usually cannot load.");
        LoadInterstitial();
#else
        Debug.Log("[Ads] (Dev) Interstitial would show now (skipped — AdMob not enabled)");
        lastInterstitialTime = Time.realtimeSinceStartup;
        lastInterstitialGameCount = gameOverCountThisSession;
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

    public bool IsRewardedLoading
    {
        get
        {
#if ADMOB_ENABLED
            return !sdkInitialized || rewardedLoading;
#else
            return false;
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
        if (rewardedLoading) return;

        rewardedAd?.Destroy();
        rewardedAd = null;

        string adUnit = GetRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnit)) { Debug.LogWarning("[Ads] Rewarded ad unit ID empty"); return; }

        rewardedLoading = true;
        NotifyRewardedStateChanged();
        Debug.Log($"[Ads] Loading rewarded ad. unit={adUnit}, useTestAds={USE_TEST_ADS}");
        var request = new AdRequest();
        RewardedAd.Load(adUnit, request, (ad, error) =>
        {
            rewardedLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Rewarded load failed: {FormatLoadError(error)}");
                NotifyRewardedStateChanged();
                return;
            }
            rewardedAd = ad;
            Debug.Log("[Ads] Rewarded loaded");
            NotifyRewardedStateChanged();
        });
    }

    private void LoadInterstitial()
    {
        if (!sdkInitialized) return;
        if (interstitialLoading) return;

        interstitialAd?.Destroy();
        interstitialAd = null;

        string adUnit = GetInterstitialAdUnitId();
        if (string.IsNullOrEmpty(adUnit)) { Debug.LogWarning("[Ads] Interstitial ad unit ID empty"); return; }

        interstitialLoading = true;
        Debug.Log($"[Ads] Loading interstitial ad. unit={adUnit}, useTestAds={USE_TEST_ADS}");
        var request = new AdRequest();
        InterstitialAd.Load(adUnit, request, (ad, error) =>
        {
            interstitialLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[Ads] Interstitial load failed: {FormatLoadError(error)}");
                return;
            }
            interstitialAd = ad;
            Debug.Log("[Ads] Interstitial loaded");
        });
    }

    private static string FormatLoadError(LoadAdError error)
    {
        if (error == null) return "null error";
        return $"code={error.GetCode()}, domain={error.GetDomain()}, message={error.GetMessage()}, raw={error}";
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

#if ADMOB_ENABLED && UNITY_ANDROID
    private static void LogGooglePlayServicesAvailability()
    {
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var apiAvailability = new AndroidJavaClass("com.google.android.gms.common.GoogleApiAvailability"))
            using (var api = apiAvailability.CallStatic<AndroidJavaObject>("getInstance"))
            {
                int code = api.Call<int>("isGooglePlayServicesAvailable", activity);
                string message = api.Call<string>("getErrorString", code);
                Debug.Log($"[Ads] Google Play Services availability: code={code}, message={message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Ads] Could not check Google Play Services availability: {e.GetType().Name}: {e.Message}");
        }
    }
#else
    private static void LogGooglePlayServicesAvailability()
    {
    }
#endif

    // ============================================================
    // 事件钩子
    // ============================================================
    private void HandleGameOver(string reason)
    {
        gameOverCountThisSession++;
        Debug.Log($"[Ads] GameOver received: reason={reason}, count={gameOverCountThisSession}");
        ShowInterstitial();
    }

    // ============================================================
    // NoAds 持久化
    // ============================================================
    public static bool IsNoAdsPurchased()
    {
        return PlayerPrefs.GetInt(KEY_NO_ADS, 0) == 1;
    }

    private static void NotifyRewardedStateChanged()
    {
        var instance = Instance;
        if (instance == null)
        {
            RewardedStateChanged?.Invoke();
            return;
        }

        instance.RunOnMainThread(() => RewardedStateChanged?.Invoke());
    }

    private void RunOnMainThread(Action action)
    {
        if (action == null) return;

        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void FlushMainThreadActions()
    {
        while (true)
        {
            Action action;
            lock (mainThreadActions)
            {
                if (mainThreadActions.Count == 0) return;
                action = mainThreadActions.Dequeue();
            }

            action?.Invoke();
        }
    }
}
