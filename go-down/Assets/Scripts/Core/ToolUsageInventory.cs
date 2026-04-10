using System;
using UnityEngine;

/// <summary>
/// Persistent inventory for purchasable tool uses.
/// </summary>
public class ToolUsageInventory : MonoBehaviour
{
    public static ToolUsageInventory Instance { get; private set; }

    public static event Action OnUsesChanged;

    [Header("调试")]
    [Tooltip("开启后工具不消耗次数（无限使用）")]
    public bool unlimitedUses = false;

    public int ResetUses { get; private set; }
    public int RainbowUses { get; private set; }

    private const string KEY_RESET_USES = "ToolUses_Reset";
    private const string KEY_RAINBOW_USES = "ToolUses_Rainbow";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ToolUsageInventory>() != null) return;

        GameObject go = new GameObject("ToolUsageInventory");
        go.AddComponent<ToolUsageInventory>();
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

    public bool TryConsumeResetUse()
    {
        if (unlimitedUses) return true;
        if (ResetUses <= 0) return false;

        ResetUses--;
        Save();
        OnUsesChanged?.Invoke();
        return true;
    }

    public bool TryConsumeRainbowUse()
    {
        if (unlimitedUses) return true;
        if (RainbowUses <= 0) return false;

        RainbowUses--;
        Save();
        OnUsesChanged?.Invoke();
        return true;
    }

    public void AddResetUses(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return;

        ResetUses += add;
        Save();
        OnUsesChanged?.Invoke();
    }

    public void AddRainbowUses(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return;

        RainbowUses += add;
        Save();
        OnUsesChanged?.Invoke();
    }

    private void Load()
    {
        ResetUses = Mathf.Max(0, PlayerPrefs.GetInt(KEY_RESET_USES, 0));
        RainbowUses = Mathf.Max(0, PlayerPrefs.GetInt(KEY_RAINBOW_USES, 0));
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_RESET_USES, ResetUses);
        PlayerPrefs.SetInt(KEY_RAINBOW_USES, RainbowUses);
        PlayerPrefs.Save();
    }
}
