using System;
using UnityEngine;

/// <summary>
/// Persistent coin currency manager.
/// Coins are saved in PlayerPrefs and survive game resets / restarts.
/// </summary>
public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    /// <summary>
    /// Fired when coins are gained at a specific world position (for UI feedback).
    /// Args: worldPosition, deltaCoins.
    /// </summary>
    public static event Action<Vector3, int> OnCoinsGained;

    public event Action<int> OnCoinsChanged;

    public int CurrentCoins { get; private set; }

    private const string COINS_KEY = "Coins";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CoinManager>() != null) return;

        GameObject go = new GameObject("CoinManager");
        go.AddComponent<CoinManager>();
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

        CurrentCoins = Mathf.Max(0, PlayerPrefs.GetInt(COINS_KEY, 0));
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    public void AddCoins(int amount)
    {
        AddCoinsInternal(amount);
    }

    public void AddCoinsAt(Vector3 worldPosition, int amount)
    {
        int add = AddCoinsInternal(amount);
        if (add == 0) return;

        try
        {
            OnCoinsGained?.Invoke(worldPosition, add);
        }
        catch
        {
            // Don't let UI listeners break currency updates.
        }
    }

    private int AddCoinsInternal(int amount)
    {
        int add = Mathf.Max(0, amount);
        if (add == 0) return 0;

        CurrentCoins = Mathf.Max(0, CurrentCoins + add);
        Save();
        OnCoinsChanged?.Invoke(CurrentCoins);
        return add;
    }

    public bool TrySpendCoins(int amount)
    {
        int cost = Mathf.Max(0, amount);
        if (cost == 0) return true;
        if (CurrentCoins < cost) return false;

        CurrentCoins = Mathf.Max(0, CurrentCoins - cost);
        Save();
        OnCoinsChanged?.Invoke(CurrentCoins);
        return true;
    }

    public void SetCoins(int value)
    {
        CurrentCoins = Mathf.Max(0, value);
        Save();
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(COINS_KEY, CurrentCoins);
        PlayerPrefs.Save();
    }
}
