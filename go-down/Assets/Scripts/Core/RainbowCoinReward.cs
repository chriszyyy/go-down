using UnityEngine;

/// <summary>
/// Awards coins when the HexagonBall collides with this block.
/// Designed to be attached only to rainbow/special blocks.
/// Each block can award coins at most once.
/// </summary>
public class RainbowCoinReward : MonoBehaviour
{
    [Tooltip("Coins awarded when HexagonBall collides with this block (once).")]
    public int coinsPerActivation = 5;

    [Tooltip("Optional tag check for the HexagonBall. If empty, uses component check only.")]
    public string hexagonBallTag = "HexagonBall";

    private bool awarded;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null) return;
        TryAward(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAward(other);
    }

    private void TryAward(Collider2D other)
    {
        if (awarded) return;
        if (other == null) return;

        bool isHexagon = other.GetComponent<HexagonBall>() != null;
        if (!isHexagon && !string.IsNullOrEmpty(hexagonBallTag))
        {
            isHexagon = other.CompareTag(hexagonBallTag);
        }

        if (!isHexagon) return;

        awarded = true;

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(Mathf.Max(0, coinsPerActivation));
        }
        else
        {
            // CoinManager bootstraps after scene load; if something calls before that,
            // we still avoid awarding multiple times.
            Debug.LogWarning("RainbowCoinReward: CoinManager.Instance is null. Coins not awarded.");
        }
    }
}
