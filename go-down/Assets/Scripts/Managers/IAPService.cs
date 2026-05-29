using System;
using System.Collections.Generic;
using UnityEngine;

#if IAP_ENABLED
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// <summary>
/// 内购服务（基于 Unity IAP / Unity Purchasing，支持 Android Google Play 与 iOS App Store）。
///
/// 接入步骤（未安装包前 IAP_ENABLED 未定义，本服务以 Dev 模式工作 —— 购买调用直接返回成功）：
///   1. Package Manager → In Project → 安装 "In App Purchasing"（com.unity.purchasing 4.12.x）
///      安装后 GoDown.Managers asmdef 的 versionDefines 会自动定义 IAP_ENABLED
///   2. Window → General → Services 启用 In-App Purchasing（首次会要求绑定项目到 Unity Dashboard）
///      或者跳过 Services，直接走自定义初始化（本类已实现，无需 IAPButton 之流）
///   3. 在下方常量里确认产品 ID 与 Google Play / App Store Connect 后台一致：
///        - no_ads_lifetime         (Non-Consumable)
///        - coin_pack_100/500/...   (Consumable)
///   4. Google Play Console：创建对应 SKU + 上传 .aab 到 Internal Testing；测试账号必须是 LICENSE TESTER
///   5. App Store Connect：在 App 下创建对应 IAP，Sandbox 测试账号在 TestFlight 登录
///   6. 启动时 InitializePurchasing() 自动从商店拉 ProductCatalog；
///      非消耗品已购状态会自动写入 PlayerPrefs（NoAds_Removed），AdsService 立即生效
///
/// TODO before release:
/// - 服务端回执校验（防止本地破解）：把 receipt 发到自己后端校验后再发奖；当前是客户端 trust
/// - Restore Purchases 按钮（iOS 提交要求必备）已在 RestorePurchases() 实现
/// - 价格本地化：用 GetLocalizedPrice(productId) 替换 ShopPanel 里硬编码的 "$2.99"
/// - 失败 / 取消的错误码上报到 Analytics 方便后续追广告归因
///
/// 当前实现：
/// - 商品定义内嵌（s_products）；Initialize 时一次性向商店注册
/// - 购买成功：消耗品 → 触发 PurchaseCompleted；非消耗品 → 写 PlayerPrefs + 触发 PurchaseCompleted
/// - 商店未就绪时 BuyProduct() 直接走 PurchaseFailed("not_ready")，避免 NPE
/// - Dev 模式（IAP_ENABLED 未定义）：所有调用立刻成功并触发回调，方便编辑器跑通业务流程
/// </summary>
public class IAPService : MonoBehaviour
#if IAP_ENABLED
    , IDetailedStoreListener
#endif
{
    // ============================================================
    // Product IDs（必须与 Google Play / App Store Connect 后台一致）
    // ============================================================
    public const string PRODUCT_NO_ADS = "no_ads_lifetime";
    public const string PRODUCT_COIN_100 = "coin_pack_100";
    public const string PRODUCT_COIN_500 = "coin_pack_500";
    public const string PRODUCT_COIN_1200 = "coin_pack_1200";
    public const string PRODUCT_COIN_2500 = "coin_pack_2500";

    // 产品类型定义
    public enum ProductKind { Consumable, NonConsumable }

    private struct ProductDef
    {
        public string id;
        public ProductKind kind;
        public int coinAmount; // 仅 Consumable 金币包使用；非金币商品为 0
    }

    private static readonly ProductDef[] s_products = new[]
    {
        new ProductDef { id = PRODUCT_NO_ADS,    kind = ProductKind.NonConsumable, coinAmount = 0 },
        new ProductDef { id = PRODUCT_COIN_100,  kind = ProductKind.Consumable,    coinAmount = 100 },
        new ProductDef { id = PRODUCT_COIN_500,  kind = ProductKind.Consumable,    coinAmount = 500 },
        new ProductDef { id = PRODUCT_COIN_1200, kind = ProductKind.Consumable,    coinAmount = 1200 },
        new ProductDef { id = PRODUCT_COIN_2500, kind = ProductKind.Consumable,    coinAmount = 2500 },
    };

    // 与 AdsService 对齐的去广告持久化 key
    private const string KEY_NO_ADS = "NoAds_Removed";

    // ============================================================
    // 单例 + 事件
    // ============================================================
    public static IAPService Instance { get; private set; }

    /// <summary>商店初始化完成（成功或失败）。可用于刷新 UI 的「价格 / 加载中」状态。</summary>
    public static event Action InitializeStateChanged;

    /// <summary>购买成功。参数：productId。消耗品发奖、非消耗品授权都在事件触发前完成。</summary>
    public static event Action<string> PurchaseCompleted;

    /// <summary>购买失败。参数：productId, 简短失败原因（"not_ready" / "user_canceled" / "payment_declined" / ...）。</summary>
    public static event Action<string, string> PurchaseFailed;

    /// <summary>商店是否已就绪可发起购买。</summary>
    public bool IsReady
    {
        get
        {
#if IAP_ENABLED
            return storeController != null;
#else
            return true; // Dev 模式：永远 ready
#endif
        }
    }

    public bool IsInitializing
    {
        get
        {
#if IAP_ENABLED
            return initializeStarted && storeController == null;
#else
            return false;
#endif
        }
    }

    // ============================================================
    // 内部状态
    // ============================================================
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();

#if IAP_ENABLED
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private bool initializeStarted;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<IAPService>() != null) return;
        var go = new GameObject("IAPService");
        go.AddComponent<IAPService>();
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
        InitializePurchasing();
    }

    private void Update()
    {
        FlushMainThreadActions();
    }

    // ============================================================
    // 初始化
    // ============================================================
    private void InitializePurchasing()
    {
#if IAP_ENABLED
        if (initializeStarted) return;
        initializeStarted = true;

        Debug.Log($"[IAP] Initializing Unity IAP. platform={Application.platform}");
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        foreach (var p in s_products)
        {
            builder.AddProduct(p.id, ToUnityProductType(p.kind));
        }
        UnityPurchasing.Initialize(this, builder);
#else
        Debug.Log("[IAP] Dev mode — Unity IAP 未启用，购买调用将直接成功（安装 com.unity.purchasing 启用 SDK）");
        NotifyInitializeStateChanged();
#endif
    }

#if IAP_ENABLED
    private static ProductType ToUnityProductType(ProductKind kind)
    {
        switch (kind)
        {
            case ProductKind.Consumable: return ProductType.Consumable;
            case ProductKind.NonConsumable: return ProductType.NonConsumable;
            default: return ProductType.Consumable;
        }
    }
#endif

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>发起购买。失败 / 不可用走 PurchaseFailed 事件，业务方不要 catch 异常。</summary>
    public void BuyProduct(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            FireFail(productId, "invalid_id");
            return;
        }

#if IAP_ENABLED
        if (storeController == null)
        {
            Debug.LogWarning($"[IAP] BuyProduct({productId}) before store ready");
            FireFail(productId, "not_ready");
            return;
        }
        var product = storeController.products.WithID(productId);
        if (product == null || !product.availableToPurchase)
        {
            Debug.LogWarning($"[IAP] BuyProduct({productId}) product unavailable");
            FireFail(productId, "unavailable");
            return;
        }
        Debug.Log($"[IAP] Initiating purchase: {productId}");
        storeController.InitiatePurchase(product);
#else
        Debug.Log($"[IAP] (Dev) BuyProduct({productId}) auto-success");
        GrantEntitlement(productId);
        FireCompleted(productId);
#endif
    }

    /// <summary>恢复购买（iOS 必备按钮；Android 平台自动恢复非消耗品 entitlements，仅作兜底）。</summary>
    public void RestorePurchases()
    {
#if IAP_ENABLED
        if (storeController == null || extensionProvider == null)
        {
            Debug.LogWarning("[IAP] RestorePurchases before store ready");
            return;
        }
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("[IAP] Restoring iOS purchases");
            var apple = extensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((success, error) =>
            {
                Debug.Log($"[IAP] iOS restore done. success={success}, error={error ?? "(none)"}");
                // 成功走 ProcessPurchase 回调路径自动发奖 / 写 entitlement
            });
        }
        else
        {
            // Android：Google Play 在 Initialize 阶段已经把已拥有的非消耗品回放到 ProcessPurchase
            Debug.Log("[IAP] Restore on Android: Google Play replays owned products on initialize. Re-scanning...");
            RescanOwnedNonConsumables();
        }
#else
        Debug.Log("[IAP] (Dev) RestorePurchases noop");
#endif
    }

    public bool IsOwned(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return false;

        // 非消耗品的最终权威在本地 entitlement key（IAP 商店重装后会通过 ProcessPurchase 回放回填）
        if (productId == PRODUCT_NO_ADS) return PlayerPrefs.GetInt(KEY_NO_ADS, 0) == 1;

#if IAP_ENABLED
        if (storeController == null) return false;
        var product = storeController.products.WithID(productId);
        return product != null && product.hasReceipt;
#else
        return false;
#endif
    }

    /// <summary>商店返回的本地化价格字符串（如 "$2.99" / "¥18.00"）。未就绪时返回 fallback。</summary>
    public string GetLocalizedPrice(string productId, string fallback = null)
    {
#if IAP_ENABLED
        if (storeController == null) return fallback;
        var product = storeController.products.WithID(productId);
        if (product == null || product.metadata == null) return fallback;
        return product.metadata.localizedPriceString ?? fallback;
#else
        return fallback;
#endif
    }

    // ============================================================
    // IStoreListener 回调（仅 IAP_ENABLED 编译）
    // ============================================================
#if IAP_ENABLED
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("[IAP] Store initialized");
        storeController = controller;
        extensionProvider = extensions;

        // 启动时同步非消耗品 entitlement（覆盖装机后首次启动 / 卸载重装场景）
        RescanOwnedNonConsumables();
        NotifyInitializeStateChanged();
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning($"[IAP] Store initialize failed: {error}");
        NotifyInitializeStateChanged();
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogWarning($"[IAP] Store initialize failed: {error}, msg={message}");
        NotifyInitializeStateChanged();
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string id = args.purchasedProduct?.definition?.id;
        Debug.Log($"[IAP] ProcessPurchase: {id}");

        // TODO: 上线前在这里加服务端回执校验（args.purchasedProduct.receipt）后再发奖

        GrantEntitlement(id);
        FireCompleted(id);

        // 消耗品需要返回 Complete 才会从商店队列里清掉；非消耗品同样 Complete
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        string id = product?.definition?.id;
        Debug.LogWarning($"[IAP] Purchase failed: {id}, reason={failureReason}");
        FireFail(id, failureReason.ToString());
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        string id = product?.definition?.id;
        Debug.LogWarning($"[IAP] Purchase failed: {id}, reason={failureDescription?.reason}, msg={failureDescription?.message}");
        FireFail(id, failureDescription?.reason.ToString() ?? "unknown");
    }

    private void RescanOwnedNonConsumables()
    {
        if (storeController == null) return;
        foreach (var p in s_products)
        {
            if (p.kind != ProductKind.NonConsumable) continue;
            var product = storeController.products.WithID(p.id);
            if (product != null && product.hasReceipt)
            {
                GrantEntitlement(p.id);
            }
        }
    }
#endif

    // ============================================================
    // 发奖 / Entitlement 持久化
    // ============================================================
    private static void GrantEntitlement(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return;

        if (productId == PRODUCT_NO_ADS)
        {
            PlayerPrefs.SetInt(KEY_NO_ADS, 1);
            PlayerPrefs.Save();
            Debug.Log("[IAP] NoAds entitlement granted");
            return;
        }

        // 金币包 → 通过商品定义查金额，调用 CoinManager
        foreach (var p in s_products)
        {
            if (p.id != productId) continue;
            if (p.coinAmount > 0 && CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(p.coinAmount);
                Debug.Log($"[IAP] Coin pack granted: +{p.coinAmount} ({productId})");
            }
            return;
        }
    }

    // ============================================================
    // 事件派发（保证回到主线程；Unity IAP 4.x 实际就是主线程，但保留与 AdsService 一致的模式）
    // ============================================================
    private void FireCompleted(string productId)
    {
        RunOnMainThread(() => PurchaseCompleted?.Invoke(productId));
    }

    private void FireFail(string productId, string reason)
    {
        RunOnMainThread(() => PurchaseFailed?.Invoke(productId, reason));
    }

    private static void NotifyInitializeStateChanged()
    {
        var instance = Instance;
        if (instance == null)
        {
            InitializeStateChanged?.Invoke();
            return;
        }
        instance.RunOnMainThread(() => InitializeStateChanged?.Invoke());
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
