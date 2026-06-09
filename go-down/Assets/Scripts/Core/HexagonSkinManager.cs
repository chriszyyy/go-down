using System;
using UnityEngine;

/// <summary>
/// 六边形球皮肤的解锁与选择状态（持久化到 PlayerPrefs）。
/// - Gold 默认解锁且默认选中（免费）。
/// - 其它皮肤需要花费 UNLOCK_PRICE 解锁。
/// - 不消费金币：扣金币交给调用方（ShopPanel + CoinManager）。
/// </summary>
public class HexagonSkinManager : MonoBehaviour
{
    public static HexagonSkinManager Instance { get; private set; }

    /// <summary>解锁状态或选中皮肤发生变化时触发。</summary>
    public static event Action OnChanged;

    public const int UNLOCK_PRICE = 500;

    /// <summary>支持的皮肤 ID（与 UXML 中卡片 name 后缀对应：item-hex-gold 等）。</summary>
    public static readonly string[] AllSkinIds = new[] { "gold", "blue", "purple", "green", "red", "rainbow" };
    public const string DefaultSkinId = "gold";

    public static int GetUnlockPrice(string skinId)
    {
        switch (skinId)
        {
            case "blue": return 500;
            case "purple": return 800;
            case "green": return 1000;
            case "red": return 1200;
            case "rainbow": return 1500;
            case "gold":
            default: return UNLOCK_PRICE;
        }
    }

    public string SelectedSkinId { get; private set; } = DefaultSkinId;

    private const string KEY_SELECTED = "HexSkin_Selected";
    private const string KEY_UNLOCKED_PREFIX = "HexSkin_Unlocked_";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<HexagonSkinManager>() != null) return;

        GameObject go = new GameObject("HexagonSkinManager");
        go.AddComponent<HexagonSkinManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public bool IsUnlocked(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return false;
        if (skinId == DefaultSkinId) return true; // gold 始终解锁
        return PlayerPrefs.GetInt(KEY_UNLOCKED_PREFIX + skinId, 0) == 1;
    }

    /// <summary>标记皮肤为已解锁。返回 true 表示这次调用真的产生了状态变化。</summary>
    public bool Unlock(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return false;
        if (IsUnlocked(skinId)) return false;

        PlayerPrefs.SetInt(KEY_UNLOCKED_PREFIX + skinId, 1);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>切换当前选中的皮肤。皮肤必须已解锁，否则返回 false。</summary>
    public bool TrySelect(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return false;
        if (!IsUnlocked(skinId)) return false;
        if (SelectedSkinId == skinId) return true;

        SelectedSkinId = skinId;
        PlayerPrefs.SetString(KEY_SELECTED, skinId);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
        return true;
    }

    private void Load()
    {
        string saved = PlayerPrefs.GetString(KEY_SELECTED, DefaultSkinId);
        // 保险：若存档里的皮肤未解锁，回退到 gold
        SelectedSkinId = IsUnlocked(saved) ? saved : DefaultSkinId;
    }

    /// <summary>调试：清除所有解锁皮肤，并把选中皮肤重置为默认（gold）。</summary>
    public void ResetAllUnlocks()
    {
        foreach (var skinId in AllSkinIds)
        {
            if (skinId == DefaultSkinId) continue;
            PlayerPrefs.DeleteKey(KEY_UNLOCKED_PREFIX + skinId);
        }
        SelectedSkinId = DefaultSkinId;
        PlayerPrefs.SetString(KEY_SELECTED, DefaultSkinId);
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}
