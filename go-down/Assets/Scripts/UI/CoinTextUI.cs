using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a UI Text to CoinManager and keeps it updated.
/// Attach this to the Text GameObject you created, or assign the reference in Inspector.
/// </summary>
public class CoinTextUI : MonoBehaviour
{
    [Tooltip("If null, will try to use the Text on this GameObject.")]
    public Text coinText;

    [Tooltip("Display format. {0} = coin count.")]
    public string format = "{0}";

    private bool subscribed;

    private void Awake()
    {
        if (coinText == null)
        {
            coinText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
        RefreshImmediate();
    }

    private void Update()
    {
        // In case CoinManager bootstraps after this UI enables.
        if (!subscribed)
        {
            TrySubscribe();
            if (subscribed) RefreshImmediate();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (CoinManager.Instance == null) return;

        CoinManager.Instance.OnCoinsChanged += HandleCoinsChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
        }

        subscribed = false;
    }

    private void HandleCoinsChanged(int coins)
    {
        if (coinText == null) return;
        coinText.text = string.Format(format, coins);
    }

    private void RefreshImmediate()
    {
        if (coinText == null) return;

        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        coinText.text = string.Format(format, coins);
    }
}
